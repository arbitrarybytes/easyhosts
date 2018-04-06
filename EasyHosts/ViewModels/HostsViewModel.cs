using ICSharpCode.AvalonEdit.Document;
using Microsoft.Practices.Prism.Commands;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Security;
using System.Threading.Tasks;
using System.Windows.Input;

namespace EasyHosts.ViewModels
{
    public class HostsViewModel : ViewModelBase
    {

        public HostsViewModel()
        {
            Name = "HOSTS";
            CanDirty = true;
            _isHostsEnabled = Utilities.Hosts.HostFileExists;
            _disabledHostFile = Path.Combine(Utilities.Hosts.HostFileDirectory, "hosts_disabled");
            
            _document = new TextDocument();
            Document.TextChanged += Document_TextChanged;
            
            LoadHostsFiles();
            SelectedHostFile = HostFiles.FirstOrDefault();
            SetMessage(HostFiles.Count + " hosts file(s) were discovered and added to the list.", MessageType.Success);
        }

        #region :: Methods & Events ::

        void Document_TextChanged(object sender, EventArgs e)
        {
            if (!SelectedHostFile.IsDirty && !IgnoreFileContentChange)
            {
                SelectedHostFile.IsDirty = true;
                OnPropertyChanged("CanSaveChanges");
            }
            IgnoreFileContentChange = false;
        }

        public void LoadHostsFiles()
        {
            ClearMessage();
            //Add System Hosts file
            HostFiles = new ObservableCollection<HostsEntryViewModel>
            {
                new HostsEntryViewModel
                {
                    Name = "SYSTEM HOSTS",
                    IsSystem = true,
                    CanDirty = true,
                    IsDirty = false,
                    HostEntryPath = Utilities.Hosts.HostFilePath,
                    Content = GetHostFileContent()
                }
            };

            try
            {
                var currDir = Environment.CurrentDirectory;
                var hostFiles = Directory.GetFiles(currDir, "*.host", SearchOption.AllDirectories);

                if (hostFiles.Length == 0)
                {
                    SetMessage(HostFiles.Count + " hosts file(s) were discovered and added to the list.", MessageType.Success);
                    return;
                }

                foreach (var file in hostFiles)
                {
                    HostFiles.Add(new HostsEntryViewModel
                    {
                        CanDirty = true,
                        IsDirty = false,
                        HostEntryPath = file,
                        Name = Convert.ToString(Path.GetFileNameWithoutExtension(file)).ToUpper(),
                        Content = TryGetFileContent(file)
                    });
                }

                SetMessage(HostFiles.Count + " hosts file(s) were discovered and added to the list.", MessageType.Success);
            }
            catch (Exception ex)
            {
                SetMessage("A problem was encountered while adding hosts file to the list.", MessageType.Failure, ex.Message);
            }

        }

        private void DisableHostFile()
        {
            ClearMessage();
            IsBusy = true;
            try
            {
                //Check if hosts exist
                if (!Utilities.Hosts.HostFileExists)
                {
                    SetMessage("Good to go, your system didn't have any active hosts file.", MessageType.Information);
                    return;
                }

                //string disabledHostFile = System.IO.Path.Combine(_hostFileDir, "hosts_disabled");

                if (File.Exists(_disabledHostFile))
                    File.Delete(_disabledHostFile);

                //Rename hosts to hosts_disabled
                File.Move(Utilities.Hosts.HostFilePath, _disabledHostFile);

                SetMessage("Done! System hosts file has been disabled.", MessageType.Success);

                //If currently selected host file is system host file, clear its contents
                if (SelectedHostFile.IsSystem)
                {
                    SelectedHostFile.Content = string.Empty;
                    Document.Text = SelectedHostFile.Content;
                    SelectedHostFile.IsDirty = false;
                }

                string flushError;
                if (AutoFlushDns)
                    Utilities.Common.TryFlushDns(out flushError);

                if (AutoLaunchUrls)
                    StartPredefinedUrls();
            }
            catch (UnauthorizedAccessException uaEx)
            {
                SetMessage(InvalidAccessMessage, MessageType.Failure, uaEx.Message);
                _isHostsEnabled = true;
            }
            catch (SecurityException secEx)
            {
                _isHostsEnabled = true;
                SetMessage("System hosts file could not be disabled. :( Looks like your user account is facing some security permissions issue.", MessageType.Failure, secEx.Message);
            }
            catch (IOException ioEx)
            {
                _isHostsEnabled = true;
                SetMessage("System hosts file could not be disabled. :( Looks like an internal disk related problem occurred.", MessageType.Failure, ioEx.Message);
            }
            catch (Exception ex)
            {
                _isHostsEnabled = true;
                SetMessage("An unexpected issue occurred while disabling system hosts file. Try restarting the application and retry.", MessageType.Failure, ex.Message);
            }
            finally
            {
                IsBusy = false;
            }
        }

