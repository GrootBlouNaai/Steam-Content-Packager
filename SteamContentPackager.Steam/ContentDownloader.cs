using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using SteamContentPackager.Packing;
using SteamContentPackager.UI.Converters;
using SteamContentPackager.Utils;
using SteamKit2;

namespace SteamContentPackager.Steam;

internal class ContentDownloader : SubTask
{
	private string _appDir;

	private int _totalChunks;

	private int _processedChunks = 0;

	private bool _aborted = false;

	private readonly ConcurrentDictionary<string, Stream> _streams = new ConcurrentDictionary<string, Stream>();

	public List<FileMapping> Mappings;

	private ulong _lastBytesDownloaded;

	private Stopwatch _progressTimerStopwatch;

	private ulong _totalBytes;

	private double _lastProgressTime;

	private List<double> _speedAverageList = new List<double>();

	private List<TimeSpan> _etaAverageList = new List<TimeSpan>();

	private readonly object _writeLock = new object();

	private ulong _bytesDownloaded = 0uL;

	public AppConfig AppConfig => ParentTask.AppConfig;

	public ContentDownloader(PackageTask parentTask, List<FileMapping> filemappings)
		: base(parentTask)
	{
		Mappings = filemappings.OrderBy((FileMapping x) => x.ParentDepotId).ToList();
	}

	public override async Task<Result> Run()
	{
		Timer timer = null;
		try
		{
			Log.Write("Pre-allocating disk space");
			Dictionary<string, List<ChunkData>> dupeChunks = new Dictionary<string, List<ChunkData>>();
			List<ChunkData> chunks = Mappings.SelectMany((FileMapping mapping) => mapping.Chunks ?? new List<ChunkData>()).ToList();
			foreach (IGrouping<string, ChunkData> chunkGroup in from x in chunks
				group x by BitConverter.ToString(x.Sha))
			{
				dupeChunks[chunkGroup.Key] = chunkGroup.ToList();
			}
			foreach (KeyValuePair<string, List<ChunkData>> keyValuePair in dupeChunks.OrderBy((KeyValuePair<string, List<ChunkData>> pair) => pair.Value.Count))
			{
				Console.WriteLine($"{keyValuePair.Key} - {keyValuePair.Value.Count}");
			}
			timer = CreateProgressTimer(dupeChunks.Values.Select((List<ChunkData> x) => x.First()).ToList());
			_appDir = $"{AppConfig.LibraryFolder}\\steamapps\\common\\{AppConfig.AppInstallDir}\\";
			Parallel.ForEach(Mappings, delegate(FileMapping mapping)
			{
				FileInfo fileInfo = new FileInfo($"{_appDir}{mapping.FileName}");
				fileInfo.Directory?.Create();
				FileStream fs = File.Open(fileInfo.FullName, FileMode.OpenOrCreate, FileAccess.Write);
				fs.SetLength((long)mapping.Size);
				if (mapping.Size == 0)
				{
					fs.Close();
				}
				else
				{
					_streams.AddOrUpdate(mapping.FileName.ToLower(), fs, (string s, Stream stream) => fs);
				}
			});
			_totalChunks = dupeChunks.Count;
			_progressTimerStopwatch.Start();
			timer.Change(0, 500);
			Log.Write($"Starting Download: {AppConfig.SteamApp.Name}");
			await dupeChunks.ParallelForEach(async delegate(KeyValuePair<string, List<ChunkData>> pair)
			{
				CheckPaused();
				if (!CheckCancelledOrAborted())
				{
					await DownloadChunk(pair);
				}
			}, Config.MaxConnections);
			CloseOpenStreams();
		}
		catch (Exception ex)
		{
			Exception e = ex;
			_aborted = true;
			Log.WriteException(e);
		}
		timer?.Change(-1, -1);
		Result result = new Result
		{
			Success = !CheckCancelledOrAborted()
		};
		if (result.Success)
		{
			Log.Write("Download Complete");
		}
		return result;
	}

	private Timer CreateProgressTimer(List<ChunkData> chunks)
	{
		chunks.ForEach(delegate(ChunkData x)
		{
			_totalBytes += x.CompressedSize;
		});
		_progressTimerStopwatch = new Stopwatch();
		_bytesDownloaded = 0uL;
		_lastBytesDownloaded = 0uL;
		_lastProgressTime = 0.0;
		_speedAverageList = new List<double>();
		return new Timer(ProgressTimerCallback, null, TimeSpan.FromMilliseconds(-1.0), TimeSpan.FromMilliseconds(-1.0));
	}

