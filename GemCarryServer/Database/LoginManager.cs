using GemCarryServer.User;
using Amazon.DynamoDBv2.Model;
using System;
using System.Collections.Generic;

namespace GemCarryServer.Database
{
    /// <summary>
    /// Used to handle connections to the GCLogin Table
    /// </summary>
    public class LoginManager
    {        
        private const string TABLE = "GCLogin"; // DynamoDB table name
        private const string primaryKey = "email";  // primary key to be used in table
        private const string ACCOUNT_ID = "account_id"; // account_id column in table
        private const string ACCOUNT_VALIDATED = "account_validated";   // account_validated column in table
        private const string EMAIL_VERIFICATION_GUID = "email_verification_guid";   // email_verification_guid column in table
        private const string ENCRYPTION = "encryption"; // encryption column in table
        private const string ITERATIONS = "iterations"; // iterations column in table
        private const string PASSWORD_HASH = "password_hash";   // password_hash column in table
        private const string SALT_HASH = "salt_hash";   // salt_hash column in table

        private static LoginManager sInstance = null;

        private DBManager dbManager = DBManager.GetInstance();  // Creating the dbManager instance
        private GameLogger logger = GameLogger.GetInstance();   // Creating the GameLogger instance
        private PasswordHash pwh = new PasswordHash();  // Creating the PasswordHash instance

        /// <summary>Default Constructor for Singleton</summary>
        private LoginManager () { }

        /// <summary>
        /// PRIVATE Create Singleton for LoginManager
        /// </summary>
        /// <returns>LoginManager</returns>
        public static LoginManager GetInstance ()
        {
            // if not already created, create instance of itself.
            if (null == sInstance)
            {
                sInstance = new LoginManager();
            }
            return sInstance;
        }

        /// <summary>
        /// PRIVATE: Retrieves the user information from GCLogin DB
        /// </summary>
        /// <param name="email">email of user</param>
        /// <param name="returnResponse">out GetItemResponse</param>
        /// <returns>0 if successful, otherwise > 0</returns>
        private int GetUserLoginInfo(string email, out GetItemResponse returnResponse)
        {            
            // Gets the user's login information from GCLogin Table and sets it in returnResponse
            int response = dbManager.GetItem(primaryKey, email, TABLE, out returnResponse);

            // Checks the error response
            switch(response)
            {
                // Success
                case (int)DBEnum.DBResponseCodes.SUCCESS:
                    logger.WriteLog(GameLogger.LogLevel.Debug, string.Format("Successfully retrieved email: {0} data from table: {1}", email, TABLE));
                    break;

                // User Does Not Exist
                case (int)DBEnum.DBResponseCodes.DOES_NOT_EXIST:
                    logger.WriteLog(GameLogger.LogLevel.Warning, string.Format("Email: {0} in table: {1} does not exist.", email, TABLE));
                    break;

                case (int)DBEnum.DBResponseCodes.DYNAMODB_EXCEPTION:
                    logger.WriteLog(GameLogger.LogLevel.Error, string.Format("DynamoDB Exception Error when fetching email {0} in table: {1}", email, TABLE));
                    break;
            }
                       
            return response;
        }

        /// <summary>
        /// PRIVATE: Gets a single item from the itemResponse if string type and returns it
        /// </summary>
        /// <param name="itemResponse">Value you're getting item from</param>
        /// <param name="columnName">Column you want value for</param>
        /// <param name="outResponse">value you want returned</param>
        /// <returns>0 if successful, otherwise > 0</returns>
        private int GetUserLoginInfoItemValue(GetItemResponse itemResponse, string columnName, out string outResponse)
        {
            int response = dbManager.GetItemValue(itemResponse, columnName, out outResponse);          
            return response;            
        }

