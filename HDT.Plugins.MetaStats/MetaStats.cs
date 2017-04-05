using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Xml.Serialization;
using System.Threading.Tasks;
using System.Net.NetworkInformation;
using Hearthstone_Deck_Tracker;
using HearthDb.Enums;
using Hearthstone_Deck_Tracker.Enums;
using Hearthstone_Deck_Tracker.Hearthstone;
using Hearthstone_Deck_Tracker.Hearthstone.Entities;
using HDT.Plugins.MetaStats.Logging;
using HDT.Plugins.MetaStats.Controls;


namespace HDT.Plugins.MetaStats
{

    public class MetaStats
    {
        private bool _validGameMode = true;

        private List<Card> _opponentCardsPlayed;
        private MetaConfig _appConfig;
        private int _opponentTurnCount = 0;
        private int _playerTurnCount = 0;


        Dictionary<int, CardInfo> _trackOpponentCards = new Dictionary<int, CardInfo>();
        Dictionary<int, CardInfo> _trackPlayerCards = new Dictionary<int, CardInfo>();

        public MetaStats(MetaConfig conf)
        {
            try
            {
                //_mainWindow = new OpDeckWindow();

                _appConfig = conf;
                _opponentCardsPlayed = new List<Card>();
                bool showBubble = false;

                if (showBubble)
                {
                    try
                    {
                        NotificationWindow notify = new NotificationWindow();
                        notify.Show();
                    }
                    catch (Exception ex)
                    {
                        MetaLog.Error(ex);
                    }
                }

                MetaLog.Initialize();

                MetaLog.Info("Meta Stats Initialized", "MetaStats");
            }
            catch(Exception ex)
            {
                MetaLog.Error(ex);
            }
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
            try
            {
                _trackPlayerCards.FirstOrDefault(x => x.Value.cardId == c.Id).Value.mulligan = true;
                //_trackPlayerCards.FirstOrDefault(x => x.Value.cardId == c.Id).Value.cardId = "";

                MetaLog.Info("Player Mulliganed " + c.Name, "PlayerMulligan");
            }
            catch(Exception ex)
            {
                MetaLog.Error(ex);
            }
        }

        internal void OpponentMulligan()
        {

        }

        internal void TurnStart(ActivePlayer activePlayer)
        {
            try
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
            catch(Exception ex)
            {
                MetaLog.Error(ex);
            }
        }

        public void PlayerDraw(Card c)
        {
            try
            {
                updatePlayerHandCards();
                //Core.Game.Player.PlayerCardList.Where(x => !x.IsCreated).ToDictionary( g=>g.Id, g=>g.Count);
            }
            catch (Exception ex)
            {
                MetaLog.Error(ex);
            }
        }