        public void StartPredefinedUrls()
        {
            if (!AutoLaunchUrls || string.IsNullOrWhiteSpace(LaunchUrls))
                return;

            var urls = LaunchUrls.Split(';');
            if (urls.Length == 0)
                return;

            IsBusy = true;

            foreach (var url in urls.Where(u => !string.IsNullOrWhiteSpace(u)))
            {
                Task.Factory.StartNew(() => Utilities.Common.TryStartProcess(url));
            }

            IsBusy = false;
        }

        private string GetHostFileContent()
        {
            ClearMessage();

            if (!Utilities.Hosts.HostFileExists)
            {
                SetMessage("There is no active hosts file in your system.", MessageType.Warning);
                return string.Empty;
            }

            var content = string.Empty;

            IsBusy = true;
            try
            {
                content = File.ReadAllText(Utilities.Hosts.HostFilePath);
            }
            catch (UnauthorizedAccessException uaEx)
            {
                SetMessage(InvalidAccessMessage, MessageType.Failure, uaEx.Message);
            }
            catch (Exception ex)
            {
                SetMessage("An unexpected issue occurred and we were unable to load the hosts file content.",
                    MessageType.Failure, ex.Message);
            }
            finally
            {
                IsBusy = false;
            }

            return content;
        }

        private string TryGetFileContent(string filePath)
        {
            ClearMessage();

            if (!File.Exists(filePath))
            {
                SetMessage("The selected file no longer exists (may have been moved or renamed).", MessageType.Failure);
                return string.Empty;
            }

            var content = string.Empty;
            try
            {
                content = File.ReadAllText(filePath);
            }
            catch (Exception e)
            {
                SetMessage("The contents of the selected file could not be loaded.", MessageType.Failure, e.Message);
            }

            return content;
        }

        public void SetHostFileContent(HostsEntryViewModel hostEntry)
        {
            ClearMessage();
            IsBusy = true;
            try
            {
                //Create or Update host file content
                File.WriteAllText(Utilities.Hosts.HostFilePath, hostEntry.Content);

                //if (SelectedHostFile != HostFiles.First())
                HostFiles.First().Content = GetHostFileContent();

                if (SelectedHostFile.IsSystem)
                {
                    IgnoreFileContentChange = true;
                    Document.Text = HostFiles.First().Content;
                    IgnoreFileContentChange = false;
                }

                SetMessage("Done! Hosts file content was updated.", MessageType.Success);

                string flushError;
                if (AutoFlushDns)
                    Utilities.Common.TryFlushDns(out flushError);

                if (AutoLaunchUrls)
                    StartPredefinedUrls();

            }
            catch (UnauthorizedAccessException uaEx)
            {
                SetMessage(InvalidAccessMessage, MessageType.Failure, uaEx.Message);
            }
            catch (Exception ex)
            {
                SetMessage("Some unexpected issue occurred while updating the hosts file.", MessageType.Failure, ex.Message);
            }
            finally
            {
                IsBusy = false;
            }
        }

        private void EnableHostFile()
        {
            ClearMessage();
            IsBusy = true;
            try
            {
                //Check if hosts exist
                if (Utilities.Hosts.HostFileExists)
                {
                    //if does skip and return
                    SetMessage("Good to go! Hosts file already exists.", MessageType.Information);
                    return;
                }

                //If there are no active hosts file and there is a previously disabled hosts file, rename it to hosts
                //Rename hosts_disabled to hosts
                if (File.Exists(_disabledHostFile))
                {
                    File.Move(_disabledHostFile, Utilities.Hosts.HostFilePath);
                }
                else //Create a new file with hosts name and dump the content as appears in the application
                {
                    File.WriteAllText(Utilities.Hosts.HostFilePath, HostFiles.First().Content);
                }

                //Refresh host file content
                HostFiles.First().Content = GetHostFileContent();
                if (SelectedHostFile.IsSystem)
                {
                    Document.Text = SelectedHostFile.Content;
                    SelectedHostFile.IsDirty = false;
                }

                SetMessage("Done! Your system's hosts file has been enabled.", MessageType.Success);

                string flushError;
                if (AutoFlushDns)
                    Utilities.Common.TryFlushDns(out flushError);

                if (AutoLaunchUrls)
                    StartPredefinedUrls();

            }
            catch (UnauthorizedAccessException uaEx)
            {
                SetMessage(InvalidAccessMessage, MessageType.Failure, uaEx.Message);
                _isHostsEnabled = false;
            }
            catch (SecurityException secEx)
            {
                _isHostsEnabled = false;
                SetMessage("Can't enable the hosts. :( Looks like your user account is facing some security permissions issue.", MessageType.Failure, secEx.Message);
            }
            catch (IOException ioEx)
            {
                _isHostsEnabled = false;
                SetMessage("Can't enable the hosts. :( Looks like an internal disk related problem occurred.", MessageType.Failure, ioEx.Message);
            }
            catch (Exception ex)
            {
                _isHostsEnabled = false;
                SetMessage("An unexpected issue occurred while enabling hosts file.", MessageType.Failure, ex.Message);
            }
            finally
            {
                IsBusy = false;
            }
        }

