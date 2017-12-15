using ParkingManagement.Library.Helpers;
using ParkingManagement.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ParkingManagement.ViewModel
{
    class MembershipSchemeViewModel : BaseViewModel 
    {
        private MembershipScheme _Scheme;

        public MembershipScheme Scheme { get { return _Scheme; } set { _Scheme = value; OnPropertyChanged("Scheme"); } }


        public MembershipSchemeViewModel()
        {
            Scheme = new MembershipScheme();
            Scheme.ValidHoursList = new ObservableCollection<ValidHour>();
        }
    }
}
