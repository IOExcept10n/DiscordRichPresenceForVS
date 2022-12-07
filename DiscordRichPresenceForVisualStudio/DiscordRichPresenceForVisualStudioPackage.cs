using DiscordRichPresenceForVisualStudio.Properties;
using DiscordRPC;
using EnvDTE;
using Microsoft.Extensions.FileSystemGlobbing;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using System;
using System.Configuration;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Collections.Generic;
using System.Windows.Documents;
using Newtonsoft.Json;
using Task = System.Threading.Tasks.Task;
using Microsoft.VisualStudio.Settings;
using DiscordRichPresenceForVisualStudio.WindowsUI;
using System.Reflection;
using MSXML;

namespace DiscordRichPresenceForVisualStudio
{
    /// <summary>
    /// This is the class that implements the package exposed by this assembly.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The minimum requirement for a class to be considered a valid package for Visual Studio
    /// is to implement the IVsPackage interface and register itself with the shell.
    /// This package uses the helper classes defined inside the Managed Package Framework (MPF)
    /// to do it: it derives from the Package class that provides the implementation of the
    /// IVsPackage interface and uses the registration attributes defined in the framework to
    /// register itself and its components with the shell. These attributes tell the pkgdef creation
    /// utility what data to put into .pkgdef file.
    /// </para>
    /// <para>
    /// To get loaded into VS, the package must be referred by &lt;Asset Type="Microsoft.VisualStudio.VsPackage" ...&gt; in .vsixmanifest file.
    /// </para>
    /// </remarks>
    [PackageRegistration(UseManagedResourcesOnly = true, AllowsBackgroundLoading = true)]
    [ProvideAutoLoad(UIContextGuids80.NoSolution, PackageAutoLoadFlags.BackgroundLoad)]
    [ProvideAutoLoad(UIContextGuids80.SolutionExists, PackageAutoLoadFlags.BackgroundLoad)]
    [InstalledProductRegistration("#110", "#112", "1.0", IconResourceID = 400, LanguageIndependentName = "Discord Rich Presence")]
    [ProvideMenuResource("Menus.ctmenu", 1)]
    [ProvideBindingPath]
    [Guid(PackageGuidString)]
    [ProvideToolWindow(typeof(SettingsWindow))]
    public sealed class DiscordRichPresenceForVisualStudioPackage : AsyncPackage
    {
        /// <summary>
        /// DiscordRichPresenceForVisualStudioPackage GUID string.
        /// </summary>
        public const string PackageGuidString = "1cfeb6df-f540-4313-933e-6436c042e5ef";

        public const string DiscordAppID = "1047822641878290482";

        #region Package Members
        public static Settings ExtensionSettings { get; set; }
        public static Dictionary<string, LanguageInfo> ProgrammingLanguages { get; set; }

        public static FilePatternMatcher FileMatcher { get; set; }

        private string versionName;
        private string versionImageKey;

        private DiscordRpcClient client;
        private Timestamps currentTimestamps;
        private Assets assets;
        private DateTime startupTime;
        private bool closing = false;

        private static DTE ide;

        #endregion

