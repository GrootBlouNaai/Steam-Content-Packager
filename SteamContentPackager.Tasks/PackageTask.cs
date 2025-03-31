using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using SteamContentPackager.Steam;
using SteamContentPackager.UI;
using SteamContentPackager.UI.Controls;
using SteamContentPackager.Utils;
using SteamKit2;
using SteamKit2.Types;
using SteamKit2.Util;

namespace SteamContentPackager.Tasks;

public class PackageTask : TaskBase
{
	public SteamApp SteamApp { get; set; }

	public bool ManifestsAvailable => SteamApp.Depots.All((Depot x) => x.ManifestAvailable);

	public PackageTask(SteamApp steamApp)
	{
		SteamApp = steamApp;
		new Thread(SteamApp.FetchImage).Start();
	}

	public override async Task Run()
	{
		base.State = TaskState.Running;
		Logger.StartNewLog(SteamApp.InstallDir);
		base.State = (CheckForManifests() ? TaskState.Running : TaskState.Failed);
		if (base.State == TaskState.Failed)
		{
			base.Status = "Failed to retreive all manifests";
		}
		else
		{
			if (base.State == TaskState.Cancelled)
			{
				return;
			}
			List<FileMapping> mappings = await Validate();
			if (base.State == TaskState.Cancelled)
			{
				return;
			}
			base.State = ((!mappings.Any((FileMapping x) => (int)x.State > 0)) ? TaskState.Running : TaskState.Failed);
			if (base.State == TaskState.Failed && !Settings.DownloadContent)
			{
				base.Status = "Failed to validate SteamApp";
				return;
			}
			TaskState state;
			if (base.State == TaskState.Failed && Settings.DownloadContent)
			{
				state = ((await RepairInvalidFiles(mappings)) ? TaskState.Running : TaskState.Failed);
				base.State = state;
			}
			if (base.State == TaskState.Cancelled)
			{
				return;
			}
			if (base.State == TaskState.Failed)
			{
				base.Status = "Failed to repair files";
				return;
			}
			state = ((await CopyFiles(mappings)) ? TaskState.Running : TaskState.Failed);
			base.State = state;
			if (base.State == TaskState.Cancelled || base.State == TaskState.Cancelled)
			{
				return;
			}
			if (base.State == TaskState.Failed)
			{
				base.Status = "Failed to copy files to output directory";
			}
			else
			{
				if (base.State == TaskState.Cancelled)
				{
					return;
				}
				WriteDepotInfo();
				if (base.State == TaskState.Cancelled)
				{
					return;
				}
				BBCode.WriteForumBbCode(SteamApp);
				if (base.State == TaskState.Cancelled)
				{
					return;
				}
				state = ((await RunOnPackageComplete()) ? TaskState.Running : TaskState.Failed);
				base.State = state;
				if (base.State == TaskState.Failed)
				{
					base.Status = "OnPackageComplete failed";
					return;
				}
				if (Settings.ShowNotifications)
				{
					Application.Current.Dispatcher.Invoke(delegate
					{
						new Notification(SteamApp).Show();
					});
				}
				TaskCompleted?.Invoke(this);
				base.State = TaskState.Completed;
				base.Status = "Packing Complete";
			}
		}
	}

	private bool CheckForManifests()
	{
		List<Task> list = new List<Task>();
		foreach (Depot depot in SteamApp.Depots)
		{
			if (base.State == TaskState.Cancelled)
			{
				return false;
			}
			if (depot.ManifestAvailable || !Settings.DownloadContent)
			{
				continue;
			}
			if (!SteamSession.LoggedOn)
			{
				base.Status = "Login Required!";
				base.State = TaskState.WaitingForLogin;
			}
			while (!SteamSession.LoggedOn)
			{
				Thread.Sleep(50);
			}
			base.State = TaskState.Running;
			list.Add(Task.Run(delegate
			{
				if (ContentDownloader.CheckDepotAccess(SteamApp.Appid, depot.Id).Result)
				{
					base.Status = $"Downloading Manifest: {depot.Id}_{depot.ManifestId}";
					_ = ContentDownloader.DownloadManifest(SteamApp.Appid, depot.Id, depot.ManifestId, CancellationToken.None).Result;
				}
			}));
		}
		if (list.Count > 0 && !SteamSession.LoggedOn)
		{
			Logger.WriteEntry("You must be logged on to steam to download content", LogLevel.Error);
			return false;
		}
		Task.WaitAll(list.ToArray());
		return ManifestsAvailable;
	}

