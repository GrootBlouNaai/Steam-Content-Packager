using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using SteamContentPackager.Steam;
using SteamContentPackager.UI;
using SteamContentPackager.Utils;
using SteamKit2;

namespace SteamContentPackager.Packing;

public class PackageTask_OLD : BindableBase
{
	private TaskState _state;

	private float _progress;

	private string _currentSpeed;

	private string _currentSubTask;

	public CancellationTokenSource CancellationTokenSource;

	public Dictionary<string, bool> FileDictionary = new Dictionary<string, bool>();

	private static SemaphoreSlim _pluginRateLimiter = new SemaphoreSlim(1);

	public AppConfig AppConfig { get; }

	public TaskState State
	{
		get
		{
			return _state;
		}
		set
		{
			_state = value;
			OnPropertyChanged("State");
			this.StateChanged?.Invoke(this, value);
		}
	}

	public float Progress
	{
		get
		{
			return _progress;
		}
		set
		{
			_progress = value;
			OnPropertyChanged("Progress");
		}
	}

	public string CurrentSubTask
	{
		get
		{
			return _currentSubTask;
		}
		set
		{
			_currentSubTask = value;
			OnPropertyChanged("CurrentSubTask");
		}
	}

	public RelayCommand CancelCommand => new RelayCommand(Cancel);

	public string CurrentSpeed
	{
		get
		{
			return _currentSpeed;
		}
		set
		{
			_currentSpeed = value;
			OnPropertyChanged("CurrentSpeed");
		}
	}

	public event EventHandler TaskCancelled;

	public event EventHandler<TaskState> StateChanged;

	public PackageTask_OLD(AppConfig config)
	{
		AppConfig = config.Copy();
		foreach (SteamApp.Depot item in AppConfig.Depots.Where((SteamApp.Depot x) => !x.IsChecked).ToList())
		{
			AppConfig.Depots.Remove(item);
		}
		CancellationTokenSource = new CancellationTokenSource();
	}

	private void ExecutePlugin()
	{
		_pluginRateLimiter.Wait();
		if (AppConfig.Plugin != null)
		{
			AppConfig.Plugin.Args.LibraryFolder = AppConfig.LibraryFolder;
			AppConfig.Plugin.Args.FileList = FileDictionary.Keys.ToList();
			Log.Write($"Running Plugin: {AppConfig.Plugin.Name}");
			AppConfig.Plugin.ProgressChanged += delegate(object sender, float f)
			{
				Progress = f;
			};
			if (!AppConfig.Plugin.Run())
			{
				State = (CancellationTokenSource.IsCancellationRequested ? TaskState.Cancelled : TaskState.Failed);
				return;
			}
		}
		if (Config.DeleteDownloads)
		{
			Cleanup();
		}
		State = TaskState.Complete;
		CurrentSubTask = "";
		_pluginRateLimiter.Release();
	}

	private async Task WriteBBCode(string outputFile)
	{
		if (Config.AutoSaveBbCode)
		{
			File.WriteAllText(contents: await BbCode.Generate(AppConfig.SteamApp), path: $"{outputFile} BBCode.txt");
		}
	}

	private void WriteDepotInfo(string outputFile)
	{
		if (!Config.AutoSaveDepotInfo)
		{
			return;
		}
		using StreamWriter streamWriter = File.CreateText($"{outputFile} Depots.txt");
		foreach (SteamApp.Depot depot in AppConfig.Depots)
		{
			streamWriter.WriteLine($"{depot.Id} - {depot.Name}");
		}
	}

	private void CreateFileList()
	{
		if (!AppConfig.SteamApp.Installed)
		{
			KeyValue keyValue = CreateAcf(AppConfig.SteamApp, AppConfig.Depots, AppConfig.Language);
			keyValue.SaveToFile($"{AppConfig.LibraryFolder}\\steamapps\\appmanifest_{AppConfig.SteamApp.Appid}.acf", asBinary: false);
		}
		FileDictionary[$"steamapps\\appmanifest_{AppConfig.SteamApp.Appid}.acf"] = !AppConfig.SteamApp.Installed;
		foreach (string appFile in GetAppFileList())
		{
			FileDictionary[appFile] = !AppConfig.SteamApp.Installed;
		}
	}

