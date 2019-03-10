"use strict";

var util = require("util")
  , url = require("url")
  , http = require("http")
  , crypto = require("crypto")
  , querystring = require("querystring")
  , events = require("events")
  , extend = require("extend")
  , HubabubaError = require("./lib/error")
  , HubabubaItem = require("./lib/item");

util.inherits(Hubabuba, events.EventEmitter);

/**
*
* @class Hubabuba
* @constructor
* @param callbackUrl {String} the url to use as the callback
* @param options {Object} options to be applied to this instance
* 
* options: 
*
* bool debug - turns on debug mdoe to allow diagnosing issues (false)
* function verification - callback function with the subscription item 
*                        allows customization about whether a (un)subscription
*                        is allowed by returning a bool (always return true)
* secret - a string to use as part of the HMAC, as per working draft this should only be used for hubs running on HTTPS (null)
* number leaseSeconds - number of seconds that the subscription should be active for, please note that the hub does
*                       not need to honor this value so always use the returned leaseSeconds from subscribed
*                       event as a guide to when expiry is to be expected (86400 1day)
* number maxNotificationSize - Maximum number of bytes that is allowed to be posted as a notification (4.194e6 = 4MB)
*  
* @example options
*   {
*     url : "http://www.myhost.com/hubabuba",
*     debug : true,
*     secret: "AMt323Dkpf2j1qQ",
*     verification : function (item) {
*       var sub = subs.find(item.id);
*       if (item.mode === modes.SUBSCRIBE) {
*         return (sub) && (sub.status === modes.PENDING);
*       }
*     },
*     leaseSeconds : 10000,
*     maxNotificationSize : 1.049e6 // 1MB
*   }
*
* @fires Hubabuba#error
* @fires Hubabuba#subscribed
* @fires Hubabuba#unsubscribed
* @fires Hubabuba#notification
* @fires Hubabuba#denied
*
*/
function Hubabuba (callbackUrl, options) {
  if (!callbackUrl)
    throw new HubabubaError("callbackUrl must be supplied");
  
  this.url = callbackUrl;
  this.callbackUrl = url.parse(this.url);
  
  this.opts = {
    debug : false,
    secret: null,
    verification: function () { return true; },
    leaseSeconds: 86400, // 1day
    maxNotificationSize: 4.194e6 // 4MB
  };
    
  extend(true, this.opts, options);
  events.EventEmitter.call(this); 
  
  this.debugLog = function (msg) {
    if (this.opts.debug)
      console.log(msg);
  }.bind(this);
    
}

/**
* @method
* @return {Function} a connect function with the signature (req, res, next)
*
* This is the method that is hooked into connect in order to handle callbacks from the hub, the handler should use the same url that
* is passed as the options.url 
*
* Before this handler is plugged into the connect pipeline make sure that the connect.query middleware is placed before
*
* @example
*   app.use(hubabuba.handler());
*
*/
Hubabuba.prototype.handler = function() {
  return function hubabubaHandler(req, res, next) {
    var requestUrl, mode;
    requestUrl = req.originalUrl || req.url;
    
    if (this.callbackUrl.pathname === url.parse(requestUrl).pathname) {
      if (!req.query) {
        HubabubaError.raiseError.call(this, new HubabubaError("req.query is not defined"));
        res.writeHead(400); // bad request
        res.end();
        return;
      }
            
      if (req.method === "GET") {
        mode = req.query["hub.mode"];
        if (!mode) {
          HubabubaError.raiseError.call(this, new HubabubaError("mode was not supplied"));
          res.writeHead(400); // bad request
          res.end();
          return;
        }
        
        handleDenied.call(this, req, res);
        handleVerification.call(this, req, res);
      } else if (req.method === "POST") {
        handleNotification.call(this, req, res);
      } else {
        HubabubaError.raiseError.call(this, new HubabubaError("method supplied is not GET or POST"));
        res.writeHead(405); //method not allowed
        res.end();
        return;
      }
      
      return;
    }
        
    return next();
  }.bind(this);
};

/**
* @method
* @param item {Object}
* @param cb {Function} standard callback matching signature (err, item)
*
* Used to subscribe to a pubsubhubub hub, item should be defined as:
*
* {
*   id: "52ab86db7d468bb12bb455a8", (allows the caller to specify a custom id such as a db id)
*   hub: "http://pubsubhubbubprovider.com/hub", (the hub provider that is proving the pubsubhubub capability)
*   topic: "http://www.blog.com/feed", (the topic the caller wants to subscribe to)
*   leaseSeconds: 604800 // 1wk (optional) (how long the subscription should remain active for, can be changed by hub)
* }
*
* The callback returns an error (null if everything worked) and also the item passed to it (if it is defined), this callback
* confirms that the subscription request has reached the hub (if err is null) but does not means that we are now subscribed
* as there are further steps that need to take place (validation / verification)
*
*/
Hubabuba.prototype.subscribe = function (item, cb) {
  subscriptionRequest.call(this, item, cb, "subscribe");
};

