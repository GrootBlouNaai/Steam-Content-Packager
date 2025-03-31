using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Threading;
using SteamContentPackager.UI.Controls;
using SteamContentPackager.Utils;
using SteamKit2;
using SteamKit2.Internal;

namespace SteamContentPackager.Steam;

public static class SteamSession
{
	public static SteamClient SteamClient;

	public static SteamUser SteamUser;

	public static SteamApps SteamApps;

	public static CallbackManager CallbackManager;

	public static CDNClientPool CdnClientPool;

	public static bool IsRunning;

	public static bool LoggedOn;

	private static Thread _callbackThread;

	public static Dictionary<uint, ulong> AppTokens { get; set; } = new Dictionary<uint, ulong>();

	public static Dictionary<uint, byte[]> AppTickets { get; set; } = new Dictionary<uint, byte[]>();

	public static Dictionary<Tuple<uint, string>, CDNAuthTokenCallback> CdnAuthTokens { get; set; } = new Dictionary<Tuple<uint, string>, CDNAuthTokenCallback>();

	public static void Init()
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		//IL_001d: Expected O, but got Unknown
		//IL_0022: Unknown result type (might be due to invalid IL or missing references)
		//IL_002c: Expected O, but got Unknown
		SteamClient = new SteamClient(ProtocolType.Tcp)
		{
			ConnectionTimeout = TimeSpan.FromMilliseconds(Settings.ConnectionTimeout)
		};
		CallbackManager = new CallbackManager(SteamClient);
		SteamUser = SteamClient.GetHandler<SteamUser>();
		SteamApps = SteamClient.GetHandler<SteamApps>();
		CallbackManager.Subscribe<DisconnectedCallback>((Action<DisconnectedCallback>)OnDisconnected);
		CallbackManager.Subscribe<ConnectedCallback>((Action<ConnectedCallback>)OnConnected);
		CallbackManager.Subscribe<LoginKeyCallback>((Action<LoginKeyCallback>)LoginKeyRecieved);
		CallbackManager.Subscribe<UpdateMachineAuthCallback>((Action<UpdateMachineAuthCallback>)OnMachineAuth);
		CallbackManager.Subscribe<LoggedOnCallback>((Action<LoggedOnCallback>)CallbackFunc);
		_callbackThread = new Thread(RunCallbacks)
		{
			IsBackground = true
		};
		_callbackThread.Start();
	}

	private static void CallbackFunc(LoggedOnCallback loggedOnCallback)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Invalid comparison between Unknown and I4
		LoggedOn = (int)loggedOnCallback.Result == 1;
	}

	private static void RunCallbacks()
	{
		while (true)
		{
			CallbackManager.RunWaitCallbacks(TimeSpan.FromSeconds(1.0));
		}
	}

	public static void Connect()
	{
		IsRunning = true;
		IPEndPoint iPEndPoint = (string.IsNullOrEmpty(Settings.ServerAddress) ? null : CreateIpEndPoint(Settings.ServerAddress));
		if (string.IsNullOrEmpty(Settings.ServerAddress))
		{
			((CMClient)SteamClient).Connect((IPEndPoint)null);
		}
		else
		{
			((CMClient)SteamClient).Connect(iPEndPoint);
		}
	}

	public static void Disconnect()
	{
		if (IsRunning)
		{
			IsRunning = false;
			SteamUser.LogOff();
			Thread.Sleep(500);
		}
	}

	public static void Logon(string username, string password)
	{
		//IL_001a: Unknown result type (might be due to invalid IL or missing references)
		//IL_001f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0026: Unknown result type (might be due to invalid IL or missing references)
		//IL_002d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0038: Unknown result type (might be due to invalid IL or missing references)
		//IL_0043: Unknown result type (might be due to invalid IL or missing references)
		//IL_0053: Unknown result type (might be due to invalid IL or missing references)
		//IL_005a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0065: Unknown result type (might be due to invalid IL or missing references)
		//IL_007c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0088: Expected O, but got Unknown
		((CMClient)SteamClient).CellID = Settings.CellId;
		Random random = new Random();
		LogOnDetails val = new LogOnDetails
		{
			Username = username,
			Password = password,
			AuthCode = Settings.SteamGuardCode,
			TwoFactorCode = Settings.TwoFactorCode,
			SentryFileHash = StringToByteArray(Settings.SentryHash),
			ShouldRememberPassword = true,
			LoginKey = Settings.LoginKey,
			LoginID = (uint)random.Next(1, 9999999),
			CellID = Settings.CellId
		};
		SteamUser.LogOn(val);
	}

	public static void LogOnAnonymous()
	{
		//IL_0019: Unknown result type (might be due to invalid IL or missing references)
		//IL_001e: Unknown result type (might be due to invalid IL or missing references)
		//IL_002e: Expected O, but got Unknown
		((CMClient)SteamClient).CellID = Settings.CellId;
		SteamUser.LogOnAnonymous(new AnonymousLogOnDetails
		{
			CellID = Settings.CellId
		});
	}

	private static byte[] StringToByteArray(string hex)
	{
		if (string.IsNullOrEmpty(hex))
		{
			return null;
		}
		hex = hex.Replace("-", "");
		return (from x in Enumerable.Range(0, hex.Length)
			where x % 2 == 0
			select Convert.ToByte(hex.Substring(x, 2), 16)).ToArray();
	}

	private static IPEndPoint CreateIpEndPoint(string endPoint)
	{
		if (string.IsNullOrEmpty(endPoint))
		{
			return null;
		}
		string[] array = endPoint.Split(':');
		if (array.Length != 2)
		{
			throw new FormatException("Invalid endpoint format");
		}
		if (!IPAddress.TryParse(array[0], out var address))
		{
			throw new FormatException("Invalid ip-adress");
		}
		if (!int.TryParse(array[1], NumberStyles.None, NumberFormatInfo.CurrentInfo, out var result))
		{
			throw new FormatException("Invalid port");
		}
		return new IPEndPoint(address, result);
	}

	private static void OnMachineAuth(UpdateMachineAuthCallback callback)
	{
		//IL_0085: Unknown result type (might be due to invalid IL or missing references)
		//IL_008a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0096: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ae: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c8: Unknown result type (might be due to invalid IL or missing references)
		//IL_00cf: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ec: Expected O, but got Unknown
		int fileSize;
		byte[] array;
		using (FileStream fileStream = File.Open(callback.FileName, FileMode.OpenOrCreate, FileAccess.ReadWrite))
		{
			fileStream.Seek(callback.Offset, SeekOrigin.Begin);
			fileStream.Write(callback.Data, 0, callback.BytesToWrite);
			fileSize = (int)fileStream.Length;
			fileStream.Seek(0L, SeekOrigin.Begin);
			using SHA1CryptoServiceProvider sHA1CryptoServiceProvider = new SHA1CryptoServiceProvider();
			array = sHA1CryptoServiceProvider.ComputeHash(fileStream);
			Settings.SentryHash = BitConverter.ToString(array).Replace("-", "");
		}
		SteamUser.SendMachineAuthResponse(new MachineAuthDetails
		{
			JobID = ((CallbackMsg)callback).JobID,
			FileName = callback.FileName,
			BytesWritten = callback.BytesToWrite,
			FileSize = fileSize,
			Offset = callback.Offset,
			Result = (EResult)1,
			LastError = 0,
			OneTimePassword = OTPDetails.op_Implicit(callback.OneTimePassword),
			SentryFileHash = array
		});
	}

	private static void LoginKeyRecieved(LoginKeyCallback loginKeyCallback)
	{
		Logger.WriteEntry("LoginKey recieved", LogLevel.Debug, writeToFile: false);
		Settings.LoginKey = loginKeyCallback.LoginKey;
	}

	private static void OnConnected(ConnectedCallback connectedCallback)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Invalid comparison between Unknown and I4
		if ((int)connectedCallback.Result != 1)
		{
			IsRunning = false;
			return;
		}
		Logger.WriteEntry("Connected to steam", LogLevel.Debug, writeToFile: false);
		CdnClientPool = new CDNClientPool();
	}

	private static void OnDisconnected(DisconnectedCallback callback)
	{
		LoggedOn = false;
		_ = callback.UserInitiated;
		IsRunning = false;
	}
}