        private void updatePlayerHandCards()
        {
            try
            {
                foreach (var e in Core.Game.Entities.Where(x => x.Value.CardId != null && x.Value.CardId != "" &&
                        !x.Value.IsHero && !x.Value.IsHeroPower && x.Value.IsInHand &&
                        x.Value.GetTag(GameTag.CONTROLLER) == Core.Game.Player.Id).ToList())
                {
                    if (_trackPlayerCards.Where(x => x.Key == e.Value.Id).Count() > 0)
                    {
                        if (_trackPlayerCards[e.Value.Id].turnDrawn == 0 && e.Value.Info.Turn > 0)
                            _trackPlayerCards[e.Value.Id].turnDrawn = e.Value.Info.Turn;
                    }
                    else
                    {
                        MetaLog.Info("Turn " + _playerTurnCount + ": Player Draws " + e.Value.LocalizedName);
                        CardInfo tempInfo = new CardInfo();

                        tempInfo.turnDrawn = e.Value.Info.Turn;
                        tempInfo.mulligan = e.Value.Info.Mulliganed;
                        tempInfo.cardId = e.Value.CardId;
                        tempInfo.cardName = e.Value.Card.Name;
                        tempInfo.isCreated = e.Value.Info.Created;
                        tempInfo.activePlayer = "Player";

                        if (e.Value.GetTag(GameTag.CREATOR) > 0)
                            tempInfo.createdBy = Core.Game.Entities[e.Value.GetTag(GameTag.CREATOR)].CardId;

                        _trackPlayerCards.Add(e.Value.Id, tempInfo);
                    }
                }
            }
            catch(Exception ex)
            {
                MetaLog.Error(ex);            }
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
                            _trackOpponentCards[e.Id].turn = e.Info.Turn;
                            _trackOpponentCards[e.Id].mana = Core.Game.OpponentEntity.GetTag(GameTag.RESOURCES);
                            _trackOpponentCards[e.Id].manaOverload = Core.Game.OpponentEntity.GetTag(GameTag.OVERLOAD_OWED);
                            _trackOpponentCards[e.Id].activePlayer = "Opponent";
                            if (e.GetTag(GameTag.CREATOR) > 0)
                            {
                                _trackOpponentCards[e.Id].isCreated = e.Info.Created;
                                _trackOpponentCards[e.Id].createdBy = Core.Game.Entities[e.GetTag(GameTag.CREATOR)].CardId;
                            }
                        }
                    }
                    else
                    {
                        /*
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
                        }*/

                        CardInfo tempInfo = new CardInfo();

                        tempInfo.turnDrawn = e.Info.Turn;
                        tempInfo.mulligan = e.Info.Mulliganed;
                        tempInfo.cardId = e.CardId;
                        tempInfo.cardName = e.Card.Name;
                        tempInfo.isCreated = e.Info.Created;
                        tempInfo.activePlayer = "Opponent";

                        if (e.GetTag(GameTag.CREATOR) > 0)
                            tempInfo.createdBy = Core.Game.Entities[e.GetTag(GameTag.CREATOR)].CardId;

                        _trackOpponentCards.Add(e.Id, tempInfo);

                        tempInfo = null;
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
                            _trackOpponentCards[e.Id].turnDrawn = e.Info.Turn;
                            _trackOpponentCards[e.Id].mulligan = e.Info.Mulliganed;
                            _trackOpponentCards[e.Id].isCreated = e.Info.Created;
                            _trackOpponentCards[e.Id].activePlayer = "Opponent";

                            if (e.GetTag(GameTag.CREATOR) > 0)
                                _trackOpponentCards[e.Id].createdBy = Core.Game.Entities[e.GetTag(GameTag.CREATOR)].CardId;
                        }
                        else
                        {
                            CardInfo tempInfo = new CardInfo();

                            tempInfo.turnDrawn = e.Info.Turn;
                            tempInfo.mulligan = e.Info.Mulliganed;
                            tempInfo.isCreated = e.Info.Created;
                            tempInfo.activePlayer = "Opponent";

                            if (e.GetTag(GameTag.CREATOR) > 0)
                                tempInfo.createdBy = Core.Game.Entities[e.GetTag(GameTag.CREATOR)].CardId;

                            _trackOpponentCards.Add(e.Id, tempInfo);

                            tempInfo = null;
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
            try
            {
                foreach (Entity e in Core.Game.Player.Board.Where(x => !x.IsHero && !x.IsHeroPower))
                {
                    CardInfo temp;
                    if (_trackPlayerCards.TryGetValue(e.Id, out temp))
                    {
                        _trackPlayerCards[e.Id].turn = e.Info.Turn;
                        _trackPlayerCards[e.Id].cardName = e.Card.Name;
                        _trackPlayerCards[e.Id].activePlayer = "Player";

                        if (e.GetTag(GameTag.CREATOR) > 0)
                        {
                            _trackPlayerCards[e.Id].isCreated = e.Info.Created;
                            _trackPlayerCards[e.Id].createdBy = Core.Game.Entities[e.GetTag(GameTag.CREATOR)].CardId;
                        }
                    }
                    else
                    {
                        CardInfo tempInfo = new CardInfo();

                        tempInfo.turn = e.Info.Turn;
                        tempInfo.cardId = e.CardId;
                        tempInfo.cardName = e.Card.Name;
                        tempInfo.isCreated = e.Info.Created;
                        tempInfo.activePlayer = "Player";

                        if (e.GetTag(GameTag.CREATOR) > 0)
                            tempInfo.createdBy = Core.Game.Entities[e.GetTag(GameTag.CREATOR)].CardId;

                        _trackPlayerCards.Add(e.Id, tempInfo);
                    }
                }
            }
            catch(Exception ex)
            {
                MetaLog.Error(ex);
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
                if (cardPlayed != null)
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
                            _trackOpponentCards[e.Id].turn = e.Info.Turn;
                            _trackOpponentCards[e.Id].activePlayer = "Opponent";
                            _trackOpponentCards[e.Id].mana = Core.Game.OpponentEntity.GetTag(GameTag.RESOURCES);
                            _trackOpponentCards[e.Id].manaOverload = Core.Game.OpponentEntity.GetTag(GameTag.OVERLOAD_OWED);

                            if (e.GetTag(GameTag.CREATOR) > 0)
                            {
                                _trackOpponentCards[e.Id].isCreated = e.Info.Created;
                                _trackOpponentCards[e.Id].createdBy = Core.Game.Entities[e.GetTag(GameTag.CREATOR)].CardId;
                            }
                        }
                    }
                    else
                    {
                        CardInfo tempInfo = new CardInfo();

                        tempInfo.turn = e.Info.Turn;
                        tempInfo.cardId = e.CardId;
                        tempInfo.cardName = e.Card.Name;
                        tempInfo.activePlayer = "Opponent";
                        tempInfo.mana = Core.Game.OpponentEntity.GetTag(GameTag.RESOURCES);
                        tempInfo.manaOverload = Core.Game.OpponentEntity.GetTag(GameTag.OVERLOAD_OWED);
                        tempInfo.isCreated = e.Info.Created;

                        if (e.GetTag(GameTag.CREATOR) > 0)
                            tempInfo.createdBy = Core.Game.Entities[e.GetTag(GameTag.CREATOR)].CardId;

                        _trackPlayerCards.Add(e.Id, tempInfo);
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
            try
            {
                CardInfo temp = new CardInfo();

                temp.cardId = "HERO_POWER";
                temp.cardName = "Hero Power";
                temp.turn = Convert.ToInt16(Math.Ceiling(Core.Game.GameEntity.GetTag(GameTag.TURN) / 2.0));
                temp.mana = Core.Game.OpponentEntity.GetTag(GameTag.RESOURCES);
                temp.manaOverload = Core.Game.OpponentEntity.GetTag(GameTag.OVERLOAD_OWED);
                temp.activePlayer = "Opponent";

                _trackOpponentCards.Add((_opponentTurnCount * 500), temp);
                /*
                _cardsPlayed.Add("HERO_POWER", Convert.ToInt16(Math.Ceiling(Core.Game.GameEntity.GetTag(GameTag.TURN) / 2.0)), false, -1, false, -1,
                    Core.Game.MatchInfo.OpposingPlayer.StandardRank, Core.Game.MatchInfo.OpposingPlayer.StandardLegendRank, Core.Game.MatchInfo.LocalPlayer.StandardRank,
                    Core.Game.MatchInfo.LocalPlayer.StandardLegendRank, Core.Game.MatchInfo.RankedSeasonId,
                    "Hero Power", Core.Game.OpponentEntity.GetTag(GameTag.RESOURCES), Core.Game.OpponentEntity.GetTag(GameTag.OVERLOAD_OWED), "Opponent");
                */
                MetaLog.Info("Turn " + _opponentTurnCount + ": Opponent Hero Power", "OpponentHeroPower");
            }
            catch(Exception ex)
            {
                MetaLog.Error(ex);
            }
        }

