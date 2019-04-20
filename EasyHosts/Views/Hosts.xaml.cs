using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Xml;
using EasyHosts.Properties;
using EasyHosts.Utilities;
using EasyHosts.ViewModels;
using ICSharpCode.AvalonEdit.Highlighting;
using ICSharpCode.AvalonEdit.Highlighting.Xshd;
using ICSharpCode.AvalonEdit.Search;
using Microsoft.Win32;

namespace EasyHosts.Views
{
    /// <summary>
    /// Interaction logic for Hosts.xaml
    /// </summary>
    public partial class Hosts
    {
        public Hosts()
        {
            InitializeComponent();
            HostsEditor.TextArea.MouseWheel += HostsEditor_MouseWheel;
        }

        void HostsEditor_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl))
            {
                if (e.Delta > 0)
                    HostsEditor.FontSize = Math.Min(HostsEditor.FontSize + 1, 72);
                else if (e.Delta < 0)
                    HostsEditor.FontSize = Math.Max(HostsEditor.FontSize - 1, 8);
            }
        }

        private void HostsEditor_OnLoaded(object sender, RoutedEventArgs e)
        {
            try
            {
                HostsEditor.TextArea.Caret.CaretBrush = Settings.Default.IsDarkTheme ? Common.GetColorFromHex("White") : Common.GetColorFromHex("Black");
                HostsEditor.Background = Settings.Default.IsDarkTheme ? Common.GetColorFromHex("#222324") : Common.GetColorFromHex("#E4E4E5FF");
                HostsEditor.TextArea.SelectionBrush = Common.GetColorFromHex("#838b8b");
                HostsEditor.TextArea.SelectionCornerRadius = 4;
                HostsEditor.TextArea.Options.AllowScrollBelowDocument = true;
                HostsEditor.TextArea.SelectionBorder = new Pen(Common.GetColorFromHex("#696969"), 2);

                var highlightFile = Settings.Default.IsDarkTheme
                                    ? "EasyHosts.hosts_highlight_dark.xshd"
                                    : "EasyHosts.hosts_highlight.xshd";


                using (var s = Assembly.GetExecutingAssembly()
                                       .GetManifestResourceStream(highlightFile))
                {
                    if (s != null)
                    {
                        using (var reader = new XmlTextReader(s))
                        {
                            HostsEditor.SyntaxHighlighting = HighlightingLoader.Load(reader,
                                HighlightingManager.Instance);
                        }
                    }
                }

                SearchPanel.Install(HostsEditor);

                //HostsEditor.TextArea.DefaultInputHandler.NestedInputHandlers.Add(new SearchInputHandler(HostsEditor.TextArea));

            }
            catch //(Exception ex)
            {
                //Ideally should log to something, but what can we do to recover from this error?
            }
        }

        /// <summary>
        /// Gets the underlying ViewModel associated with this control
        /// </summary>
        public HostsViewModel HostsContext
        {
            get
            {
                return (DataContext as HostsViewModel);
            }
        }

        public void UpdateSyntaxHighlighter()
        {
            HostsEditor_OnLoaded(null, null);
        }

        #region :: Commands ::

        private void PingCommandBindingExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            if (HostsContext == null || HostsEditor.SelectionLength == 0)
                return;

            var pingCmd = HostsEditor.SelectedText.Trim();

            HostsContext.ClearMessage();

            HostsContext.IsBusy = true;
            try
            {
                if (Settings.Default.AutoClosePing)
                    Common.TryStartProcess("cmd", "ping " + pingCmd);
                else
                    Common.TryStartProcess("cmd", "/K ping " + pingCmd);
            }
            catch (Exception ex)
            {
                if (HostsContext != null)
                    HostsContext.SetMessage("Yikes! We couldn't ping '" + pingCmd + "', try re-selecting and pinging again (fingers crossed).", MessageType.Failure, ex.Message);
            }
            finally
            {
                if (HostsContext != null)
                    HostsContext.IsBusy = false;
            }
        }


        private void SaveAsCommandBindingExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            if (HostsEditor.SelectionLength == 0 || HostsContext == null)
                return;

            var content = HostsEditor.SelectedText;

            //if (HostsContext != null)
            HostsContext.ClearMessage();

            if (string.IsNullOrWhiteSpace(content))
            {
                HostsContext.SetMessage("Enough is enough, we are not going to dump emptiness in to the beloved hard drive.", MessageType.Warning, "For heaven's sake, select something other than empty spaces next time.", string.Empty);
                return;
            }

            HostsContext.IsBusy = true;
            try
            {
                //ShowInputDialog(null, null);

                //return;

                //This is where you prompt for file name alone and create it in the base directory
                var saveDlg = new SaveFileDialog
                {
                    OverwritePrompt = true,
                    InitialDirectory = AppDomain.CurrentDomain.BaseDirectory,
                    Filter = "Hosts File (*.host)|*.host"
                };

                var saved = saveDlg.ShowDialog();
                if (saved.HasValue && saved.Value)
                {
                    File.WriteAllText(saveDlg.FileName, content);

                    if (HostsContext != null)
                    {
                        HostsContext.HostFiles.Add(new HostsEntryViewModel
                        {
                            Name = Convert.ToString(Path.GetFileNameWithoutExtension(saveDlg.FileName)).ToUpper(),
                            Content = content,
                            HostEntryPath = saveDlg.FileName,
                            IsDirty = false,
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                if (HostsContext != null)
                    HostsContext.SetMessage("OMG! We couldn't save your selection to the host file at your specified location.", MessageType.Failure, ex.Message);
            }
            finally
            {
                if (HostsContext != null)
                    HostsContext.IsBusy = false;
            }
        }

        private void SaveAsCommandBindingCanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = (HostsEditor != null && HostsEditor.SelectionLength > 0);
        }


        private void OpenCommandBindingExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            if (HostsContext == null || HostsContext.SelectedHostFile == null) return;

            Cursor = Cursors.Wait;
            try
            {
                var dir = Path.GetDirectoryName(HostsContext.SelectedHostFile.HostEntryPath);
                Process.Start("explorer", dir);
            }
            catch
            {
                // ignored
            } // (Exception exception)
            finally
            {
                Cursor = Cursors.Arrow;
            }
        }

        private void SaveCommandBindingExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            if (HostsContext == null || HostsContext.HostFiles == null || HostsContext.SelectedHostFile == null)
                return;

            HostsContext.IsBusy = true;
            try
            {
                HostsContext.SelectedHostFile.Content = HostsEditor.Text;
                File.WriteAllText(HostsContext.SelectedHostFile.HostEntryPath, HostsContext.SelectedHostFile.Content);
                HostsContext.SelectedHostFile.IsDirty = false;

                HostsContext.RaisePropertyChanged("CanSaveChanges");
                if (HostsContext.SelectedHostFile.IsSystem) //If System Hosts file was saved flush DNS if AutoFlush is enabled
                {
                    string flushError;
                    if (HostsContext.AutoFlushDns)
                        Common.TryFlushDns(out flushError);

                    if (HostsContext.AutoLaunchUrls)
                        HostsContext.StartPredefinedUrls();
                }
            }
            catch (Exception ex)
            {
                HostsContext.SetMessage("Yikes! We are unable to save the changes to the edits you made :(", MessageType.Failure, ex.Message);
            }
            finally
            {
                HostsContext.IsBusy = false;
            }
        }

        private void SaveCommandBindingCanExecute(object sender, CanExecuteRoutedEventArgs e)
        {

            if (HostsContext != null && HostsContext.SelectedHostFile != null)
            {
                e.CanExecute = HostsContext.CanSaveChanges; //define if command can be executed
                return;
            }

            e.CanExecute = false;
        }

        private void RefreshCommandBindingExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            if (HostsContext.SelectedHostFile == null)
                // || (SelectedHostFile.IsSystem && !Utilities.Hosts.HostFileExists))
                return;

            HostsContext.ClearMessage();
            HostsContext.IsBusy = true;
            try
            {
                HostsContext.IgnoreFileContentChange = true;
                HostsContext.SelectedHostFile.Content = File.ReadAllText(HostsContext.SelectedHostFile.HostEntryPath);

                HostsContext.Document.Text = HostsContext.SelectedHostFile.Content;
                //HostsContext.HostFiles.First().Content;
                HostsContext.SelectedHostFile.IsDirty = false;
            }
            catch (FileNotFoundException fEx)
            {
                HostsContext.SetMessage(
                    "404! The file went missing (you renamed or moved it?), can't load what doesn't exist buddy :(",
                    MessageType.Failure, fEx.Message);
            }
            catch (Exception ex)
            {
                HostsContext.SetMessage(
                    "Nopes! Can't refresh the contents of the selected file :( See if the message below helps.",
                    MessageType.Failure, ex.Message);
            }
            finally
            {
                HostsContext.IsBusy = false;
            }
        }

        private void RefreshCommandBindingCanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = HostsContext != null && HostsContext.SelectedHostFile != null
                         && !(HostsContext.SelectedHostFile.IsSystem && !Utilities.Hosts.HostFileExists);
        }

        private void AddNewHostsCommandBindingExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            var win = Application.Current.MainWindow as Shell;
            if (win == null)
            {
                HostsContext.SetMessage("Oh Snap! Unable to perform the requested operation :( ", MessageType.Failure);
                return;
            }

            win.NewFileTextBox.Text = string.Empty;
            win.NewFileFlyout.IsOpen = !win.NewFileFlyout.IsOpen;
            win.NewFileTextBox.Focus();
        }

        #endregion
    }
}