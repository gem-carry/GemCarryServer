using System;


namespace GemCarryServer
{
    public class GameLogger
    {

        public static GameLogger sInstance = null;

        // Default blank Contructor
        private GameLogger() { }        

        // Creates singleton of GameLogger named GetInstance()
        public static GameLogger GetInstance()
        {
            if (null == sInstance)
                {
                    sInstance = new GameLogger();
                }
            return sInstance;
        }
   
        // Writes log using LogType:Message                    
        public void WriteLog(LogLevel l, string m)
        {        
            Console.WriteLine(l + ":" + m);
        }

        // ENUM for LogTypes
        public enum LogLevel
        {
            Warning,
            Error,
            Debug
        }
    }
}
