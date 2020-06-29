using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ParkingManagement.Models
{
    public class TrnMode
    {
        public int Id { get; set; }
        public string Name { get; set; }
    }
    public class TrnModes
    {
        public static ObservableCollection<TrnMode> TrnModeList = new ObservableCollection<TrnMode> {
                new TrnMode { Id=0, Name="Credit"},
                new TrnMode { Id=1, Name="Cash"},
            };
        //public TrnModes()
        //{
        //    TrnModeList = new ObservableCollection<TrnMode> {
        //        new TrnMode { Id=0, Name="Credit"},
        //        new TrnMode { Id=1, Name="Cash"},
        //    };
        //}
    }
}