        public void PlayerHeroPower()
        {
            try
            {
                CardInfo temp = new CardInfo();

                temp.cardId = "HERO_POWER";
                temp.cardName = "Hero Power";
                temp.turn = Convert.ToInt16(Math.Ceiling(Core.Game.GameEntity.GetTag(GameTag.TURN) / 2.0));
                temp.mana = Core.Game.OpponentEntity.GetTag(GameTag.RESOURCES);
                temp.manaOverload = Core.Game.OpponentEntity.GetTag(GameTag.OVERLOAD_OWED);
                temp.activePlayer = "Player";

                _trackPlayerCards.Add((_playerTurnCount * 500), temp);

                /*_cardsPlayed.Add("HERO_POWER", Convert.ToInt16(Math.Ceiling(Core.Game.GameEntity.GetTag(GameTag.TURN) / 2.0)), false, -1, false, -1,
                    Core.Game.MatchInfo.OpposingPlayer.StandardRank, Core.Game.MatchInfo.OpposingPlayer.StandardLegendRank, Core.Game.MatchInfo.LocalPlayer.StandardRank,
                    Core.Game.MatchInfo.LocalPlayer.StandardLegendRank, Core.Game.MatchInfo.RankedSeasonId,
                    "Hero Power", Core.Game.PlayerEntity.GetTag(GameTag.RESOURCES), Core.Game.PlayerEntity.GetTag(GameTag.OVERLOAD_OWED), "Player");
                */
                MetaLog.Info("Turn " + _playerTurnCount + ": Player Hero Power", "PlayerHeroPower");
            }
            catch(Exception ex)
            {
                MetaLog.Error(ex);
            }
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
                        _trackOpponentCards[x.Id].turnToGraveyard = x.Info.Turn;
                        _trackOpponentCards[x.Id].cardName = x.Card.Name;
                    }

                }

                foreach (var x in Core.Game.Player.Graveyard.Where(x => !x.IsHero && !x.IsHeroPower && x.CardId != ""))
                {
                    CardInfo temp;
                    if (_trackPlayerCards.TryGetValue(x.Id, out temp))
                    {
                        _trackPlayerCards[x.Id].turnToGraveyard = x.Info.Turn;
                        _trackPlayerCards[x.Id].cardName = x.Card.Name;
                    }
                }

                GameInfo tempGameInfo = new GameInfo();
                var standard = Core.Game.CurrentFormat == Format.Standard;

                tempGameInfo.userKey = _appConfig.userKey;
                tempGameInfo.gameId = null;
                tempGameInfo.gameFormat = Core.Game.CurrentFormat.ToString();
                tempGameInfo.gameMode = Core.Game.CurrentGameMode.ToString();
                tempGameInfo.region = Core.Game.CurrentRegion.ToString();
                tempGameInfo.rankedSeasonId = Core.Game.MatchInfo.RankedSeasonId;
                tempGameInfo.playerClass = Core.Game.CurrentGameStats.PlayerHero;
                tempGameInfo.playerRank = standard ? Core.Game.MatchInfo.LocalPlayer.StandardRank : Core.Game.MatchInfo.LocalPlayer.WildRank;
                tempGameInfo.playerLegendRank = standard ? Core.Game.MatchInfo.LocalPlayer.StandardLegendRank : Core.Game.MatchInfo.LocalPlayer.WildLegendRank;
                tempGameInfo.opponentClass = Core.Game.CurrentGameStats.OpponentHero;
                tempGameInfo.opponentRank = standard ? Core.Game.MatchInfo.OpposingPlayer.StandardRank : Core.Game.MatchInfo.OpposingPlayer.WildRank;
                tempGameInfo.opponentLegendRank = standard ? Core.Game.MatchInfo.OpposingPlayer.StandardLegendRank : Core.Game.MatchInfo.OpposingPlayer.WildLegendRank;
                tempGameInfo.opponentCoin = Core.Game.Opponent.HasCoin;

                if (Core.Game.CurrentGameStats.Result == GameResult.Win)
                {
                    tempGameInfo.playerWin = true;
                    tempGameInfo.opponentWin = false;
                }
                else if (Core.Game.CurrentGameStats.Result == GameResult.Loss)
                {
                    tempGameInfo.playerWin = false;
                    tempGameInfo.opponentWin = true;
                }
                else if (Core.Game.CurrentGameStats.Result == GameResult.Draw)
                {
                    tempGameInfo.playerWin = false;
                    tempGameInfo.opponentWin = false;
                }


                //var ActivePlayerDeck = DeckList.Instance.ActiveDeck.Cards.Select(x => x.Id).ToArray();
                List<string> playerCards;

                try
                {
                    if (DeckList.Instance.ActiveDeck != null)
                    {
                        playerCards = DeckList.Instance.ActiveDeck.Cards.Where(x => x.Count == 2).Select(x => x.Id).ToList();
                        playerCards.AddRange(Core.Game.Player.PlayerCardList.Select(x => x.Id).ToList());
                        tempGameInfo.playerCards = String.Join(",", playerCards.Select(x => x.ToString()).ToArray());
                    }
                }
                catch{ }

                var opponentCards = Core.Game.Opponent.OpponentCardList.Where(x => !x.IsCreated).Select(x => x.Id).ToList();
                //opponentCards.AddRange(Core.Game.Opponent.OpponentCardList.Where(x => !x.IsCreated).Select(x => x.Id).ToList());

                tempGameInfo.opponentCards = String.Join(",", opponentCards.Select(x => x.ToString()).ToArray());

                GameStats tempStats = new GameStats();

                tempStats.gameInfo = tempGameInfo;

                tempStats.cardsPlayed.AddRange(_trackOpponentCards.Values.Where(x => x.cardId != "" && x.cardId != null).OrderBy(x => x.turn).ToList());
                tempStats.cardsPlayed.AddRange(_trackPlayerCards.Values.Where(x => x.cardId != "" && x.cardId != null).OrderBy(x => x.turn).ToList());



                //_cardsPlayed.Clear();
                /*
                foreach (CardInfo x in _trackOpponentCards.Values.Where(x => x.cardId != "" && x.cardId != null).OrderBy(x => x.turnCardPlayed).ToList())
                {
                    tempTrackCards.Add(new trackCards(x.cardId, ));
                    _cardsPlayed.Add(x.cardId, x.turnCardPlayed, x.created, x.turnInHand, x.mulligan,
                        x.turnCardDied,
                        Core.Game.MatchInfo.OpposingPlayer.StandardRank, Core.Game.MatchInfo.OpposingPlayer.StandardLegendRank, 
                        Core.Game.MatchInfo.LocalPlayer.StandardRank,
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
                    */

                tempStats.Save();
                await tempStats.sendCardStats();
            }
            catch (Exception ex)
            {
                MetaLog.Error(ex);
            }
        }

        /*internal async Task<string> sendCardStats()
        {
            try
            {
                if (_validGameMode)
                {
                    {
                        MetaLog.Info("Uploading Game to MetaStats.net ...", "sendRequest");

                        string url = "http://metastats.net/metadetector/cards.php?a=Stats&v=0.0.2";


                        string postData = _cardsPlayed.GetCardStats();

                        if (postData != "")
                        {

                            WebClient client = new WebClient();
                            byte[] data = Encoding.UTF8.GetBytes(postData);
                            Uri uri = new Uri(url);
                            var response = Encoding.UTF8.GetString(await client.UploadDataTaskAsync(uri, "POST", data));

                            _appConfig.lastUpload = DateTime.Now;
                            _appConfig.Save();

                            MetaLog.Info("Game Upload Done", "sendRequest");

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
        }*/
    }

    public class CardInfo
    {
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

        public CardInfo()
        {
            cardId = "";
            cardName = "";
            turn = -1;
            turnDrawn = -1;
            turnToGraveyard = -1;
            mulligan = false;
            mana = -1;
            manaOverload = -1;
            isCreated = false;
            createdBy = "";
            activePlayer = "";
        }
    }

    public class GameStats
    {
        private static string statsDirectory = Path.Combine(Config.AppDataPath, "MetaStats");
        private static string statsPath = Path.Combine(statsDirectory, "gameStats.xml");

        public GameInfo gameInfo = new GameInfo();
        public List<CardInfo> cardsPlayed = new List<CardInfo>();
        

        public string GetCardStats()
        {
            try
            {
                var serializer = new XmlSerializer(typeof(GameStats));

                using (StringWriter textWriter = new StringWriter())
                {
                    serializer.Serialize(textWriter, this);
                    return textWriter.ToString();
                }
            }
            catch (Exception ex)
            {
                MetaLog.Error(ex);
                return "";
            }
        }

        public void Save()
        {
            try
            {
                this.cardsPlayed = cardsPlayed.OrderBy(x => x.turn).ThenBy(x => x.activePlayer).ToList();
                if (!Directory.Exists(statsDirectory))
                    Directory.CreateDirectory(statsDirectory);

                var serializer = new XmlSerializer(typeof(GameStats));
                using (var writer = new StreamWriter(statsPath))
                    serializer.Serialize(writer, this);
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
                MetaLog.Info("Uploading Game to MetaStats.net ...", "sendRequest");

                string url = "http://metastats.net/metadetector/stats.php?a=Stats&v=0.0.2";


                string postData = GetCardStats();

                if (postData != "")
                {

                    WebClient client = new WebClient();
                    byte[] data = Encoding.UTF8.GetBytes(postData);
                    //byte[] data = Compress.Zip(postData);
                    Uri uri = new Uri(url);
                    var response = Encoding.UTF8.GetString(await client.UploadDataTaskAsync(uri, "POST", data));

                    MetaLog.Info("Game Upload Done", "sendRequest");

                    return response;
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

    public class GameInfo
    {
        public string userKey { get; set; }
        public string gameId { get; set; }
        public string gameFormat { get; set; }
        public string gameMode { get; set; }
        public string region { get; set; }
        public int rankedSeasonId { get; set; }
        public string playerClass { get; set; }
        public int playerRank { get; set; }
        public int playerLegendRank { get; set; }
        public bool playerWin { get; set; }
        public int opponentRank { get; set; }
        public string opponentClass { get; set; }
        public int opponentLegendRank { get; set; }
        public bool opponentCoin { get; set; }
        public bool opponentWin { get; set; }
        public string playerCards { get; set; }
        public string opponentCards { get; set; }
    }
}
