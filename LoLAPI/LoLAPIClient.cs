using CloudBroGaming.LoLAPI.Riot.Kudos;
using CloudBroGaming.LoLAPI.Riot.Platform;
using CloudBroGaming.LoLAPI.Riot.Team;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RtmpSharp.IO;
using RtmpSharp.Messaging;
using RtmpSharp.Net;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Threading.Tasks;
using System.Timers;
using System.Web;

namespace CloudBroGaming.LoLAPI
{
    public class LoLAPIClient
    {

        private ILoLCredential credentials;
        private RtmpClient rtmpClient;
        private String loginToken = "";
        private Session clientSession;
        private LoginDataPacket loginPacket;
        private bool _loggedIn = false;
        private int failedLogins = 0;
        private Timer failedLoginTimer;
        private int heartbeatCount = 0;
        private Timer heartbeatTimer;
        private const Double loginRetryPeriod = 300000; //5 Minutes before retry login
        private const Double loginRetryLongDelay = 3600000; //60 Minutes for long retry period
        private const int Retries = 5;
        private ArrayList notifications = new ArrayList();

        public LoLAPIClient(ILoLCredential credentials)
        {
            this.credentials = credentials;
            failedLoginTimer = new Timer(); //Use the failedLoginTimer to save memory
            failedLoginTimer.AutoReset = false;
            failedLoginTimer.Interval = 50;
            failedLoginTimer.Elapsed += new ElapsedEventHandler((a, b) => AttemptLogin());
            failedLoginTimer.Start(); //Schedule a login for 50ms from now
        }

        public LoLAPIClient(ILoLCredential credentials, ReadyStateChangeHandler readyHandler) : this(credentials)
        {
            OnReadyStateChange += readyHandler;
        }

        #region HeartbeatMethods

        internal void beginHeartbeat()
        {
            heartbeatTimer = new Timer();
            heartbeatTimer.AutoReset = true;
            heartbeatTimer.Interval = 120000; //2 minutes
            heartbeatTimer.Elapsed += new ElapsedEventHandler(doHeartbeat);
            heartbeatTimer.Start();
        }

        internal void endHeartbeat()
        {
            heartbeatTimer.Stop();
            heartbeatTimer = null;
        }

        private void doHeartbeat(object sender, ElapsedEventArgs e)
        {
            if (loggedIn)
            {
                InvokeAsync<string>("loginService", "performLCDSHeartBeat", Convert.ToInt32(loginPacket.AllSummonerData.Summoner.AcctId), clientSession.Token, ++heartbeatCount, DateTime.Now.ToString("ddd MMM d yyyy HH:mm:ss 'GMT-0700'"));
                Console.WriteLine("Performed Heartbeat for " + credentials.Username + " (" + credentials.Region.ToString() + ")");
            }
        }

        #endregion

        #region SerializationContext

        private SerializationContext RegisterSerializationContext()
        {
            var context = new SerializationContext();

            Assembly thisAssembly = Assembly.GetExecutingAssembly();

            var x = thisAssembly.GetTypes().Where(t => String.Equals(t.Namespace, "CloudBroGaming.LoLAPI.Riot.Platform", StringComparison.Ordinal));
            foreach (Type Platform in x)
                context.Register(Platform);

            x = thisAssembly.GetTypes().Where(t => String.Equals(t.Namespace, "CloudBroGaming.LoLAPI.Riot.Leagues", StringComparison.Ordinal));
            foreach (Type League in x)
                context.Register(League);

            x = thisAssembly.GetTypes().Where(t => String.Equals(t.Namespace, "CloudBroGaming.LoLAPI.Riot.Team", StringComparison.Ordinal));
            foreach (Type Team in x)
                context.Register(Team);

            context.Register(typeof(PendingKudosDTO));
            context.RegisterAlias(typeof(Icon), "com.riotgames.platform.summoner.icon.SummonerIcon", true);
            context.RegisterAlias(typeof(StoreAccountBalanceNotification), "com.riotgames.platform.messaging.StoreAccountBalanceNotification", true);

            //Hack to make aram work
            context.RegisterAlias(typeof(PlayerParticipant), "com.riotgames.platform.reroll.pojo.AramPlayerParticipant", true);

            return context;
        }

        #endregion

        #region LoginMethods

