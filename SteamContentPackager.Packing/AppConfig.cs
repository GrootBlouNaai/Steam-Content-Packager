using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using SteamContentPackager.Plugin;
using SteamContentPackager.Steam;
using SteamContentPackager.UI;
using SteamKit2;

namespace SteamContentPackager.Packing;

public class AppConfig : BindableBase
{
	private AppBranch _branch;

	private string _language;

	private string _os;

	private string _arch;

	public bool ShouldRefresh;

	public string LibraryFolder;

	public string AppInstallDir;

	public BasePlugin Plugin;

	public SteamApp SteamApp { get; }

	public ObservableCollection<SteamApp.Depot> Depots { get; } = new ObservableCollection<SteamApp.Depot>();

	public string Language
	{
		get
		{
			return _language;
		}
		set
		{
			_language = value;
			OnPropertyChanged("Language");
			RefreshDepots();
		}
	}

	public string OperatingSystem
	{
		get
		{
			return _os;
		}
		set
		{
			_os = value;
			OnPropertyChanged("OperatingSystem");
			RefreshDepots();
		}
	}

	public string Arch
	{
		get
		{
			return _arch;
		}
		set
		{
			_arch = value;
			OnPropertyChanged("Arch");
			RefreshDepots();
		}
	}

	public AppBranch Branch
	{
		get
		{
			return _branch;
		}
		set
		{
			_branch = value;
			OnPropertyChanged("Branch");
			RefreshDepots();
		}
	}

	public AppConfig(SteamApp steamapp)
	{
		SteamApp = steamapp;
		KeyValue keyValues = SteamSession.AppInfo.Items[SteamApp.Appid].KeyValues;
		LibraryFolder = (SteamApp.Installed ? new FileInfo(SteamContentPackager.Steam.Utils.GetACFByAppid(SteamApp.Appid)).Directory.Parent.FullName : $"{Config.LibraryFolder}");
		AppInstallDir = keyValues["config"]["installdir"].Value;
	}

	public void RefreshDepots()
	{
		if (!ShouldRefresh)
		{
			return;
		}
		Depots.Clear();
		List<KeyValue> children = SteamSession.AppInfo.Items[SteamApp.Appid].KeyValues["depots"].Children;
		if (SteamApp.Installed)
		{
			KeyValue keyValue = KeyValue.LoadAsText(SteamContentPackager.Steam.Utils.GetACFByAppid(SteamApp.Appid));
			KeyValue keyValue2 = SteamSession.AppInfo.Items[SteamApp.Appid].KeyValues["depots"];
			{
				foreach (KeyValue child in keyValue["mountedDepots"].Children)
				{
					if (uint.TryParse(child.Name, out var result) && result != SteamApp.Appid)
					{
						SteamApp.Depot item = new SteamApp.Depot(keyValue2[child.Name], SteamApp, Branch);
						Depots.Add(item);
					}
				}
				return;
			}
		}
		foreach (KeyValue item7 in children)
		{
			if (uint.TryParse(item7.Name, out var _))
			{
				SteamApp.Depot item2 = new SteamApp.Depot(item7, SteamApp, Branch);
				Depots.Add(item2);
			}
		}
		SteamApp.Depot[] array = Depots.Where((SteamApp.Depot x) => x.ManifestId == 0L && string.IsNullOrEmpty(x.EncryptedManifestId)).ToArray();
		foreach (SteamApp.Depot item3 in array)
		{
			Depots.Remove(item3);
		}
		if (Arch != null)
		{
			SteamApp.Depot[] array2 = Depots.Where((SteamApp.Depot depot) => depot.Architecture != null && depot.Architecture != Arch).ToArray();
			SteamApp.Depot[] array3 = array2;
			foreach (SteamApp.Depot item4 in array3)
			{
				Depots.Remove(item4);
			}
		}
		if (OperatingSystem != null)
		{
			SteamApp.Depot[] array4 = Depots.Where((SteamApp.Depot depot) => depot.OSList.Count > 0 && !depot.OSList.Contains(OperatingSystem)).ToArray();
			SteamApp.Depot[] array5 = array4;
			foreach (SteamApp.Depot item5 in array5)
			{
				Depots.Remove(item5);
			}
		}
		if (Language != null)
		{
			SteamApp.Depot[] array6 = Depots.Where((SteamApp.Depot depot) => depot.Language != null && !string.Equals(depot.Language, Language, StringComparison.CurrentCultureIgnoreCase)).ToArray();
			SteamApp.Depot[] array7 = array6;
			foreach (SteamApp.Depot item6 in array7)
			{
				Depots.Remove(item6);
			}
		}
	}
}
