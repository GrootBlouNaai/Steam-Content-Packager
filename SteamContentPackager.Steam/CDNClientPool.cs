using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using SteamKit2;

namespace SteamContentPackager.Steam;

internal class CDNClientPool
{
	private const int ServerEndpointMinimumSize = 8;

	private ConcurrentBag<CDNClient> activeClientPool;

	private ConcurrentDictionary<CDNClient, Tuple<uint, CDNClient.Server>> activeClientAuthed;

	private BlockingCollection<CDNClient.Server> availableServerEndpoints;

	private AutoResetEvent populatePoolEvent;

	private Task monitorTask;

	public ConcurrentDictionary<string, SteamApps.CDNAuthTokenCallback> CDNAuthTokens { get; } = new ConcurrentDictionary<string, SteamApps.CDNAuthTokenCallback>();

	public CancellationTokenSource ExhaustedToken { get; set; }

	public ConcurrentDictionary<string, int> ContentServerPenalty { get; private set; } = new ConcurrentDictionary<string, int>();

	public CDNClientPool()
	{
		activeClientPool = new ConcurrentBag<CDNClient>();
		activeClientAuthed = new ConcurrentDictionary<CDNClient, Tuple<uint, CDNClient.Server>>();
		availableServerEndpoints = new BlockingCollection<CDNClient.Server>();
		populatePoolEvent = new AutoResetEvent(initialState: true);
		monitorTask = Task.Factory.StartNew((Func<Task>)ConnectionPoolMonitorAsync).Unwrap();
	}

	private async Task<IList<CDNClient.Server>> FetchBootstrapServerListAsync()
	{
		CDNClient bootstrap = new CDNClient(SteamSession.SteamClient);
		uint? cell = ((Config.CellID == 0) ? SteamSession.SteamClient.CellID : new uint?(Config.CellID));
		while (true)
		{
			try
			{
				IList<CDNClient.Server> cdnServers = await bootstrap.FetchServerListAsync(null, cell).ConfigureAwait(continueOnCapturedContext: false);
				if (cdnServers != null)
				{
					return cdnServers;
				}
			}
			catch (Exception ex)
			{
				Exception ex2 = ex;
				Console.WriteLine("Failed to retrieve content server list: {0}", ex2.Message);
			}
		}
	}

	private async Task ConnectionPoolMonitorAsync()
	{
		bool didPopulate = false;
		while (true)
		{
			populatePoolEvent.WaitOne(TimeSpan.FromSeconds(1.0));
			if (availableServerEndpoints.Count < 8 && SteamSession.SteamClient.IsConnected && SteamSession.SteamClient.GetServersOfType(EServerType.CS).Count > 0)
			{
				IOrderedEnumerable<Tuple<CDNClient.Server, int>> weightedCdnServers = from x in (await FetchBootstrapServerListAsync().ConfigureAwait(continueOnCapturedContext: false)).Select(delegate(CDNClient.Server x)
					{
						ContentServerPenalty.TryGetValue(x.Host, out var value);
						return Tuple.Create(x, value);
					})
					orderby x.Item2, x.Item1.WeightedLoad
					select x;
				foreach (Tuple<CDNClient.Server, int> endpoint in weightedCdnServers)
				{
					for (int i = 0; i < endpoint.Item1.NumEntries; i++)
					{
						availableServerEndpoints.Add(endpoint.Item1);
					}
				}
				didPopulate = true;
			}
			else if (availableServerEndpoints.Count == 0 && !SteamSession.SteamClient.IsConnected && didPopulate)
			{
				ExhaustedToken?.Cancel();
			}
		}
	}

	private void ReleaseConnection(CDNClient client)
	{
		activeClientAuthed.TryRemove(client, out var _);
	}

	public string ResolveCDNTopLevelHost(string host)
	{
		if (host.EndsWith(".steampipe.steamcontent.com"))
		{
			return "steampipe.steamcontent.com";
		}
		return host;
	}

