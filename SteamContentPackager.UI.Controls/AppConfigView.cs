using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;
using SteamContentPackager.Annotations;
using SteamContentPackager.Packing;
using SteamContentPackager.Plugin;
using SteamContentPackager.Steam;
using SteamContentPackager.Utils;
using SteamKit2;

namespace SteamContentPackager.UI.Controls;

public partial class AppConfigView : UserControl, INotifyPropertyChanged, IComponentConnector
{
	public static readonly DependencyProperty AppProperty = DependencyProperty.Register("App", typeof(SteamApp), typeof(AppConfigView), new PropertyMetadata(null, AppChangedCallback));

	private AppBranch _branch;

	private PluginManager.PluginInfo _selectedPlugin;

	private UserControl _pluginControl;

	private static AppConfigView _instance;

	private AppConfig _appConfig;

	public ObservableCollection<string> Languages { get; } = new ObservableCollection<string>();

	public ObservableCollection<string> OperatingSystems { get; } = new ObservableCollection<string>();

	public ObservableCollection<string> Architectures { get; } = new ObservableCollection<string>();

	public ObservableCollection<AppBranch> Branches { get; } = new ObservableCollection<AppBranch>();

	public List<PluginManager.PluginInfo> Plugins { get; }

	public PluginManager.PluginInfo SelectedPlugin
	{
		get
		{
			return _selectedPlugin;
		}
		set
		{
			_selectedPlugin = value;
			OnPropertyChanged("SelectedPlugin");
			PluginControl = (UserControl)Activator.CreateInstance(_selectedPlugin.ControlType);
		}
	}

	public UserControl PluginControl
	{
		get
		{
			return _pluginControl;
		}
		set
		{
			_pluginControl = value;
			OnPropertyChanged("PluginControl");
		}
	}

	public SteamApp App
	{
		get
		{
			return (SteamApp)GetValue(AppProperty);
		}
		set
		{
			SetValue(AppProperty, value);
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
		}
	}

	public AppConfig AppConfig
	{
		get
		{
			return _appConfig;
		}
		set
		{
			_appConfig = value;
			OnPropertyChanged("AppConfig");
		}
	}

	public event PropertyChangedEventHandler PropertyChanged;

	private static void AppChangedCallback(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs args)
	{
		if (args.NewValue != null)
		{
			AppConfigView appConfigView = (AppConfigView)dependencyObject;
			appConfigView.Refresh();
		}
	}

	public AppConfigView()
	{
		Plugins = PluginManager.LoadPlugins();
		if (Plugins.Count > 0)
		{
			SelectedPlugin = Plugins[0];
		}
		_instance = this;
		InitializeComponent();
	}

	public void Refresh()
	{
		AppConfig = new AppConfig(App);
		KeyValue keyValues = SteamSession.AppInfo.Items[App.Appid].KeyValues;
		if (!App.Installed)
		{
			GetBranches(keyValues);
			GetLanguages(keyValues);
			GetOperatingSystems(keyValues);
			GetArchitectures(keyValues);
		}
		else
		{
			KeyValue keyValue = KeyValue.LoadAsText(SteamContentPackager.Steam.Utils.GetACFByAppid(App.Appid));
			Languages.Add(keyValue["userconfig"]["language"]?.Value);
			IEnumerable<AppBranch> source = keyValues["depots"]["branches"].Children.Select((KeyValue x) => new AppBranch(x.Name, x["pwdrequired"].AsBoolean(), x["buildId"].AsUnsignedInteger()));
			string branchName = keyValue["userconfig"]["betaKey"]?.Value ?? "public";
			Branches.Add(source.First((AppBranch x) => x.Name == branchName));
			OperatingSystems.Add("windows");
			AppConfig.Language = Languages[0];
			AppConfig.Branch = Branches[0];
			AppConfig.OperatingSystem = OperatingSystems[0];
		}
		AppConfig.ShouldRefresh = true;
		AppConfig.RefreshDepots();
	}

	private void GetBranches(KeyValue keyValues)
	{
		Branches.Clear();
		keyValues["depots"]["branches"].Children.ForEach(delegate(KeyValue x)
		{
			Branches.Add(new AppBranch(x.Name, x["pwdrequired"].AsBoolean(), x["buildId"].AsUnsignedInteger()));
		});
		AppConfig.Branch = Branches[0];
	}

