using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GCMessaging;

namespace GemCarryServer
{
    public static class LoginManager
    {
        private static GemDatabase LoginDb = new GemDatabase();
        public static int AttemptLoginForClient(LoginMessage msg /*, out UserDetails user*/)
        {
            // TODO: Eventually set up a thread that handles logins at a set rate,
            // For now, we can just handle them as called
            //mLoginClientQueue.Add(msg);

            int mReturn = -1;
            
            if (LoginDb.ValidateCredentials(msg.mUsername,msg.mPassword))
            {
                mReturn = 0;
            }

            return mReturn;
        }

    }
}
