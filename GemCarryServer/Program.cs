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
            mServerHost = new ServerHost();
            mServerHost.StartServer();
            //Trace.WriteLine("Brian is a butthole");
        }
    }
}