        /// <summary>
        /// PRIVATE: Gets a single item from the itemResponse if bool type and returns it
        /// </summary>
        /// <param name="itemResponse">Value you're getting item from</param>
        /// <param name="columnName">Column you want value for</param>
        /// <param name="outResponse">value you want returned</param>
        /// <returns>0 if successful, otherwise > 0</returns>
        private int GetUserLoginInfoItemValue(GetItemResponse itemResponse, string columnName, out bool? outResponse)
        {
            int response = dbManager.GetItemValue(itemResponse, columnName, out outResponse);
            return response;
        }

        /// <summary>
        /// Set's the GCUser.LoginInfo classes attributes using the .Email variable
        /// </summary>
        /// <param name="loginInfo">GCUser.LoginInfo class passed to set the values to</param>
        /// <returns>0 if successful, otherwise > 0</returns>
        public int SetUserLoginInfo(GCUser.LoginInfo loginInfo)
        {                           
            GetItemResponse returnResponse = new GetItemResponse();
            
            int response = GetUserLoginInfo(loginInfo.Email, out returnResponse);            

            if ((int)DBEnum.DBResponseCodes.SUCCESS == response)
            {
                string tempStringResult;
                bool? tempBoolResult;             
                           
                GetUserLoginInfoItemValue(returnResponse, ACCOUNT_ID, out tempStringResult);                               
                loginInfo.AccountId = tempStringResult;

                GetUserLoginInfoItemValue(returnResponse, ACCOUNT_VALIDATED, out tempBoolResult);
                loginInfo.AccountValidated = tempBoolResult;

                GetUserLoginInfoItemValue(returnResponse, EMAIL_VERIFICATION_GUID, out tempStringResult);
                loginInfo.EmailVerificationGuid = tempStringResult;

                GetUserLoginInfoItemValue(returnResponse, ENCRYPTION, out tempStringResult);
                loginInfo.Encryption = tempStringResult;

                GetUserLoginInfoItemValue(returnResponse, ITERATIONS, out tempStringResult);
                loginInfo.Iterations = tempStringResult;

                GetUserLoginInfoItemValue(returnResponse, PASSWORD_HASH, out tempStringResult);
                loginInfo.PasswordHash = tempStringResult;

                GetUserLoginInfoItemValue(returnResponse, SALT_HASH, out tempStringResult);
                loginInfo.SaltHash = tempStringResult;

                logger.WriteLog(GameLogger.LogLevel.Debug, string.Format("Set user data for user {0} on table {1}", loginInfo.Email, TABLE));
            }
            
            else
            {
                logger.WriteLog(GameLogger.LogLevel.Warning, "USER DOES NOT EXIST");
            }

            return response;
        }

