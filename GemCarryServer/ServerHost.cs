using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GemCarryServer
{
    public class ServerHost
    {
        private List<GameSession> mGameSessions;
        private int mCurrentSocketId;

        public void StartServer()
        {
            mCurrentSocketId = 0;
            AsyncSocketListener.StartListening(this);
        }

        public int GetNextClientId()
        {
            return mCurrentSocketId++;
        }
    }
}
