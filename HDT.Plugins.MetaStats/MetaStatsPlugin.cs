using System;
using System.IO;
using System.Windows.Controls;
using Hearthstone_Deck_Tracker.API;
using Hearthstone_Deck_Tracker.Plugins;
using HDT.Plugins.MetaStats.Controls;
using HDT.Plugins.MetaStats.Logging;
using System.Threading.Tasks;

namespace HDT.Plugins.MetaStats
{
    public class MetaStatsPlugin : IPlugin
    {
        private static string pluginDir = Path.Combine(Hearthstone_Deck_Tracker.Config.Instance.DataDir, "MetaStats");
        private MenuItem _MetaDetectorMenuItem;
        private MetaStats _MetaStats = null;

        public string Author
        {
            get { return "AdnanC"; }
        }

        public string ButtonText
        {
            get { return "Settings"; }
        }

        public string Description
        {
            get { return "Get Hearthstone Stats"; }
        }

        public MenuItem MenuItem
        {
            //get { return null; }
            get { return _MetaDetectorMenuItem; }
        }

        public string Name
        {
            get { return "Meta Stats"; }
        }

        public void OnButtonPress()
        {

        }

        public void OnLoad()
        {
            try
            {
                if (!Directory.Exists(pluginDir))
                    Directory.CreateDirectory(pluginDir);

                _MetaStats = new MetaStats();
                _MetaDetectorMenuItem = new PluginMenu();

                GameEvents.OnGameStart.Add(_MetaStats.GameStart);
                GameEvents.OnGameEnd.Add(_MetaStats.GameEnd);

                GameEvents.OnTurnStart.Add(_MetaStats.TurnStart);

                GameEvents.OnOpponentPlay.Add(_MetaStats.OpponentPlay);
                GameEvents.OnOpponentDraw.Add(_MetaStats.OpponentDraw);

                GameEvents.OnOpponentCreateInPlay.Add(_MetaStats.OpponentCreateInPlay);
                GameEvents.OnOpponentCreateInDeck.Add(_MetaStats.OpponentCreateInDeck);
                GameEvents.OnOpponentHeroPower.Add(_MetaStats.OpponentHeroPower);
                GameEvents.OnOpponentSecretTriggered.Add(_MetaStats.OpponentSecretTriggered);
                GameEvents.OnOpponentPlayToGraveyard.Add(_MetaStats.OpponentPlayToGraveyard);
                GameEvents.OnOpponentMulligan.Add(_MetaStats.OpponentMulligan);

                GameEvents.OnPlayerDraw.Add(_MetaStats.PlayerDraw);
                GameEvents.OnPlayerPlay.Add(_MetaStats.PlayerPlay);
                GameEvents.OnPlayerCreateInPlay.Add(_MetaStats.PlayerCreateInPlay);
                GameEvents.OnPlayerCreateInDeck.Add(_MetaStats.PlayerCreateInDeck);
                GameEvents.OnPlayerHeroPower.Add(_MetaStats.PlayerHeroPower);
                GameEvents.OnPlayerMulligan.Add(_MetaStats.PlayerMulligan);

                CheckForUpdate();

                //_MainWindow.Show();
                //_MainWindow.Visibility = System.Windows.Visibility.Hidden;
                MetaLog.Info("Plugin Load Successful");

            }
            catch (Exception ex)
            {
                MetaLog.Error(ex);
                MetaLog.Info("Plugin Load Unsuccessful");
            }
        }

        public void OnUnload()
        {
            _MetaStats = null;
            MetaLog.Info("Plugin Unload Successful");
        }

        public void OnUpdate()
        {

        }

        public Version Version
        {
            get { return new Version(0, 0, 1); }
        }

        private async void CheckForUpdate()
        {
            var latest = await GitHub.CheckForUpdate("adnanc", "HDT.Plugins.MetaStats", Version);
            if (latest != null)
            {
                VersionWindow newVersion = new VersionWindow();
                newVersion.Show();
            }
        }
    }
}
