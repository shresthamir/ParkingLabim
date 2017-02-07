using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Web;
using Dapper;
namespace LabimParking
{
    public static class GlobalClass
    {
        public static string DataConnectionString;
        static GlobalClass()
        {
            DataConnectionString = System.Configuration.ConfigurationManager.ConnectionStrings["DBSetting"].ConnectionString;
        }

        public static int StartSession(string Terminal, int UID)
        {
            int Session;
            try
            {
                using (SqlConnection conn = new SqlConnection(DataConnectionString))
                {
                    Session = (int)conn.ExecuteScalar(string.Format
                         (
                             @"INSERT INTO [MOBILE_SESSION] ( SESSION_ID, [START_DATE], DEVICE_ID, [UID], SESSION_CREATE_MODE)
                                OUTPUT INSERTED.SESSION_ID
                                VALUES ((SELECT ISNULL(MAX(SESSION_ID),0) + 1 FROM MOBILE_SESSION), GETDATE(), '{0}', {1}, 'LOGIN')", Terminal, UID
                         ));
                    return Session;
                }
            }
            catch (Exception ex)
            {
                return 0;
            }
        }


        public static Exception GetRootException(Exception ex)
        {
            try
            {

                if (ex.InnerException != null)
                    ex = GetRootException(ex);
                return ex;
            }
            catch (Exception exs)
            {
                return exs;
            }
        }

        public static string GetEncryptedPWD(string pwd, ref string Salt)
        {
            StringBuilder sBuilder;
            if (string.IsNullOrEmpty(Salt))
            {
                System.Security.Cryptography.RNGCryptoServiceProvider rng = new System.Security.Cryptography.RNGCryptoServiceProvider();
                byte[] saltByte = new byte[8];
                rng.GetNonZeroBytes(saltByte);

                sBuilder = new StringBuilder();

                // Loop through each byte of the hashed data 
                // and format each one as a hexadecimal string.
                for (int i = 0; i < saltByte.Length; i++)
                {
                    sBuilder.Append(saltByte[i].ToString("x2"));
                }

                Salt = sBuilder.ToString();
            }

            System.Security.Cryptography.SHA256CryptoServiceProvider sha = new System.Security.Cryptography.SHA256CryptoServiceProvider();
            //System.Security.Cryptography.MD5CryptoServiceProvider x = new System.Security.Cryptography.MD5CryptoServiceProvider();
            byte[] data = System.Text.Encoding.ASCII.GetBytes(pwd + Salt);
            data = sha.ComputeHash(data);

            sBuilder = new StringBuilder();

            // Loop through each byte of the hashed data 
            // and format each one as a hexadecimal string.
            for (int i = 0; i < data.Length; i++)
            {
                sBuilder.Append(data[i].ToString("x2"));
            }
            return sBuilder.ToString();
        }
    }
}