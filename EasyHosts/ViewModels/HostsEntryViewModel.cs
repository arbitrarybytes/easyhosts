namespace EasyHosts.ViewModels
{
    public class HostsEntryViewModel : ViewModelBase
    {
        public HostsEntryViewModel()
        {
            CanDirty = true;
            IsDirty = false;
        }

        //public string HostEntryName
        //{
        //    get { return _hostEntryName; }
        //    set
        //    {
        //        _hostEntryName = value;
        //        OnPropertyChanged("HostEntryName");
        //    }
        //}
        public string HostEntryPath
        {
            get { return _hostEntryPath; }
            set
            {
                _hostEntryPath = value;
                OnPropertyChanged("HostEntryPath");
            }
        }
        public string Content
        {
            get { return _content; }
            set
            {
                _content = value;
                OnPropertyChanged("Content");
            }
        }
        public bool IsSystem { get; set; }

        //private string _hostEntryName;
        private string _hostEntryPath;
        private string _content;

    }
}