        private void AttemptLogin()
        {
            try
            {
                Login();
            }
            catch (Exception)
            {
                failedLogins++;
                failedLoginTimer = new Timer();
                failedLoginTimer.AutoReset = false;
                failedLoginTimer.Elapsed += new ElapsedEventHandler((a, b) => { AttemptLogin(); });
                failedLoginTimer.Interval = failedLogins >= 5 ? loginRetryLongDelay : loginRetryPeriod;
                failedLoginTimer.Start();
                return;
            }
            failedLogins = 0;
        }

        internal void Login()
        {
            if (!loggedIn)
            {
                this.rtmpClient = new RtmpClient(new Uri(credentials.Region.getAddress()), RegisterSerializationContext(), ObjectEncoding.Amf3);
                rtmpClient.CallbackException += CallbackException;
                rtmpClient.MessageReceived += OnMessageReceived;
                rtmpClient.Disconnected += OnDisconnect;
                using (var webclient = new WebClient())
                {
                login:
                    String payload = "user=" + credentials.Username.ToLower() + ",password=" + credentials.Password;
                    String queueResponse = webclient.UploadString(credentials.Region.getLoginQueueAddress() + "/login-queue/rest/queue/authenticate", payload);
                    var queue = JsonConvert.DeserializeObject(queueResponse) as JObject;
                    String status = (String)queue["status"];
                    if (status == null || status == "") throw new Exception("FATAL ERROR");
                    if (status == "LOGIN")
                    {
                        String token = (String)queue["token"];
                        if (token == null || token == "") throw new Exception("FATAL ERROR");
                        this.loginToken = token;
                        RtmpConnect();
                        return;
                    }
                    else if (status == "QUEUE")
                    {
                        var delay = (int)queue["delay"];
                        var node = (int)queue["node"];
                        foreach (var ticker in (queue["tickers"] as JArray))
                        {
                            if ((int)ticker["node"] == node)
                            {
                                Console.WriteLine("Currently #" + ((int)ticker["id"] - (int)ticker["current"]) + " in Queue for " + credentials.Region.ToString() + " with Account: " + credentials.Username);
                                break;
                            }
                        }
                        Task.Delay(delay / 2).Wait();
                        goto login;
                    }
                }
            }
        }



        internal void RtmpConnect()
        {
            Console.WriteLine("Connecting to RTMPS with Token: " + loginToken);
            var authLoginPacket = new AuthenticationCredentials();
            authLoginPacket.Username = credentials.Username;
            authLoginPacket.Password = credentials.Password;
            authLoginPacket.Domain = "lolclient.lol.riotgames.com";
            authLoginPacket.Locale = "en_US";
            authLoginPacket.ClientVersion = ClientVersion.Get(credentials.Region);
            authLoginPacket.IpAddress = GetIPAddress();
            authLoginPacket.AuthToken = loginToken;
            rtmpClient.ConnectAsync().Wait();
            var loginTask = InvokeAsync<Session>("loginService", "login", authLoginPacket);
            loginTask.Wait();
            clientSession = loginTask.Result;
            rtmpClient.SubscribeAsync("my-rtmps", "messagingDestination", "bc", "bc-" + clientSession.AccountSummary.AccountId.ToString()).Wait();
            rtmpClient.SubscribeAsync("my-rtmps", "messagingDestination", "gn-" + clientSession.AccountSummary.AccountId.ToString(), "gn-" + clientSession.AccountSummary.AccountId.ToString()).Wait();
            rtmpClient.SubscribeAsync("my-rtmps", "messagingDestination", "cn-" + clientSession.AccountSummary.AccountId.ToString(), "cn-" + clientSession.AccountSummary.AccountId.ToString()).Wait();
            RtmpLogin();
            var loginPacketTask = InvokeAsync<LoginDataPacket>("clientFacadeService", "getLoginDataPacketForUser");
            loginPacketTask.Wait();
            this.loginPacket = loginPacketTask.Result;
            if (loginPacket.AllSummonerData == null) SetSummonerNameForNewAccount();
            notifications = loginPacket.BroadcastNotification.broadcastMessages;
            var stateTask = InvokeAsync<String>("accountService", "getAccountStateForCurrentSession");
            stateTask.Wait();
            String state = stateTask.Result;
            if (state != "ENABLED")
            {
                if (loggedIn) RtmpLogout();
                return; //Keep the client but mark it as logged out
            }
            beginHeartbeat();
        }

        #endregion

