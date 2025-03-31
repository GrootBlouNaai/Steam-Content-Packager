using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using SteamContentPackager.Utils;
using SteamKit2;
using SteamKit2.Types;

namespace SteamContentPackager.Steam;

public class PICSUpdater
{
	public class UpdateCompleteEventArgs : EventArgs
	{
		public Dictionary<uint, uint> Owners;
	}

	private ReadOnlyCollection<SteamApps.LicenseListCallback.License> _licenses;

	public static HashSet<uint> OwnedDepots;

	public static HashSet<uint> OwnedApps;

	private uint _lastUpdated;

	private SemaphoreSlim _rateLimiter = new SemaphoreSlim(1);

	public static event EventHandler<UpdateCompleteEventArgs> UpdateComplete;

	public PICSUpdater()
	{
		SteamSession.CallbackManager.Subscribe<SteamApps.LicenseListCallback>(OnLicenseListReceived);
	}

	private async void OnLicenseListReceived(SteamApps.LicenseListCallback licenseListCallback)
	{
		await _rateLimiter.WaitAsync();
		SteamSession.PackageInfo = new PackageInfo($"{AppDomain.CurrentDomain.BaseDirectory}\\appcache\\packageinfo.vdf");
		_licenses = licenseListCallback.LicenseList;
		List<uint> ownedPackages = _licenses.Select((SteamApps.LicenseListCallback.License x) => x.PackageID).ToList();
		await UpdatePackageInfo(await GetUpdatePackages(ownedPackages));
		OwnedApps = new HashSet<uint>(SteamSession.PackageInfo.Items.Where((KeyValuePair<uint, PackageInfo.Item> x) => ownedPackages.Contains(x.Key)).SelectMany((KeyValuePair<uint, PackageInfo.Item> y) => y.Value.Appids).Distinct());
		OwnedDepots = new HashSet<uint>(SteamSession.PackageInfo.Items.Where((KeyValuePair<uint, PackageInfo.Item> x) => ownedPackages.Contains(x.Key)).SelectMany((KeyValuePair<uint, PackageInfo.Item> y) => y.Value.DepotIds).Distinct());
		await UpdateAppInfo(await GetUpdatedApps(OwnedApps));
		Config.LastPicsChangeNumber = _lastUpdated;
		Config.Save();
		Dictionary<uint, uint> packageOwners = _licenses.ToDictionary((SteamApps.LicenseListCallback.License license) => license.PackageID, (SteamApps.LicenseListCallback.License license) => license.OwnerId);
		Dictionary<uint, uint> appOwners = new Dictionary<uint, uint>();
		foreach (PackageInfo.Item packageInfoItem in SteamSession.PackageInfo.Items.Values)
		{
			foreach (uint appid in packageInfoItem.Appids)
			{
				if (!appOwners.ContainsKey(appid))
				{
					if (packageOwners.ContainsKey(packageInfoItem.Id))
					{
						appOwners[appid] = packageOwners[packageInfoItem.Id];
					}
					else
					{
						appOwners[appid] = 0u;
					}
				}
			}
		}
		_rateLimiter.Release();
		PICSUpdater.UpdateComplete?.Invoke(this, new UpdateCompleteEventArgs
		{
			Owners = appOwners
		});
	}

	private async Task<List<uint>> GetUpdatePackages(List<uint> ownedPackages)
	{
		if (SteamSession.PackageInfo.Items.Count == 0)
		{
			Config.LastPicsChangeNumber = 0u;
		}
		SteamApps.PICSChangesCallback packageCallback = await SteamSession.SteamApps.PICSGetChangesSince(Config.LastPicsChangeNumber, sendAppChangelist: false, sendPackageChangelist: true);
		_lastUpdated = packageCallback.CurrentChangeNumber;
		List<uint> updatedPackageIds;
		if (packageCallback.RequiresFullPackageUpdate || packageCallback.RequiresFullUpdate)
		{
			Log.Write("Full package update required");
			updatedPackageIds = ownedPackages;
		}
		else
		{
			updatedPackageIds = (from change in packageCallback.PackageChanges.Values
				where ownedPackages.Contains(change.ID)
				select change into x
				select x.ID).ToList();
			updatedPackageIds.AddRange(ownedPackages.Where((uint x) => !SteamSession.PackageInfo.Items.ContainsKey(x)));
		}
		return updatedPackageIds;
	}

	private async Task<List<uint>> GetUpdatedApps(ICollection<uint> ownedAppids)
	{
		SteamApps.PICSChangesCallback appCallback = await SteamSession.SteamApps.PICSGetChangesSince(Config.LastPicsChangeNumber);
		List<uint> updatedAppids;
		if (appCallback.RequiresFullAppUpdate || appCallback.RequiresFullUpdate)
		{
			Log.Write("Full app update required");
			updatedAppids = ownedAppids.ToList();
			if (!updatedAppids.Contains(228980u))
			{
				updatedAppids.Add(228980u);
			}
		}
		else
		{
			updatedAppids = (from change in appCallback.AppChanges.Values
				where ownedAppids.Contains(change.ID)
				select change into x
				select x.ID).ToList();
			updatedAppids.AddRange(ownedAppids.Where((uint x) => !SteamSession.AppInfo.Items.ContainsKey(x)));
		}
		return updatedAppids;
	}

