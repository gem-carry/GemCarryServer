using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Net.Sockets;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;
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

            Thread clientThread = new Thread(ListenLoop);
            clientThread.Start();
        }

        private void ListenLoop()
        {
            StateObject state = new StateObject();
            state.workSocket = mClientSocket;

            while (mClientSocket.Connected)
            {
                try
                {
                    mClientSocket.BeginReceive(state.buffer, 0, StateObject.BufferSize, SocketFlags.None, new AsyncCallback(ReadCallback), state);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(" >> " + ex.ToString());
                }

                Thread.Sleep(CLIENT_THREAD_TIMEOUT);
            }
        }

        public void ReadCallback(IAsyncResult ar)
        {
            String content = String.Empty;

            // Retrieve the state object and the handler socket
            // from the asynchronous state object.
            StateObject state = (StateObject)ar.AsyncState;
            Socket handler = state.workSocket;

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
                        // There  might be more data, so store the data received so far.
                        byte[] oldData = state.data;
                        if(null != oldData)
                        {
                            int newLength = bytesRead + state.dataCount;
                            state.data = new byte[newLength];
                            Array.Copy(oldData, 0, state.data, 0, state.dataCount);
                            Array.Copy(state.buffer, 0, state.data, state.dataCount, bytesRead);
                            state.dataCount = newLength;
                        }
                        else
                        {
                            state.data = new byte[bytesRead];
                            Array.Copy(state.buffer, 0, state.data, 0, bytesRead);
                            state.dataCount = bytesRead;
                        }

                        // Check for end-of-file tag. If it is not there, read 
                        // more data.
                        if(MessageHelper.FindEOM(state.data) > -1)
                        {
                            // All the data has been read from the 
                            // client. Display it on the console.
                            Console.WriteLine("Read {0} bytes from socket {1}. \n Data : {2}",
                                state.data.Length, mSocketId, state.data.ToString());

                            byte[] dataMsg;
                            MessageHelper.RemoveEOM(state.data, out dataMsg);

                            // Do something with client message
                            MessageHandler.HandleMessage(dataMsg, mGameSession, this);
                        }
                        else
                        {
                            // Not all data received. Get more.
                            handler.BeginReceive(state.buffer, 0, StateObject.BufferSize, 0,
                            new AsyncCallback(ReadCallback), state);
                        }
                    }
                }
            }
            catch(Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }

        public void DispatchMessage(MessageBase outMsg)
        {
            IFormatter formatter = new BinaryFormatter();
            Stream stream = new MemoryStream();
            StateObject state = new StateObject();

            formatter.Serialize(stream, outMsg);

            byte[] buffer = ((MemoryStream)stream).ToArray();
            mClientSocket.Send(buffer, buffer.Length, SocketFlags.None);
        }
    }

}