        /// <summary>
        /// Initialization of the package; this method is called right after the package is sited, so this is the place
        /// where you can put all the initialization code that rely on services provided by VisualStudio.
        /// </summary>
        /// <param name="cancellationToken">A cancellation token to monitor for initialization cancellation, which can occur when VS is shutting down.</param>
        /// <param name="progress">A provider for progress updates.</param>
        /// <returns>A task representing the async work of package initialization, or an already completed task if there is none. Do not return null from this method.</returns>
        protected override async Task InitializeAsync(CancellationToken cancellationToken, IProgress<ServiceProgressData> progress)
        {
            try
            {
                // When initialized asynchronously, the current thread may be a background thread at this point.
                // Do any initialization that requires the UI thread after switching to the UI thread.
                await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

                progress.Report(new ServiceProgressData("Initialization is in progress", "Loading preferences", 1, 3));

                ExtensionSettings = (Settings)SettingsBase.Synchronized(Settings.Default);
                ExtensionSettings.Upgrade();
                if (ExtensionSettings.PathsBlacklist == null)
                {
                    ExtensionSettings.PathsBlacklist = new System.Collections.Specialized.StringCollection();
                    ExtensionSettings.Save();
                }

                Translation.Culture = ExtensionSettings.UseEnglishPresence ? CultureInfo.GetCultureInfo("en-US") : CultureInfo.CurrentUICulture;

                ide = GetGlobalService(typeof(SDTE)) as DTE;
                ide.Events.WindowEvents.WindowActivated += OnWindowActivated;
                ide.Events.SolutionEvents.BeforeClosing += OnSolutionClosing;
                ide.Events.WindowEvents.WindowClosing += OnWindowClosing;

                string ideVersion = ide.Version.Split('.')[0];
                versionName = $"Visual Studio {ConvertVersionToTitle(ideVersion)}";
                versionImageKey = $"dev{ConvertVersionToTitle(ideVersion)}";

                progress.Report(new ServiceProgressData("Initialization is in progress", "Loading excluded paths", 1, 3));

                FileMatcher = new FilePatternMatcher();
                var excludedPaths = ExtensionSettings.PathsBlacklist.Cast<string>();
                FileMatcher.AddExcludes(excludedPaths);

                progress.Report(new ServiceProgressData("Initialization is in progress", "Loading languages settings", 1, 3));

                if (File.Exists(".rpcconfig"))
                {
                    using (StreamReader reader = new StreamReader(".rpcconfig"))
                    {
                        ProgrammingLanguages = JsonConvert.DeserializeObject<Dictionary<string, LanguageInfo>>(await reader.ReadToEndAsync());
                    }
                }
                else
                {
                    ProgrammingLanguages = JsonConvert.DeserializeObject<Dictionary<string, LanguageInfo>>(Resources.Languages);
                }

                client = new DiscordRpcClient(DiscordAppID);
                assets = new Assets();
                await SettingsWindowCommand.InitializeAsync(this);
                startupTime = DateTime.UtcNow;

                ResetPresence(null);

                if (ExtensionSettings.LoadOnStartup)
                    UpdatePresence(ide.ActiveDocument);

                await base.InitializeAsync(cancellationToken, progress);
            }
            catch (OperationCanceledException ex)
            {
                ActivityLog.LogError("DiscordRPC", ex.Message);
            }
        }

        private string ConvertVersionToTitle(string majorVersion)
        {
            switch (majorVersion)
            {
                case "15":
                    return "2017";
                case "16":
                    return "2019";
                case "17":
                    return "2022";
                default:
                    return string.Empty;
            }
        }

        private void OnSolutionClosing()
        {
            closing = true;
            UpdatePresence(null);
        }

        private void OnWindowClosing(Window window)
        {
            closing = true;
            UpdatePresence(null);
        }

        private void OnWindowActivated(Window GotFocus, Window LostFocus)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            if (closing) return;

            if (client == null)
            {
                ActivityLog.LogError("DiscordRPC", "Client is not assigned.");
                return;
            }

            if (!client.IsInitialized && !client.IsDisposed && !client.Initialize())
            {
                ActivityLog.LogError("DiscordRPC", "Cannot run client.");
            }

            if (GotFocus.Document != null)
            {
                UpdatePresence(GotFocus.Document);
            }
        }

