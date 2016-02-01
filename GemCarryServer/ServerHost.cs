using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;

namespace GemCarryServer
{
    public class ServerHost
    {
        private List<GameSession> mGameSessions;
        private int mCurrentSocketId;

        private AsyncServer mServer;

        public void StartServer()
        {
            mCurrentSocketId = 0;
            mGameSessions = new List<GameSession>();

            mServer = new AsyncServer(this, 1000, 1024);

            // Establish the local endpoint for the socket
            IPAddress ipAddress = IPAddress.Parse("0.0.0.0");
            IPEndPoint localEndPoint = new IPEndPoint(ipAddress, 1025);

            mServer.Start(localEndPoint);
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
