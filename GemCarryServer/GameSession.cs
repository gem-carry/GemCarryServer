﻿
using System.Collections.Generic;


namespace GemCarryServer
{
    public class GameSession
    {
        private int mPlayerCount;
        private bool mLookingForPlayers;
        private int mLocation; //!< For determining what region this game session is in

        private List<GamePlayer> mPlayers;

        private const int MAX_SESSION_PLAYERS = 10;

        public GameSession()
        {
            mPlayers = new List<GamePlayer>();
            mPlayerCount = 0;
            mLookingForPlayers = true;
            mLocation = -1;
        }

        public bool IsSessionOpen(/*type?, matchmaking critera?*/)
        {
            return mLookingForPlayers && mPlayerCount < MAX_SESSION_PLAYERS;
        }

        public bool ShouldMatchPlayer(/* type, matchmaking critera, location shit? */)
        {
            // TODO: Do matchmaking logic here

            return true;
        }

        public void AddPlayer(GamePlayer p)
        {
            mPlayers.Add(p);
            mPlayerCount++;
        }

        public void RemovePlayer(GamePlayer p)
        {
            mPlayers.Remove(p);
            mPlayerCount--;
        }
    }
}