        private void UpdatePresence(Document document, bool overrideTimestampReset = false)
        {
            if (closing) return;
            try
            {
                ThreadHelper.ThrowIfNotOnUIThread();

                if (client == null)
                {
                    ActivityLog.LogError("DiscordRPC", "Client is not assigned.");
                    return;
                }

                if (!ExtensionSettings.IsEnabled)
                {
                    if (!client.IsInitialized && !client.IsDisposed && !client.Initialize())
                    {
                        ActivityLog.LogError("DiscordRPC", "Cannot run client.");
                    }
                    client.ClearPresence();
                    return;
                }

                Translation.Culture = ExtensionSettings.UseEnglishPresence ? CultureInfo.GetCultureInfo("en-US") : CultureInfo.CurrentUICulture;

                if (ExtensionSettings.IsSecretModeEnabled)
                {
                    assets.LargeImageKey = versionImageKey;
                    assets.LargeImageText = versionName;
                    assets.SmallImageText = assets.SmallImageKey = string.Empty;
                    RichPresence presence = new RichPresence()
                        .WithDetails(Translation.PresenceDetails)
                        .WithState(Translation.PresenceState)
                        .WithAssets(assets);
                    ResetPresence(presence);
                    return;
                }

                if (document != null && !FileMatcher.ValidatePath(document.FullName))
                {
                    ResetPresence(null);
                    return;
                }

                var currentLanguage = document == null ? default : (from x in ProgrammingLanguages
                                      where x.Value.Extensions.Any(y =>
                                      {
                                          ThreadHelper.ThrowIfNotOnUIThread();
                                          return document.FullName.EndsWith(y, StringComparison.OrdinalIgnoreCase);
                                      })
                                      select x).FirstOrDefault();

                if (ExtensionSettings.LargeLanguageIcon)
                {
                    assets.SmallImageKey = versionImageKey;
                    assets.SmallImageText = versionName;
                    if (string.IsNullOrEmpty(currentLanguage.Key))
                    {
                        assets.LargeImageKey = "text";
                        assets.LargeImageText = Translation.UnrecognizedExtension;
                    }
                    else
                    {
                        assets.LargeImageKey = currentLanguage.Value.ImageKey;
                        assets.LargeImageText = currentLanguage.Key;
                    }
                }
                else
                {
                    assets.LargeImageKey = versionImageKey;
                    assets.LargeImageText = versionName;
                    if (currentLanguage.Equals(default))
                    {
                        assets.SmallImageKey = "text";
                        assets.SmallImageText = Translation.UnrecognizedExtension;
                    }
                    else
                    {
                        assets.SmallImageKey = currentLanguage.Value.ImageKey;
                        assets.SmallImageText = currentLanguage.Key;
                    }
                }

                RichPresence resPresence = new RichPresence();

                if (ExtensionSettings.ShowFileName)
                {
                    resPresence.Details = document != null ? Path.GetFileName(document.FullName) : Translation.NoFile;
                }

                if (ExtensionSettings.ShowSolutionName)
                {
                    bool idle = ide.Solution == null || string.IsNullOrEmpty(ide.Solution.FullName);
                    resPresence.State = idle ? Translation.Idling : (Translation.Developing + " " + Path.GetFileNameWithoutExtension(ide.Solution.FullName));

                    if (idle)
                    {
                        assets.LargeImageKey = versionImageKey;
                        assets.LargeImageText = versionName;
                        assets.SmallImageKey = assets.SmallImageText = string.Empty;
                    }
                }

                if (ExtensionSettings.ShowTimestamp && document != null)
                {
                    if (ExtensionSettings.ResetTimestamp)
                    {
                        if (overrideTimestampReset)
                            resPresence.Timestamps = currentTimestamps;
                        else
                           resPresence.Timestamps = new Timestamps() { Start = DateTime.UtcNow };
                    }
                    else if (!overrideTimestampReset)
                    {
                        resPresence.Timestamps = new Timestamps() { Start = startupTime };
                    }

                    currentTimestamps = resPresence.Timestamps;
                }
                resPresence.Assets = assets;

                ResetPresence(resPresence);
            }
            catch (Exception ex)
            {
                ActivityLog.LogError("DiscordRPC", ex.Message);
            }
        }

        private void ResetPresence(RichPresence presence)
        {
            if (!client.IsInitialized && !client.IsDisposed && !client.Initialize())
            {
                ActivityLog.LogError("DiscordRPC", "Cannot run client.");
                return;
            }

            if (presence == null)
            {
                client.ClearPresence();
                return;
            }

            client.SetPresence(presence);
        }
    }

    public struct LanguageInfo
    {
        public List<string> Extensions { get; set; }

        public string ImageKey { get; set; }
    }
}
