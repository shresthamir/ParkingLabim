using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections.ObjectModel;
namespace ParkingManagement.Library.Interfaces
{
    public interface ITreeNode
    {
        #region Properties
        bool IsGroup { get; }
        bool IsExpanded { get; set; }
        bool IsSelected { get; set; }
        bool IsMatch { get; set; }
        string ParentID { get; }
        ITreeNode Parent { get; set; }
        ObservableCollection<ITreeNode> Children { get; set; }
        string NodeName { get; }
        string NodeID { get; }
        #endregion

        //#region Methods
        //bool IsCriteriaMatched(string criteria);
        //void ApplyCriteria(string criteria, Stack<ITreeNode> ancestors);
        //void Search(string criteria);
        //void ExpandParent(ITreeNode CurItem);
        //#endregion
    }
}
