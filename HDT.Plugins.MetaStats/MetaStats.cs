using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Xml.Serialization;
using System.Threading.Tasks;
using Hearthstone_Deck_Tracker;
using HearthDb.Enums;
using Hearthstone_Deck_Tracker.Enums;
using HDT.Plugins.MetaStats.Logging;
using Hearthstone_Deck_Tracker.Hearthstone;
using System.Text;
using Hearthstone_Deck_Tracker.Hearthstone.Entities;

namespace HDT.Plugins.MetaStats
{

    public class MetaStats
    {
        

        private bool _validGameMode = true;

        private List<Card> _opponentCardsPlayed;
        private MyConfig _appConfig;
        private trackCards _cardsPlayed;
        private int _opponentTurnCount = 0;
        private int _playerTurnCount = 0;


        Dictionary<int, CardInfo> _trackOpponentCards = new Dictionary<int, CardInfo>();
        Dictionary<int, CardInfo> _trackPlayerCards = new Dictionary<int, CardInfo>();

        public MetaStats()
        {
            //_mainWindow = new OpDeckWindow();

            

            _opponentCardsPlayed = new List<Card>();
            _cardsPlayed = new trackCards();
            _appConfig = MyConfig.Load();
            _appConfig.Save();

            MetaLog.Initialize();

            MetaLog.Info("Meta Stats Initialized", "MetaDetector");
        }

        internal void GameStart()
        {
            try
            {
                //MetaLog.Info("Opponent Class: " + Core.Game.MatchInfo.OpposingPlayer.Name, "GameStart");
                MetaLog.Info("Game Mode: " + Core.Game.CurrentGameMode, "GameStart");
                MetaLog.Info("Game Format: " + Core.Game.CurrentFormat, "GameStart");
                MetaLog.Info("Region: " + Core.Game.CurrentRegion, "GameStart");
                MetaLog.Info("Mode: " + Core.Game.CurrentMode, "GameStart");

                _cardsPlayed.Clear();
                _trackOpponentCards.Clear();
                _trackPlayerCards.Clear();

                if (_validGameMode)
                {
                    _opponentTurnCount = 0;
                    _playerTurnCount = 0;

                    MetaLog.Info("New Game Started. Waiting for opponent to play cards.", "GameStart");
                }
            }
            catch (Exception ex)
            {
                MetaLog.Error(ex);
            }
        }

        internal void PlayerMulligan(Card c)
        {
            _trackPlayerCards.FirstOrDefault(x => x.Value.cardId == c.Id).Value.mulligan = true;
            //_trackPlayerCards.FirstOrDefault(x => x.Value.cardId == c.Id).Value.cardId = "";

            MetaLog.Info("Player Mulliganed " + c.Name, "PlayerMulligan");
        }

        internal void OpponentMulligan()
        {
        }

        internal void TurnStart(ActivePlayer activePlayer)
        {
            if (ActivePlayer.Player == activePlayer)
            {
                _playerTurnCount = Convert.ToInt16(Math.Ceiling(Core.Game.GameEntity.GetTag(GameTag.TURN) / 2.0));
                updateOpponentCardsPlayed();
            }
            else
            {
                _opponentTurnCount = Convert.ToInt16(Math.Ceiling(Core.Game.GameEntity.GetTag(GameTag.TURN) / 2.0));
            }
        }

        public void PlayerDraw(Card c)
        {
            try
            {
                updatePlayerHandCards();
            }
            catch (Exception ex)
            {
                MetaLog.Error(ex);
            }
        }

