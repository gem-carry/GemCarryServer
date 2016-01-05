using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Amazon.SimpleEmail;
using Amazon.SimpleEmail.Model;


namespace GemCarryServer
{    
    class GemDatabase
    {
        private PasswordHash pwh = new PasswordHash();
        private AmazonDynamoDBClient client;
        private GameLogger logger = GameLogger.GetInstance();
        private const string LOGIN_TABLE_NAME = "GCLogin";

        // constants for Login DB Indexes
        private const int EMAIL_INDEX = 0;
        private const int ENCRYPTION_INDEX = 1;
        private const int ITERATION_INDEX = 2;
        private const int PASSWORD_HASH_INDEX = 3;
        private const int SALT_HASH_INDEX = 4;
        private const int ACCOUNTID_INDEX = 5;
        private const int LOGIN_TOTAL_COLUMNS = 6;

        private const string ITERATION_COUNT = "1000";
        private const string ENCRYPTION_TYPE = "sha1";

        /// <summary>
        /// Creates a connection with DynamoDB client
        /// </summary>    
        /// <returns>True if connection succeeds. False otherwise.</returns>        
        public GemDatabase()
        {
            try
            {
                this.client = new AmazonDynamoDBClient();
                #if DEBUG
                    logger.WriteLog(GameLogger.LogLevel.Debug, "Opening connection to DynamoDB Client");
                #endif // DEBUG                
            }
            catch (AmazonDynamoDBException ex)
            {
                logger.WriteLog(GameLogger.LogLevel.Error, ex.Message.ToString());
            }
        }


        /// <summary>
        /// Checks to see if user exists within the DB
        /// </summary>
        /// <param name="u">The user in which we are checking.</param>       
        /// <param name="r">The output return result of the user info if user is found. Else return null.</param>   
        /// <returns>true if found and false if not found. Also returns output string r from input parameters</returns>
        public bool DoesUserExist(string u, out string r)
        {
            // Setting return value to false with no results.           
            GetItemRequest request = new GetItemRequest();
            request.TableName = LOGIN_TABLE_NAME;
            AttributeValue[] av = new AttributeValue[LOGIN_TOTAL_COLUMNS];
            request.Key = new Dictionary<string, AttributeValue>() { { "email", new AttributeValue { S = u } } };
            try
            {
                var response = this.client.GetItem(request);
                if (0 == response.Item.Count)
                {
                    #if DEBUG
                        logger.WriteLog(GameLogger.LogLevel.Debug, string.Format("User {0} does not exist in table", u));
                    #endif // DEBUG
                    r = null;
                    return false;
                }                
                #if DEBUG
                    logger.WriteLog(GameLogger.LogLevel.Debug, string.Format("User {0} exists in table", u));
                #endif // DEBUG
                response.Item.TryGetValue("email", out av[EMAIL_INDEX]);
                response.Item.TryGetValue("encryption", out av[ENCRYPTION_INDEX]);
                response.Item.TryGetValue("iterations", out av[ITERATION_INDEX]);
                response.Item.TryGetValue("password_hash", out av[PASSWORD_HASH_INDEX]);
                response.Item.TryGetValue("salt_hash", out av[SALT_HASH_INDEX]);
                response.Item.TryGetValue("account_id", out av[ACCOUNTID_INDEX]);
                r = string.Format("{0}:{1}:{2}:{3}:{4}:{5}",
                    av[EMAIL_INDEX].S, 
                    av[ACCOUNTID_INDEX].S, 
                    av[ENCRYPTION_INDEX].S, 
                    av[ITERATION_INDEX].S,
                    av[SALT_HASH_INDEX].S,
                    av[PASSWORD_HASH_INDEX].S);                
                return true;
            }
            catch (AmazonDynamoDBException ex)
            {
                logger.WriteLog(GameLogger.LogLevel.Error, ex.Message.ToString());
                r = null;
                return false;
            }
                     
        }

