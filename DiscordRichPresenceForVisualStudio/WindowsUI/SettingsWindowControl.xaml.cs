using Microsoft.VisualStudio.Shell;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Globalization;
using Microsoft.VisualStudio.PlatformUI;
using DiscordRichPresenceForVisualStudio.Properties;
using System.Linq;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.StartPanel;
using Microsoft.Win32;
using System.IO;
using Microsoft.Extensions.FileSystemGlobbing;

namespace DiscordRichPresenceForVisualStudio.WindowsUI
{
    /// <summary>
    /// Interaction logic for SettingsWindow.xaml
    /// </summary>
    public partial class SettingsWindowControl : UserControl
    {
        readonly SettingsViewModel viewModel;

        public SettingsWindowControl()
        {
            DataContext = viewModel = new SettingsViewModel();
            InitializeComponent();
        }

        private void UpdateSettings(object sender, RoutedEventArgs e)
        {
            viewModel.Settings.Save();
        }

        private void ResetFilterFile(object sender, RoutedEventArgs e)
        {
            OpenFileDialog fileDialog = new OpenFileDialog()
            {
                Filter = "Path ignore files|*.ignore|All files|*.*",
                CheckFileExists = true,
            };
            if (fileDialog.ShowDialog() == true)
            {
                try
                {
                    using (StreamReader reader = new StreamReader(fileDialog.FileName))
                    {
                        viewModel.Blacklist.Clear();
                        while (true)
                        {
                            string line = reader.ReadLine();
                            if (line == null) break;
                            viewModel.Blacklist.Add(line);
                        }
                        viewModel.Settings.PathsBlacklist = new System.Collections.Specialized.StringCollection();
                        viewModel.Settings.PathsBlacklist.AddRange(viewModel.Blacklist.ToArray());
                        var matcher = new FilePatternMatcher();
                        matcher.AddExcludes(viewModel.Blacklist);
                        DiscordRichPresenceForVisualStudioPackage.FileMatcher = matcher;
                        UpdateSettings(sender, e);
                    }
                }
                catch (Exception ex)
                {
                    ActivityLog.LogError("DiscordRPC", ex.Message);
                    MessageBox.Show("An error while file reading. Please, retry loading file with right content.");
                }
            }
        }
    }
}