	private async Task UpdatePackageInfo(IList<uint> packageIds)
	{
		if (packageIds.Count == 0)
		{
			return;
		}
		Log.Write($"Updating {packageIds.Count} packages");
		IEnumerable<IEnumerable<uint>> packageIdSets = packageIds.Chunk(Config.MaxPICSRequests);
		foreach (IEnumerable<uint> packageIdSet in packageIdSets)
		{
			foreach (SteamApps.PICSProductInfoCallback.PICSProductInfo productInfo in (await SteamSession.SteamApps.PICSGetProductInfo(new List<uint>(), packageIdSet)).Results.SelectMany((SteamApps.PICSProductInfoCallback x) => x.Packages.Values))
			{
				PackageInfo.Item packageInfoItem = new PackageInfo.Item(productInfo, 0u);
				if (SteamSession.PackageInfo.Items.ContainsKey(productInfo.ID))
				{
					SteamSession.PackageInfo.Items[productInfo.ID] = packageInfoItem;
				}
				else
				{
					SteamSession.PackageInfo.Items.Add(productInfo.ID, packageInfoItem);
				}
			}
		}
		SteamSession.PackageInfo.Save($"{AppDomain.CurrentDomain.BaseDirectory}\\appcache\\packageinfo.vdf");
		Log.Write("PackageInfo update complete");
	}

	private async Task UpdateAppInfo(ICollection<uint> appIds)
	{
		if (appIds.Count == 0)
		{
			return;
		}
		Log.Write($"Updating {appIds.Count} apps");
		SteamSession.AppInfo = new AppInfoCache($"{AppDomain.CurrentDomain.BaseDirectory}\\appcache\\appinfo.vdf");
		IEnumerable<IEnumerable<uint>> appIdSets = appIds.Chunk(Config.MaxPICSRequests);
		List<uint> nonpublicAppids = new List<uint>();
		foreach (IEnumerable<uint> appIdSet in appIdSets)
		{
			foreach (SteamApps.PICSProductInfoCallback.PICSProductInfo picsProductInfo in (await SteamSession.SteamApps.PICSGetProductInfo(appIdSet, new List<uint>(), onlyPublic: false)).Results.SelectMany((SteamApps.PICSProductInfoCallback x) => x.Apps.Values))
			{
				AppInfoCache.Item item = new AppInfoCache.Item(picsProductInfo);
				if ((item.Type == "game" || item.Type == "application" || item.Type == "config" || item.Type == "tool") && item.IsPublicOnly)
				{
					Log.Write($"AppToken Required for app: {item.AppId}");
					nonpublicAppids.Add(item.AppId);
				}
				else if (SteamSession.AppInfo.Items.ContainsKey(picsProductInfo.ID))
				{
					SteamSession.AppInfo.Items[picsProductInfo.ID] = item;
				}
				else
				{
					SteamSession.AppInfo.Items.Add(picsProductInfo.ID, item);
				}
			}
		}
		if (nonpublicAppids.Count > 0)
		{
			Log.Write($"Requesting PICS tokens for {nonpublicAppids.Count} apps");
			appIdSets = nonpublicAppids.Chunk(25);
			Dictionary<uint, ulong> appTokens = new Dictionary<uint, ulong>();
			foreach (IEnumerable<uint> appTokenSet in appIdSets)
			{
				foreach (KeyValuePair<uint, ulong> keyValuePair in (await SteamSession.SteamApps.PICSGetAccessTokens(appTokenSet, new List<uint>())).AppTokens)
				{
					appTokens.Add(keyValuePair.Key, keyValuePair.Value);
				}
			}
			appIdSets = nonpublicAppids.Chunk(Config.MaxPICSRequests);
			foreach (IEnumerable<uint> appIdSet2 in appIdSets)
			{
				IEnumerable<SteamApps.PICSRequest> picsRequests = from x in appIdSet2
					where appTokens.ContainsKey(x)
					select new SteamApps.PICSRequest(x, appTokens[x], only_public: false);
				foreach (SteamApps.PICSProductInfoCallback.PICSProductInfo picsProductInfo2 in (await SteamSession.SteamApps.PICSGetProductInfo(picsRequests, new List<SteamApps.PICSRequest>())).Results.SelectMany((SteamApps.PICSProductInfoCallback x) => x.Apps.Values))
				{
					AppInfoCache.Item item2 = new AppInfoCache.Item(picsProductInfo2);
					SteamSession.AppInfo.Items[picsProductInfo2.ID] = item2;
				}
			}
		}
		SteamSession.AppInfo.Save($"{AppDomain.CurrentDomain.BaseDirectory}\\appcache\\appinfo.vdf");
		Log.Write("Appinfo update complete");
	}
}
