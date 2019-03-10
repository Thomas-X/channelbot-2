namespace channelbot_2
{
    internal class Program
    {
        
        static void Main(string[] args)
        {
            var hubbub = new PubSubHubBub(); 
            hubbub.Start();

            while (true)
            {
            }
        }
    }
}
