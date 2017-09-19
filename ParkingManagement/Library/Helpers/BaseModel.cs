using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ParkingManagement.Library.Helpers
{
    public class BaseModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        public void OnPropertyChanged(string propname)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propname));
            }
        }

        public virtual bool Save(SqlTransaction tran)
        {
            return false;
        }

        public virtual bool Update(SqlTransaction tran)
        {
            return false;
        }

        public virtual bool Delete(SqlTransaction tran)
        {
            return false;
        }
    }
}