        public Task<T> InvokeAsync<T>(string destination, string method, params object[] argument)
        {
            return rtmpClient.InvokeAsync<T>("my-rtmps", destination, method, argument);
        }

        #region PublicCalls

        /// <summary>
        /// Get the summoner names for an array of Summoner IDs.
        /// </summary>
        /// <param name="SummonerIds">Array of Summoner IDs</param>
        /// <returns>Returns an array of Summoner Names</returns>
        public Task<String[]> GetSummonerNames(Double[] SummonerIds)
        {
            return InvokeAsync<String[]>("summonerService", "getSummonerNames", SummonerIds);
        }

        public Task<ArrayList> GetLeagueNotifications()
        {
            var tsc = new TaskCompletionSource<ArrayList>();
            tsc.SetResult(notifications);
            return tsc.Task;
        }

        /// <summary>
        /// Finds a player by Summoner Id
        /// </summary>
        /// <param name="SummonerId">The summoner id</param>
        /// <returns>Returns the information for a player</returns>
        public Task<PlayerDTO> FindPlayer(Double SummonerId)
        {
            return InvokeAsync<PlayerDTO>("summonerTeamService", "findPlayer", SummonerId);
        }

        /// <summary>
        /// Gets summoner by name
        /// </summary>
        /// <param name="SummonerName">The name of the summoner</param>
        /// <returns>Returns the summoner</returns>
        public Task<PublicSummoner> GetSummonerByName(String SummonerName)
        {
            return InvokeAsync<PublicSummoner>("summonerService", "getSummonerByName", SummonerName);
        }

        /// <summary>
        /// Gets the public summoner data by account id
        /// </summary>
        /// <param name="AccountId">The account id</param>
        /// <returns>Returns all the public summoner data for an account</returns>
        public Task<AllPublicSummonerDataDTO> GetAllPublicSummonerDataByAccount(Double AccountId)
        {
            return InvokeAsync<AllPublicSummonerDataDTO>("summonerService", "getAllPublicSummonerDataByAccount", AccountId);
        }

        /// <summary>
        /// Gets the players overall stats
        /// </summary>
        /// <param name="AccountId">The account id</param>
        /// <param name="Season">The season you want to retrieve stats from</param>
        /// <returns>Returns the player stats for a season</returns>
        public Task<PlayerLifetimeStats> RetrievePlayerStatsByAccountId(Double AccountId, String Season)
        {
            return InvokeAsync<PlayerLifetimeStats>("playerStatsService", "retrievePlayerStatsByAccountId", AccountId, Season);
        }

        /// <summary>
        /// Gets the top 3 played champions for a player
        /// </summary>
        /// <param name="AccountId">The account id</param>
        /// <param name="GameMode">The game mode</param>
        /// <returns>Returns an array of the top 3 champions</returns>
        public Task<ChampionStatInfo[]> RetrieveTopPlayedChampions(Double AccountId, String GameMode)
        {
            return InvokeAsync<ChampionStatInfo[]>("playerStatsService", "retrieveTopPlayedChampions", AccountId, GameMode);
        }

        /// <summary>
        /// Gets the aggregated stats of a players ranked games
        /// </summary>
        /// <param name="SummonerId">The summoner id of a player</param>
        /// <param name="GameMode">The game mode requested</param>
        /// <param name="Season">The season you want to retrieve stats from</param>
        /// <returns>Returns the aggregated stats requested</returns>
        public Task<AggregatedStats> GetAggregatedStats(Double SummonerId, String GameMode, String Season)
        {
            return InvokeAsync<AggregatedStats>("playerStatsService", "getAggregatedStats", SummonerId, GameMode, Season);
        }

        /// <summary>
        /// Gets the top 10 recent games for a player
        /// </summary>
        /// <param name="AccountId">The account id of a player</param>
        /// <returns>Returns the recent games for a player</returns>
        public Task<RecentGames> GetRecentGames(Double AccountId)
        {
            return InvokeAsync<RecentGames>("playerStatsService", "getRecentGames", AccountId);
        }

        /// <summary>
        /// Gets the end of game stats for a team for any game
        /// </summary>
        /// <param name="TeamId">The team id</param>
        /// <param name="GameId">The game id</param>
        /// <returns>Returns the end of game stats for a game</returns>
        public Task<EndOfGameStats> GetRankedTeamEndOfGameStats(TeamId TeamId, long GameId)
        {
            return InvokeAsync<EndOfGameStats>("playerStatsService", "getTeamEndOfGameStats", TeamId, GameId);
        }

