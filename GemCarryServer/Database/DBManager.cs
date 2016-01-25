using System.Collections.Generic;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;

namespace GemCarryServer.Database
{
    public sealed class DBManager
    {
        private static DBManager sInstance = null;
        private AmazonDynamoDBClient client = new AmazonDynamoDBClient();        

        /// <summary>Default Constructor for Singleton</summary>
        private DBManager() { }

        public static DBManager GetInstance()
        {
            if (null == sInstance)
            {
                sInstance = new DBManager();
            }

            return sInstance;
        }

        public int GetItem(string dbKey, string dbKeyValue, string table, out GetItemResponse paramResponse)
        {
            GetItemRequest request = new GetItemRequest();

            request.TableName = table;     // set the table name for DynamoDB
            request.Key = new Dictionary<string, AttributeValue>() { { dbKey, new AttributeValue { S = dbKeyValue } } };

            int response = (int)DBEnum.DBResponseCodes.DEFAULT_VALUE; 

            try
            {                
                paramResponse = this.client.GetItem(request);  // value set to NOT null 

                //Check to see if entry exist
                if (0 == paramResponse.Item.Count) // Entry does not exist
                {                    
                    response = (int)DBEnum.DBResponseCodes.DOES_NOT_EXIST; 
                }

                else // Entry exists
                {
                    response = (int)DBEnum.DBResponseCodes.SUCCESS;
                }
                
            }

            catch 
            {                
                response = (int)DBEnum.DBResponseCodes.DYNAMODB_EXCEPTION;  // set reponse to DB Exception flag
                paramResponse = null;       // set to null on Error
            }

            return response;
        }

        public int GetItemValue(GetItemResponse itemResponse, string columnName, out string outResponse)
        {
            int response = (int)DBEnum.DBResponseCodes.DEFAULT_VALUE;
            AttributeValue av = new AttributeValue();
            itemResponse.Item.TryGetValue(columnName, out av);
            string tempOutResponse;            

            try
            {
                response = (int)DBEnum.DBResponseCodes.SUCCESS;
                tempOutResponse = av.S;
            }

            catch 
            {
                response = (int)DBEnum.DBResponseCodes.DYNAMODB_EXCEPTION;
                tempOutResponse = null;                
            }

            outResponse = tempOutResponse;
            return response;
        }

        public int GetItemValue(GetItemResponse itemResponse, string columnName, out bool? outResponse)
        {
            int response = (int)DBEnum.DBResponseCodes.DEFAULT_VALUE;
            AttributeValue av = new AttributeValue();
            itemResponse.Item.TryGetValue(columnName, out av);
            bool? tempOutResponse;

            try
            {
                response = (int)DBEnum.DBResponseCodes.SUCCESS;  //0 = success return
                tempOutResponse = av.BOOL;
            }

            catch
            {
                response = (int)DBEnum.DBResponseCodes.DYNAMODB_EXCEPTION; 
                tempOutResponse = null;                                   
            }

            outResponse = tempOutResponse;
            return response;
        }

        public int PutItem(PutItemRequest request)
        {            
            int response = (int)DBEnum.DBResponseCodes.DEFAULT_VALUE;            

            try
            {
                this.client.PutItem(request);   // use the DynamoDB PutItem API
                response = (int)DBEnum.DBResponseCodes.SUCCESS;
            }

            catch
            {
                response = (int)DBEnum.DBResponseCodes.DYNAMODB_EXCEPTION;
            }

            return response;
        }

        public int UpdateItem(UpdateItemRequest request)
        {
            int response = (int)DBEnum.DBResponseCodes.DEFAULT_VALUE;
            
            try
            {
                this.client.UpdateItem(request);
                response = (int)DBEnum.DBResponseCodes.SUCCESS;
            }
            catch
            {
                response = (int)DBEnum.DBResponseCodes.DYNAMODB_EXCEPTION;
            }

            return response;
        }

        public int DeleteItem(string primaryKeyName, string primaryKeyValue, string table)
        {
            int response = (int)DBEnum.DBResponseCodes.DEFAULT_VALUE;

            DeleteItemRequest request = new DeleteItemRequest(); // generate new deleterequest
            request.TableName = table;  // set to table name
            request.Key = new Dictionary<string, AttributeValue>() { { primaryKeyName , new AttributeValue { S = primaryKeyValue } } };
            try
            {
                this.client.DeleteItem(request);
                response = (int)DBEnum.DBResponseCodes.SUCCESS;
            }
            catch
            {
                response = (int)DBEnum.DBResponseCodes.DYNAMODB_EXCEPTION;
            }
            
            return response;
        }
    }
}
