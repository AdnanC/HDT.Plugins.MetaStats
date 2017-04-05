using System.Windows;

namespace HDT.Plugins.MetaStats.Controls
{
    /// <summary>
    /// Interaction logic for PluginMenu.xaml
    /// </summary>
    public partial class PluginMenu
    {

        private string _userID = "";
        private SettingsWindow _wndSettings;

        public PluginMenu(string u)
        {
            _userID = u;
            _wndSettings = new SettingsWindow(_userID);
            InitializeComponent();
        }

        private void MenuItem_Click(object sender, RoutedEventArgs e)
        {
            _wndSettings.Show();
        }
    }
}
