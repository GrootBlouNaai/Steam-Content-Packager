using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using SteamContentPackager.Utils;
using SteamKit2;
using SteamKit2.Internal;

namespace SteamContentPackager.Steam;

public class CDNClientPool
{
	private const int ServerEndpointMinimumSize = 30;

	private ConcurrentBag<CDNClient> activeClientPool;

	private ConcurrentDictionary<CDNClient, Tuple<uint, Server>> activeClientAuthed;

	private BlockingCollection<Server> availableServerEndpoints;

	private AutoResetEvent populatePoolEvent;

	private Thread monitorThread;

	private static readonly object _locker = new object();

	public static Dictionary<Tuple<uint, string>, CDNAuthTokenCallback> CdnAuthTokens { get; set; } = new Dictionary<Tuple<uint, string>, CDNAuthTokenCallback>();

	public CDNClientPool()
	{
		activeClientPool = new ConcurrentBag<CDNClient>();
		activeClientAuthed = new ConcurrentDictionary<CDNClient, Tuple<uint, Server>>();
		availableServerEndpoints = new BlockingCollection<Server>();
		populatePoolEvent = new AutoResetEvent(initialState: true);
		monitorThread = new Thread(ConnectionPoolMonitor);
		monitorThread.Name = "CDNClient Pool Monitor";
		monitorThread.IsBackground = true;
		monitorThread.Start();
	}

	private List<Server> FetchBootstrapServerList()
	{
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		//IL_000c: Expected O, but got Unknown
		CDNClient val = new CDNClient(SteamSession.SteamClient, (byte[])null);
		while (true)
		{
			try
			{
				List<Server> list = val.FetchServerList((IPEndPoint)null, ((CMClient)SteamSession.SteamClient).CellID, 30);
				if (list != null)
				{
					return list;
				}
			}
			catch (Exception)
			{
			}
		}
	}

	private void ConnectionPoolMonitor()
	{
		while (true)
		{
			populatePoolEvent.WaitOne(TimeSpan.FromSeconds(1.0));
			if (availableServerEndpoints.Count >= 30 || !((CMClient)SteamSession.SteamClient).IsConnected || ((CMClient)SteamSession.SteamClient).GetServersOfType((EServerType)36).Count <= 0)
			{
				continue;
			}
			foreach (Tuple<Server, int> item in from x in FetchBootstrapServerList().Select(delegate(Server x)
				{
					int value = 0;
					Settings.ContentServerPenalty.TryGetValue(x.Host, out value);
					return Tuple.Create<Server, int>(x, value);
				})
				orderby x.Item2, x.Item1.WeightedLoad
				select x)
			{
				for (int i = 0; i < item.Item1.NumEntries; i++)
				{
					availableServerEndpoints.Add(item.Item1);
				}
			}
		}
	}

	private void ReleaseConnection(CDNClient client)
	{
		activeClientAuthed.TryRemove(client, out var _);
	}

	private CDNClient BuildConnection(uint appid, uint depotId, byte[] depotKey, Server serverSeed, CancellationToken token)
	{
		//IL_0068: Unknown result type (might be due to invalid IL or missing references)
		//IL_006e: Expected O, but got Unknown
		Server val = null;
		CDNClient val2 = null;
		while (val2 == null)
		{
			if (serverSeed != null)
			{
				val = serverSeed;
				serverSeed = null;
			}
			else
			{
				if (availableServerEndpoints.Count < 30)
				{
					populatePoolEvent.Set();
				}
				val = availableServerEndpoints.Take(token);
			}
			if (!val.Host.ToLower().Contains("bigpond"))
			{
				val2 = new CDNClient(SteamSession.SteamClient, ContentDownloader.AppTickets[appid]);
				string text = null;
				if (val.Type == "CDN")
				{
					RequestCdnAuthToken(appid, depotId, val.Host);
					text = CdnAuthTokens[Tuple.Create(depotId, val.Host)].Token;
				}
				try
				{
					val2.Connect(val);
					val2.AuthenticateDepot(depotId, depotKey, text);
				}
				catch (Exception ex)
				{
					val2 = null;
					Console.WriteLine("Failed to connect to content server {0}: {1}", val, ex.Message);
					int value = 0;
					Settings.ContentServerPenalty.TryGetValue(val2.ConnectedServer.Host, out value);
					Settings.ContentServerPenalty[val2.ConnectedServer.Host] = value + 1;
				}
			}
		}
		activeClientAuthed[val2] = Tuple.Create<uint, Server>(depotId, val);
		return val2;
	}

	public CDNClient GetConnectionForDepot(uint appid, uint depotId, byte[] depotKey, CancellationToken token)
	{
		CDNClient result = null;
		activeClientPool.TryTake(out result);
		if (result == null)
		{
			result = BuildConnection(appid, depotId, depotKey, null, token);
		}
		if (!activeClientAuthed.TryGetValue(result, out var value) || value.Item1 != depotId)
		{
			ReleaseConnection(result);
			result = BuildConnection(appid, depotId, depotKey, null, token);
		}
		return result;
	}

	public void ReturnConnection(CDNClient client)
	{
		if (client != null)
		{
			activeClientPool.Add(client);
		}
	}

	public void ReturnBrokenConnection(CDNClient client)
	{
		if (client != null)
		{
			ReleaseConnection(client);
		}
	}

	public static void RequestCdnAuthToken(uint appid, uint depotid, string host)
	{
		//IL_003e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0044: Invalid comparison between Unknown and I4
		lock (_locker)
		{
			if (!CdnAuthTokens.ContainsKey(Tuple.Create(depotid, host)))
			{
				CDNAuthTokenCallback result = SteamSession.SteamApps.GetCDNAuthToken(appid, depotid, host).ToTask().Result;
				if ((int)result.Result == 1)
				{
					CdnAuthTokens[Tuple.Create(depotid, host)] = result;
				}
			}
		}
	}
}