	private KeyValue CreateAcf(SteamApp steamApp, IList<SteamApp.Depot> depots, string language)
	{
		KeyValue keyValue = new KeyValue("AppState");
		keyValue.AddChild("appid", steamApp.Appid);
		keyValue.AddChild("Universe", 1);
		keyValue.AddChild("name", steamApp.Name);
		keyValue.AddChild("StateFlags", 4);
		keyValue.AddChild("installdir", AppConfig.AppInstallDir);
		keyValue.AddChild("LastUpdated", 0);
		keyValue.AddChild("UpdateResult", 0);
		keyValue.AddChild("SizeOnDisk", GetSizeFromDepots());
		keyValue.AddChild("buildid", AppConfig.Branch.BuildId);
		keyValue.AddChild("LastOwner", Config.AcfLastOwner);
		keyValue.AddChild("BytesToDownload", 0);
		keyValue.AddChild("BytesDownloaded", 0);
		keyValue.AddChild("AutoUpdateBehavior", 0);
		keyValue.AddChild("AllowOtherDownloadsWhileRunning", 0);
		if (language != null)
		{
			KeyValue keyValue2 = keyValue.AddChild("UserConfig");
			keyValue2.AddChild("language", language);
		}
		KeyValue keyValue3 = keyValue.AddChild("InstalledDepots");
		KeyValue keyValue4 = keyValue.AddChild("MountedDepots");
		List<FileMapping> list = new List<FileMapping>();
		foreach (SteamApp.Depot item in depots.Where((SteamApp.Depot x) => x.ParentAppId == steamApp.Appid))
		{
			Manifest manifest = new Manifest($"{AppConfig.LibraryFolder}\\depotcache\\{item.Id}_{item.ManifestId}.manifest", item.Id);
			list.AddRange(manifest.InstallScripts);
			KeyValue keyValue5 = keyValue3.AddChild(item.Id.ToString());
			keyValue5.AddChild("manifest", item.ManifestId);
			if (item.IsDlc)
			{
				keyValue5.AddChild("dlcappid", item.DlcAppId);
			}
			keyValue4.AddChild(item.Id.ToString(), item.ManifestId);
		}
		if (depots.Any((SteamApp.Depot x) => x.ParentAppId != steamApp.Appid))
		{
			List<uint> list2 = new List<uint>();
			KeyValue keyValue6 = keyValue.AddChild("SharedDepots");
			foreach (SteamApp.Depot item2 in depots.Where((SteamApp.Depot x) => x.ParentAppId != steamApp.Appid))
			{
				keyValue6.AddChild(item2.Id.ToString(), item2.ParentAppId);
				if (!list2.Contains(item2.ParentAppId))
				{
					list2.Add(item2.ParentAppId);
				}
			}
			foreach (uint item3 in list2)
			{
				SteamApp steamApp2 = new SteamApp(SteamSession.AppInfo.Items[item3].AppId, SteamSession.AppInfo.Items[item3].Name, 0u);
				KeyValue keyValue7 = CreateAcf(steamApp2, depots.Where((SteamApp.Depot x) => x.ParentAppId != AppConfig.SteamApp.Appid).ToList(), null);
				keyValue7.SaveToFile($"{AppConfig.LibraryFolder}\\steamapps\\appmanifest_{item3}.acf", asBinary: false);
				FileDictionary[$"steamapps\\appmanifest_{item3}.acf"] = true;
			}
		}
		if (list.Count > 0)
		{
			KeyValue keyValue8 = keyValue.AddChild("InstallScripts");
			foreach (FileMapping item4 in list)
			{
				keyValue8.AddChild(item4.ParentDepotId.ToString(), item4.FileName);
			}
		}
		return keyValue;
	}

	private ulong GetSizeFromDepots()
	{
		Dictionary<string, FileMapping> dictionary = new Dictionary<string, FileMapping>();
		foreach (SteamApp.Depot depot in AppConfig.Depots)
		{
			Manifest manifest = new Manifest($"{AppConfig.LibraryFolder}\\depotcache\\{depot.Id}_{depot.ManifestId}.manifest", depot.Id);
			foreach (FileMapping file in manifest.Files)
			{
				dictionary[file.FileName.ToLower()] = file;
			}
		}
		ulong num = 0uL;
		foreach (FileMapping value in dictionary.Values)
		{
			num += value.Size;
		}
		return num;
	}

	private List<string> GetAppFileList()
	{
		Dictionary<string, string> dictionary = new Dictionary<string, string>();
		foreach (SteamApp.Depot depot in AppConfig.Depots)
		{
			Manifest manifest = new Manifest($"{AppConfig.LibraryFolder}\\depotcache\\{depot.Id}_{depot.ManifestId}.manifest", depot.Id);
			foreach (FileMapping file in manifest.Files)
			{
				dictionary[file.FileName.ToLower()] = $"steamapps\\common\\{AppConfig.AppInstallDir}\\{file.FileName}";
			}
		}
		return dictionary.Values.ToList();
	}

