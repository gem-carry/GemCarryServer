using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Amazon.SimpleEmail;
using Amazon.SimpleEmail.Model;
using System.IO;
using MimeKit;

namespace GemCarryServer
{
    class MailSender
    {
        private static GameLogger logger = GameLogger.GetInstance();

        public static void SendVerificationEmail(string emailaddress, string validation_guid)
        {
            const string VERIFICATION_DOMAIN = "https://nzixo03fx1.execute-api.us-west-2.amazonaws.com/prod/emailvalidation?verificationstring="; //TODO: Move this to our domain name prior to launch
            const string FROM = "gemcarry@brianwthomas.com"; //TODO: Change to real domain name
            const string SUBJECT = "Please verify your email address";
            string TO = emailaddress;
            string mBase64EncodedGuid = Convert.ToBase64String(Encoding.UTF8.GetBytes(emailaddress + ":" + validation_guid));

            var message = new MimeMessage();            
            message.From.Add(new MailboxAddress("GEM CARRY EMAIL VERIFICATION", FROM)); 
            message.To.Add(new MailboxAddress("New Gamer", TO));
            message.Subject = SUBJECT;

            var builder = new BodyBuilder();
            builder.TextBody = string.Format("To activate your account, please click the following link to verifiy your email address {0}{1}", VERIFICATION_DOMAIN, mBase64EncodedGuid);
            builder.HtmlBody = string.Format("To activate your account, please click <a href={0}{1}> here</a> to verify your email address.",VERIFICATION_DOMAIN,mBase64EncodedGuid);

            message.Body = builder.ToMessageBody();

            var stream = new MemoryStream();
            message.WriteTo(stream);

            try
            {
                AmazonSimpleEmailServiceClient client = new AmazonSimpleEmailServiceClient();
                var request = new SendRawEmailRequest { RawMessage = new RawMessage { Data = stream } };
                client.SendRawEmail(request);
#if DEBUG
                logger.WriteLog(GameLogger.LogLevel.Debug, string.Format("Generating Validation Email for {0}.", emailaddress));
#endif // DEBUG
                
            }
            catch (Exception ex)
            {
                logger.WriteLog(GameLogger.LogLevel.Error, ex.Message.ToString());
            }
        }
       
    }
}
