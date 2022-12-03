namespace DiscordRichPresenceForVisualStudio
{
    public class DialogWindow : Microsoft.VisualStudio.PlatformUI.DialogWindow
    {
        public DialogWindow()
        {
            HasMaximizeButton = true;
            HasMinimizeButton = true;
            HasHelpButton = true;
        }
    }
}
