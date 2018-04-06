using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using EasyHosts.Properties;
using EasyHosts.Utilities;
using EasyHosts.ViewModels;
using MahApps.Metro;

namespace EasyHosts
{
    public partial class Shell
    {

        AppTheme _currentTheme;
        Accent _currentAccent;
        bool _isFirstLoad = true;
        public List<AccentColorMenuData> AccentColors { get; set; }

        public Shell()
        {
            InitializeComponent();
            Loaded += Shell_Loaded;
            Closing += Shell_Closing;
        }

        void Shell_Closing(object sender, CancelEventArgs e)
        {
            try
            {
                if (HostsView == null || HostsView.HostsEditor == null) return;
                Settings.Default.FontSize = HostsView.HostsEditor.FontSize;
                Settings.Default.Save();
            }
            catch
            {
                //Console.WriteLine(exception);
            }
        }


        void Shell_Loaded(object sender, RoutedEventArgs e)
        {
            LoadUserSettings();
            PrepareTheme();
            var appVer = Assembly.GetExecutingAssembly().GetName().Version;
            VersionTextBlock.Text = string.Format("Version {0} (Build {1}.{2})", appVer.Major, appVer.Minor, appVer.Build);

            _isFirstLoad = false;
        }

        private void PrepareTheme()
        {
            AccentColors = ThemeManager.Accents.Select(a => new AccentColorMenuData
            {
                Name = a.Name,
                ColorBrush = a.Resources["AccentColorBrush"] as Brush
            }).ToList();

            var removeAccents = "Indigo,Yellow,Brown";

            AccentColors.RemoveAll(a => removeAccents.Contains(a.Name));

            ThemeColorsList.ItemsSource = AccentColors;
            var selectedTheme = AccentColors.FirstOrDefault(a => String.Compare(a.Name, Settings.Default.ThemeAccent, StringComparison.OrdinalIgnoreCase) == 0)
                                ?? AccentColors.First();

            ThemeColorsList.SelectedItem = selectedTheme;

            DarkTheme.IsChecked = Settings.Default.IsDarkTheme;

            _currentAccent = ThemeManager.Accents.First(a => String.Compare(a.Name, selectedTheme.Name, StringComparison.OrdinalIgnoreCase) == 0);
            _currentTheme = Settings.Default.IsDarkTheme ? ThemeManager.AppThemes.First(a => a.Name.ToLower().Contains("dark"))
                                                         : ThemeManager.AppThemes.First(a => a.Name.ToLower().Contains("light"));

            if (_currentTheme != null && _currentAccent != null)
                ThemeManager.ChangeAppStyle(Application.Current, _currentAccent, _currentTheme);

            ChangeEditorColors();
        }

        private void LoadUserSettings()
        {
            ChkAutoFlush.IsChecked = Settings.Default.AutoFlushDNS;
            HostsView.HostsEditor.FontSize = Settings.Default.FontSize;

            if (HostsContext == null) return;

            HostsContext.AutoFlushDns = Settings.Default.AutoFlushDNS;
            HostsContext.AutoLaunchUrls = Settings.Default.AutoLaunchURL;
            HostsContext.LaunchUrls = Settings.Default.LaunchURLList;
        }

        private void SettingsClicked(object sender, RoutedEventArgs e)
        {
            ThemeSettingsFlyout.IsOpen = !ThemeSettingsFlyout.IsOpen;
        }

        private void ThemeColors_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_isFirstLoad || (sender as ListBox) == null)
                return;

            var selectedColor = (((ListBox)sender).SelectedItem as AccentColorMenuData);
            if (selectedColor == null || _currentTheme == null) return;

            _currentAccent = GetAccentColor(selectedColor);
            ThemeManager.ChangeAppStyle(Application.Current, _currentAccent, _currentTheme);