	private async Task<bool> RunOnPackageComplete()
	{
		if (!File.Exists("OnPackageComplete.bat"))
		{
			Logger.WriteEntry("Skipping OnPackageComplete");
			return true;
		}
		await Task.Delay(TimeSpan.FromSeconds(1.0));
		ProgressChanged?.Invoke(0uL, 0uL, "Running OnPackageComplete...");
		LogEntry entry = Logger.WriteEntry("Running OnPackageComplete");
		Process process = new Process
		{
			StartInfo = 
			{
				FileName = "OnPackageComplete.bat"
			}
		};
		process.StartInfo.Arguments = $"\"{SteamApp.InstallDir}\" {SteamApp.Appid} \"{Settings.OutputDirectory}\"";
		process.Start();
		await process.WaitForExitAsync();
		if (process.ExitCode != 0)
		{
			Logger.UpdateEntry(entry, "FAILED");
			return false;
		}
		Logger.UpdateEntry(entry, "SUCCEEDED");
		return true;
	}

	public void WriteDepotInfo()
	{
		//IL_003a: Unknown result type (might be due to invalid IL or missing references)
		if (!Settings.WriteDepotInfo)
		{
			return;
		}
		Logger.WriteEntry("Writing depotinfo");
		Item val = new AppInfo($"{Settings.SteamPath}\\appcache\\appinfo.vdf", new List<uint> { SteamApp.Appid }).Items[SteamApp.Appid];
		if (val == null)
		{
			Logger.WriteEntry("Failed to write depot info");
			return;
		}
		val.Root.SaveToFile($"{Settings.OutputDirectory}\\{SteamApp.InstallDir}_appinfo.txt", false);
		using StreamWriter streamWriter = new StreamWriter(File.Open($"{Settings.OutputDirectory}\\{SteamApp.InstallDir}_DepotInfo.txt", FileMode.Create));
		KeyValue val2 = val.Root.Children.Find((KeyValue x) => x.Name.ToLower() == "depots");
		foreach (Depot depot in SteamApp.Depots)
		{
			string value = val2[(depot.DlcDepot ? depot.DlcAppid : depot.Id).ToString()]["name"].Value;
			streamWriter.WriteLine(depot.Id + " - " + value);
		}
	}

	public async Task<List<FileMapping>> Validate()
	{
		List<FileMapping> mappings = BuildMappingList();
		base.ProgressValue = 0uL;
		base.ProgressMax = mappings.Aggregate(0uL, (ulong a, FileMapping c) => a + c.Size);
		foreach (FileMapping item in mappings)
		{
			if (base.State != TaskState.Cancelled)
			{
				base.Status = $"Validating File: {item.FileName}";
				await ValidateFile(item);
				continue;
			}
			break;
		}
		ProgressChanged?.Invoke(0uL, 0uL, "");
		return mappings;
	}

	private async Task ValidateFile(FileMapping mapping)
	{
		LogEntry entry = Logger.WriteEntry($"Validating File: {mapping.FileName}");
		if (base.State == TaskState.Cancelled)
		{
			return;
		}
		string path = $"{new FileInfo(SteamApp.AcfPath).Directory?.FullName}\\common\\{SteamApp.InstallDir}\\{mapping.FileName}";
		if (!File.Exists(path))
		{
			mapping.Chunks.ForEach(delegate(ChunkData chunk)
			{
				chunk.Valid = false;
			});
			mapping.State = (FileState)2;
			base.ProgressValue += mapping.Size;
			ProgressChanged?.Invoke(base.ProgressValue, base.ProgressMax);
		}
		else if (mapping.Chunks != null)
		{
			foreach (ChunkData chunk2 in mapping.Chunks)
			{
				while (base.State == TaskState.Paused && base.State != TaskState.Cancelled)
				{
					await Task.Delay(50);
				}
				if (base.State != TaskState.Cancelled)
				{
					uint num = await Adler32.GetAdler(path, (long)chunk2.Offset, (int)chunk2.Size);
					base.ProgressValue += chunk2.Size;
					ProgressChanged?.Invoke(base.ProgressValue, base.ProgressMax);
					if (num != chunk2.Adler32)
					{
						chunk2.Valid = false;
						mapping.State = (FileState)1;
					}
					continue;
				}
				break;
			}
		}
		FileState state = mapping.State;
		Logger.UpdateEntry(entry, ((object)(FileState)(ref state)/*cast due to .constrained prefix*/).ToString().ToUpper());
	}

