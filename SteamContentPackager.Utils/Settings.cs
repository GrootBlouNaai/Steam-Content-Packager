using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Xml.Serialization;
using SteamContentPackager.Annotations;

namespace SteamContentPackager.Utils;

public class Settings : INotifyPropertyChanged
{
	private static SettingsFile _currentSettings = new SettingsFile();

	public static ConcurrentDictionary<string, int> ContentServerPenalty = new ConcurrentDictionary<string, int>();

	public static string ServerAddress => _currentSettings.Steam.ServerAddress;

	public static uint ConnectionTimeout => _currentSettings.Steam.ConnectionTimeout;

	public static string SteamGuardCode { get; set; }

	public static string TwoFactorCode { get; set; }

	[DefaultValue(0)]
	public static uint CellId
	{
		get
		{
			return _currentSettings.Steam.CellId;
		}
		set
		{
			_currentSettings.Steam.CellId = value;
			Save();
		}
	}

	public static string SteamPath
	{
		get
		{
			return _currentSettings.Steam.SteamPath;
		}
		set
		{
			_currentSettings.Steam.SteamPath = value;
			Save();
		}
	}

	public static string Username
	{
		get
		{
			return _currentSettings.Steam.Username;
		}
		set
		{
			_currentSettings.Steam.Username = value;
			Save();
		}
	}

	public static string Password
	{
		get
		{
			return _currentSettings.Steam.Password;
		}
		set
		{
			_currentSettings.Steam.Password = value;
			Save();
		}
	}

	public static string LoginKey
	{
		get
		{
			return _currentSettings.Steam.LoginKey;
		}
		set
		{
			_currentSettings.Steam.LoginKey = value;
			Save();
		}
	}

	public static string SentryHash
	{
		get
		{
			return _currentSettings.Steam.SentryHash;
		}
		set
		{
			_currentSettings.Steam.SentryHash = value;
			Save();
		}
	}

	public static string OutputDirectory
	{
		get
		{
			return _currentSettings.Output.OutputDirectory;
		}
		set
		{
			_currentSettings.Output.OutputDirectory = value;
			Save();
		}
	}

	public static List<string> FailedServers
	{
		get
		{
			return _currentSettings.FailedServerList;
		}
		set
		{
			_currentSettings.FailedServerList = value;
			Save();
		}
	}

	public static bool DownloadContent
	{
		get
		{
			return _currentSettings.Output.DownloadContent;
		}
		set
		{
			_currentSettings.Output.DownloadContent = value;
			Save();
		}
	}

	public static bool GenerateBBCode
	{
		get
		{
			return _currentSettings.Output.GenerateBBCode;
		}
		set
		{
			_currentSettings.Output.GenerateBBCode = value;
			Save();
		}
	}

	public static bool WriteDepotInfo
	{
		get
		{
			return _currentSettings.Output.WriteDepotInfo;
		}
		set
		{
			_currentSettings.Output.WriteDepotInfo = value;
			Save();
		}
	}

	public static string LastOwner
	{
		get
		{
			return _currentSettings.Output.LastOwner;
		}
		set
		{
			_currentSettings.Output.LastOwner = value;
			Save();
		}
	}

	public static string HosterName
	{
		get
		{
			return _currentSettings.Output.HosterName;
		}
		set
		{
			_currentSettings.Output.HosterName = value;
			Save();
		}
	}

	public static string UploaderName
	{
		get
		{
			return _currentSettings.Output.UploaderName;
		}
		set
		{
			_currentSettings.Output.UploaderName = value;
			Save();
		}
	}

	public static bool ShowAppSettings
	{
		get
		{
			return _currentSettings.ShowAppSettings;
		}
		set
		{
			_currentSettings.ShowAppSettings = value;
			Save();
		}
	}

	public static bool ShowLog
	{
		get
		{
			return _currentSettings.ShowLog;
		}
		set
		{
			_currentSettings.ShowLog = value;
			Save();
		}
	}

	public static bool ShowNotifications
	{
		get
		{
			return _currentSettings.ShowNotifications;
		}
		set
		{
			_currentSettings.ShowNotifications = value;
			Save();
		}
	}

	public static bool SlimTasks
	{
		get
		{
			return _currentSettings.SlimTasks;
		}
		set
		{
			_currentSettings.SlimTasks = value;
			Save();
		}
	}

	public static int WindowWidth
	{
		get
		{
			return _currentSettings.WindowWidth;
		}
		set
		{
			_currentSettings.WindowWidth = value;
			Save();
		}
	}

	public static int WindowHeight
	{
		get
		{
			return _currentSettings.WindowHeight;
		}
		set
		{
			_currentSettings.WindowHeight = value;
			Save();
		}
	}

	public event PropertyChangedEventHandler PropertyChanged;

	public static void Load()
	{
		string path = AppDomain.CurrentDomain.BaseDirectory + "\\settings.xml";
		if (!File.Exists(path))
		{
			_currentSettings = new SettingsFile();
			Save();
		}
		using FileStream stream = File.OpenRead(path);
		_currentSettings = (SettingsFile)new XmlSerializer(typeof(SettingsFile)).Deserialize(stream);
	}

	private static void Save()
	{
		using FileStream stream = File.Create(AppDomain.CurrentDomain.BaseDirectory + "\\settings.xml");
		new XmlSerializer(typeof(SettingsFile)).Serialize(stream, _currentSettings);
	}

	[NotifyPropertyChangedInvocator]
	protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
	{
		this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
	}
}
