using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Net;
using System.Net.Sockets;

namespace GemCarryServer
{
    public class AsyncServer
    {
        // Thread signal
        private static Mutex allDone;

        private static ServerHost mContext;

        private SocketAsyncEventArgsPool mSocketPool; //<! Pool of reusable SocketAsyncEventArgs objects for accept, read, and write operations
        private BufferManager mBufferManager;
        private Socket mListener;
        
        private int mNumConnections; // Max number of connections to allow
        private int mReceiveBufferSize; // Buffer size to use for each i/o operation
        const int opsToPreAlloc = 2; // One for read, one for write, zero for accepts

        private int mNumConnectedSockets;
        Semaphore mMaxNumberAcceptedClients;

        public AsyncServer(ServerHost host, int numConnections, int receiveBufferSize)
        {
            mContext = host;
            allDone = new Mutex();

            mNumConnectedSockets = 0;
            mNumConnections = numConnections;
            mReceiveBufferSize = receiveBufferSize;
            
            // Allocate buffers so that the maximum number of sockets can each have one simultaneous read and write
            int totalBytes = receiveBufferSize * numConnections * opsToPreAlloc;
            mBufferManager = new BufferManager(totalBytes, receiveBufferSize);

            mSocketPool = new SocketAsyncEventArgsPool(numConnections);
            mMaxNumberAcceptedClients = new Semaphore(numConnections, numConnections);

            Init();
        }

        /// <summary>
        /// Sets up our buffer and pool of Socket Event Args
        /// </summary>
        private void Init()
        {
            // Allocate one large buffer which all I/O operations use. This prevents memory fragmentation
            mBufferManager.InitBuffer();

            // Pre-Allocate pool of SocketAsyncEventArgs objects
            SocketAsyncEventArgs readWriteEventArg;

            for(int i = 0; i < mNumConnections; ++i)
            {
                // Pre-Allocate a set of reusable SocketAsyncEventArgs
                readWriteEventArg = new SocketAsyncEventArgs();
                readWriteEventArg.Completed += new EventHandler<SocketAsyncEventArgs>(IO_Completed);

                // assign a byte buffer from the buffer pool to the SocketAsyncEventArg object
                mBufferManager.SetBuffer(readWriteEventArg);

                // add our arg to the pool
                mSocketPool.Push(readWriteEventArg);
            }
        }