	private List<FileMapping> BuildMappingList()
	{
		List<FileMapping> mappings = new List<FileMapping>();
		SteamApp.Depots.ForEach(delegate(Depot depot)
		{
			//IL_0043: Unknown result type (might be due to invalid IL or missing references)
			new Manifest($"{Settings.SteamPath}\\depotcache\\{depot.Id}_{depot.ManifestId}.manifest").Payload.Mappings.ForEach(delegate(FileMapping mapping)
			{
				//IL_0019: Unknown result type (might be due to invalid IL or missing references)
				//IL_001a: Unknown result type (might be due to invalid IL or missing references)
				EDepotFileFlag val = (EDepotFileFlag)mapping.Flags;
				if (!((Enum)val).HasFlag((Enum)(object)(EDepotFileFlag)64))
				{
					FileMapping val2 = mappings.Find((FileMapping m) => m.FileName == mapping.FileName);
					if (val2 != null)
					{
						mappings.Remove(val2);
					}
					mapping.DepotId = depot.Id;
					mappings.Add(mapping);
				}
			});
		});
		return mappings;
	}

	public async Task<bool> RepairInvalidFiles(List<FileMapping> mappings)
	{
		if (base.State == TaskState.Cancelled)
		{
			return false;
		}
		if (!SteamSession.LoggedOn)
		{
			base.Status = "Login Required!";
			base.State = TaskState.WaitingForLogin;
		}
		while (!SteamSession.LoggedOn)
		{
			Thread.Sleep(50);
		}
		base.State = TaskState.Running;
		base.ProgressMax = 0uL;
		base.ProgressValue = 0uL;
		ProgressChanged?.Invoke(0uL, 0uL);
		foreach (FileMapping mapping2 in mappings)
		{
			mapping2.Chunks?.ForEach(delegate(ChunkData chunk)
			{
				if (!chunk.Valid)
				{
					base.ProgressMax += chunk.Size;
				}
			});
		}
		ProgressChanged?.Invoke(0uL, base.ProgressMax);
		foreach (FileMapping mapping in mappings)
		{
			if ((int)mapping.State == 0)
			{
				continue;
			}
			if (!(await ContentDownloader.CheckDepotAccess(SteamApp.Appid, mapping.DepotId)))
			{
				ProgressChanged?.Invoke(0uL, base.ProgressMax, "Access Denied");
				return false;
			}
			string text = $"{new FileInfo(SteamApp.AcfPath).Directory?.FullName}\\common\\{SteamApp.InstallDir}\\{mapping.FileName}";
			new FileInfo(text).Directory?.Create();
			FileStream fs = File.Open(text, FileMode.OpenOrCreate);
			fs.SetLength((long)mapping.Size);
			if (mapping.Chunks == null)
			{
				fs.Close();
				continue;
			}
			base.Status = $"Repairing File: {mapping.FileName}";
			ProgressChanged?.Invoke(base.ProgressValue, base.ProgressMax, $"Repairing File: {mapping.FileName}");
			LogEntry entry = Logger.WriteEntry($"Repairing File: {mapping.FileName}");
			foreach (ChunkData chunk2 in mapping.Chunks)
			{
				if (base.State == TaskState.Cancelled)
				{
					return false;
				}
				if (!chunk2.Valid)
				{
					Logger.WriteEntry($"Downloading Chunk: {Convert.ToBase64String(chunk2.Sha)}");
					await ContentDownloader.DownloadChunk(fs, chunk2, SteamApp.Appid, mapping.DepotId);
					base.ProgressValue += chunk2.Size;
					ProgressChanged?.Invoke(base.ProgressValue, base.ProgressMax);
				}
			}
			string result = (mapping.Chunks.All((ChunkData x) => x.Valid) ? "SUCCEEDED" : "FAILED");
			Logger.UpdateEntry(entry, result);
			fs.Close();
		}
		return true;
	}