            //Commit Accent theme to user profile
            Settings.Default.ThemeAccent = selectedColor.Name;
            Settings.Default.Save();
        }

        private Accent GetAccentColor(AccentColorMenuData selectedColor)
        {
            var selectedAccent = ThemeManager.Accents.First(a => String.Compare(a.Name, selectedColor.Name, StringComparison.OrdinalIgnoreCase) == 0);
            return selectedAccent ?? _currentAccent;
        }

        //TODO: Refactor > Use VM property instead of events
        private void DarkTheme_IsCheckedChanged(object sender, EventArgs e)
        {
            if (_isFirstLoad) return;

            _currentTheme = DarkTheme.IsChecked.HasValue && DarkTheme.IsChecked.Value
                                ? ThemeManager.AppThemes.First(a => a.Name.ToLower().Contains("dark"))
                                : ThemeManager.AppThemes.First(a => a.Name.ToLower().Contains("light"));

            if (_currentTheme != null && _currentAccent != null)
            {
                ThemeManager.ChangeAppStyle(Application.Current, _currentAccent, _currentTheme);
            }

            Settings.Default.IsDarkTheme = DarkTheme.IsChecked.GetValueOrDefault();
            Settings.Default.Save();
            ChangeEditorColors();
        }

        private void ChangeEditorColors()
        {
            //TODO: Make configurable (v2 ?)
            HostsView.UpdateSyntaxHighlighter();
        }

        public class AccentColorMenuData
        {
            public string Name { get; set; }
            public Brush ColorBrush { get; set; }
        }

        public HostsViewModel HostsContext
        {
            get
            {
                var shellVm = DataContext as ShellViewModel;
                return (shellVm != null && shellVm.Hosts != null) ? shellVm.Hosts : null;
            }
        }

        private void AutoFlushOnCheckedChanged(object sender, RoutedEventArgs e)
        {
            if (HostsContext == null) return;

            HostsContext.AutoFlushDns = ChkAutoFlush.IsChecked.GetValueOrDefault();
            Settings.Default.AutoFlushDNS = HostsContext.AutoFlushDns;
            Settings.Default.Save();
        }

        void AboutClicked(object sender, RoutedEventArgs e)
        {
            AboutFlyout.IsOpen = !AboutFlyout.IsOpen;
        }

        private void CheckForUpdatesClicked(object sender, RoutedEventArgs e)
        {
            Common.TryStartProcess("https://github.com/arbitrarybytes/easyhosts");
        }

        private void NewFileClicked(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(NewFileTextBox.Text))
            {
                NewFileMsg.Text = "We'll need a valid filename without extension.";
                return;
            }

            try
            {
                var fileName = NewFileTextBox.Text.Trim();
                var newFilePath = Path.Combine(Environment.CurrentDirectory, fileName + ".host");

                if (File.Exists(newFilePath))
                {
                    NewFileMsg.Text = "A hosts file with similar name already exists, try another name.";
                    return;
                }

                File.WriteAllText(newFilePath, NewFileTemplate);

                if (HostsContext != null)
                {
                    HostsContext.HostFiles.Add(new HostsEntryViewModel
                    {
                        Name = Convert.ToString(Path.GetFileNameWithoutExtension(fileName)).ToUpper(),
                        Content = NewFileTemplate,
                        HostEntryPath = newFilePath,
                        IsDirty = false
                    });

                    HostsContext.SetMessage("New hosts file '" + fileName + "' added to your hosts collection.", MessageType.Success);
                }

                NewFileFlyout.IsOpen = false;
            }
            catch (IOException ix)
            {
                NewFileMsg.Text = "Oh Snap! " + ix.Message;
            }
            catch (Exception ex)
            {
                NewFileMsg.Text = "Oh Snap! " + ex.Message;
            }
        }

        private const string NewFileTemplate = "## Here is your newly created hosts file.\n## Right clicking the editor gives you cool options, try it now.\n## And if you select some text and right click, you can quickly create a new file out of the selection.\n\n## Thanks for using Easy Hosts! Please rate this release or suggest features at:\n## http://easyhosts.codeplex.com";
    }
}