	private async Task<CDNClient> BuildConnectionAsync(uint appId, uint depotId, byte[] depotKey, CDNClient.Server serverSeed, CancellationToken token)
	{
		CDNClient.Server server = null;
		CDNClient client = null;
		while (client == null)
		{
			if (serverSeed != null)
			{
				server = serverSeed;
				serverSeed = null;
			}
			else
			{
				if (availableServerEndpoints.Count < 8)
				{
					populatePoolEvent.Set();
				}
				server = availableServerEndpoints.Take(token);
			}
			client = new CDNClient(SteamSession.SteamClient);
			string cdnAuthToken = null;
			try
			{
				if (server.Type == "CDN")
				{
					await RequestCDNAuthToken(appId, depotId, server.Host);
					string cdnKey = $"{depotId:D}:{ResolveCDNTopLevelHost(server.Host)}";
					if (!CDNAuthTokens.TryGetValue(cdnKey, out var authTokenCallback))
					{
						throw new Exception($"Failed to retrieve CDN token for server {server.Host} depot {depotId}");
					}
					cdnAuthToken = authTokenCallback.Token;
					authTokenCallback = null;
				}
				await client.ConnectAsync(server).ConfigureAwait(continueOnCapturedContext: false);
				await client.AuthenticateDepotAsync(depotId, depotKey, cdnAuthToken).ConfigureAwait(continueOnCapturedContext: false);
			}
			catch (Exception ex)
			{
				client = null;
				Console.WriteLine("Failed to connect to content server {0}: {1}", server, ex.Message);
				ContentServerPenalty.TryGetValue(server.Host, out var penalty);
				ContentServerPenalty[server.Host] = penalty + 1;
			}
		}
		Console.WriteLine("Initialized connection to content server {0} with depot id {1}", server, depotId);
		activeClientAuthed[client] = Tuple.Create(depotId, server);
		return client;
	}

	private async Task RequestCDNAuthToken(uint appid, uint depotId, string hostname)
	{
		hostname = ResolveCDNTopLevelHost(hostname);
		SteamApps.CDNAuthTokenCallback callback = await SteamSession.SteamApps.GetCDNAuthToken(appid, depotId, hostname);
		if (callback.Result == EResult.OK)
		{
			string cdnKey = $"{depotId:D}:{ResolveCDNTopLevelHost(hostname)}";
			CDNAuthTokens[cdnKey] = callback;
		}
	}

	private async Task<bool> ReauthConnectionAsync(CDNClient client, CDNClient.Server server, uint appId, uint depotId, byte[] depotKey)
	{
		string cdnAuthToken = null;
		try
		{
			if (server.Type == "CDN")
			{
				await RequestCDNAuthToken(appId, depotId, server.Host);
				string cdnKey = $"{depotId:D}:{ResolveCDNTopLevelHost(server.Host)}";
				if (!CDNAuthTokens.TryGetValue(cdnKey, out var authTokenCallback))
				{
					throw new Exception($"Failed to retrieve CDN token for server {server.Host} depot {depotId}");
				}
				cdnAuthToken = authTokenCallback.Token;
			}
			await client.AuthenticateDepotAsync(depotId, depotKey, cdnAuthToken).ConfigureAwait(continueOnCapturedContext: false);
			activeClientAuthed[client] = Tuple.Create(depotId, server);
			return true;
		}
		catch (Exception ex)
		{
			Exception ex2 = ex;
			Console.WriteLine("Failed to reauth to content server {0}: {1}", server, ex2.Message);
		}
		return false;
	}

	public async Task<CDNClient> GetConnectionForDepotAsync(uint appId, uint depotId, byte[] depotKey, CancellationToken token)
	{
		activeClientPool.TryTake(out var client);
		if (client == null)
		{
			client = await BuildConnectionAsync(appId, depotId, depotKey, null, token).ConfigureAwait(continueOnCapturedContext: false);
		}
		if (!activeClientAuthed.TryGetValue(client, out var authData) || authData.Item1 != depotId)
		{
			bool flag = authData.Item2.Type == "CDN";
			bool flag2 = flag;
			if (flag2)
			{
				flag2 = await ReauthConnectionAsync(client, authData.Item2, appId, depotId, depotKey).ConfigureAwait(continueOnCapturedContext: false);
			}
			if (flag2)
			{
				Console.WriteLine("Re-authed CDN connection to content server {0} from {1} to {2}", authData.Item2, authData.Item1, depotId);
			}
			else
			{
				bool flag3 = authData.Item2.Type == "CS";
				bool flag4 = flag3;
				if (flag4)
				{
					flag4 = await ReauthConnectionAsync(client, authData.Item2, appId, depotId, depotKey).ConfigureAwait(continueOnCapturedContext: false);
				}
				if (flag4)
				{
					Console.WriteLine("Re-authed anonymous connection to content server {0} from {1} to {2}", authData.Item2, authData.Item1, depotId);
				}
				else
				{
					ReleaseConnection(client);
					client = await BuildConnectionAsync(appId, depotId, depotKey, authData.Item2, token).ConfigureAwait(continueOnCapturedContext: false);
				}
			}
		}
		return client;
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
}