/**
* @method
* @param item {Object}
* @param cb {Function} standard callback matching signature (err, item)
*
* Used to unsubscribe from a pubsubhub hub, works in the same way as the subscribe method, also does not mean that we are
* unsubscribed from the hub as the hub will verify that the request is legitimate
*
*/
Hubabuba.prototype.unsubscribe = function (item, cb) {
  subscriptionRequest.call(this, item, cb, "unsubscribe");
};

var createSecretKey = function (topic) {
  return crypto.createHmac("sha1", this.opts.secret)
               .update(topic)
               .digest("hex");
};

var handleDenied = function (req, res) {
  var required, valid;
  
  if (req.query["hub.mode"] !== "denied") return;
    
  if (!objectHasProperties(req.query, ["id", "hub.topic", "hub.reason"])) {
    HubabubaError.raiseError.call(this, new HubabubaError("missing required query parameters"));
    return;
  }
  
  /**
  *
  * when a hub denies a subscription (can happen at anytime)
  * 
  * @event Hubabuba#denied
  * @type {object}
  *
  */
  this.emit("denied", {
    id : req.query.id,
    topic : req.query["hub.topic"],
    reason : req.query["hub.reason"]
  });
  
  res.writeHead(200);
  res.end();
};

var subscriptionRequest = function (item, cb, mode) {
  var hub, protocol, callback, req, params, http, leaseSeconds, reqOptions, body, responseHandler, secret;
  callback = cb || function () {}; // default to a no-op
    
  if (!item) {
    callback(new HubabubaError("item not supplied"));
    return;
  }
  
  if (!objectHasProperties(item, ["id", "hub", "topic"])) {
    callback(new HubabubaError("required params not supplied on item", item.id), item);
    return;
  }
  
  item.leaseSeconds = item.leaseSeconds || this.opts.leaseSeconds;
  hub = url.parse(item.hub);
  protocol = hub.protocol.substr(0, hub.protocol.length - 1);
  if (!/^https?$/.test(protocol)) {
    callback(new HubabubaError("protocol of hub is not supported", item.id));
    return;
  }
  
  http = require(protocol); // either http or https
  
  params = {
    "hub.mode": mode,
    "hub.callback": this.url + "?id=" + item.id,
    "hub.topic": item.topic,
    "hub.lease_seconds": item.leaseSeconds
  };
  
  if (this.opts.secret) {
    if (protocol === "http")
      console.warn("secret is being used however the request is not being sent over HTTPS");
    
    secret = createSecretKey.call(this, item.topic);
    this.debugLog("generating secret: " + secret);
    params["hub.secret"] = secret;
  }
    
  body = querystring.stringify(params);
  
  reqOptions = {
    method: "POST",
    hostname: hub.hostname,
    path: hub.path,
    port: (protocol === "http") ? 80 : 443,
    headers : {
      "Content-Type" : "application/x-www-form-urlencoded",
      "Content-Length": Buffer.byteLength(body)
    }
  };
  
  this.debugLog("making web request with options: " + JSON.stringify(reqOptions));  
  req = http.request(reqOptions);
  
  // partial apply the function so that we can use it with the response event callback
  responseHandler = subscriptionResponse.bind(this, item, callback);
  req.on("response", responseHandler)
     .on("error", function responseError(err) {
      callback(new HubabubaError(err.message, item.id), item);
  });
    
  this.debugLog("sending body params: " + body);  
  req.write(body);
  req.end();
};

var subscriptionResponse = function (item, callback, res) {
  var code, reason;
    reason = "";
    code = Math.floor(res.statusCode / 100);
    if (code != 2) {
      // according to working draft error details will be provided in the body as plaintext
      res.on("data", function responseData(data) { 
        reason += data;  
      }).on("end", function responseError() {
        callback(new HubabubaError(reason, item.id), item);
      });
      
      return;
    }
    
    callback(null, item);
};