        /// <summary>
        /// Gets all the practice games
        /// </summary>
        /// <returns>Returns an array of practice games</returns>
        public Task<PracticeGameSearchResult[]> ListAllPracticeGames()
        {
            return InvokeAsync<PracticeGameSearchResult[]>("gameService", "listAllPracticeGames");
        }

        /// <summary>
        /// Get a Ranked Team by their Team ID
        /// </summary>
        /// <param name="TeamId"></param>
        /// <returns>The Teams Information</returns>
        public Task<TeamDTO> GetTeamById(TeamId TeamId)
        {
            return InvokeAsync<TeamDTO>("summonerTeamService", "findTeamById", TeamId);
        }

        /// <summary>
        /// Get the Spectator information for an In-Progress Game by the Summoner Name of 1 of the players
        /// </summary>
        /// <param name="summonerName"></param>
        /// <returns>Spectator and Misc Info about the game in question</returns>
        public Task<PlatformGameLifecycleDTO> RetrieveInProgressGameInfo(String summonerName)
        {
            return InvokeAsync<PlatformGameLifecycleDTO>("gameService", "retrieveInProgressSpectatorGameInfo", summonerName);
        }

        #endregion

        #region UtilityMethods

        private String GetIPAddress()
        {
            String ipJSON;
            using (var wc = new WebClient())
            {
                ipJSON = wc.DownloadString("http://ll.leagueoflegends.com/services/connection_info");
            }
            var ip = JsonConvert.DeserializeObject(ipJSON) as JObject;
            return (String)ip["ip_address"];
        }

        private void SetSummonerNameForNewAccount()
        {
            int i = 1;
            while (i < int.MaxValue)
            {
                Console.WriteLine("Attempting to Set Summoner Name");
                var res = InvokeAsync<AllSummonerData>("summonerService", "createDefaultSummoner", "CBG API " + i);
                res.Wait();
                if (res.Result != null)
                {
                    loginPacket.AllSummonerData = res.Result;
                    InvokeAsync<dynamic>("summonerService", "updateProfileIconId", 28).Wait();
                    InvokeAsync<dynamic>("playerStatsService", "processEloQuestionaire", "EXPERT").Wait();
                    InvokeAsync<dynamic>("summonerService", "saveSeenTutorialFlag").Wait();
                    return;
                }
                i++;
            }
        }

        public ILoLCredential GetCredentials()
        {
            return credentials;
        }

        private void RtmpLogin()
        {
            if (!loggedIn)
            {
                Task<bool> res = rtmpClient.LoginAsync(credentials.Username.ToLower(), clientSession.Token);
                res.Wait();
                loggedIn = res.Result;
            }
        }

        private void RtmpLogout()
        {
            if (loggedIn)
            {
                rtmpClient.LogoutAsync().Wait();
                loggedIn = false;
            }
        }

        #endregion

        #region Events

        private void OnMessageReceived(object sender, MessageReceivedEventArgs message)
        {
            if (message.Body is BroadcastNotification)
            {
                notifications = ((BroadcastNotification)message.Body).broadcastMessages;
            }
            Console.WriteLine(message);
        }

        private void CallbackException(object sender, Exception e)
        {
            if (e is ClientDisconnectedException)
            {
                //loggedIn = false;
                //endHeartbeat();
                //Login();
                Console.WriteLine("Got ClientDisconnectedException!!!");
            }
        }

        private void OnDisconnect(object sender, EventArgs e)
        {
            loggedIn = false;
            endHeartbeat();
            AttemptLogin();
        }
        
        public delegate void ReadyStateChangeHandler(LoLAPIClient sender, bool newState, EventArgs e);
        public event ReadyStateChangeHandler OnReadyStateChange;

        #endregion

        #region Properties

        public bool Ready
        {
            get { return loggedIn; }
        }

        public bool RegionSuspectedOffline
        {
            get { return !loggedIn && failedLogins > 5; }
        }

        private bool loggedIn
        {
            get { return _loggedIn; }
            set
            {
                if (value != _loggedIn)
                {
                    _loggedIn = value;
                    if (OnReadyStateChange != null) OnReadyStateChange(this, value, EventArgs.Empty);
                }
            }
        }

        #endregion

        ~LoLAPIClient()
        {
            endHeartbeat();
            failedLoginTimer.Stop();
            RtmpLogout();
        }
    }
}