        private void updatePlayerHandCards()
        {
            foreach (var e in Core.Game.Entities.Where(x => x.Value.CardId != null && x.Value.CardId != "" &&
                    !x.Value.IsHero && !x.Value.IsHeroPower && x.Value.IsInHand &&
                    x.Value.GetTag(GameTag.CONTROLLER) == Core.Game.Player.Id).ToList())
            {
                if (_trackPlayerCards.Where(x => x.Key == e.Value.Id).Count() > 0)
                {
                    if (_trackPlayerCards[e.Value.Id].turnInHand == 0 && e.Value.Info.Turn > 0)
                        _trackPlayerCards[e.Value.Id].turnInHand = e.Value.Info.Turn;
                }
                else
                {
                    MetaLog.Info("Turn " + _playerTurnCount + ": Player Draws " + e.Value.LocalizedName);

                    if (e.Value.GetTag(GameTag.CREATOR) > 0)
                    {
                        _trackPlayerCards.Add(e.Value.Id, new CardInfo(e.Value.Info.Turn, e.Value.Info.Mulliganed, e.Value.CardId, -1, -1, -1, -1,
                            e.Value.Info.Created, Core.Game.Entities[e.Value.GetTag(GameTag.CREATOR)].CardId));
                    }
                    else
                    {
                        _trackPlayerCards.Add(e.Value.Id, new CardInfo(e.Value.Info.Turn, e.Value.Info.Mulliganed, e.Value.CardId, -1, -1, -1, -1,
                            e.Value.Info.Created, ""));
                    }
                }
            }
        }

