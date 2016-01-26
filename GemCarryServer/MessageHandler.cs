using System;
using System.Runtime.Serialization;
using System.IO;
using System.IO.Compression;
using System.Runtime.Serialization.Formatters.Binary;
using GCMessaging;
using GemCarryServer.Database;

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

                        int status = 0;// LoginManager.GetInstance().ValidateCredentials(new User.GCUser.LoginInfo(loginMsg.mUsername), loginMsg.mPassword);

                        if(0 == status)
                        {
                            // Success!
                            ChatMessage omsg = new ChatMessage();
                            omsg.mSender = "Server";
                            omsg.mMessage = "You have logged in.";

                            client.DispatchMessage(omsg);

                            Console.WriteLine("User {0} logged in", loginMsg.mUsername);
                        }
                        else
                        {
                            Console.WriteLine("Failed with reason " + status);
                        }

                        break;
                    }
                case MessageType.CHAT:
                    {
                        ChatMessage cMsg = (ChatMessage) msg;
                        ChatMessage omsg = new ChatMessage();
                        omsg.mSender = "Server";
                        omsg.mMessage = String.Format("Got your message {0}", cMsg.mSender);                        
                        client.DispatchMessage(omsg);
                        Console.WriteLine(String.Format("User: {0} - {1}", cMsg.mSender, cMsg.mMessage));
                        break;
                    }
                case MessageType.JOINSESSION:
                    {                        
                        
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