        /// <summary>
        /// Creates a user and sets the values in the GCLogin DynamoDB Table
        /// </summary>
        /// <param name="email">User's email address</param>
        /// <param name="password">Users Password</param>
        /// <returns>0 if successful, otherwise > 0</returns>
        public int CreateUser(GCUser.LoginInfo loginInfo, string password)
        {
            int response = (int)DBEnum.DBResponseCodes.DEFAULT_VALUE;
            PutItemRequest request = new PutItemRequest();
            GetItemResponse giResponse = new GetItemResponse(); // created just to dump response but won't be used ever

            // If user does NOT exist...Create User
            if ((int)DBEnum.DBResponseCodes.DOES_NOT_EXIST == dbManager.GetItem(primaryKey, loginInfo.Email, TABLE, out giResponse))
            {
                try  // Try to create User
                {
                    string accountGuid = Guid.NewGuid().ToString(); // AccountId GUID
                    string emailVerificationGuid = Guid.NewGuid().ToString(); // Email verification hash
                    char[] delimeter = { ':' }; // delimeter for password hashing
                    string[] split = pwh.CreateHash(password).Split(delimeter); // create a hash based on the pw and split it using delimeter
                    
                    // set table to "GCLogin"
                    request.TableName = TABLE;
                    // set items to add
                    request.Item.Add(primaryKey, new AttributeValue { S = loginInfo.Email });
                    request.Item.Add(ACCOUNT_ID, new AttributeValue { S = accountGuid });
                    request.Item.Add(ENCRYPTION, new AttributeValue { S = split[(int)DBEnum.GCLoginIndex.ENCRYPTION_INDEX] });
                    request.Item.Add(ITERATIONS, new AttributeValue { S = split[(int)DBEnum.GCLoginIndex.ITERATION_INDEX] });
                    request.Item.Add(SALT_HASH, new AttributeValue { S = split[(int)DBEnum.GCLoginIndex.SALT_INDEX] });
                    request.Item.Add(PASSWORD_HASH, new AttributeValue { S = split[(int)DBEnum.GCLoginIndex.PBKDF2_INDEX] });
                    request.Item.Add(EMAIL_VERIFICATION_GUID, new AttributeValue { S = emailVerificationGuid });
                    request.Item.Add(ACCOUNT_VALIDATED, new AttributeValue { BOOL = false });
                    // put items in DB
                    dbManager.PutItem(request);

                    response = (int)DBEnum.DBResponseCodes.SUCCESS;
                    logger.WriteLog(GameLogger.LogLevel.Debug, string.Format("Created user {0} in Table {1}", loginInfo.Email, TABLE));
                }
                catch  // If creation fails
                {
                    response = (int)DBEnum.DBResponseCodes.DYNAMODB_EXCEPTION;
                    logger.WriteLog(GameLogger.LogLevel.Error, string.Format("Failed to create user {0} due to DynamoDB Exception.", loginInfo.Email));
                }
            }
            // if user DOES exist or error
            else
            {
                logger.WriteLog(GameLogger.LogLevel.Debug, string.Format("Failed to create user {0}, it already exists or another error happened.", loginInfo.Email));
                response = (int)DBEnum.DBResponseCodes.USER_EXIST;
            }
        
            return response;
        }

        /// <summary>
        /// Validates if the user provided credentials are correct for their user account
        /// </summary>
        /// <param name="loginInfo">Uses GCUser.LoginInfo.Email to validate the email address</param>
        /// <param name="password">Password to validate with hash</param>
        /// <returns>0 if successful, otherwise > 0</returns>
        public int ValidateCredentials(GCUser.LoginInfo loginInfo, string password)
        {
            int response = (int)DBEnum.DBResponseCodes.DEFAULT_VALUE;
            GetItemResponse giResponse = new GetItemResponse();

            // check to see if the user exists (Email needs to be set prior to submitting)
            if ((int)DBEnum.DBResponseCodes.SUCCESS == SetUserLoginInfo(loginInfo))
            {
                if (pwh.ValidatePassword(password, string.Format("{0}:{1}:{2}:{3}",
                    loginInfo.Encryption,
                    loginInfo.Iterations,
                    loginInfo.SaltHash,
                    loginInfo.PasswordHash)))
                {
                    logger.WriteLog(GameLogger.LogLevel.Debug, string.Format("User: {0} authenticated.", loginInfo.Email));
                    response = (int)DBEnum.DBResponseCodes.SUCCESS;
                }
                else
                {
                    logger.WriteLog(GameLogger.LogLevel.Debug, string.Format("User: {0} failed to authenticate.", loginInfo.Email));
                    response = (int)DBEnum.DBResponseCodes.INVALID_USERNAME_PASSWORD;
                }
            }
            
            // User does not exist or something failed to set (either way return error)
            else
            {
                logger.WriteLog(GameLogger.LogLevel.Debug, string.Format("User: {0} failed to authenticate.", loginInfo.Email));
                response = (int)DBEnum.DBResponseCodes.DOES_NOT_EXIST;
            }

            return response;
        }

