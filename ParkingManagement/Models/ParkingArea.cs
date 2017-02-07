using ParkingManagement.Library.Helpers;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dapper;
using System.ComponentModel;
using ParkingManagement.Library;
namespace ParkingManagement.Models
{
    public class TParkingArea : BaseModel
    {
        private short _PA_ID;
        private short _Capacity;
        private byte _VehicleType;
        private string _Description;
        private string _PA_Name;
        private int _UID;
        private string _FLOOR;
        private short _MinVacantLot;

        public short PA_ID { get { return _PA_ID; } set { _PA_ID = value; OnPropertyChanged("PA_ID"); } }
        public string PA_Name { get { return _PA_Name; } set { _PA_Name = value; OnPropertyChanged("PA_Name"); } }
        public string Description { get { return _Description; } set { _Description = value; OnPropertyChanged("Description"); } }
        public byte VehicleType { get { return _VehicleType; } set { _VehicleType = value; OnPropertyChanged("VehicleType"); } }
        public short Capacity { get { return _Capacity; } set { _Capacity = value; OnPropertyChanged("Capacity"); } }
        public short MinVacantLot { get { return _MinVacantLot; } set { _MinVacantLot = value; OnPropertyChanged("MinVacantLot"); } }
        public int UID { get { return _UID; } set { _UID = value; OnPropertyChanged("UID"); } }
        public string FLOOR { get { return _FLOOR; } set { _FLOOR = value; OnPropertyChanged("FLOOR"); } }

        public override bool Save(SqlTransaction tran)
        {
            string strSql = "INSERT INTO PARKINGAREA (PA_ID, PA_NAME, [Description], VehicleType, Capacity, UID, [FLOOR], MinVacantLot) VALUES (@PA_ID, @PA_NAME, @Description, @VehicleType, @Capacity, @UID, @FLOOR, @MinVacantLot)";
            return tran.Connection.Execute(strSql, param: this, transaction: tran) == 1;
        }

        public override bool Update(SqlTransaction tran)
        {
            string strSql = "UPDATE PARKINGAREA SET  PA_NAME = @PA_NAME, [Description] = @Description, VehicleType = @VehicleType, Capacity = @Capacity, UID = @UID, [FLOOR] = @FLOOR, MinVacantLot = @MinVacantLot WHERE PA_ID = @PA_ID";
            return tran.Connection.Execute(strSql, param: this, transaction: tran) == 1;
        }

        public override bool Delete(SqlTransaction tran)
        {
            string strSql = "DELETE FROM PARKINGAREA WHERE PA_ID = @PA_ID";
            return tran.Connection.Execute(strSql, param: this, transaction: tran) == 1;
        }
    }

    public class ParkingArea : TParkingArea, IDataErrorInfo
    {
        private VehicleType _VType;
        public VehicleType VType { get { return _VType; } set { _VType = value; OnPropertyChanged("VType"); } }

        private int _Occupency;
        public int Occupency { get { return _Occupency; } set { _Occupency = value; OnPropertyChanged("Occupency"); OnPropertyChanged("Available"); OnPropertyChanged("IsOverMaxLimit"); OnPropertyChanged("IsOverHalf"); } }
        public int Available { get { return Capacity - Occupency; } }

        public bool IsOverMaxLimit { get { return Available <= MinVacantLot; } }
        public bool IsOverHalf { get { return Occupency > Available; } }
        public ParkingArea()
        {
            UID = GlobalClass.User.UID;
            MinVacantLot = GlobalClass.DefaultMinVacantLot;
        }

        public string Error
        {
            get
            {
                if (string.IsNullOrEmpty(PA_Name))
                    return "Parking area name cannot be empty";
                else if (string.IsNullOrEmpty(FLOOR))
                    return "Floor cannot be empty";
                else if (VehicleType <= 0)
                    return "Please select a vehicle type.";
                else if (Capacity < 0)
                    return "Vehicle Capacity cannot be less than Zero. Please enter non negative number.";
                return string.Empty;
            }

        }

        public string this[string columnName]
        {
            get
            {
                string Result = string.Empty;
                switch (columnName)
                {
                    case "PA_Name":
                        if (string.IsNullOrEmpty(PA_Name))
                            Result = "Parking area name cannot be empty";
                        break;
                    case "FLOOR":
                        if (string.IsNullOrEmpty(FLOOR))
                            Result = "FLOOR cannot be empty";
                        break;
                    case "VehicleType":
                        if (VehicleType <= 0)
                            Result = "Please select a vehicle type.";
                        break;
                    case "Capacity":
                        if (Capacity < 0)
                            Result = "Vehicle Capacity cannot be less than Zero. Please enter non negative number.";
                        break;
                }
                return Result;
            }
        }
    }
}
