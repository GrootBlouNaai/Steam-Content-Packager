using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;
using SteamContentPackager.Annotations;
using SteamContentPackager.Steam;
using SteamContentPackager.Utils;
using SteamKit2;
using SteamKit2.Types;

namespace SteamContentPackager.UI.Controls;

public partial class AppList : UserControl, INotifyPropertyChanged, IComponentConnector
{
	public static readonly DependencyProperty SelectedAppProperty = DependencyProperty.Register("SelectedApp", typeof(SteamApp), typeof(AppList), new PropertyMetadata((object)null));

	private string _appFilter;

	private List<SteamApp> _apps { get; set; } = new List<SteamApp>();

	public ObservableCollection<SteamApp> FilteredApps { get; set; } = new ObservableCollection<SteamApp>();

	public string AppFilter
	{
		get
		{
			return _appFilter;
		}
		set
		{
			_appFilter = value;
			OnPropertyChanged("AppFilter");
			OnFilterChanged();
		}
	}

	public SteamApp SelectedApp
	{
		get
		{
			return (SteamApp)GetValue(SelectedAppProperty);
		}
		set
		{
			SetValue(SelectedAppProperty, value);
		}
	}

	public event PropertyChangedEventHandler PropertyChanged;

	private void OnFilterChanged()
	{
		FilteredApps = new ObservableCollection<SteamApp>(_apps.Where((SteamApp x) => x.ToString().ToLower().Contains(_appFilter.ToLower())));
		OnPropertyChanged("FilteredApps");
	}

	public AppList()
	{
		PICSUpdater.UpdateComplete += PICSUpdaterOnAppListUpdated;
		InitializeComponent();
	}

	private void PICSUpdaterOnAppListUpdated(object sender, PICSUpdater.UpdateCompleteEventArgs eventArgs)
	{
		Stopwatch stopwatch = new Stopwatch();
		stopwatch.Start();
		List<uint> installedApps = SteamContentPackager.Steam.Utils.InstalledApps;
		HashSet<uint> hashSet = new HashSet<uint>(_apps.Select((SteamApp x) => x.Appid));
		foreach (AppInfoCache.Item value in SteamSession.AppInfo.Items.Values)
		{
			if (eventArgs.Owners.ContainsKey(value.AppId) && !hashSet.Contains(value.AppId))
			{
				KeyValue keyValue = value.KeyValues["common"];
				string text = keyValue["type"]?.Value?.ToLower() ?? "Unknown";
				if (text == "game" || text == "application" || text == "tool")
				{
					_apps.Add(new SteamApp(value.AppId, keyValue["name"].Value, eventArgs.Owners[value.AppId], installedApps.Contains(value.AppId)));
				}
			}
		}
		stopwatch.Stop();
		_apps = _apps.OrderBy((SteamApp x) => x.Name).ToList();
		Application.Current.Dispatcher.Invoke(delegate
		{
			FilteredApps = new ObservableCollection<SteamApp>(_apps);
		});
		Log.Write($"Loaded {_apps.Count} Apps: {stopwatch.Elapsed}");
		OnPropertyChanged("FilteredApps");
	}

	[NotifyPropertyChangedInvocator]
	protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
	{
		this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
	}
}
