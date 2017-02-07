using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
namespace ParkingManagement.Library.Helpers
{
    public class BaseViewModel : INotifyPropertyChanged
    {

        public event PropertyChangedEventHandler PropertyChanged;
        public ButtonAction _action;
        private bool _EntryPanelEnabled, _KeyFieldEnabled;
        private bool _NewEnabled;
        private bool _EditEnabled;
        private bool _SaveEnabled;
        private bool _DeleteEnabled;
        private string _SearchCriteria, _FailureText;
        private string _TMODE;
        private short _FocusedElement;
        protected string MessageBoxCaption;
        protected string SaveConfirmText = "You are about to save new {0}. Do you want to proceed?";
        protected string UpdateConfirmText = "You are about to update selected {0}. Do you want to proceed?";
        protected string DeleteConfirmText = "You are about to delete selected {0}. Do you want to proceed?";
        public short FocusedElement { get { return _FocusedElement; } set { _FocusedElement = value; OnPropertyChanged("FocusedElement");  } }
        public string TMODE { get { return _TMODE; } set { _TMODE = value; OnPropertyChanged("TMODE"); } }
        public string SearchCriteria
        {
            get { return _SearchCriteria; }
            set
            {
                _SearchCriteria = value;
                OnPropertyChanged("SearchCriteria");
            }
        }

        public string FailureText
        {
            get { return _FailureText; }
            set { _FailureText = value; OnPropertyChanged("FailureText"); }
        }

        public enum ButtonAction
        {
            New = 1, Edit = 2, Init = 0, Selected = 3, RePrint = 4, InvoiceLoaded = 5
        }

        public void OnPropertyChanged(string propname)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propname));
            }
        }

        public RelayCommand NewCommand { get; set; }
        public RelayCommand EditCommand { get; set; }
        public RelayCommand SaveCommand { get; set; }
        public RelayCommand DeleteCommand { get; set; }
        public RelayCommand UndoCommand { get; set; }
        public RelayCommand LoadData { get; set; }
        public RelayCommand PrintPreviewCommand { get; set; }
        public RelayCommand PrintCommand { get; set; }
        public RelayCommand ExportCommand { get; set; }

        //public RelayCommand NumericField_GotFocus { get; set; }

        public bool EntryPanelEnabled { get { return _EntryPanelEnabled; } set { _EntryPanelEnabled = value; OnPropertyChanged("EntryPanelEnabled"); } }
        public bool KeyFieldEnabled { get { return _KeyFieldEnabled; } set { _KeyFieldEnabled = value; OnPropertyChanged("KeyFieldEnabled"); } }
        public bool NewEnabled { get { return _NewEnabled; } set { _NewEnabled = value; OnPropertyChanged("NewEnabled"); } }
        public bool EditEnabled { get { return _EditEnabled; } set { _EditEnabled = value; OnPropertyChanged("EditEnabled"); } }
        public bool SaveEnabled { get { return _SaveEnabled; } set { _SaveEnabled = value; OnPropertyChanged("SaveEnabled"); } }
        public bool DeleteEnabled { get { return _DeleteEnabled; } set { _DeleteEnabled = value; OnPropertyChanged("DeleteEnabled"); } }


        public BaseViewModel()
        {
            //NumericField_GotFocus = new RelayCommand(ChangeInputLanguageToDefault);
        }

        protected void SetAction(ButtonAction action)
        {
            SaveEnabled = false;
            EditEnabled = false;
            NewEnabled = false;
            EntryPanelEnabled = false;            
            DeleteEnabled = false;
            switch (action)
            {
                case ButtonAction.New:
                    SaveEnabled = true;
                    EntryPanelEnabled = true;
                    TMODE = "NEW";
                    break;
                case ButtonAction.Edit:
                    SaveEnabled = true;
                    EntryPanelEnabled = true;
                    TMODE = "EDIT";
                    break;
                case ButtonAction.Init:                    
                    NewEnabled = true;
                    TMODE = "INIT";
                    break;
                case ButtonAction.Selected:
                    DeleteEnabled = true;
                    NewEnabled = true;
                    EditEnabled = true;
                    TMODE = "SELECTED";
                    break;
            }
            _action = action;
        }
    }



}
