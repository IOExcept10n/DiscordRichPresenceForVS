using Microsoft.VisualStudio.PlatformUI;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;

namespace DiscordRichPresenceForVisualStudio
{
    public class SettingsViewModel : ObservableObject
    {

        public Properties.Settings Settings
        {
            get => DiscordRichPresenceForVisualStudioPackage.ExtensionSettings;
            set => DiscordRichPresenceForVisualStudioPackage.ExtensionSettings = value;
        }

        public ObservableCollection<string> Blacklist { get; }

        public SettingsViewModel()
        {
            Blacklist = new ObservableCollection<string>(Settings.PathsBlacklist.Cast<string>());
        }
    }
}