        /// <summary>
        /// Updates the users credentials
        /// </summary>
        /// <param name="loginInfo">Uses GCUser.LoginInfo.Email to validate the email address</param>
        /// <param name="oldPassword">Old password to validate</param>
        /// <param name="newPassword">New password to set to</param>
        /// <returns>0 if successful, otherwise > 0</returns>
        public int UpdateCredentials(GCUser.LoginInfo loginInfo, string oldPassword, string newPassword)
        {
            int response = (int)DBEnum.DBResponseCodes.DEFAULT_VALUE;
            GetItemResponse giResponse = new GetItemResponse();

            // validate to see if user exist
            if ((int)DBEnum.DBResponseCodes.SUCCESS == SetUserLoginInfo(loginInfo))
            {
                // validate password correct
                if (pwh.ValidatePassword(oldPassword, string.Format("{0}:{1}:{2}:{3}",
                    loginInfo.Encryption,
                    loginInfo.Iterations,
                    loginInfo.SaltHash,
                    loginInfo.PasswordHash)))
                {
                    char[] delimiter = { ':' }; // delimiter for password hash
                    string[] split = pwh.CreateHash(newPassword).Split(delimiter); // split the string into array

                    UpdateItemRequest request = new UpdateItemRequest();
                    request.TableName = TABLE;  // set table to "GCLogin"
                    request.Key = new Dictionary<string, AttributeValue>() { { primaryKey, new AttributeValue { S = loginInfo.Email } } };
                    request.UpdateExpression = string.Format("SET #s =:{0}, #p =:{1}", SALT_HASH, PASSWORD_HASH);

                    request.ExpressionAttributeNames = new Dictionary<string, string>
                    {
                        { "#s", SALT_HASH },
                        { "#p", PASSWORD_HASH }
                    };

                    request.ExpressionAttributeValues = new Dictionary<string, AttributeValue>
                    {
                        {string.Format(":{0}",SALT_HASH), new AttributeValue {S = split[(int)DBEnum.GCLoginIndex.SALT_INDEX] } },
                        {string.Format(":{0}",PASSWORD_HASH), new AttributeValue {S = split[(int)DBEnum.GCLoginIndex.PBKDF2_INDEX] } }
                    };
                    dbManager.UpdateItem(request);

                    logger.WriteLog(GameLogger.LogLevel.Debug, string.Format("User: {0} credentials updated.", loginInfo.Email));
                    response = (int)DBEnum.DBResponseCodes.SUCCESS;
                }
                // if not correct set response > 0
                else
                {
                    logger.WriteLog(GameLogger.LogLevel.Debug, string.Format("Failed to update User: {0} credentials.", loginInfo.Email));
                    response = (int)DBEnum.DBResponseCodes.INVALID_USERNAME_PASSWORD;
                }
            }

            // user does not exist
            else
            {
                logger.WriteLog(GameLogger.LogLevel.Debug, string.Format("User: {0} credentials updated.", loginInfo.Email));
                response = (int)DBEnum.DBResponseCodes.DOES_NOT_EXIST;
            }

            return response;
        }

        /// <summary>
        /// Deletes user account.
        /// </summary>
        /// <param name="loginInfo"></param>
        /// <param name="password"></param>
        /// <returns></returns>
        public int DeleteAccount(GCUser.LoginInfo loginInfo, string password)
        {
            int response = (int)DBEnum.DBResponseCodes.DEFAULT_VALUE;

            if ((int)DBEnum.DBResponseCodes.SUCCESS == ValidateCredentials(loginInfo, password))
            {
                dbManager.DeleteItem(primaryKey, loginInfo.Email, TABLE);                
                response = (int)DBEnum.DBResponseCodes.SUCCESS;
                logger.WriteLog(GameLogger.LogLevel.Debug, string.Format("User: {0} has been deleted from GCLogin Table.", loginInfo.Email));
            }
            else
            {
                response = (int)DBEnum.DBResponseCodes.INVALID_USERNAME_PASSWORD;
            }

            return response;
        }
    }

}