        /// <summary>
        /// Creates User within the DB
        /// </summary>
        /// <param name="e">The user's email address.</param>       
        /// <param name="p">The user's password.</param>   
        /// <param name="r">Output response when user created.</param>   
        /// <returns>DBCreateUserEnum Also returns output string r from input parameters</returns>
        public GemCarryEnum.DbCreateUserEnum CreateUser(string e, string p, out string r)
        {
            if (DoesUserExist(e, out r))
            {
                #if DEBUG
                    logger.WriteLog(GameLogger.LogLevel.Debug, string.Format("Could not create User {0}; already exists in table", e));
                #endif // DEBUG
                r = null;
                return GemCarryEnum.DbCreateUserEnum.Exists;
            }
            else
            {
                try
                {
                    const int TEMP_ENCRYPTION_INDEX = 0;
                    const int TEMP_ITERATION_INDEX = 1;
                    const int TEMP_SALT_INDEX = 2;
                    const int TEMP_PBKDF2_INDEX = 3;

                    string mAccountGuid = Guid.NewGuid().ToString();
                    string mEmailVerificationGuid = Guid.NewGuid().ToString();
                    char[] delimiter = { ':' };
                    string[] split = pwh.CreateHash(p).Split(delimiter);
 
                    PutItemRequest request = new PutItemRequest();
                    request.TableName = LOGIN_TABLE_NAME;
                    request.Item.Add("email", new AttributeValue { S = e });
                    request.Item.Add("account_id", new AttributeValue { S = mAccountGuid });
                    request.Item.Add("encryption", new AttributeValue { S = split[TEMP_ENCRYPTION_INDEX] });
                    request.Item.Add("iterations", new AttributeValue { S = split[TEMP_ITERATION_INDEX] });
                    request.Item.Add("salt_hash", new AttributeValue { S = split[TEMP_SALT_INDEX] });
                    request.Item.Add("password_hash", new AttributeValue { S = split[TEMP_PBKDF2_INDEX] });
                    request.Item.Add("email_verification_guid", new AttributeValue { S = mEmailVerificationGuid });
                    request.Item.Add("account_validated", new AttributeValue { BOOL = false });
                    this.client.PutItem(request);
                    #if DEBUG
                        logger.WriteLog(GameLogger.LogLevel.Debug, string.Format("User {0} added to GemCarryLogin table.", e));
                    #endif // DEBUG
                    r = String.Format("{0}:{1}",e,mAccountGuid);    // out emailaddress:accountid
                    SendVerificationEmail(e,mEmailVerificationGuid);
                    return GemCarryEnum.DbCreateUserEnum.Success;
                }
                catch (AmazonDynamoDBException ex)
                {
                    r = null;
                    logger.WriteLog(GameLogger.LogLevel.Error, ex.Message.ToString());
                    return GemCarryEnum.DbCreateUserEnum.ConnectionError;
                }
            }            
        }

        public void SendVerificationEmail(string emailaddress, string validation_guid)
        {
            const string VERIFICATION_DOMAIN = "https://nzixo03fx1.execute-api.us-west-2.amazonaws.com/prod/emailvalidation?verificationstring="; //TODO: Move this to our domain name prior to launch
            const string FROM = "gemcarry@brianwthomas.com"; //TODO: Change to real domain name
            const string SUBJECT = "Please verify your email address";
            string TO = emailaddress;
            string mBase64EncodedGuid = Convert.ToBase64String(Encoding.UTF8.GetBytes(emailaddress + ":" + validation_guid));
             
            Destination destination = new Destination();
            destination.ToAddresses = (new List<string> { TO });

            Content subject = new Content(SUBJECT);
            Content textBody = new Content(string.Format("Please click the following link to verifiy your email address {0}{1}", VERIFICATION_DOMAIN,mBase64EncodedGuid));
            Body body = new Body(textBody);
            
            // Create a message with the specified subject/body
            Message message = new Message(subject, body);

            // assemble the avengers..err email
            SendEmailRequest request = new SendEmailRequest(FROM, destination, message);
            
            AmazonSimpleEmailServiceClient client = new AmazonSimpleEmailServiceClient();
            try
            {                
#if DEBUG
    logger.WriteLog(GameLogger.LogLevel.Debug, string.Format("Generating Validation Email for {0}.", emailaddress));
#endif // DEBUG
                client.SendEmail(request);
            }
            catch (Exception ex)
            {
                logger.WriteLog(GameLogger.LogLevel.Error, ex.Message.ToString());                
            }
        }

        /// <summary>
        /// Validates User Credentilas in Table
        /// </summary>
        /// <param name="e">The user's email address.</param>       
        /// <param name="p">The user's password.</param>           
        /// <returns>true if credentials match, otherwise false</returns>
        public bool ValidateCredentials(string e, string p)
        {
            // temp string for output of below functions
            string r;
            if (!DoesUserExist(e, out r))
            {
                #if DEBUG
                    logger.WriteLog(GameLogger.LogLevel.Debug, string.Format("Validating password for user {0}, but user {0} doesn't exist in table; ValidateCredentials() = false", e));
                #endif // DEBUG
                return false;
            }
            // setting constant for the 4 items we need (encryption:iterations:salt:hash)
            const int ITEMS_RETURNED = 4;    
                                   
            GetItemRequest request = new GetItemRequest();
            request.TableName = LOGIN_TABLE_NAME;
            AttributeValue[] av = new AttributeValue[ITEMS_RETURNED];
            request.Key = new Dictionary<string, AttributeValue>() { { "email", new AttributeValue { S = e } } };
            try
            {
                var response = this.client.GetItem(request);
                                               
                response.Item.TryGetValue("encryption", out av[0]);
                response.Item.TryGetValue("iterations", out av[1]);
                response.Item.TryGetValue("salt_hash", out av[2]);
                response.Item.TryGetValue("password_hash", out av[3]);
                r = string.Format("{0}:{1}:{2}:{3}",                    
                    av[0].S,    // encryption
                    av[1].S,    // iterations
                    av[2].S,    // salt hash
                    av[3].S);   // password hash                                
                if (pwh.ValidatePassword(p,r))
                {
                    #if DEBUG
                        logger.WriteLog(GameLogger.LogLevel.Debug, string.Format("Validating Password for User {0}; ValidateCredentials() = true", e));
                    #endif // DEBUG   
                    return true;
                }
                else
                {
                    #if DEBUG
                        logger.WriteLog(GameLogger.LogLevel.Debug, string.Format("Validating Password for User {0}; ValidateCredentials() = false", e));
                    #endif // DEBUG   
                    return false;
                }
            }
            catch (AmazonDynamoDBException ex)
            {
                logger.WriteLog(GameLogger.LogLevel.Error, ex.Message.ToString());                
                return false;
            }
        }