	private void ProgressTimerCallback(object o)
	{
		float progress = (float)_bytesDownloaded / (float)_totalBytes * 100f;
		double totalSeconds = _progressTimerStopwatch.Elapsed.TotalSeconds;
		ulong num = _bytesDownloaded - _lastBytesDownloaded;
		double num2 = (double)num / (totalSeconds - _lastProgressTime);
		double item = num2;
		_lastProgressTime = totalSeconds;
		_lastBytesDownloaded = _bytesDownloaded;
		_speedAverageList.Add(item);
		while (_speedAverageList.Count > 5)
		{
			_speedAverageList.RemoveAt(0);
		}
		double num3 = _speedAverageList.Average();
		ulong num4 = _totalBytes - _bytesDownloaded;
		if (num2 > 0.0)
		{
			TimeSpan item2 = TimeSpan.FromSeconds((double)num4 / num2);
			_etaAverageList.Add(item2);
		}
		string arg = "Unknown";
		if (_etaAverageList.Count > 0)
		{
			while (_etaAverageList.Count > 5)
			{
				_etaAverageList.RemoveAt(0);
			}
			TimeSpan timeSpan = TimeSpan.FromSeconds(_etaAverageList.Select((TimeSpan x) => x.TotalSeconds).Average());
			arg = ((Math.Abs(num2) > 0.01) ? $"{timeSpan:hh\\:mm\\:ss}" : "Unknown");
		}
		ParentTask.Progress = progress;
		base.Info = $"Downloading - Speed: {num3 / 1000.0 / 1000.0:###.00} MB/s\tETA: {arg}\nRemaining: {SizeStringConverter.FormatBytes(num4)}";
	}

	private async Task DownloadChunk(KeyValuePair<string, List<ChunkData>> pair)
	{
		using (IEnumerator<FileMapping> enumerator = pair.Value.Select((ChunkData x) => x.ParentMapping).GetEnumerator())
		{
			while (enumerator.MoveNext())
			{
				FileInfo fileInfo = new FileInfo(string.Format(arg1: enumerator.Current.FileName, format: "{0}{1}", arg0: _appDir));
				fileInfo.Directory?.Create();
			}
		}
		CDNClient.DepotChunk downloadedChunk = await DownloadChunk(pair.Value[0], AppConfig.SteamApp.Appid, pair.Value[0].ParentMapping.ParentDepotId, DepotKeyCache.Keys[pair.Value[0].ParentMapping.ParentDepotId]);
		if (downloadedChunk != null)
		{
			OnChunkComplete(downloadedChunk, pair.Value);
		}
	}

	private async Task DownloadChunk(ChunkData x)
	{
		FileInfo fileInfo = new FileInfo($"{_appDir}{x.ParentMapping.FileName}");
		fileInfo.Directory?.Create();
		CDNClient.DepotChunk downloadedChunk = await DownloadChunk(x, AppConfig.SteamApp.Appid, x.ParentMapping.ParentDepotId, DepotKeyCache.Keys[x.ParentMapping.ParentDepotId]);
		if (downloadedChunk != null)
		{
			OnChunkComplete(downloadedChunk);
		}
	}

	private async Task<CDNClient.DepotChunk> DownloadChunk(ChunkData chunkinfo, uint appid, uint depotId, byte[] depotKey)
	{
		int retryCount = 0;
		CDNClient.DepotChunk downloadedChunk = null;
		CDNClient client = null;
		while (retryCount < 5)
		{
			try
			{
				if (retryCount > 0)
				{
					Log.Write($"Retrying chunk => {chunkinfo.Adler32}");
				}
				client = await SteamSession.CDNClientPool.GetConnectionForDepotAsync(appid, depotId, depotKey, CancellationToken.None);
				downloadedChunk = await client.DownloadDepotChunkAsync(depotId, chunkinfo, delegate(int read)
				{
					_bytesDownloaded += (ulong)read;
				});
				if (downloadedChunk.Data != null)
				{
					SteamSession.CDNClientPool.ReturnConnection(client);
					break;
				}
				SteamSession.CDNClientPool.ReturnBrokenConnection(client);
				retryCount++;
			}
			catch (Exception ex)
			{
				int num = retryCount + 1;
				retryCount = num;
				Log.Write($"Chunk download failed {num} of 5: {ex.Message} => {chunkinfo.Adler32}", LogLevel.Warning);
				if (client != null)
				{
					SteamSession.CDNClientPool.ReturnBrokenConnection(client);
				}
			}
		}
		if (downloadedChunk == null)
		{
			Log.Write("Chunk download failed: Too many retries", LogLevel.Error);
		}
		return downloadedChunk;
	}

	private void OnChunkComplete(CDNClient.DepotChunk downloadedChunk, List<ChunkData> chunks)
	{
		if (_aborted)
		{
			return;
		}
		try
		{
			downloadedChunk.Process(DepotKeyCache.Keys[downloadedChunk.ChunkInfo.ParentMapping.ParentDepotId]);
			foreach (ChunkData chunk in chunks)
			{
				_streams.TryGetValue(chunk.ParentMapping.FileName.ToLower(), out var value);
				lock (_writeLock)
				{
					value.Seek((long)chunk.Offset, SeekOrigin.Begin);
					value.Write(downloadedChunk.Data, 0, downloadedChunk.Data.Length);
				}
				chunk.Valid = true;
				if (chunk.ParentMapping.Chunks.All((ChunkData x) => x.Valid))
				{
					value.Close();
					_streams.TryRemove(chunk.ParentMapping.FileName.ToLower(), out var _);
				}
			}
		}
		catch (Exception)
		{
			if (_aborted)
			{
				return;
			}
			throw;
		}
		_processedChunks++;
	}

