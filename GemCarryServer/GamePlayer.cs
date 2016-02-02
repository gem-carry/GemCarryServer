using System;
using System.Threading;
using System.Net.Sockets;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;
using System.IO.Compression;
using System.Text;
using GCMessaging;
using ProtoBuf;

namespace GemCarryServer
{
    public class GamePlayer
    {
        private ServerHost  mContext;
        private GameSession mGameSession;

        // Read socket info
        private int         mSocketId;
        private int         mBufferSize;
        private byte[]      mLocalBuffer;
        private int         mCurrentOffset;
        private int         mMaxBufferSize;

        public Socket Socket
        {
            get;
            set;
        }

        private const int CLIENT_THREAD_TIMEOUT = 40;

        public GamePlayer(ServerHost context, Socket socket, int bufferSize)
        {
            this.mContext = context;
            this.Socket = socket;
            mBufferSize = bufferSize;
            mMaxBufferSize = bufferSize * 2;
            mLocalBuffer = new byte[mMaxBufferSize]; //<! For now, allow max buffer storage be 2x size of max buffer

            ConnectResponse msg = new ConnectResponse{ success = true };
            DispatchMessage(msg);

            // Temporary
            JoinGameSession();
        }

        public bool ReadSocketData(SocketAsyncEventArgs e)
        {
            // Read all the bytes, create messages in FIFO stack to be processed
            if (mCurrentOffset + e.BytesTransferred < mMaxBufferSize)
            {
                Array.Copy(e.Buffer, e.Offset, mLocalBuffer, mCurrentOffset, e.BytesTransferred);
                mCurrentOffset += e.BytesTransferred;
                return true;
            }
            else
            {
                // Error!
                return false;
            }
        }

        public void ProcessData(SocketAsyncEventArgs e)
        {
            int msgEnd = MessageHelper.FindEOM(mLocalBuffer);
            if (msgEnd > -1)
            {
                // At least one full message read
                while (msgEnd > -1)
                {
                    byte[] message;

                    // Sorts out the message data into at least one full message, saves any spare bytes for next message
                    mCurrentOffset = MessageHelper.PullMessageFromBuffer(msgEnd, mLocalBuffer, mCurrentOffset, out message);

                    // Do something with client message
                    MessageHandler.HandleMessage(message, mGameSession, this);

                    // If we still have data in the buffer
                    if (mCurrentOffset > 0)
                    {
                        // More data in buffer, look for another message in Buffer
                        msgEnd = MessageHelper.FindEOM(mLocalBuffer);
                    }
                    else
                    {
                        msgEnd = -1;
                    }
                }
            }
        }

        public void DispatchMessage(GCMessaging.BaseMessage outMsg)
        {
            using (MemoryStream stream = new MemoryStream())
            {
                Serializer.Serialize(stream, outMsg);

                byte[] msg;
                MessageHelper.AppendEOM(stream.ToArray(), out msg);

                Socket.Send(msg, msg.Length, SocketFlags.None);
            }
        }

        public void DispatchMessageBytes(Byte[] outMsg)
        {
            byte[] msg;
            MessageHelper.AppendEOM(outMsg, out msg);

            Socket.Send(msg, msg.Length, SocketFlags.None);
        }

        public void JoinGameSession(/*type requested, matchmaking criteria, etc*/)
        {
            // Leave any pre-existing game session
            if(null != mGameSession)
            {
                QuitGameSession();
            }

            // Find new game session
            mGameSession = mContext.FindGameSession();

            //GCAssert(null != mGameSession);

            // Tie the connection between session and player
            mGameSession.AddPlayer(this);

            ChatMessage msg = new ChatMessage()
            {
                sender = "Server",
                message = "You have joined the Game Session."
            };
            DispatchMessage(msg);
        }

        public void QuitGameSession()
        {
            mGameSession.RemovePlayer(this);
            mGameSession = null;

            // Tell client socket that they have quit the game? maybe
        }
    }

}
