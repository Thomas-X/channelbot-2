using System;
using System.Net;
using System.Net.Sockets;
using System.Reflection.Metadata;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace channelbot_2
{
    /// <summary>
    /// Basic idea of pubsubhubbub is:
    ///
    /// subscribe to a "topic"
    /// confirm subscription 
    /// on an update fire callback from https://pubsubhubbub.appspot.com
    /// </summary>



    public class PubSubHubBub
    {
        // 1000000 byte = 1mb 
        private int _receivingByteSize = 1000000;

        private void OnConnected(IAsyncResult result)
        {
            var listener = (TcpListener)result.AsyncState;
            TcpClient client;
            try
            {
                // Get the client 
                client = listener.EndAcceptTcpClient(result);
            }
            // If server socket is closed, catch error and return
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
                return;
            }

            listener.BeginAcceptTcpClient(OnConnected, listener);
            // Handle all logic in a separate thread
            Task.Factory.StartNew(() =>
            {
                HandleRequest(client);
            });
        }

        private void HandleRequest(TcpClient client)
        {
            var stream = client.GetStream();
            var x = 0;
            var buffer = new byte[_receivingByteSize];
            while ((x = stream.Read(buffer, 0, buffer.Length)) != 0)
            {
                var data = Encoding.UTF8.GetString(buffer, 0, x);
                var lines = data.Split("\r\n");
                Console.WriteLine("Received: {0}", data);
                var queryString = lines[0].Split(" ")[1];
                var query = HttpUtility.ParseQueryString(queryString);
                var str = $"HTTP/1.1 200 OK\r\nAccept-Ranges:bytes\r\nContent-Length:{query["hub.challenge"].Length}\r\n\r\n{query["hub.challenge"]}";
                stream.Write(Encoding.UTF8.GetBytes(str));
                if (!stream.DataAvailable || !stream.CanRead || !stream.CanWrite)
                {
                    break;
                }
            }
            stream.Close();
            stream.Dispose();
            client.Close();
            client.Dispose();
        }

        public void Start()
        {
            // Subscribe to a 



            // Set the TcpListener on port 13000.
            var port = 3000;
            var localAddr = IPAddress.Any;

            var server = new TcpListener(localAddr, port);

            // Start listening for client requests.
            server.Start();

            server.BeginAcceptTcpClient(OnConnected, server);
        }
    }
}