        /// <summary>
        /// Start the server
        /// </summary>
        /// <param name="context"></param>
        /// <param name="localEndPoint"></param>
        public void Start(IPEndPoint localEndPoint)
        {
            // Create TCP/IP Socket
            mListener = new Socket(localEndPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            mListener.Bind(localEndPoint);
            
            // Start listening with a listen backlog of 100 connections
            mListener.Listen(100);

            Console.WriteLine("Waiting for client connections...");

            // post accepts on the listening socket
            StartAcceptAsync(null);

            // Block this thread to keep listening open.
            allDone.WaitOne();
        }

        public void Stop()
        {
            mListener.Close();
            allDone.ReleaseMutex();
        }

        /// <summary>
        /// Begins an operation to accept a connection request from the client
        /// </summary>
        /// <param name="acceptEventArg"></param>
        public void StartAcceptAsync(SocketAsyncEventArgs acceptEventArg)
        {
            if(null == acceptEventArg)
            {
                acceptEventArg = new SocketAsyncEventArgs();
                acceptEventArg.Completed += new EventHandler<SocketAsyncEventArgs>(AcceptEventArg_Completed);
            }
            else
            {
                // socket must be cleared since the context object is being re-used
                acceptEventArg.AcceptSocket = null;
            }

            //mMaxNumberAcceptedClients.WaitOne();
            bool acceptPending = mListener.AcceptAsync(acceptEventArg);
            if(false == acceptPending)
            {
                ProcessAccept(acceptEventArg);
            }
        }

        /// <summary>
        /// This method is the callback method associated with Socket.AcceptAsync
        /// operations and is invoked when an accept operation is complete
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void AcceptEventArg_Completed(object sender, SocketAsyncEventArgs e)
        {
            ProcessAccept(e);
        }

        /// <summary>
        /// Process an accepted connection
        /// </summary>
        /// <param name="e"></param>
        private void ProcessAccept(SocketAsyncEventArgs e)
        {
            if (true == e.AcceptSocket.Connected)
            {
                try
                {
                    // Get the socket for the accepted client connection and put it into the ReadEventArg object user token
                    SocketAsyncEventArgs readEventArgs = mSocketPool.Pop();

                    if (null != readEventArgs)
                    {
                        Interlocked.Increment(ref mNumConnectedSockets);
                        Console.WriteLine("Client connection accepted. Now {0} clients connected.", mNumConnectedSockets);

                        readEventArgs.UserToken = new GamePlayer(mContext, e.AcceptSocket, mReceiveBufferSize);

                        // As soon as the client is connected, post a receive to the connection
                        bool pendingIO = e.AcceptSocket.ReceiveAsync(readEventArgs);
                        if (false == pendingIO)
                        {
                            ProcessReceive(readEventArgs);
                        }
                    }
                    else
                    {
                        e.AcceptSocket.Close();
                        Console.WriteLine("Client connection refused - Maximum connections reached");
                        var ex = new Exception("No more connections can be accepted on the server.");
                        throw ex;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.ToString());
                }
            }
            
            // Accept the next connection request
            StartAcceptAsync(e);
        }

        /// <summary>
        /// Called whenever a send or receive is completed
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void IO_Completed(object sender, SocketAsyncEventArgs e)
        {
            // determine which type of operation just completed and call the associated handler
            switch (e.LastOperation)
            {
                case SocketAsyncOperation.Receive:
                    ProcessReceive(e);
                    break;
                case SocketAsyncOperation.Send:
                    ProcessSend(e);
                    break;
                default:
                    throw new ArgumentException("The last operation completed on the socket was not a receive or send");
            }

        }


        /// <summary>
        /// Handle async receive operations
        /// </summary>
        /// <param name="e"></param>
        private void ProcessReceive(SocketAsyncEventArgs e)
        {
            if(e.BytesTransferred > 0)
            {
                if (SocketError.Success == e.SocketError)
                {
                    GamePlayer player = e.UserToken as GamePlayer;

                    if(player.ReadSocketData(e))
                    {
                        Socket s = player.Socket;

                        // Has the client received all the data?
                        if(0 == s.Available)
                        {
                            player.ProcessData(e);
                        }

                        // Start another request immediately
                        bool IOPending = s.ReceiveAsync(e);
                        if(false == IOPending)
                        {
                            ProcessReceive(e);
                        }
                    }
                    //e.SetBuffer(e.Offset, e.BytesTransferred);
                }
                else
                {
                    CloseClientSocket(e);
                }
            }
            else // Client is closing connection
            {
                CloseClientSocket(e);
            }
        }

        /// <summary>
        /// Handles async send operations
        /// </summary>
        /// <param name="e"></param>
        private void ProcessSend(SocketAsyncEventArgs e)
        {
            if(SocketError.Success == e.SocketError)
            {
                GamePlayer player = (GamePlayer)e.UserToken;

                bool willRaiseEvent = player.Socket.ReceiveAsync(e);
                if(false == willRaiseEvent)
                {
                    ProcessReceive(e);
                }
            }
            else
            {
                CloseClientSocket(e);
            }
        }

        /// <summary>
        /// Properly closes a client socket and returns things to their pools
        /// </summary>
        /// <param name="e"></param>
        private void CloseClientSocket(SocketAsyncEventArgs e)
        {
            GamePlayer player = e.UserToken as GamePlayer;

            // Close the socket associated with this client
            try
            {
                player.QuitGameSession();
                player.Socket.Shutdown(SocketShutdown.Send);
            }
            catch (Exception) { }
            player.Socket.Close();

            Interlocked.Decrement(ref mNumConnectedSockets);
            Console.WriteLine("A client has disconnected. There are now {0} clients connected.", mNumConnectedSockets);

            // Free the SocketAsyncEventArg so they can be re-used by another client
            mSocketPool.Push(e);
        }
    }
}
