using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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

    }  
}