        public void OpponentSecretTriggered(Card c)
        {
            try
            {
                foreach (Entity e in Core.Game.Entities.Where(x => x.Value.CardId != null && !x.Value.IsHeroPower
                && !x.Value.IsHero && x.Value.GetTag(GameTag.CONTROLLER) == Core.Game.Opponent.Id
                && x.Value.IsSecret).Select(x => x.Value))
                {
                    CardInfo temp;
                    if (_trackOpponentCards.TryGetValue(e.Id, out temp))
                    {
                        if (_trackOpponentCards[e.Id].cardId == "")
                        {
                            _trackOpponentCards[e.Id].cardId = e.CardId;
                            _trackOpponentCards[e.Id].turnCardPlayed = e.Info.Turn;
                            _trackOpponentCards[e.Id].mana = Core.Game.OpponentEntity.GetTag(GameTag.RESOURCES);
                            _trackOpponentCards[e.Id].manaoverload = Core.Game.OpponentEntity.GetTag(GameTag.OVERLOAD_OWED);

                            if (e.GetTag(GameTag.CREATOR) > 0)
                            {
                                _trackOpponentCards[e.Id].created = e.Info.Created;
                                _trackOpponentCards[e.Id].createdBy = Core.Game.Entities[e.GetTag(GameTag.CREATOR)].CardId;
                            }
                        }
                    }
                    else
                    {
                        if (e.GetTag(GameTag.CREATOR) > 0)
                        {
                            _trackOpponentCards.Add(e.Id, new CardInfo(-1, false, e.CardId, e.Info.Turn,
                            Core.Game.OpponentEntity.GetTag(GameTag.RESOURCES), Core.Game.OpponentEntity.GetTag(GameTag.OVERLOAD_OWED), -1,
                            e.Info.Created, Core.Game.Entities[e.GetTag(GameTag.CREATOR)].CardId));
                        }
                        else
                        {
                            _trackOpponentCards.Add(e.Id, new CardInfo(-1, false, e.CardId, e.Info.Turn,
                            Core.Game.OpponentEntity.GetTag(GameTag.RESOURCES), Core.Game.OpponentEntity.GetTag(GameTag.OVERLOAD_OWED), -1,
                            e.Info.Created, ""));
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MetaLog.Error(ex);
            }
        }

        public void OpponentDraw()
        {
            try
            {
                foreach (Entity e in Core.Game.Entities.Select(x => x.Value).Where(x => !x.IsHero
                && !x.IsHeroPower && x.GetTag(GameTag.CONTROLLER) == Core.Game.Opponent.Id))
                {
                    if (e.CardId == null || e.CardId == "")
                    {
                        if (_trackOpponentCards.Where(x => x.Key == e.Id).Count() > 0)
                        {
                            _trackOpponentCards[e.Id].turnInHand = e.Info.Turn;
                            _trackOpponentCards[e.Id].mulligan = e.Info.Mulliganed;
                            _trackOpponentCards[e.Id].created = e.Info.Created;

                            if (e.GetTag(GameTag.CREATOR) > 0)
                                _trackOpponentCards[e.Id].createdBy = Core.Game.Entities[e.GetTag(GameTag.CREATOR)].CardId;
                        }
                        else
                        {
                            if (e.GetTag(GameTag.CREATOR) > 0)
                            {
                                _trackOpponentCards.Add(e.Id, new CardInfo(e.Info.Turn, e.Info.Mulliganed, "", -1, -1, -1, -1,
                                e.Info.Created, Core.Game.Entities[e.GetTag(GameTag.CREATOR)].CardId));
                            }
                            else
                            {
                                _trackOpponentCards.Add(e.Id, new CardInfo(e.Info.Turn, e.Info.Mulliganed, "", -1, -1, -1, -1,
                                e.Info.Created, ""));
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MetaLog.Error(ex);
            }
        }

        public void PlayerPlay(Card cardPlayed)
        {
            try
            {
                updatePlayerHandCards();
                updatePlayerBoardEntities();

                MetaLog.Info("Turn " + _playerTurnCount + ": Player Played - " + cardPlayed.Name, "PlayerPlay");
            }
            catch (Exception ex)
            {
                MetaLog.Error(ex);
            }
        }

        private void updatePlayerBoardEntities()
        {
            foreach (Entity e in Core.Game.Player.Board.Where(x => !x.IsHero && !x.IsHeroPower))
            {
                CardInfo temp;
                if (_trackPlayerCards.TryGetValue(e.Id, out temp))
                {
                    _trackPlayerCards[e.Id].turnCardPlayed = e.Info.Turn;

                    if (e.GetTag(GameTag.CREATOR) > 0)
                    {
                        _trackPlayerCards[e.Id].created = e.Info.Created;
                        _trackPlayerCards[e.Id].createdBy = Core.Game.Entities[e.GetTag(GameTag.CREATOR)].CardId;
                    }
                }
                else
                {
                    if (e.GetTag(GameTag.CREATOR) > 0)
                    {
                        _trackPlayerCards.Add(e.Id, new CardInfo(-1, false, e.CardId, e.Info.Turn, -1, -1, -1,
                            e.Info.Created, Core.Game.Entities[e.GetTag(GameTag.CREATOR)].CardId));
                    }
                    else
                    {
                        _trackPlayerCards.Add(e.Id, new CardInfo(-1, false, e.CardId, e.Info.Turn, -1, -1, -1,
                            e.Info.Created, ""));
                    }
                }
            }
        }

        public void OpponentCreateInPlay(Card cardCreated)
        {
            updateOpponentCardsPlayed();
        }

        public void OpponentCreateInDeck(Card cardCreated)
        {
        }

        public void PlayerCreateInPlay(Card cardCreated)
        {
            updatePlayerBoardEntities();
        }

        public void PlayerCreateInDeck(Card cardCreated)
        {
        }

        public void OpponentPlay(Card cardPlayed)
        {
            try
            {
                updateOpponentCardsPlayed();
                MetaLog.Info("Turn " + _opponentTurnCount + ": Opponent Played - " + cardPlayed.Name, "OpponentPlay");
            }
            catch (Exception ex)
            {
                MetaLog.Error(ex);
            }
        }

        private void updateOpponentCardsPlayed()
        {
            try
            {
                foreach (Entity e in Core.Game.Entities.Select(v => v.Value).Where(x => x.CardId != null && !x.IsHeroPower
                        && !x.IsHero && x.GetTag(GameTag.CONTROLLER) == Core.Game.Opponent.Id && x.IsInPlay)
                    )
                /*                foreach (Entity e in Core.Game.Entities.Where(x => x.Value.CardId != null && !x.Value.IsHeroPower
                                && !x.Value.IsHero && x.Value.GetTag(GameTag.CONTROLLER) == Core.Game.Opponent.Id
                                && x.Value.IsInPlay).Select(x => x.Value))*/
                {
                    CardInfo temp;
                    if (_trackOpponentCards.TryGetValue(e.Id, out temp))
                    {
                        if (_trackOpponentCards[e.Id].cardId == "")
                        {
                            _trackOpponentCards[e.Id].cardId = e.CardId;
                            _trackOpponentCards[e.Id].turnCardPlayed = e.Info.Turn;
                            _trackOpponentCards[e.Id].mana = Core.Game.OpponentEntity.GetTag(GameTag.RESOURCES);
                            _trackOpponentCards[e.Id].manaoverload = Core.Game.OpponentEntity.GetTag(GameTag.OVERLOAD_OWED);

                            if (e.GetTag(GameTag.CREATOR) > 0)
                            {
                                _trackOpponentCards[e.Id].created = e.Info.Created;
                                _trackOpponentCards[e.Id].createdBy = Core.Game.Entities[e.GetTag(GameTag.CREATOR)].CardId;
                            }
                        }
                    }
                    else
                    {
                        if (e.GetTag(GameTag.CREATOR) > 0)
                        {
                            _trackOpponentCards.Add(e.Id, new CardInfo(-1, false, e.CardId, e.Info.Turn,
                            Core.Game.OpponentEntity.GetTag(GameTag.RESOURCES), Core.Game.OpponentEntity.GetTag(GameTag.OVERLOAD_OWED), -1,
                            e.Info.Created, Core.Game.Entities[e.GetTag(GameTag.CREATOR)].CardId));
                        }
                        else
                        {
                            _trackOpponentCards.Add(e.Id, new CardInfo(-1, false, e.CardId, e.Info.Turn,
                            Core.Game.OpponentEntity.GetTag(GameTag.RESOURCES), Core.Game.OpponentEntity.GetTag(GameTag.OVERLOAD_OWED), -1,
                            e.Info.Created, ""));
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                //MetaLog.Error(ex);
            }

        }

        public void OpponentHeroPower()
        {
            _cardsPlayed.Add("HERO_POWER", Convert.ToInt16(Math.Ceiling(Core.Game.GameEntity.GetTag(GameTag.TURN) / 2.0)), false, -1, false, -1,
                Core.Game.MatchInfo.OpposingPlayer.StandardRank, Core.Game.MatchInfo.OpposingPlayer.StandardLegendRank, Core.Game.MatchInfo.LocalPlayer.StandardRank,
                Core.Game.MatchInfo.LocalPlayer.StandardLegendRank, Core.Game.MatchInfo.RankedSeasonId,
                "Hero Power", Core.Game.OpponentEntity.GetTag(GameTag.RESOURCES), Core.Game.OpponentEntity.GetTag(GameTag.OVERLOAD_OWED), "Opponent");
            MetaLog.Info("Turn " + _opponentTurnCount + ": Opponent Hero Power", "OpponentHeroPower");
        }

        public void PlayerHeroPower()
        {
            _cardsPlayed.Add("HERO_POWER", Convert.ToInt16(Math.Ceiling(Core.Game.GameEntity.GetTag(GameTag.TURN) / 2.0)), false, -1, false, -1,
                Core.Game.MatchInfo.OpposingPlayer.StandardRank, Core.Game.MatchInfo.OpposingPlayer.StandardLegendRank, Core.Game.MatchInfo.LocalPlayer.StandardRank,
                Core.Game.MatchInfo.LocalPlayer.StandardLegendRank, Core.Game.MatchInfo.RankedSeasonId,
                "Hero Power", Core.Game.PlayerEntity.GetTag(GameTag.RESOURCES), Core.Game.PlayerEntity.GetTag(GameTag.OVERLOAD_OWED), "Player");
            MetaLog.Info("Turn " + _playerTurnCount + ": Player Hero Power", "PlayerHeroPower");
        }

        public void OpponentPlayToGraveyard(Card c)
        {
            MetaLog.Info("Turn " + _opponentTurnCount + ": Minion Dead - " + c.Name, "OpponentPlayToGraveyard");
        }

        public async void GameEnd()
        {
            if (_validGameMode)
                try
                {
                    MetaLog.Info("Game Ended. Waiting for new Game", "GameEnd");
                }
                catch (Exception ex)
                {
                    MetaLog.Error(ex);
                }

            //get all cards played / died in the game
            try
            {
                foreach (var x in Core.Game.Opponent.Graveyard.Where(x => !x.IsHero && !x.IsHeroPower && x.CardId != ""))
                {
                    CardInfo temp;
                    if (_trackOpponentCards.TryGetValue(x.Id, out temp))
                    {
                        _trackOpponentCards[x.Id].turnCardDied = x.Info.Turn;
                    }
                }

                foreach (var x in Core.Game.Player.Graveyard.Where(x => !x.IsHero && !x.IsHeroPower && x.CardId != ""))
                {
                    CardInfo temp;
                    if (_trackPlayerCards.TryGetValue(x.Id, out temp))
                    {
                        _trackPlayerCards[x.Id].turnCardDied = x.Info.Turn;
                    }
                }

                //_cardsPlayed.Clear();
                foreach (CardInfo x in _trackOpponentCards.Values.Where(x => x.cardId != "" && x.cardId != null).OrderBy(x => x.turnCardPlayed).ToList())
                {
                    _cardsPlayed.Add(x.cardId, x.turnCardPlayed, x.created, x.turnInHand, x.mulligan,
                        x.turnCardDied,
                        Core.Game.MatchInfo.OpposingPlayer.StandardRank, Core.Game.MatchInfo.OpposingPlayer.StandardLegendRank, Core.Game.MatchInfo.LocalPlayer.StandardRank,
                        Core.Game.MatchInfo.LocalPlayer.StandardLegendRank, Core.Game.MatchInfo.RankedSeasonId,
                        Database.GetCardFromId(x.cardId).Name, x.mana, x.manaoverload, "Opponent", x.createdBy);
                }

                foreach (CardInfo x in _trackPlayerCards.Values.Where(x => x.cardId != "" && x.cardId != null).OrderBy(x => x.turnCardPlayed).ToList())
                {
                    _cardsPlayed.Add(x.cardId, x.turnCardPlayed, x.created, x.turnInHand, x.mulligan,
                        x.turnCardDied,
                        Core.Game.MatchInfo.OpposingPlayer.StandardRank, Core.Game.MatchInfo.OpposingPlayer.StandardLegendRank, Core.Game.MatchInfo.LocalPlayer.StandardRank,
                        Core.Game.MatchInfo.LocalPlayer.StandardLegendRank, Core.Game.MatchInfo.RankedSeasonId,
                        Database.GetCardFromId(x.cardId).Name, x.mana, x.manaoverload, "Player", x.createdBy);
                }

                if (Core.Game.CurrentGameStats.Result == GameResult.Win)
                    _cardsPlayed.setPlayerWin();
                else if (Core.Game.CurrentGameStats.Result == GameResult.Loss)
                    _cardsPlayed.setOpponentWin();

                _cardsPlayed.Save();
                await sendCardStats();
            }
            catch (Exception ex)
            {
                MetaLog.Error(ex);
            }
        }

        internal async Task<string> sendCardStats()
        {
            try
            {
                if (_validGameMode)
                {
                    {
                        MetaLog.Info("Uploading Card Stats...", "sendRequest");

                        string url = "http://metastats.net/metadetector/cards.php?v=3";


                        string postData = _cardsPlayed.GetCardStats();

                        if (postData != "")
                        {

                            WebClient client = new WebClient();
                            byte[] data = Encoding.UTF8.GetBytes(postData);
                            Uri uri = new Uri(url);
                            var response = Encoding.UTF8.GetString(await client.UploadDataTaskAsync(uri, "POST", data));

                            _appConfig.lastUpload = DateTime.Now;
                            _appConfig.Save();

                            MetaLog.Info("Uploading Card Stats Done", "sendRequest");

                            return response;
                        }
                    }
                }

                return null;
            }
            catch (Exception ex)
            {
                MetaLog.Error(ex);
                return null;
            }
        }
    }

    public class CardInfo
    {
        public string cardId;
        public int turnCardPlayed;
        public int turnInHand;
        public int turnCardDied;
        public bool mulligan;
        public bool created;
        public string createdBy;
        public int mana;
        public int manaoverload;

        public CardInfo(int InHand, bool mul = false, string scardId = "",
            int nturnplayed = -1, int nmana = -1, int nmanaoverload = -1, int nturndied = -1, bool bCreated = false, string sCreatedBy = "")
        {
            this.mulligan = mul;
            this.turnInHand = InHand;
            this.cardId = scardId;
            this.turnCardPlayed = nturnplayed;
            this.mana = nmana;
            this.manaoverload = nmanaoverload;
            this.turnCardDied = nturndied;
            this.created = bCreated;
            this.createdBy = sCreatedBy;
        }
    }

    public class trackCards
    {
        private static string statsDirectory = Path.Combine(Config.AppDataPath, "MetaDetector");
        public static string statsPath = Path.Combine(statsDirectory, "cardStats.xml");

        public string gameId { get; set; }
        public string playerClass { get; set; }
        public string opponentClass { get; set; }
        public string gameFormat { get; set; }
        public string gameMode { get; set; }
        public string rankString { get; set; }
        public string region { get; set; }
        public int rankedSeasonId { get; set; }
        public int opponentRank { get; set; }
        public int opponentLegendRank { get; set; }
        public bool opponentCoin { get; set; }
        public int playerRank { get; set; }
        public int playerLegendRank { get; set; }
        public bool opponentWin { get; set; }
        public bool playerWin { get; set; }
        public string cardId { get; set; }
        public string cardName { get; set; }
        public int turn { get; set; }
        public int turnDrawn { get; set; }
        public int turnToGraveyard { get; set; }
        public bool mulligan { get; set; }
        public int mana { get; set; }
        public int manaOverload { get; set; }
        public bool isCreated { get; set; }
        public string createdBy { get; set; }
        public string activePlayer { get; set; }

        private List<trackCards> _cardsPlayed = new List<trackCards>();

        public trackCards()
        {
            this.turnDrawn = -1;
            this.turnToGraveyard = -1;
            this.turn = -1;
            this.mana = -1;
            this.manaOverload = -1;
            this.mulligan = false;
        }

        public void Add(string cardId, int nturn, bool isCreated,
            int nturnInHand, bool bmulligan,
            int nturnCardDied,
            int nopponentRank, int nopponentLengendRank, int nplayrank, int nplayerLegendRank,
            int nrankedSeasonId,
            string sName = "", int nMana = -1,
            int nManaOverload = -1, string sActivePlayer = "Opponent", string createdBy = ""
             )
        {
            trackCards temp = new trackCards();

            var standard = Core.Game.CurrentFormat == Format.Standard;

            temp.gameId = null;
            temp.playerClass = Core.Game.CurrentGameStats.PlayerHero;
            temp.opponentClass = Core.Game.CurrentGameStats.OpponentHero;
            temp.gameFormat = Core.Game.CurrentFormat.ToString();
            temp.gameMode = Core.Game.CurrentGameMode.ToString();

            //temp.opponentRank = Core.Game.MatchInfo.OpposingPlayer.StandardRank;

            /*temp.opponentRank = standard ? Core.Game.MatchInfo.OpposingPlayer.StandardRank : Core.Game.MatchInfo.OpposingPlayer.WildRank;
            temp.opponentLegendRank = standard ? Core.Game.MatchInfo.OpposingPlayer.StandardLegendRank : Core.Game.MatchInfo.OpposingPlayer.WildLegendRank;
            temp.playerLegendRank = standard ? Core.Game.MatchInfo.LocalPlayer.StandardLegendRank : Core.Game.MatchInfo.LocalPlayer.WildLegendRank;
            temp.playerRank = standard ? Core.Game.MatchInfo.LocalPlayer.StandardRank : Core.Game.MatchInfo.LocalPlayer.WildRank;
            temp.rankedSeasonId = Core.Game.MatchInfo.RankedSeasonId;*/

            temp.opponentRank = nopponentRank;
            temp.opponentLegendRank = nopponentLengendRank;
            temp.playerRank = nplayrank;
            temp.playerLegendRank = nplayerLegendRank;
            temp.rankedSeasonId = nrankedSeasonId;

            temp.region = Core.Game.CurrentRegion.ToString();
            temp.opponentCoin = Core.Game.Opponent.HasCoin;
            temp.opponentWin = false;
            temp.playerWin = false;
            temp.isCreated = isCreated;
            temp.createdBy = createdBy;
            temp.turnDrawn = nturnInHand;
            temp.mulligan = bmulligan;
            temp.turnToGraveyard = nturnCardDied;
            temp.turn = nturn;
            temp.cardId = cardId;
            temp.cardName = sName;
            temp.mana = nMana;
            temp.manaOverload = nManaOverload;
            temp.activePlayer = sActivePlayer;

            _cardsPlayed.Add(temp);
        }

        public void Clear()
        {
            _cardsPlayed.Clear();
        }

        public string GetCardStats()
        {
            var serializer = new XmlSerializer(typeof(List<trackCards>));

            using (StringWriter textWriter = new StringWriter())
            {
                serializer.Serialize(textWriter, _cardsPlayed);
                return textWriter.ToString();
            }
        }

        public void Save()
        {
            try
            {
                if (!Directory.Exists(statsDirectory))
                    Directory.CreateDirectory(statsDirectory);

                var serializer = new XmlSerializer(typeof(List<trackCards>));
                using (var writer = new StreamWriter(statsPath))
                    serializer.Serialize(writer, _cardsPlayed);
            }
            catch (Exception ex)
            {
                MetaLog.Error(ex);
            }
        }

        public void setPlayerWin()
        {
            foreach (trackCards c in _cardsPlayed)
            {
                c.playerWin = true;
            }
        }

        public void setOpponentWin()
        {
            foreach (trackCards c in _cardsPlayed)
            {
                c.opponentWin = true;
            }
        }

        public void setTurnInHand(Dictionary<int, CardInfo> cardsTracked)
        {
            foreach (var c in cardsTracked.Where(x => x.Value.cardId != null))
            {
                var t = (trackCards)_cardsPlayed.Where(x => x.turn == c.Value.turnCardPlayed && x.cardId == c.Value.cardId);
                t.mulligan = c.Value.mulligan;
                t.turnDrawn = c.Value.turnInHand;
            }
        }
    }

    public class MyConfig
    {
        private static string configDirectory = Path.Combine(Config.AppDataPath, "MetaDetector");
        private static string configPath = Path.Combine(configDirectory, "metaConfig.xml");

        public string currentVersion { get; set; }
        public DateTime lastCheck { get; set; }
        public DateTime lastUpload { get; set; }

        public MyConfig()
        {

        }

        public MyConfig(string v, DateTime c, DateTime u)
        {
            this.currentVersion = v;
            this.lastCheck = c;
            this.lastUpload = u;
        }

        public static MyConfig Load()
        {
            try
            {
                if (File.Exists(configPath))
                {
                    var serializer = new XmlSerializer(typeof(MyConfig));

                    using (var reader = new StreamReader(configPath))
                        return (MyConfig)serializer.Deserialize(reader);
                }
                else
                {
                    return new MyConfig("1", DateTime.Now, DateTime.Now);
                }
            }
            catch (Exception ex)
            {
                MetaLog.Error(ex);
                return new MyConfig("1", DateTime.Now, DateTime.Now);
                //return null;
            }
        }

        public void Save()
        {
            try
            {
                if (!Directory.Exists(configDirectory))
                    Directory.CreateDirectory(configDirectory);

                var serializer = new XmlSerializer(typeof(MyConfig));
                using (var writer = new StreamWriter(configPath))
                    serializer.Serialize(writer, this);
            }
            catch (Exception ex)
            {
                MetaLog.Error(ex);
            }
        }
    }
}
