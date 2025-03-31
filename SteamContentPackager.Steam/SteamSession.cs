using System;
using System.Collections.Concurrent;
using System.IO;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using SteamContentPackager.Utils;
using SteamKit2;
using SteamKit2.Discovery;
using SteamKit2.Types;

namespace SteamContentPackager.Steam;

internal static class SteamSession
{
	public static SteamClient SteamClient;

	public static SteamUser SteamUser;

	public static SteamUserStats SteamUserStats;

	public static SteamApps SteamApps;

	public static SteamFriends SteamFriends;

	public static CallbackManager CallbackManager;

	private static bool _isRunning;

	private static bool _disconnectionExpected;

	public static CDNClientPool CDNClientPool;

	private static Thread _callbackThread;

	public static UserData.User User;

	public static PICSUpdater PICSUpdater;

	public static ConcurrentDictionary<uint, byte[]> AppTickets;

	private static AppInfoCache _appInfo;

	private static PackageInfo _packageInfo;

	private static ScheduledFunction _reconnectFunction;

	public static PackageInfo PackageInfo
	{
		get
		{
			if (_packageInfo == null)
			{
				_packageInfo = new PackageInfo($"{AppDomain.CurrentDomain.BaseDirectory}\\appcache\\packageinfo.vdf");
			}
			return _packageInfo;
		}
		set
		{
			_packageInfo = value;
		}
	}

	public static AppInfoCache AppInfo
	{
		get
		{
			if (_appInfo == null)
			{
				_appInfo = new AppInfoCache($"{AppDomain.CurrentDomain.BaseDirectory}\\appcache\\appinfo.vdf");
			}
			return _appInfo;
		}
		set
		{
			_appInfo = value;
		}
	}

	static SteamSession()
	{
		AppTickets = new ConcurrentDictionary<uint, byte[]>();
		if (Config.CellID != 0)
		{
			Log.Write($"Initializing SteamClient with CellID: {Config.CellID}");
			SteamClient = new SteamClient(SteamConfiguration.Create(delegate(ISteamConfigurationBuilder builder)
			{
				builder.WithCellID(Config.CellID);
			}));
		}
		else
		{
			SteamClient = new SteamClient();
		}
		SteamUser = SteamClient.GetHandler<SteamUser>();
		SteamApps = SteamClient.GetHandler<SteamApps>();
		SteamUserStats = SteamClient.GetHandler<SteamUserStats>();
		SteamFriends = SteamClient.GetHandler<SteamFriends>();
		CallbackManager = new CallbackManager(SteamClient);
		PICSUpdater = new PICSUpdater();
		CallbackManager.Subscribe<SteamClient.ConnectedCallback>(OnConnected);
		CallbackManager.Subscribe<SteamClient.DisconnectedCallback>(OnDisconnected);
		CallbackManager.Subscribe<SteamUser.LoggedOnCallback>(OnLoggedOnCallbackReceived);
		CallbackManager.Subscribe<SteamUser.LoginKeyCallback>(OnLoginKeyReceived);
		CallbackManager.Subscribe<SteamUser.UpdateMachineAuthCallback>(OnMachineAuth);
		CDNClientPool = new CDNClientPool();
	}

	public static void Connect()
	{
		Log.Write("Connecting to steam");
		_isRunning = true;
		_callbackThread = new Thread(Start)
		{
			IsBackground = true
		};
		_callbackThread.Start();
		if (!string.IsNullOrEmpty(Config.Server))
		{
			ServerRecord cmServer = ServerRecord.CreateServer(Config.Server.Split(':')[0], Convert.ToInt32(Config.Server.Split(':')[1]), ProtocolTypes.Tcp);
			SteamClient.Connect(cmServer);
		}
		else
		{
			SteamClient.Connect();
		}
	}

	private static void Start()
	{
		while (_isRunning)
		{
			CallbackManager.RunWaitAllCallbacks(TimeSpan.FromMilliseconds(100.0));
		}
	}

	public static void Disconnect()
	{
		if (SteamClient.IsConnected)
		{
			SteamClient?.Disconnect();
		}
	}

