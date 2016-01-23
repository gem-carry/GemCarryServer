namespace GemCarryServer
{
    public class DBEnum
    {
        public enum CreateUserError
        {
            Success,
            Exists,
            ConnectionError
        }

        public enum LoginColumn
        {
            EMAIL = 0,
            ENCRYPTION,
            ITERATION,
            PASSWORD_HASH,
            SALT_HASH,
            ACCOUNTID,
            NUM_COLUMNS
        }

        public enum DbTables
        {
            LOGIN = 0,
            USERS,
            NUM_TABLES
        }
        
        public enum DBResponseCodes
        {
            DEFAULT_VALUE = -1,
            // Success
            SUCCESS = 0,

            // Low Level Errors 100-199
            DOES_NOT_EXIST = 100,
            DYNAMODB_EXCEPTION = 110,

            // Login Errors 1000-1999
            USER_EXIST = 1000,
            INVALID_USERNAME_PASSWORD = 1001,        
            
        }        

        public enum GCLoginIndex
        {
            ENCRYPTION_INDEX = 0, // used for stringsplit
            ITERATION_INDEX = 1, // used for stringsplit
            SALT_INDEX = 2, // used for stringsplit
            PBKDF2_INDEX = 3, // used for stringsplit
        }

    }  
}
