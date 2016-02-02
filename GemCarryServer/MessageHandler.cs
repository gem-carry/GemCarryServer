using System;
using System.Runtime.Serialization;
using System.IO;
using System.IO.Compression;
using System.Runtime.Serialization.Formatters.Binary;
using GCMessaging;
using ProtoBuf;
using GemCarryServer.Database;

namespace GemCarryServer
{
    public class MessageHandler
    {
        
        public static void HandleMessage(Byte[] msgData, GameSession session, GamePlayer client)
        {
            using(MemoryStream stream = new MemoryStream(msgData))
            {
                BaseMessage msg = Serializer.Deserialize<BaseMessage>(stream);

                switch((MessageType)msg.messageType)
                {
                    case MessageType.LoginRequest:
                        {
                            LoginRequest loginMsg = (LoginRequest) msg;

                            int status = LoginManager.GetInstance().ValidateCredentials(new User.GCUser.LoginInfo(loginMsg.username), loginMsg.password);

                            LoginResponse response = new LoginResponse() { success = (status == 0) };
                            client.DispatchMessage(response);
                            break;
                        }
                    case MessageType.ChatMessage:
                        {
                            session.SendToPlayers(msgData);
                            break;
                        }
                    case MessageType.CreateUserRequest:
                        {
                            CreateUserRequest createUserMessage = (CreateUserRequest)msg;

                            int status = (int)DBEnum.DBResponseCodes.SUCCESS;//LoginManager.GetInstance().CreateUser(new User.GCUser.LoginInfo(createUserMessage.mUsername), createUserMessage.mPassword);

                            CreateUserResponse response = new CreateUserResponse() { success = ((int)DBEnum.DBResponseCodes.SUCCESS == status) };
                            client.DispatchMessage(response);
                            break;
                        }                
                    case MessageType.Heartbeat:
                    default:
                        {
                            break;
                        }
                }
            }
        }
    }
}