	public static void Logon(UserData.User user)
	{
		User = user;
		_disconnectionExpected = true;
		SteamUser.LogOn(new SteamUser.LogOnDetails
		{
			Username = user.Username,
			Password = user.Password,
			SentryFileHash = user.SentryHash,
			LoginKey = user.LoginKey,
			AuthCode = user.AuthCode,
			TwoFactorCode = user.TwoFactorCode,
			ShouldRememberPassword = true,
			LoginID = 123456u
		});
	}

	private static void OnLoggedOnCallbackReceived(SteamUser.LoggedOnCallback loggedOnCallback)
	{
		if (loggedOnCallback.Result == EResult.OK)
		{
			Log.Write("Logged In");
			_disconnectionExpected = false;
		}
	}

	private static void OnConnected(SteamClient.ConnectedCallback connectedCallback)
	{
		Log.Write("Connected");
		if (User?.LoginKey != null)
		{
			Logon(User);
		}
	}

	private static void OnDisconnected(SteamClient.DisconnectedCallback disconnectedCallback)
	{
		if (_disconnectionExpected)
		{
			return;
		}
		if (disconnectedCallback.UserInitiated)
		{
			Log.Write("Disconnected");
			return;
		}
		Log.Write("Unexpectedly disconnected. Retrying connection in 5 seconds");
		_reconnectFunction = new ScheduledFunction(delegate
		{
			SteamClient.Connect();
		}, TimeSpan.FromSeconds(5.0), TimeSpan.FromMilliseconds(-1.0));
	}

	private static void OnLoginKeyReceived(SteamUser.LoginKeyCallback loginKeyCallback)
	{
		Log.Write("Login Key Received", LogLevel.Debug);
		User.LoginKey = loginKeyCallback.LoginKey;
		UserData.Save();
	}

	private static void OnMachineAuth(SteamUser.UpdateMachineAuthCallback callback)
	{
		Log.Write("Sentry Hash Received", LogLevel.Debug);
		int fileSize;
		byte[] array;
		using (MemoryStream memoryStream = new MemoryStream())
		{
			memoryStream.Seek(callback.Offset, SeekOrigin.Begin);
			memoryStream.Write(callback.Data, 0, callback.BytesToWrite);
			fileSize = (int)memoryStream.Length;
			memoryStream.Seek(0L, SeekOrigin.Begin);
			using SHA1CryptoServiceProvider sHA1CryptoServiceProvider = new SHA1CryptoServiceProvider();
			array = sHA1CryptoServiceProvider.ComputeHash(memoryStream);
			User.SentryHash = array;
			UserData.Save();
		}
		SteamUser.SendMachineAuthResponse(new SteamUser.MachineAuthDetails
		{
			JobID = callback.JobID,
			FileName = callback.FileName,
			BytesWritten = callback.BytesToWrite,
			FileSize = fileSize,
			Offset = callback.Offset,
			Result = EResult.OK,
			LastError = 0,
			OneTimePassword = callback.OneTimePassword,
			SentryFileHash = array
		});
	}

	public static async Task<bool> RequestAppTicket(uint appid)
	{
		if (AppTickets.ContainsKey(appid))
		{
			return true;
		}
		Log.Write($"Requesting AppTicket for {appid}");
		SteamApps.AppOwnershipTicketCallback appTicketCallback = await SteamApps.GetAppOwnershipTicket(appid);
		if (appTicketCallback.Result != EResult.OK)
		{
			Log.Write($"Failed to get appticket for {appid}: {appTicketCallback.Result}");
			return false;
		}
		AppTickets.TryAdd(appid, appTicketCallback.Ticket);
		return true;
	}

	public static async Task<bool> RequestDepotKey(uint depotId, uint appid)
	{
		if (DepotKeyCache.Keys.ContainsKey(depotId))
		{
			return true;
		}
		Log.Write($"Requesting DepotKey for {depotId}");
		SteamApps.DepotKeyCallback appTicketCallback = await SteamApps.GetDepotDecryptionKey(depotId, appid);
		if (appTicketCallback.Result != EResult.OK)
		{
			Log.Write($"Failed to get depotkey for {depotId}: {appTicketCallback.Result}");
			return false;
		}
		DepotKeyCache.Keys[depotId] = appTicketCallback.DepotKey;
		return true;
	}
}
