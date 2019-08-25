using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using Dapper;
namespace ParkingManagement.Library
{
    class DateConverter
    {
        private string ConnStr;

        public DateConverter(string _ConnStr)
        {
            ConnStr = _ConnStr;
        }

        public string CBSDate(DateTime AD)
        {
            using (SqlConnection conn = new SqlConnection(ConnStr))
            {
                return conn.ExecuteScalar<string>("SELECT MITI FROM DateMiti WHERE AD = @AD", new { AD });
            }
        }

        public DateTime CADDate(string MITI)
        {
            using (SqlConnection conn = new SqlConnection(ConnStr))
            {
                return conn.ExecuteScalar<DateTime>("SELECT AD FROM DateMiti WHERE MITI = @MITI", new { MITI });
            }
        }

        public DateTime GetServerTime()
        {
            using (SqlConnection conn = new SqlConnection(ConnStr))
            {
                return conn.ExecuteScalar<DateTime>("SELECT GETDATE()");
            }
        }

        internal DateTime GetLastDateOfBSMonth(DateTime today)
        {
            string miti = CBSDate(today);
            var test = miti.Substring(miti.Length - 6, 6);
            using (SqlConnection conn = new SqlConnection(ConnStr))
            {
                return conn.ExecuteScalar<DateTime>("SELECT MAX(AD) FROM DateMiti where MITI LIKE '%" + miti.Substring(2, 8) + "'");
                //return conn.ExecuteScalar<DateTime>("SELECT MAX(AD) FROM DateMiti where MITI LIKE '%" + miti.Substring(miti.Length-6, 6) + "'");
            }
        }

        internal DateTime GetFirstDateOfBSMonth(DateTime today)
        {
            string miti = CBSDate(today);
            return CADDate("01" + miti.Substring(2, 8));
            //return CADDate("01" + miti.Substring(miti.Length-6, 6));
        }
    }
}
