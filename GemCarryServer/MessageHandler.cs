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
                        
                                      
                        int status = LoginManager.GetInstance().ValidateCredentials(new User.GCUser.LoginInfo(loginMsg.mUsername), loginMsg.mPassword);
                        ChatMessage omsg = new ChatMessage();
                        omsg.mSender = "Server";
                        
                        if (0 == status)
                        {
                            // Success!
                            omsg.mMessage = "You have logged in.";  
                                                      
                        }
                        else
                        {
                            omsg.mMessage = "Failed to log authenticate. Please check your username or password and try again.";
                        }
                        client.DispatchMessage(omsg);                        
                        

                        break;
                    }
                case MessageType.CHAT:
                    {
                        session.SendToPlayers(msgData);
                        break;
                    }
                case MessageType.CREATEUSER:
                    {
                        ServerResponseCodeMessage responseCodeMsg = new ServerResponseCodeMessage();
                        CreateUserMessage createUserMessage = (CreateUserMessage)msg;
                        ChatMessage omsg = new ChatMessage();
                        omsg.mSender = "Server";

                        int status = (int)DBEnum.DBResponseCodes.SUCCESS;//LoginManager.GetInstance().CreateUser(new User.GCUser.LoginInfo(createUserMessage.mUsername), createUserMessage.mPassword);
                        responseCodeMsg.mResponseCode = status;

                        if ((int)DBEnum.DBResponseCodes.SUCCESS == status)
                        {
                            omsg.mMessage = String.Format("User name: {0} has been successfully created.", createUserMessage.mUsername);
                        }   
                        else if ((int)DBEnum.DBResponseCodes.USER_EXIST == status)
                        {
                            omsg.mMessage = "Error: Username already exists, please pick another name.";
                        }
                        else
                        {
                            omsg.mMessage = String.Format("User name: {0} has been successfully created.", createUserMessage.mUsername);
                        }
                        client.DispatchMessage(responseCodeMsg);
                        client.DispatchMessage(omsg);

                        break;
                    }                
                case MessageType.HEARTBEAT:
                default:
                    {
                        return;
                    }
            }
        }


    }
}
