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
            Console.WriteLine("Welcome to GemCarryServer. Type 'quit' to shut down.");

            mServerHost = new ServerHost();
            mServerHost.StartServer();

            string userinput = "";
            while (userinput != "quit")
            {
                userinput = Console.ReadLine();
            }
        }
    }
}