	private void OnChunkComplete(CDNClient.DepotChunk downloadedChunk)
	{
		if (_aborted)
		{
			return;
		}
		try
		{
			_streams.TryGetValue(downloadedChunk.ChunkInfo.ParentMapping.FileName.ToLower(), out var value);
			downloadedChunk.Process(DepotKeyCache.Keys[downloadedChunk.ChunkInfo.ParentMapping.ParentDepotId]);
			lock (_writeLock)
			{
				value.Seek((long)downloadedChunk.ChunkInfo.Offset, SeekOrigin.Begin);
				value.Write(downloadedChunk.Data, 0, downloadedChunk.Data.Length);
			}
			downloadedChunk.ChunkInfo.Valid = true;
			if (downloadedChunk.ChunkInfo.ParentMapping.Chunks.All((ChunkData x) => x.Valid))
			{
				value.Close();
				_streams.TryRemove(downloadedChunk.ChunkInfo.ParentMapping.FileName.ToLower(), out var _);
			}
		}
		catch (Exception)
		{
			if (_aborted)
			{
				return;
			}
			throw;
		}
		_processedChunks++;
	}

	private void CheckPaused()
	{
		while (ParentTask.State == TaskState.Paused && !ParentTask.CancellationTokenSource.IsCancellationRequested)
		{
			Thread.Sleep(1000);
		}
	}

	private bool CheckCancelledOrAborted()
	{
		if (ParentTask.CancellationTokenSource.IsCancellationRequested || _aborted)
		{
			return true;
		}
		return false;
	}

	private void CloseOpenStreams()
	{
		_streams?.Values.ToList().ForEach(delegate(Stream x)
		{
			x.Close();
		});
	}

	public static async Task DownloadManifests(List<SteamApp.Depot> depots, string libraryFolder)
	{
		bool failed = false;
		await depots.ParallelForEach(async delegate(SteamApp.Depot depot)
		{
			if (depot.Subscribed && !failed && await DownloadManifest(depot.ParentAppId, depot.Id, depot.ManifestId, libraryFolder) == null)
			{
				failed = true;
			}
		}, 4);
		if (failed)
		{
			throw new Exception("Failed to retreive manifests");
		}
	}

	public static async Task<Manifest> DownloadManifest(uint appid, uint depotid, ulong manifestId, string downloadDir)
	{
		int retries = 5;
		CDNClient client = null;
		Manifest manifest = null;
		if (!(await SteamSession.RequestDepotKey(depotid, appid)))
		{
			return null;
		}
		FileInfo fileInfo = new FileInfo($"{downloadDir}\\depotcache\\{depotid}_{manifestId}.manifest");
		if (Utils.IsInstalled)
		{
			FileInfo steamManifestFileInfo = new FileInfo($"{Utils.InstallPath}\\depotcache\\{depotid}_{manifestId}.manifest");
			if (steamManifestFileInfo.Exists && !fileInfo.Exists)
			{
				manifest = new Manifest(steamManifestFileInfo.FullName, depotid);
				if (steamManifestFileInfo.FullName == fileInfo.FullName)
				{
					return manifest;
				}
				manifest.Save(fileInfo.FullName);
				Log.Write($"Manifest copied from steam installation:  {depotid}_{manifestId}");
				return manifest;
			}
		}
		if (fileInfo.Exists)
		{
			return new Manifest(fileInfo.FullName, depotid);
		}
		fileInfo.Directory?.Create();
		byte[] depotKey = DepotKeyCache.Keys[depotid];
		while (retries > 0)
		{
			try
			{
				client = await SteamSession.CDNClientPool.GetConnectionForDepotAsync(appid, depotid, depotKey, CancellationToken.None);
				manifest = await client.DownloadManifestAsync(depotid, manifestId);
				manifest.DecryptFilenames(depotKey);
				manifest.Save(fileInfo.FullName);
				SteamSession.CDNClientPool.ReturnConnection(client);
				Log.Write($"Manifest Downloaded: {depotid}_{manifestId}");
			}
			catch (WebException ex)
			{
				WebException e = ex;
				HttpWebResponse response = (HttpWebResponse)e.Response;
				if (response == null)
				{
					Log.Write($"Manifest download failed ({depotid}_{manifestId}): ({e.Message})");
					goto IL_0528;
				}
				if (response.StatusCode == HttpStatusCode.NotFound)
				{
					Log.Write("Manifest not found. Double check Manifest ID", LogLevel.Error);
					break;
				}
				Log.Write($"Manifest download failed ({depotid}_{manifestId}): ({e.Status}) : {(int)response.StatusCode} ");
				goto IL_0528;
			}
			catch (Exception ex2)
			{
				Exception e2 = ex2;
				Log.Write($"Manifest download failed ({depotid}_{manifestId}): ({e2.Message})");
				goto IL_0528;
			}
			break;
			IL_0528:
			if (client != null)
			{
				SteamSession.CDNClientPool.ReturnBrokenConnection(client);
			}
			retries--;
		}
		return manifest;
	}
}
