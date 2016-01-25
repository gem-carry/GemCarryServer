namespace GemCarryServer.User
{
    public class GCUser
    {        
        
        public class LoginInfo
        {
            private string _email;
            private string _account_id;
            private bool? _account_validated;
            private string _email_verification_guid;
            private string _encryption;
            private string _iterations;
            private string _password_hash;
            private string _salt_hash;
                            
            public string Email
            {
                get { return _email; }
                set { this._email = value; }
            }

            public string AccountId
            {
                get { return _account_id;  }
                set { this._account_id = value; }
            }

            public bool? AccountValidated
            {
                get { return _account_validated; }
                set { this._account_validated = value; }
            }

            public string EmailVerificationGuid
            {
                get { return _email_verification_guid; }
                set { this._email_verification_guid = value; }
            }

            public string Encryption
            {
                get { return _encryption; }
                set { this._encryption = value; }
            }

            public string Iterations
            {
                get { return _iterations; }
                set { this._iterations = value; }
            }

            public string PasswordHash
            {
                get { return _password_hash; }
                set { this._password_hash = value; }
            }

            public string SaltHash
            {
                get { return _salt_hash; }
                set { this._salt_hash = value; }
            }

            public LoginInfo()
            {
            }

            public LoginInfo(string email)
            {
                _email = email;
            }

        }

    }

}