var handleVerification = function (req, res) {
  var mode, modeRegexp, query, verification, item, challenge;
  query = req.query;
  mode = query["hub.mode"];
    
  if (!/^(?:un)?subscribe$/i.test(mode)) return;
  
  if (!objectHasProperties(query, ["id", "hub.topic", "hub.challenge"])) {
    HubabubaError.raiseError.call(this, new HubabubaError("missing required query parameters"));
    res.end();
    return;
  }
  
  // must be supplied for a subscribe
  if ((mode === "subscribe") && (!query["hub.lease_seconds"])) {
    HubabubaError.raiseError.call(this, new HubabubaError("missing required query parameters"));
    res.end();
    return;
  }
  
  item = new HubabubaItem(req.query);
  verification = this.opts.verification(item);
  
  if (verification) {
    challenge = query["hub.challenge"];
    this.debugLog("challenge from request: " + challenge);
    res.writeHead(200);
    res.write(challenge);
    
    var evt = (mode === "subscribe") ? "subscribed" : "unsubscribed";
    
    /**
    *
    * when either a subscription or unsubscription is confirmed
    * 
    * @event Hubabuba#subscribed
    * @event Hubabuba#unsubscribed
    * @type {object}
    *
    */
    this.emit(evt, item);
  } else {
    res.writeHead(500);
  }
  
  res.end();
};

var handleNotification = function (req, res) {
  var id, size, body, source;
  body = "";
  
  id = req.query.id;
  if (!id) {
    HubabubaError.raiseError.call(this, new HubabubaError("missing id parameter"));
    res.writeHead(500);
    res.end();
    return;
  }
  
  size = req.headers["content-length"];
  this.debugLog("size of request in header: " + size);
  if (size > this.opts.maxNotificationSize) {
    HubabubaError.raiseError.call(this, new HubabubaError("notification body size is greater than configured maximum", id));
    res.writeHead(413); // entity too large
    res.end();
    return;
  }
  
  source = parseLinkHeaders(req.headers);
  
  req.on("data", function notificationData(data) {
    body += data;
    if (Buffer.byteLength(body) > this.opts.maxNotificationSize) {
        this.debugLog("body size reached size of: " + Buffer.byteLength(body));
        HubabubaError.raiseError.call(this, new HubabubaError("notification body size is greater than configured maximum", id));
        res.writeHead(413); //entity too large
        res.end();
    }
  }.bind(this));
    
  req.on("end", function notificationEnd() {
    var header, emit, actualSignature, secretKey, expectedSignature;
    emit = true;
    
    if (this.opts.secret) {
      header = req.headers["x-hub-signature"];
      this.debugLog("receieved secret header: " + header);
            
      if (header) {
        // signature must be in the form sha1=signature
        actualSignature = header.split("=")[1];
        secretKey = createSecretKey.call(this, source.topic);
        expectedSignature = crypto.createHmac("sha1", secretKey)
                                  .update(body)
                                  .digest("hex");
        
        this.debugLog("expecting signature: " + expectedSignature);        
        if (actualSignature !== expectedSignature) {
          emit = false;
          HubabubaError.raiseError.call(this, new HubabubaError("signature supplied does not match expected signature", id));
        }
      } else {
        emit = false;
      }
    }
    
    if (emit) {
      /**
        * when new content is sent from the hub 
        * @event Hubabuba#notification
        * @type {object}
        */
      this.emit("notification", {
        id: id,
        topic: source.topic,
        hub: source.hub,
        headers: req.headers,
        params: req.query,
        content : body
      });
    }
    
    res.writeHead(200);
    res.end();
    
  }.bind(this));
};

/*
*
* Helper function that can check that all properties exist on an object
*
*/
var objectHasProperties = function (obj, props) {
  return props.every(function (prop) {
    return obj.hasOwnProperty(prop);
  });
};

/*
*
* Helper function to parse the link headers from the http request
*
* Link Example:
*
* <http://pubsubhubbub.superfeedr.com>; rel=\"hub\",<http://blog.superfeedr.com/my-resource>; rel=\"self\"
* <https://www.youtube.com/xml/feeds/videos.xml?channel_id=***>; rel=self, <http://pubsubhubbub.appspot.com/>; rel=hub
*
*/
var parseLinkHeaders = function (headers) {
  var source = {};

  headers['link'].replace(/<([^>]*)>; rel=[\\"]*([\w]*)[^\\"]?(?!=(,|$))/g, function(full, topic, rel){
    if (rel == 'hub') source.hub = topic;
    else if (rel == 'self') source.topic = topic;
  });
    
  return source;
};

/**
*@module Hubabuba
*/
module.exports = Hubabuba;