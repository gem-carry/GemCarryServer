using System;
using System.Threading;
using System.Net.Sockets;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;
using System.IO.Compression;
using System.Text;
using GCMessaging;

namespace GemCarryServer
{
    public class GamePlayer
    {
        private ServerHost  mContext;
        private Socket      mClientSocket;
        private int         mSocketId;
        private bool        mConnected;
        private GameSession mGameSession;

        private const int CLIENT_THREAD_TIMEOUT = 40;
        private const int BUFFER_SIZE = 8192;

        public GamePlayer()
        {
            mConnected = false;
        }

        public void StartConnection(ServerHost context, Socket inSocket, int socketId)
        {
            mContext = context;
            mClientSocket = inSocket;
            mSocketId = socketId;

            // Send client a response to let them know they are connected.
            MessageBase connectedMsg = new MessageBase();
            DispatchMessage(connectedMsg);

            JoinGameSession();

            Thread clientThread = new Thread(ListenLoop);
            clientThread.Start();
        }

        private void ListenLoop()
        {
            if (true == mClientSocket.Connected)
            {
                try
                {
                    SocketPacket packet = new SocketPacket(mClientSocket);

                    mClientSocket.BeginReceive(packet.buffer, 0, SocketPacket.BufferSize, SocketFlags.None, new AsyncCallback(ReadCallback), packet);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(" >> " + ex.ToString());
                }
            }
            else if (null != mGameSession)
            {
                QuitGameSession();
            }
        }

        public void ReadCallback(IAsyncResult ar)
        {
            String content = String.Empty;

            // Retrieve the packet object and the handler socket
            // from the asynchronous packet object.
            SocketPacket packet = (SocketPacket)ar.AsyncState;
            Socket handler = packet.workSocket;

            // Read data from the client socket. 
            try
            {
                if (true == handler.Connected)
                {
                    SocketError errorCode;
                    int bytesRead = handler.EndReceive(ar, out errorCode);
                    if(SocketError.Success != errorCode)
                    {
                        bytesRead = 0;
                    }

                    if (bytesRead > 0)
                    {
                        // All the data has been read from the 
                        // client. Display it on the console.
                        Console.WriteLine("Read {0} bytes from socket {1}.",
                            bytesRead, mSocketId);

                        // Check for end-of-file tag. If it is not there, read 
                        // more data.
                        int msgEnd = MessageHelper.FindEOM(packet.buffer);
                        if (msgEnd > -1)
                        {
                            // At least one full message read
                            while (msgEnd > -1)
                            {
                                byte[] dataMsg;
                                byte[] newMsg;

                                // Sorts out the message data into at least one full message, saves any spare bytes for next message
                                MessageHelper.ClearMessageFromStream(msgEnd, packet.buffer, out dataMsg, out newMsg);

                                // Do something with client message
                                MessageHandler.HandleMessage(dataMsg, mGameSession, this);

                                packet.buffer = newMsg;

                                msgEnd = MessageHelper.FindEOM(packet.buffer);
                            }
                        }
                        else
                        {
                            // Not all data received. Get more.
                            handler.BeginReceive(packet.buffer, bytesRead, SocketPacket.BufferSize, 0,
                            new AsyncCallback(ReadCallback), packet);
                        }
                    }
                }
            }
            catch(Exception e)
            {
                Console.WriteLine(e.ToString());
            }

            ListenLoop();
        }

        public void DispatchMessage(MessageBase outMsg)
        {
            IFormatter formatter = new BinaryFormatter();
            Stream stream = new MemoryStream();
            byte[] compressed;

            formatter.Serialize(stream, outMsg);

            using (MemoryStream resultStream = new MemoryStream())
            {
                using (DeflateStream compressionStream = new DeflateStream(resultStream, CompressionMode.Compress))
                {
                    stream.Seek(0, SeekOrigin.Begin);
                    stream.CopyTo(compressionStream);
                    compressionStream.Close();
                    compressed = resultStream.ToArray();
                }
            }

            byte[] msg;
            MessageHelper.AppendEOM(compressed, out msg);

            mClientSocket.Send(msg, msg.Length, SocketFlags.None);
        }

        public void DispatchMessageBytes(Byte[] outMsg)
        {
            byte[] msg;
            MessageHelper.AppendEOM(outMsg, out msg);

            mClientSocket.Send(msg, msg.Length, SocketFlags.None);
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

            ChatMessage msg = new ChatMessage();
            msg.mSender = "Server";
            msg.mMessage = "You have joined the Game Session.";

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
