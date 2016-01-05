using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;

namespace GemCarryServer
{

    class Program
    {
        private static ServerHost mServerHost;

        static void Main(string[] args)
        {
            // DO NOT PUT INVALID EMAIL ADDRESSES IN FOR CREATE USER, THIS COULD GET US BANNED FROM SES
            //   mServerHost = new ServerHost();
            //   mServerHost.StartServer();

            GemDatabase gdb = new GemDatabase();
            string r;
            // DO NOT PUT INVALID EMAIL ADDRESSES IN FOR CREATE USER, THIS COULD GET US BANNED FROM SES
            // DO NOT PUT INVALID EMAIL ADDRESSES IN FOR CREATE USER, THIS COULD GET US BANNED FROM SES
            // DO NOT PUT INVALID EMAIL ADDRESSES IN FOR CREATE USER, THIS COULD GET US BANNED FROM SES
            gdb.CreateUser("brianwthomas@gmail.com", "test", out r);
            // DO NOT PUT INVALID EMAIL ADDRESSES IN FOR CREATE USER, THIS COULD GET US BANNED FROM SES
            // DO NOT PUT INVALID EMAIL ADDRESSES IN FOR CREATE USER, THIS COULD GET US BANNED FROM SES
            // DO NOT PUT INVALID EMAIL ADDRESSES IN FOR CREATE USER, THIS COULD GET US BANNED FROM SES
            Console.Read();
        }
    }
}
