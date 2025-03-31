using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using SteamContentPackager.UI.Controls;
using SteamContentPackager.Utils;
using SteamKit2;

namespace SteamContentPackager.Steam;

public static class ContentDownloader
{
	public static Dictionary<uint, byte[]> DepotKeys = new Dictionary<uint, byte[]>();

	public static Dictionary<uint, byte[]> AppTickets { get; set; } = new Dictionary<uint, byte[]>();

	public static async Task<bool> CheckDepotAccess(uint appId, uint depotId)
	{
		if (!(await RequestAppTicket(appId)))
		{
			return false;
		}
		return await RequestDepotKey(appId, depotId);
	}

	public static async Task<bool> RequestDepotKey(uint appId, uint depotId)
	{
		if (DepotKeys.ContainsKey(depotId))
		{
			return true;
		}
		LogEntry entry = Logger.WriteEntry($"Requesting DepotKey: {depotId}");
		DepotKeyCallback val = await SteamSession.SteamApps.GetDepotDecryptionKey(depotId, appId);
		EResult result = val.Result;
		Logger.UpdateEntry(entry, ((object)(EResult)(ref result)/*cast due to .constrained prefix*/).ToString());
		if ((int)val.Result == 1)
		{
			DepotKeys[depotId] = val.DepotKey;
		}
		return (int)val.Result == 1;
	}

	public static async Task<bool> RequestAppTicket(uint appId)
	{
		if (AppTickets.ContainsKey(appId))
		{
			return true;
		}
		LogEntry entry = Logger.WriteEntry($"Requesting AppTicket: {appId}");
		AppOwnershipTicketCallback val = await SteamSession.SteamApps.GetAppOwnershipTicket(appId);
		EResult result = val.Result;
		Logger.UpdateEntry(entry, ((object)(EResult)(ref result)/*cast due to .constrained prefix*/).ToString());
		if ((int)val.Result == 1)
		{
			AppTickets[val.AppID] = val.Ticket;
		}
		return (int)val.Result == 1;
	}

	public static async Task<bool> DownloadManifest(uint appId, uint depotId, ulong manifestId, CancellationToken cancellationToken)
	{
		if (cancellationToken.IsCancellationRequested)
		{
			return false;
		}
		string manifestPath = $"{Settings.SteamPath}\\depotcache\\{depotId}_{manifestId}.manifest";
		byte[] depotKey = DepotKeys[depotId];
		int retryCount = 0;
		if (!Directory.Exists($"{Settings.SteamPath}\\depotcache"))
		{
			Directory.CreateDirectory($"{Settings.SteamPath}\\depotcache");
		}
		LogEntry entry = null;
		if (!File.Exists(manifestPath))
		{
			entry = Logger.WriteEntry($"Downloading manifest: {depotId}_{manifestId}");
		}
		Manifest manifest = null;
		await Task.Run(delegate
		{
			if (File.Exists(manifestPath))
			{
				Logger.WriteEntry($"Manifest {depotId}_{manifestId} already exists");
			}
			else
			{
				while (true)
				{
					CDNClient val = null;
					if (!cancellationToken.IsCancellationRequested)
					{
						try
						{
							val = SteamSession.CdnClientPool.GetConnectionForDepot(appId, depotId, depotKey, CancellationToken.None);
							CDNClient val2 = val;
							manifest = val2.DownloadManifest(depotId, manifestId);
							Manifest obj = manifest;
							if (obj != null)
							{
								obj.DecryptFilenames(depotKey);
							}
							Manifest obj2 = manifest;
							if (obj2 != null)
							{
								obj2.Save($"{Settings.SteamPath}\\depotcache\\{depotId}_{manifestId}.manifest");
							}
							Logger.UpdateEntry(entry, "SUCCEEDED");
							SteamSession.CdnClientPool.ReturnConnection(val2);
						}
						catch (Exception ex)
						{
							if (val != null)
							{
								Settings.ContentServerPenalty.TryGetValue(val.ConnectedServer.Host, out var value);
								Settings.ContentServerPenalty[val.ConnectedServer.Host] = value + 1;
							}
							if (++retryCount != 3)
							{
								Logger.WriteEntry($"Error downloading manifest.({ex.Message}) Retrying ({retryCount} of 3)...", LogLevel.Warning);
								continue;
							}
						}
						break;
					}
					return;
				}
				GC.Collect();
			}
		});
		if (manifest != null)
		{
			return true;
		}
		Logger.UpdateEntry(entry, "FAILED");
		return false;
	}

	public static async Task DownloadChunk(FileStream fs, ChunkData chunk, uint appid, uint depotid)
	{
		DepotChunk chunkData = null;
		await Task.Run(delegate
		{
			byte[] array = DepotKeys[depotid];
			CDNClient connectionForDepot = SteamSession.CdnClientPool.GetConnectionForDepot(appid, depotid, array, CancellationToken.None);
			int num = 0;
			while (num < 3)
			{
				try
				{
					if (num > 0)
					{
						connectionForDepot = SteamSession.CdnClientPool.GetConnectionForDepot(appid, depotid, array, CancellationToken.None);
					}
					chunkData = connectionForDepot.DownloadDepotChunk(depotid, chunk, array);
					SteamSession.CdnClientPool.ReturnConnection(connectionForDepot);
				}
				catch (Exception ex)
				{
					int value = 0;
					Settings.ContentServerPenalty.TryGetValue(connectionForDepot.ConnectedServer.Host, out value);
					Settings.ContentServerPenalty[connectionForDepot.ConnectedServer.Host] = value + 1;
					Logger.WriteEntry($"Chunk download failed. {ex.Message}. Retrying", LogLevel.Warning);
					num++;
					connectionForDepot = SteamSession.CdnClientPool.GetConnectionForDepot(appid, depotid, array, CancellationToken.None);
					SteamSession.CdnClientPool.ReturnBrokenConnection(connectionForDepot);
					continue;
				}
				break;
			}
			if (chunkData != null)
			{
				chunk.Valid = true;
				fs.Seek((long)chunk.Offset, SeekOrigin.Begin);
				fs.Write(chunkData.Data, 0, chunkData.Data.Length);
			}
		});
	}
}
