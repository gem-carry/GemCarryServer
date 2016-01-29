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
    public class AsyncSocketListener
    {
        // Thread signal
        public static ManualResetEvent allDone = new ManualResetEvent(false);

        private static ServerHost mContext;

        public AsyncSocketListener() { }

        public static void StartListening(ServerHost context)
        {
            mContext = context;

            // Data buffer for incoming data
            byte[] bytes = new Byte[1024];

            // Establish the local endpoint for the socket
            IPAddress ipAddress = IPAddress.Parse("0.0.0.0");
            IPEndPoint localEndPoint = new IPEndPoint(ipAddress, 1025);

            // Create TCP/IP Socket
            Socket listener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

            // Bind the socket to local endpoint to start listening for connections
            try
            {
                listener.Bind(localEndPoint);
                listener.Listen(100);

                while(true)
                {
                    // Set the event to a nonsignaled state
                    allDone.Reset();

                    // Start an asynchronous socket to listen for connections
                    Console.WriteLine("Waiting for a connection...");
                    listener.BeginAccept(new AsyncCallback(AcceptCallback), listener);

                    // Wait until a connection is made before continuing.
                    allDone.WaitOne();
                }
            } catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }

            listener.Close();

            Console.WriteLine("\nPress ENTER to continue..");
            Console.Read();

        }

        public static void AcceptCallback(IAsyncResult ar)
        {
            // Signal the main thread to continue.
            allDone.Set();

            // Get the socket that handles the client request.
            Socket listener = (Socket)ar.AsyncState;

            if(null != listener)
            {
                Socket handler = listener.EndAccept(ar);

                // Create the user player client
                GamePlayer newPlayer = new GamePlayer();
                newPlayer.StartConnection(mContext, handler, mContext.GetNextClientId());
            }
        }

        private static void Send(Socket handler, String data)
        {
            // Convert the string data to byte data using ASCII encoding.
            byte[] byteData = Encoding.ASCII.GetBytes(data);

            // Begin sending the data to the remote device.
            handler.BeginSend(byteData, 0, byteData.Length, 0,
                new AsyncCallback(SendCallback), handler);
        }

        private static void SendCallback(IAsyncResult ar)
        {
            try
            {
                // Retrieve the socket from the state object.
                Socket handler = (Socket)ar.AsyncState;

                // Complete sending the data to the remote device.
                int bytesSent = handler.EndSend(ar);
                Console.WriteLine("Sent {0} bytes to client.", bytesSent);

                handler.Shutdown(SocketShutdown.Both);
                handler.Close();

            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }
    }
}
