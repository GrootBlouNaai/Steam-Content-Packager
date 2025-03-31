using System;
using System.IO;
using System.Xml.Serialization;

namespace SteamContentPackager;

public class Config
{
	private static Config _instance;

	private static readonly string ConfigPath = $"{AppDomain.CurrentDomain.BaseDirectory}\\Config\\settings.xml";

	private static object _writeLocker = new object();

	public uint lastPicsChangeNumber { get; set; } = 0u;

	public uint cellID { get; set; }

	public int maxConnections { get; set; } = 4;

	public int maxPICSRequests { get; set; } = 1000;

	public string lastUser { get; set; }

	public string libraryFolder { get; set; } = "downloads\\";

	public string outputDir { get; set; } = "output\\";

	public string defaultLanguage { get; set; } = "english";

	public string server { get; set; } = null;

	public bool deleteDownloads { get; set; }

	public string hosterName { get; set; }

	public string acfLastOwner { get; set; }

	public string uploaderName { get; set; }

	public bool autoSaveBbCode { get; set; }

	public bool autoSaveDepotInfo { get; set; }

	public string outputFilenameFormat { get; set; } = "{AppName} [BuildID {buildID}] ({Arch})";

	public string dateFormat { get; set; } = "dd.MM.yyyy";

	public bool runPluginsAsync { get; set; }

	public static string LibraryFolder
	{
		get
		{
			return (!Path.IsPathRooted(Instance.libraryFolder)) ? $"{AppDomain.CurrentDomain.BaseDirectory}{Instance.libraryFolder}" : Instance.libraryFolder;
		}
		set
		{
			Instance.libraryFolder = value;
		}
	}

	public static int MaxConnections
	{
		get
		{
			return Instance.maxConnections;
		}
		set
		{
			Instance.maxConnections = value;
		}
	}

	public static int MaxPICSRequests
	{
		get
		{
			return Instance.maxPICSRequests;
		}
		set
		{
			Instance.maxPICSRequests = value;
		}
	}

	public static string OutputDir
	{
		get
		{
			return (!Path.IsPathRooted(Instance.outputDir)) ? $"{AppDomain.CurrentDomain.BaseDirectory}\\{Instance.outputDir}" : Instance.outputDir;
		}
		set
		{
			Instance.outputDir = value;
		}
	}

	public static string LastUser
	{
		get
		{
			return Instance.lastUser;
		}
		set
		{
			Instance.lastUser = value;
		}
	}

	public static string OutputFilenameFormat
	{
		get
		{
			return Instance.outputFilenameFormat;
		}
		set
		{
			Instance.outputFilenameFormat = value;
		}
	}

	public static string UploaderName
	{
		get
		{
			return Instance.uploaderName;
		}
		set
		{
			Instance.uploaderName = value;
		}
	}

	public static string AcfLastOwner
	{
		get
		{
			return Instance.acfLastOwner;
		}
		set
		{
			Instance.acfLastOwner = value;
		}
	}

	public static string HosterName
	{
		get
		{
			return Instance.hosterName;
		}
		set
		{
			Instance.hosterName = value;
		}
	}

	public static string DefaultLanguage
	{
		get
		{
			return Instance.defaultLanguage;
		}
		set
		{
			Instance.defaultLanguage = value;
		}
	}

	public static string Server
	{
		get
		{
			return Instance.server;
		}
		set
		{
			Instance.server = value;
		}
	}

	public static uint CellID
	{
		get
		{
			return Instance.cellID;
		}
		set
		{
			Instance.cellID = value;
		}
	}

	public static uint LastPicsChangeNumber
	{
		get
		{
			return Instance.lastPicsChangeNumber;
		}
		set
		{
			Instance.lastPicsChangeNumber = value;
		}
	}

	public static bool DeleteDownloads
	{
		get
		{
			return Instance.deleteDownloads;
		}
		set
		{
			Instance.deleteDownloads = value;
		}
	}

	public static bool AutoSaveBbCode
	{
		get
		{
			return Instance.autoSaveBbCode;
		}
		set
		{
			Instance.autoSaveBbCode = value;
		}
	}

	public static bool AutoSaveDepotInfo
	{
		get
		{
			return Instance.autoSaveDepotInfo;
		}
		set
		{
			Instance.autoSaveDepotInfo = value;
		}
	}

	public static bool RunPluginsAsync
	{
		get
		{
			return Instance.runPluginsAsync;
		}
		set
		{
			Instance.runPluginsAsync = value;
		}
	}

	public static Config Instance
	{
		get
		{
			if (_instance == null)
			{
				_instance = Load();
			}
			return _instance;
		}
	}

	public static string DateFormat
	{
		get
		{
			return Instance.dateFormat;
		}
		set
		{
			Instance.dateFormat = value;
		}
	}

	private static Config Load()
	{
		lock (_writeLocker)
		{
			if (!File.Exists(ConfigPath))
			{
				return new Config();
			}
			using FileStream stream = File.Open(ConfigPath, FileMode.Open);
			XmlSerializer xmlSerializer = new XmlSerializer(typeof(Config));
			return (Config)xmlSerializer.Deserialize(stream);
		}
	}

	public static void Save()
	{
		lock (_writeLocker)
		{
			new FileInfo(ConfigPath).Directory?.Create();
			using FileStream stream = File.Open(ConfigPath, FileMode.Create);
			XmlSerializer xmlSerializer = new XmlSerializer(typeof(Config));
			xmlSerializer.Serialize(stream, Instance);
		}
	}
}