	private void GetLanguages(KeyValue keyValues)
	{
		Languages.Clear();
		if (keyValues["common"]["languages"] != KeyValue.Invalid)
		{
			keyValues["common"]["languages"].Children.ForEach(delegate(KeyValue x)
			{
				Languages.Add(x.Name);
			});
		}
		if (!string.IsNullOrEmpty(keyValues["depots"]["baselanguages"]?.Value))
		{
			keyValues["depots"]["baselanguages"].Value.Split(',').ToList().ForEach(delegate(string x)
			{
				Languages.Add(x);
			});
		}
		foreach (KeyValue child in keyValues["depots"].Children)
		{
			if (!string.IsNullOrEmpty(child["config"]["language"].Value))
			{
				string value = child["config"]["language"].Value;
				if (!Languages.Contains(value) && !string.IsNullOrEmpty(value))
				{
					Languages.Add(value);
				}
			}
		}
		string[] array = Languages.ToArray();
		foreach (string text in array)
		{
			if (string.IsNullOrEmpty(text))
			{
				Languages.Remove(text);
			}
		}
		if (!Languages.Contains(Config.DefaultLanguage))
		{
			Languages.Add(Config.DefaultLanguage);
		}
		AppConfig.Language = Languages[Languages.IndexOf(Config.DefaultLanguage)];
	}

	private void GetOperatingSystems(KeyValue keyValues)
	{
		OperatingSystems.Clear();
		if (keyValues["common"]["oslist"] != KeyValue.Invalid && !string.IsNullOrEmpty(keyValues["common"]["oslist"].Value))
		{
			keyValues["common"]["oslist"].Value?.Split(',').ToList().ForEach(delegate(string x)
			{
				OperatingSystems.Add(x);
			});
		}
		foreach (KeyValue child in keyValues["depots"].Children)
		{
			if (child["config"]["oslist"] == KeyValue.Invalid)
			{
				continue;
			}
			string[] array = child["config"]["oslist"].Value?.Split(',') ?? new string[0];
			foreach (string text in array)
			{
				if (!OperatingSystems.Contains(text) && !string.IsNullOrEmpty(text))
				{
					OperatingSystems.Add(text);
				}
			}
		}
		if (OperatingSystems.Count == 0)
		{
			OperatingSystems.Add("windows");
		}
		AppConfig.OperatingSystem = OperatingSystems[OperatingSystems.IndexOf("windows")];
	}

	private void GetArchitectures(KeyValue keyValues)
	{
		Architectures.Clear();
		foreach (KeyValue child in keyValues["depots"].Children)
		{
			if (!string.IsNullOrEmpty(child["config"]["osarch"].Value))
			{
				string value = child["config"]["osarch"].Value;
				if (!Architectures.Contains(value))
				{
					Architectures.Add(value);
				}
			}
		}
		if (Architectures.Count > 0)
		{
			AppConfig.Arch = (Architectures.Contains("64") ? Architectures[Architectures.IndexOf("64")] : Architectures[0]);
		}
	}

	public static BasePlugin CreatePluginInstance(PackageTask parentTaskOld)
	{
		PluginArgs pluginArgs = (PluginArgs)_instance.PluginControl.DataContext.Copy();
		return (BasePlugin)Activator.CreateInstance(_instance.SelectedPlugin.PluginType, pluginArgs, parentTaskOld);
	}

	[NotifyPropertyChangedInvocator]
	protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
	{
		this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
	}

	private async void CheckPassword_Clicked(object sender, RoutedEventArgs e)
	{
		Log.Write("Checking branch password");
		SteamApps.CheckAppBetaPasswordCallback callback = await SteamSession.SteamApps.CheckAppBetaPassword(App.Appid, AppConfig.Branch.Password);
		if (callback.Result != EResult.OK)
		{
			Log.Write($"Invalid branch password: {callback.Result}");
		}
		else if (callback.Result == EResult.OK)
		{
			Log.Write($"Branch password accepted: {callback.BetaPasswords.First()}");
			_appConfig.Branch.BetaPasswords = callback.BetaPasswords;
			_appConfig.RefreshDepots();
		}
	}
}
