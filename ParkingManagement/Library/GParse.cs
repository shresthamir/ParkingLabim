using System;
using System.Collections.Generic;
using System.Linq;


namespace ParkingManagement.Library
{
    public static class GParse
    {
        public static int ToInteger(object objInt)
        {
            int ReturnVal;
            if (objInt == null || objInt == DBNull.Value)
            {
                return 0;
            }
            if (int.TryParse(objInt.ToString(), out ReturnVal))
                return ReturnVal;
            else
                return 0;
        }

        public static float ToFloat(object obj)
        {
            float ReturnVal;
            if (obj == null || obj == DBNull.Value)
            {
                return 0;
            }
            if (float.TryParse(obj.ToString(), out ReturnVal))
                return ReturnVal;
            else
                return 0;
        }

        public static double ToDouble(object objDouble)
        {
            double ReturnVal;
            if (objDouble == null || objDouble == DBNull.Value)
            {
                return 0;
            }
            if (double.TryParse(objDouble.ToString(), out ReturnVal))
                return ReturnVal;
            else
                return 0;
        }

        public static decimal ToDecimal(object objDouble)
        {
            decimal ReturnVal;
            if (objDouble == null || objDouble == DBNull.Value)
            {
                return 0;
            }
            if (decimal.TryParse(objDouble.ToString(), out ReturnVal))
                return ReturnVal;
            else
                return 0;
        }

        public static long ToLong(object objLong)
        {
            long ReturnVal;
            if (objLong == null || objLong == DBNull.Value)
            {
                return 0;
            }
            if (long.TryParse(objLong.ToString(), out ReturnVal))
                return ReturnVal;
            else
                return 0;
        }

        public static short ToShort(object obj)
        {
            short ReturnVal;
            if (obj == null || obj == DBNull.Value)
            {
                return 0;
            }
            if (short.TryParse(obj.ToString(), out ReturnVal))
                return ReturnVal;
            else
                return 0;
        }

        public static char ToChar(object obj)
        {
            char ReturnVal;
            if (obj == null || obj == DBNull.Value)
            {
                return '0';
            }
            if (char.TryParse(obj.ToString(), out ReturnVal))
                return ReturnVal;
            else
                return '0';
        }

        public static bool ToBool(object obj)
        {
            
            if (obj == null || obj == DBNull.Value)
            {
                return false;
            }
            try
            {
                return Convert.ToBoolean(obj);
            }
            catch (Exception)
            {
                return false;
            }
        }

        public static object GetStringOrDBNull(string str)
        {
            return string.IsNullOrEmpty(str) ? DBNull.Value : (object)str;
        }

        public static string getHHMMString(object min,bool HrsDispWithLabel)
        {
            int HH = 0;
            short MM = 0;
            try
            {
                if (min.ToString() != "" && min.ToString() != "0" && min.ToString() != "NaN")
                {
                    min = Math.Round(ToDouble(min), 0);
                    if (HrsDispWithLabel)
                    {
                        Get_HHMM(int.Parse(min.ToString()), ref HH, ref MM);
                        if (HH != 0 && MM != 0)
                            return HH.ToString() + "Hrs " + MM.ToString() + "Min";
                        else if (HH != 0 && MM == 0)
                            return HH.ToString() + "Hrs ";
                        else if (MM != 0)
                            return MM.ToString() + "Min";
                        else
                            return string.Empty;
                    }
                    else
                    {
                        return (float.Parse(min.ToString()) / 60).ToString("#0.00");
                    }
                }
                else
                    return "";
            }
            catch (Exception ex)
            {
                return "";
                // throw ex;
            }
        }

        public static void Get_HHMM(object min,ref int HH,ref short MM)
        {
            HH=ToInteger(min)/60;
            MM = (short)(ToInteger(min) % 60);
        }
    }
}