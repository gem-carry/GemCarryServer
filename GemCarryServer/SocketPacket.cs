using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;

namespace GemCarryServer
{
    public class SocketPacket
    {
        public SocketPacket(Socket socket)
        {
            workSocket = socket;
        }

        // Client  socket.
        public Socket workSocket;
        // Size of receive buffer.
        public const int BufferSize = 1024;
        // Receive buffer.
        public byte[] buffer = new byte[BufferSize];
    }
}