        #endregion

        #region :: Fields ::

        //private bool _isAdminPrivelegesEnabled;
        //private HostsUtility _hostUtil;
        private readonly string _disabledHostFile;
        private HostsEntryViewModel _selectedHostFile;
        private TextDocument _document;
        private bool _isHostsEnabled;

        #endregion

        private ICommand _setHostsCommand;
        public ICommand SetHostsCommand
        {
            get
            {
                return _setHostsCommand = _setHostsCommand ?? new DelegateCommand(SetHostsCommandExecute);
            }
        }

        private void SetHostsCommandExecute()
        {
            if (SelectedHostFile == null)
                return;

            ClearMessage();
            IsBusy = true;
            try
            {
                SetHostFileContent(SelectedHostFile);
            }
            catch (UnauthorizedAccessException uaEx)
            {
                SetMessage(InvalidAccessMessage, MessageType.Failure, uaEx.Message);
            }
            catch (Exception ex)
            {
                SetMessage("Unexpected issues occurred while updating system hosts file, please retry.", MessageType.Failure, ex.Message);
            }
            finally
            {
                IsBusy = false;
            }
        }

        public void RaisePropertyChanged(string propName)
        {
            OnPropertyChanged(propName);
        }

        #region :: Properties ::

        public bool AutoFlushDns { get; set; }
        public bool AutoLaunchUrls { get; set; }
        public string LaunchUrls { get; set; }

        public bool IsHostsEnabled
        {
            get
            {
                return _isHostsEnabled;
            }
            set
            {
                _isHostsEnabled = value;
                if (!_isHostsEnabled)
                    DisableHostFile();
                else
                    EnableHostFile();

                OnPropertyChanged("IsHostsEnabled");
                OnPropertyChanged("CanSaveChanges");
            }
        }

        public bool CanSaveChanges
        {
            get
            {
                //If hosts file is disabled dont allow to save System Hosts file
                return IsHostsEnabled ? SelectedHostFile.IsDirty
                    : (SelectedHostFile != HostFiles.First()) && SelectedHostFile.IsDirty;
            }
        }

        //public bool IsAdminPrivelegesEnabled
        //{
        //    get { return _isAdminPrivelegesEnabled; }
        //    set
        //    {
        //        _isAdminPrivelegesEnabled = value;
        //        OnPropertyChanged("IsAdminPrivelegesEnabled");
        //    }
        //}

        public ObservableCollection<HostsEntryViewModel> HostFiles { get; set; }

        public HostsEntryViewModel SelectedHostFile
        {
            get { return _selectedHostFile; }
            set
            {
                ClearMessage();
                //Commit existing changes
                if (Document != null && _selectedHostFile != null)
                    SelectedHostFile.Content = Document.Text;

                //Load newly selected file content and suppress Dirty flag
                _selectedHostFile = value;
                OnPropertyChanged("SelectedHostFile");
                OnPropertyChanged("CanSaveChanges");

                if (Document == null) return;

                IgnoreFileContentChange = true;
                Document.Text = _selectedHostFile.Content;
                IgnoreFileContentChange = false;
            }
        }

        public TextDocument Document
        {
            get
            {
                return _document;
            }
            set
            {
                _document = value;
                SelectedHostFile.Content = _document.Text;
                OnPropertyChanged("Document");
            }
        }

        public bool IgnoreFileContentChange { get; set; }

        #endregion

        private const string InvalidAccessMessage = "Couldn't access the hosts file. :( Try to close and re-run the application with Administrator privileges.";

    }
}