        /// <summary>
        /// Validates User Credentilas in Table
        /// </summary>
        /// <param name="e">The user's email address.</param>       
        /// <param name="p">The user's current password.</param>           
        /// <param name="n">The user's new password.</param>  
        /// <returns>true if credentials updated, otherwise false</returns>
        public bool UpdateCredentials(string e, string p, string n)
        {
            string r;
            if (!DoesUserExist(e, out r))
            {
                #if DEBUG
                    logger.WriteLog(GameLogger.LogLevel.Debug, string.Format("Could not change password for user {0}; user doesn't exist", e));
                #endif // DEBUG
                return false;
            }
            else
            {
                if (!ValidateCredentials(e, p))
                {
                    #if DEBUG
                        logger.WriteLog(GameLogger.LogLevel.Debug, string.Format("Could not change password for user {0}; user credentials are incorrect", e));
                    #endif // DEBUG
                    return false;
                }
                try
                {
                    const int TEMP_SALT_INDEX = 2;
                    const int TEMP_PBKDF2_INDEX = 3;

                    char[] delimiter = { ':' };
                    string[] split = pwh.CreateHash(n).Split(delimiter);                    

                    UpdateItemRequest request = new UpdateItemRequest();
                    request.TableName = LOGIN_TABLE_NAME;
                    request.Key = new Dictionary<string, AttributeValue>() { { "email", new AttributeValue { S = e } } };
                    request.UpdateExpression = "SET #s =:salt_hash, #p =:password_hash";
                    request.ExpressionAttributeNames = new Dictionary<string, string>
                    {
                        { "#s", "salt_hash" },
                        { "#p", "password_hash" }
                    };
                    request.ExpressionAttributeValues = new Dictionary<string, AttributeValue>
                    {
                        {":salt_hash", new AttributeValue {S = split[TEMP_SALT_INDEX] } },
                        {":password_hash", new AttributeValue {S = split[TEMP_PBKDF2_INDEX] } }
                    };                    
                    this.client.UpdateItem(request);
                    #if DEBUG
                        logger.WriteLog(GameLogger.LogLevel.Debug, string.Format("User {0} password changed.", e));
                    #endif // DEBUG                    
                    return true;
                }
                catch (AmazonDynamoDBException ex)
                {
                    logger.WriteLog(GameLogger.LogLevel.Error, ex.Message.ToString());
                    return false;
                }
            }
        }

        /// <summary>
        /// Deletes users entry in Table
        /// </summary>
        /// <param name="e">The user's email address.</param>       
        /// <param name="p">The user's current password.</param>                   
        /// <returns>true if entry deleted, otherwise false</returns>
        public bool DeleteAccount(string e, string p)
        {
            try
            {
                string r;
                if (!DoesUserExist(e, out r))
                {
                    #if DEBUG
                        logger.WriteLog(GameLogger.LogLevel.Debug, string.Format("Could not delete account for user {0}, user doesn't exist; DeleteAccount() = false", e));
                    #endif // DEBUG
                    return false;
                }
                else
                {
                    if (ValidateCredentials(e, p))
                    {
                        DeleteItemRequest request = new DeleteItemRequest();
                        request.TableName = LOGIN_TABLE_NAME;
                        request.Key = new Dictionary<string, AttributeValue>() { { "email", new AttributeValue { S = e } } };
                        client.DeleteItem(request);
                        #if DEBUG
                            logger.WriteLog(GameLogger.LogLevel.Debug, string.Format("Account deleted for user {0}; DeleteAccount() = true;", e));
                        #endif // DEBUG
                        return true;
                    }
                    else
                    {
                        #if DEBUG
                            logger.WriteLog(GameLogger.LogLevel.Debug, string.Format("Could not delete account for user {0}, password not correct; DeleteAccount() = false", e));
                        #endif // DEBUG
                        return false;
                    }
                }
            }
            catch (AmazonDynamoDBException ex)
            {
                logger.WriteLog(GameLogger.LogLevel.Error, ex.Message.ToString());
                return false;
            }
        }
    }
}
