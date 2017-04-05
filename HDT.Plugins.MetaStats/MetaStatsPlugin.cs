using System;
using System.IO;
using System.Windows.Controls;
using Hearthstone_Deck_Tracker.API;
using Hearthstone_Deck_Tracker.Plugins;
using HDT.Plugins.MetaStats.Controls;
using HDT.Plugins.MetaStats.Logging;
using System.Threading.Tasks;
using Hearthstone_Deck_Tracker;
using System.ComponentModel;
using System.Net;

namespace HDT.Plugins.MetaStats
{
    public class MetaStatsPlugin : IPlugin
    {
        private static string pluginDir = Path.Combine(Hearthstone_Deck_Tracker.Config.Instance.DataDir, "MetaStats");
        private MenuItem _MetaDetectorMenuItem;
        private MetaStats _MetaStats = null;
        private MetaConfig _appConfig;

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
            try
            {
                SettingsWindow wndSettings = new SettingsWindow(_appConfig.userKey);
                wndSettings.Show();
            }
            catch (Exception ex)
            {
                MetaLog.Error(ex);
            }
        }

        public void OnLoad()
        {
            try
            {

                if (!Directory.Exists(pluginDir))
                    Directory.CreateDirectory(pluginDir);

                _appConfig = MetaConfig.Load();
                _appConfig.Save();

                _MetaStats = new MetaStats(_appConfig);
                _MetaDetectorMenuItem = new PluginMenu(_appConfig.userKey);

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
            get { return new Version(0, 0, 2); }
        }

        private async void CheckForUpdate()
        {
            try
            {
                var latest = await GitHub.CheckForUpdate("adnanc", "HDT.Plugins.MetaStats", Version);
                if (latest != null)
                {
                    //VersionWindow newVersion = new VersionWindow();
                    //newVersion.Show();
                    string pluginDLL = Path.Combine(Config.Instance.DataDir, @"Plugins\MetaStats\MetaStats.tmp");

                    using (WebClient wc = new WebClient())
                    {
                        wc.DownloadFileCompleted += (wc_DownloadFileCompleted);
                        wc.DownloadFileAsync(new Uri("https://s3.amazonaws.com/metastats/MetaStats.dll"), pluginDLL);
                    }
                }
            }
            catch (Exception ex)
            {
                MetaLog.Error(ex);
            }
        }

        void wc_DownloadFileCompleted(object sender, AsyncCompletedEventArgs e)
        {
            try
            {
                string tempFile = Path.Combine(Config.Instance.DataDir, @"Plugins\MetaStats\MetaStats.tmp");
                string pluginDLL = Path.Combine(Config.Instance.DataDir, @"Plugins\MetaStats\MetaStats.dll");
                FileInfo fi = new FileInfo(tempFile);
                if (File.Exists(tempFile))
                {
                    if (fi.Length > 0)
                        File.Copy(tempFile, pluginDLL, true);

                    File.Delete(tempFile);
                }
            }
            catch (Exception ex)
            {
                MetaLog.Error(ex);
            }
        }
    }
}