	private string FormatFileName()
	{
		string outputFilenameFormat = Config.OutputFilenameFormat;
		SteamApp steamApp = AppConfig.SteamApp;
		string text = steamApp.Name;
		char[] invalidFileNameChars = Path.GetInvalidFileNameChars();
		foreach (char c in invalidFileNameChars)
		{
			text = text.Replace(c.ToString(), "");
		}
		outputFilenameFormat = outputFilenameFormat.Replace("{AppName}", text, StringComparison.CurrentCultureIgnoreCase);
		outputFilenameFormat = outputFilenameFormat.Replace("{AppID}", steamApp.Appid.ToString(), StringComparison.CurrentCultureIgnoreCase);
		outputFilenameFormat = outputFilenameFormat.Replace("{Branch}", AppConfig.Branch.Name, StringComparison.CurrentCultureIgnoreCase);
		outputFilenameFormat = outputFilenameFormat.Replace("{BuildId}", AppConfig.Branch.BuildId.ToString(), StringComparison.CurrentCultureIgnoreCase);
		outputFilenameFormat = outputFilenameFormat.Replace("{Arch}", AppConfig.Arch, StringComparison.CurrentCultureIgnoreCase);
		outputFilenameFormat = outputFilenameFormat.Replace("{OS}", AppConfig.OperatingSystem, StringComparison.CurrentCultureIgnoreCase);
		outputFilenameFormat = outputFilenameFormat.Replace("{Language}", AppConfig.Language, StringComparison.CurrentCultureIgnoreCase);
		outputFilenameFormat = outputFilenameFormat.Replace("{Date}", $"{DateTime.UtcNow.ToString(Config.DateFormat)}", StringComparison.CurrentCultureIgnoreCase);
		return $"{outputFilenameFormat}";
	}

	private async Task<bool> CheckDepotAccess()
	{
		int depotsDeniedAccess = 0;
		await AppConfig.Depots.ParallelForEach(async delegate(SteamApp.Depot depot)
		{
			if (!(await SteamSession.RequestDepotKey(depot.Id, depot.ParentAppId)))
			{
				if (depot.IsChecked)
				{
					depot.Subscribed = false;
					depot.IsChecked = false;
				}
				Interlocked.Increment(ref depotsDeniedAccess);
			}
		}, 4);
		DepotKeyCache.Save();
		if (depotsDeniedAccess > 0)
		{
			State = TaskState.Failed;
			Log.Write($"Access denied to {depotsDeniedAccess} depots", LogLevel.Error);
			return false;
		}
		return true;
	}

	private void CheckPaused()
	{
		while (State == TaskState.Paused && !CancellationTokenSource.IsCancellationRequested)
		{
			Thread.Sleep(100);
		}
	}

	private bool CheckCancelled()
	{
		return CancellationTokenSource.IsCancellationRequested;
	}

	private void Cancel(object o)
	{
		Log.Write((State != TaskState.Paused && State != TaskState.Running) ? $"Removing Task: {AppConfig.SteamApp.Name}" : $"Cancelling Task: {AppConfig.SteamApp.Name}");
		CancellationTokenSource?.Cancel();
		this.TaskCancelled?.Invoke(this, null);
	}

	public void Cleanup()
	{
		Log.Write("Cleanup");
		List<string> source = (from x in FileDictionary
			where x.Value
			select x.Key).ToList();
		IEnumerable<string> enumerable = source.Where((string x) => x.ToLower().Contains(".manifest"));
		foreach (string item in enumerable)
		{
			File.Delete($"{AppConfig.LibraryFolder}\\{item}");
		}
		if (Directory.Exists($"{AppConfig.LibraryFolder}\\depotcache") && !Directory.EnumerateFiles($"{AppConfig.LibraryFolder}\\depotcache").Any())
		{
			Directory.Delete($"{AppConfig.LibraryFolder}\\depotcache", recursive: true);
		}
		IEnumerable<string> enumerable2 = source.Where((string x) => x.ToLower().EndsWith($"_{AppConfig.SteamApp.Appid}.bin"));
		foreach (string item2 in enumerable2)
		{
			File.Delete($"{AppConfig.LibraryFolder}\\{item2}");
		}
		if (Directory.Exists($"{AppConfig.LibraryFolder}\\appcache") && !Directory.EnumerateFiles($"{AppConfig.LibraryFolder}\\appcache").Any())
		{
			Directory.Delete($"{AppConfig.LibraryFolder}\\appcache", recursive: true);
		}
		IEnumerable<string> enumerable3 = source.Where((string x) => x.ToLower().EndsWith($"_{AppConfig.SteamApp.Appid}.acf"));
		foreach (string item3 in enumerable3)
		{
			File.Delete($"{AppConfig.LibraryFolder}\\{item3}");
		}
		if (AppConfig.SteamApp.Installed)
		{
			Log.Write("Cleanup complete");
			return;
		}
		IEnumerable<string> enumerable4 = source.Where((string x) => x.ToLower().Contains(".acf"));
		foreach (string item4 in enumerable4)
		{
			File.Delete($"{AppConfig.LibraryFolder}\\{item4}");
		}
		string path = $"{AppConfig.LibraryFolder}\\steamapps\\common\\{AppConfig.AppInstallDir}";
		if (Directory.Exists(path))
		{
			Directory.Delete(path, recursive: true);
		}
		Log.Write("Cleanup complete");
	}
}