	public async Task<bool> CopyFiles(List<FileMapping> mappings)
	{
		base.ProgressMax = 0uL;
		base.ProgressValue = 0uL;
		mappings.ForEach(delegate(FileMapping mapping)
		{
			base.ProgressMax += mapping.Size;
		});
		string destFile = $"{Settings.OutputDirectory}\\{SteamApp.InstallDir}\\steamapps\\appmanifest_{SteamApp.Appid}.acf";
		new FileInfo(destFile).Directory?.Create();
		KeyValue obj = KeyValue.LoadAsText(SteamApp.AcfPath);
		obj["userconfig"]["appinstalldir"].Value = string.Empty;
		obj["lastowner"].Value = Settings.LastOwner;
		obj["lastUpdated"].Value = "0";
		obj.SaveToFile(destFile, false);
		while (base.State == TaskState.Paused && base.State != TaskState.Cancelled)
		{
			await Task.Delay(50);
		}
		foreach (Depot depot in SteamApp.Depots)
		{
			if (base.State == TaskState.Cancelled)
			{
				return false;
			}
			string sourceFile = $"{Settings.SteamPath}\\depotcache\\{depot.Id}_{depot.ManifestId}.manifest";
			destFile = $"{Settings.OutputDirectory}\\{SteamApp.InstallDir}\\depotcache\\{depot.Id}_{depot.ManifestId}.manifest";
			LogEntry entry = Logger.WriteEntry($"Copying Manifest: {depot.Id}_{depot.ManifestId}");
			while (base.State == TaskState.Paused && base.State != TaskState.Cancelled)
			{
				await Task.Delay(50);
			}
			if (!(await CopyFileAsync(sourceFile, destFile, entry, countSize: false)))
			{
				return false;
			}
		}
		ProgressChanged?.Invoke(0uL, base.ProgressMax);
		foreach (FileMapping mapping in mappings)
		{
			string sourceFile2 = $"{new FileInfo(SteamApp.AcfPath).Directory?.FullName}\\common\\{SteamApp.InstallDir}\\{mapping.FileName}";
			destFile = $"{Settings.OutputDirectory}\\{SteamApp.InstallDir}\\steamapps\\common\\{SteamApp.InstallDir}\\{mapping.FileName}";
			ProgressChanged?.Invoke(base.ProgressValue, base.ProgressMax, $"Copying File: {mapping.FileName}");
			base.Status = $"Copying File: {mapping.FileName}";
			LogEntry entry2 = Logger.WriteEntry($"Copying File: {mapping.FileName}");
			while (base.State == TaskState.Paused && base.State != TaskState.Cancelled)
			{
				await Task.Delay(50);
			}
			if (!(await CopyFileAsync(sourceFile2, destFile, entry2)))
			{
				return false;
			}
		}
		Logger.WriteEntry("Copied all files");
		return true;
	}

	private async Task<bool> CopyFileAsync(string sourcePath, string destinationPath, LogEntry logEntry, bool countSize = true)
	{
		new FileInfo(destinationPath).Directory?.Create();
		try
		{
			await Task.Run(delegate
			{
				if (base.State == TaskState.Cancelled)
				{
					return;
				}
				using Stream stream = new FileStream(sourcePath, FileMode.Open);
				using Stream stream2 = new FileStream(destinationPath, FileMode.OpenOrCreate);
				byte[] buffer = new byte[8096];
				int num;
				while ((num = stream.Read(buffer, 0, 8096)) != 0)
				{
					while (base.State == TaskState.Paused && base.State != TaskState.Cancelled)
					{
						Task.Delay(50);
					}
					if (base.State == TaskState.Cancelled)
					{
						break;
					}
					stream2.Write(buffer, 0, num);
					if (countSize)
					{
						base.ProgressValue += (ulong)num;
						ProgressChanged?.Invoke(base.ProgressValue, base.ProgressMax);
					}
				}
			});
			if (base.State == TaskState.Cancelled)
			{
				return false;
			}
		}
		catch (Exception)
		{
			Logger.UpdateEntry(logEntry, "FAILED");
			return false;
		}
		Logger.UpdateEntry(logEntry, "SUCCEEDED");
		return true;
	}

	public override void Pause()
	{
		base.State = TaskState.Paused;
	}

	public override void Resume()
	{
		base.State = TaskState.Running;
	}

	public override void Cancel()
	{
		Logger.WriteEntry("Task Cancelled");
		base.State = TaskState.Cancelled;
	}
}
