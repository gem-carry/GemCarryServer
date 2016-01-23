using System;
using GemCarryServer.Database;
using Amazon.DynamoDBv2.Model;
using GemCarryServer.User;


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

            //         GemDatabase gdb = new GemDatabase();
            //        string r;
            // DO NOT PUT INVALID EMAIL ADDRESSES IN FOR CREATE USER, THIS COULD GET US BANNED FROM SES
            // DO NOT PUT INVALID EMAIL ADDRESSES IN FOR CREATE USER, THIS COULD GET US BANNED FROM SES
            // DO NOT PUT INVALID EMAIL ADDRESSES IN FOR CREATE USER, THIS COULD GET US BANNED FROM SES
            //       gdb.CreateUser("brianwthomas@gmail.com", "test", out r);
            // DO NOT PUT INVALID EMAIL ADDRESSES IN FOR CREATE USER, THIS COULD GET US BANNED FROM SES
            // DO NOT PUT INVALID EMAIL ADDRESSES IN FOR CREATE USER, THIS COULD GET US BANNED FROM SES
            // DO NOT PUT INVALID EMAIL ADDRESSES IN FOR CREATE USER, THIS COULD GET US BANNED FROM SES

            LoginManager lm = LoginManager.GetInstance();
            GCUser.LoginInfo myUser = new GCUser.LoginInfo();
            myUser.Email = "bwt@domain.com";
       //     lm.CreateUser(myUser, "password");
           // lm.ValidateCredentials(myUser, "password1");
            lm.DeleteAccount(myUser, "password");
            Console.Read();
        }
    }
}
