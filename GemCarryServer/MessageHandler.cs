using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.Serialization;
using System.IO;
using System.IO.Compression;
using System.Runtime.Serialization.Formatters.Binary;
using GCMessaging;

namespace GemCarryServer
{
    public class MessageHandler
    {
        
        public static void HandleMessage(Byte[] msgData, GameSession session, GamePlayer client)
        {
            IFormatter formatter = new BinaryFormatter();
            MemoryStream dataStream = new MemoryStream();
            using (MemoryStream compressedStream = new MemoryStream(msgData))
            {
                using (DeflateStream ds = new DeflateStream(compressedStream, CompressionMode.Decompress))
                {
                    ds.CopyTo(dataStream);
                    ds.Close();
                }
                dataStream.Position = 0;
            }

            MessageBase msg = (MessageBase)formatter.Deserialize(dataStream);

            switch(msg.mType)
            {
                case MessageType.LOGIN:
                    {
                        LoginMessage loginMsg = (LoginMessage) msg;

                        int status = LoginManager.AttemptLoginForClient(loginMsg/*, out UserDetails user*/);

                        if(0 == status)
                        {
                            // Success!
                            //OutMessageUserDetails userMsg = new OutMessageUserDetails(
                            //client.DispatchMessage();
                            Console.WriteLine("Login successful");
                        }
                        else
                        {
                            Console.WriteLine("Failed with reason " + status);
                        }

                        break;
                    }
                case MessageType.HEARTBEAT:
                default:
                    {
                        Console.WriteLine("Received Heartbeat from client");
                        return;
                    }
            }
        }
    }
}
