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
            mGameSessions = new List<GameSession>();
            AsyncSocketListener.StartListening(this);
        }

        public int GetNextClientId()
        {
            return mCurrentSocketId++;
        }

        public GameSession FindGameSession(/*session type, matchmaking criteria*/)
        {
            GameSession gsRef = null;

            // Look through all existing game sessions to try and find an open match
            foreach(GameSession g in mGameSessions)
            {
                if(true == g.IsSessionOpen() && true == g.ShouldMatchPlayer())
                {
                    gsRef = g;
                }
            }

            // No session found, create a new one.
            if(null == gsRef)
            {
                gsRef = _CreateNewGameSession();
            }

            return gsRef;
        }

        private GameSession _CreateNewGameSession()
        {
            GameSession gs = new GameSession();
            mGameSessions.Add(gs);
            return gs;
        }


    }
}
