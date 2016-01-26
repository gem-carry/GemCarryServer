using System;
using GemCarryServer.Database;
using GemCarryServer.User;


namespace GemCarryServer
{

    class Program
    {
        private static ServerHost mServerHost;

        static void Main(string[] args)
        {
            
               mServerHost = new ServerHost();
               mServerHost.StartServer();

          
        }
    }
}
