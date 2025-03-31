using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using SteamContentPackager.Annotations;
using SteamContentPackager.Steam;
using SteamContentPackager.Tasks;
using SteamContentPackager.UI;
using SteamContentPackager.UI.Controls;
using SteamContentPackager.Utils;
using SteamKit2;

namespace SteamContentPackager;

public partial class MainWindow : Window, INotifyPropertyChanged, IComponentConnector, IStyleConnector
{
	private int _windowWidth = 500;

	private int _windowHeight = 450;

	private bool _loggedOn;

	private ObservableCollection<SteamApp> _installedApps;

	public LogWindow Log { get; set; }

	public bool LoggedOn
	{
		get
		{
			return _loggedOn;
		}
		set
		{
			_loggedOn = value;
			OnPropertyChanged("LoggedOn");
		}
	}

	public bool ShowSettings
	{
		get
		{
			return Settings.ShowAppSettings;
		}
		set
		{
			Settings.ShowAppSettings = value;
			OnPropertyChanged("ShowSettings");
		}
	}

	public bool ShowNotifications
	{
		get
		{
			return Settings.ShowNotifications;
		}
		set
		{
			Settings.ShowNotifications = value;
			OnPropertyChanged("ShowNotifications");
		}
	}

	public string OutputDirectory
	{
		get
		{
			return Settings.OutputDirectory;
		}
		set
		{
			Settings.OutputDirectory = value;
			OnPropertyChanged("OutputDirectory");
		}
	}

	public string HosterName
	{
		get
		{
			return Settings.HosterName;
		}
		set
		{
			Settings.HosterName = value;
			OnPropertyChanged("HosterName");
		}
	}

	public string UploaderName
	{
		get
		{
			return Settings.UploaderName;
		}
		set
		{
			Settings.UploaderName = value;
			OnPropertyChanged("UploaderName");
		}
	}

	public string LastOwner
	{
		get
		{
			return Settings.LastOwner;
		}
		set
		{
			Settings.LastOwner = value;
			OnPropertyChanged("LastOwner");
		}
	}

	public bool DownloadContent
	{
		get
		{
			return Settings.DownloadContent;
		}
		set
		{
			Settings.DownloadContent = value;
			OnPropertyChanged("DownloadContent");
		}
	}

	public bool GenerateBBCode
	{
		get
		{
			return Settings.GenerateBBCode;
		}
		set
		{
			Settings.GenerateBBCode = value;
			OnPropertyChanged("GenerateBBCode");
		}
	}

	public bool SlimTasks
	{
		get
		{
			return Settings.SlimTasks;
		}
		set
		{
			Settings.SlimTasks = value;
			TaskList.SlimTasks = value;
			OnPropertyChanged("SlimTasks");
		}
	}

	public bool WriteDepotInfo
	{
		get
		{
			return Settings.WriteDepotInfo;
		}
		set
		{
			Settings.WriteDepotInfo = value;
			OnPropertyChanged("WriteDepotInfo");
		}
	}

	public ObservableCollection<SteamApp> InstalledApps
	{
		get
		{
			return _installedApps;
		}
		set
		{
			_installedApps = value;
			OnPropertyChanged("InstalledApps");
		}
	}

	public int WindowHeight
	{
		get
		{
			if (Settings.WindowHeight >= 550)
			{
				return Settings.WindowHeight;
			}
			return 550;
		}
		set
		{
			Settings.WindowHeight = value;
			if (Settings.WindowHeight < 550)
			{
				Settings.WindowHeight = 550;
			}
			OnPropertyChanged("WindowHeight");
		}
	}

	public int WindowWidth
	{
		get
		{
			if (Settings.WindowWidth >= 500)
			{
				return Settings.WindowWidth;
			}
			return 500;
		}
		set
		{
			Settings.WindowWidth = value;
			if (Settings.WindowHeight < 500)
			{
				Settings.WindowHeight = 500;
			}
			OnPropertyChanged("WindowWidth");
		}
	}

	public event PropertyChangedEventHandler PropertyChanged;

	public MainWindow()
	{
		try
		{
			Log = new LogWindow();
			if (Log.ShowWindow)
			{
				Log.Show();
			}
			Logger.StartNewLog("Startup");
			Logger.WriteEntry("Launching", LogLevel.Info, writeToFile: false);
			if (!string.IsNullOrEmpty(Settings.SteamPath))
			{
				InstalledApps = new ObservableCollection<SteamApp>(GetInstalledApps());
			}
			else
			{
				base.IsEnabled = false;
				Thread thread = new Thread(GetSteamDir);
				thread.IsBackground = true;
				thread.SetApartmentState(ApartmentState.STA);
				thread.Start();
			}
			InitializeComponent();
		}
		catch (Exception ex)
		{
			System.Windows.Forms.MessageBox.Show(ex.ToString());
			throw;
		}
	}

	public void GetSteamDir()
	{
		FolderBrowserDialog fbd = new FolderBrowserDialog
		{
			Description = "Select Steam directory",
			ShowNewFolderButton = false
		};
		System.Windows.Application.Current.Dispatcher.Invoke(delegate
		{
			while (string.IsNullOrEmpty(Settings.SteamPath))
			{
				if (fbd.ShowDialog() == System.Windows.Forms.DialogResult.OK)
				{
					if (!File.Exists($"{fbd.SelectedPath}\\steam.exe"))
					{
						System.Windows.Forms.MessageBox.Show("Invalid Steam Directory");
					}
					else
					{
						Settings.SteamPath = fbd.SelectedPath;
					}
				}
				else
				{
					Environment.Exit(1);
				}
			}
			base.IsEnabled = true;
			InstalledApps = new ObservableCollection<SteamApp>(GetInstalledApps());
		});
	}

	public static List<SteamApp> GetInstalledApps()
	{
		List<SteamApp> steamApps = new List<SteamApp>();
		if (!Directory.Exists($"{Settings.SteamPath}\\steamapps"))
		{
			return steamApps;
		}
		if (!File.Exists($"{Settings.SteamPath}\\steamapps\\libraryfolders.vdf"))
		{
			return steamApps;
		}
		List<string> list = new List<string> { $"{Settings.SteamPath}\\steamapps" };
		foreach (KeyValue child in KeyValue.LoadAsText($"{Settings.SteamPath}\\steamapps\\libraryfolders.vdf").Children)
		{
			if (Directory.Exists(child.Value))
			{
				list.Add($"{child.Value}\\steamapps");
			}
		}
		list.ForEach(delegate(string library)
		{
			string[] files = Directory.GetFiles($"{library}");
			foreach (string text in files)
			{
				FileInfo fileInfo = new FileInfo(text);
				if (new Regex("^appmanifest_\\d+[.]acf$").Match(fileInfo.Name).Success)
				{
					try
					{
						steamApps.Add(new SteamApp(text));
					}
					catch (Exception)
					{
						Logger.WriteEntry($"Failed to read acf: {text}", LogLevel.Warning, writeToFile: false);
					}
				}
			}
		});
		Logger.WriteEntry($"Loaded {steamApps.Count} Apps", LogLevel.Info, writeToFile: false);
		return steamApps.OrderBy((SteamApp x) => x.Name).ToList();
	}

	private void MinimizeButton_OnClick(object sender, RoutedEventArgs e)
	{
		base.WindowState = WindowState.Minimized;
	}

	private void CloseButton_OnClick(object sender, RoutedEventArgs e)
	{
		Log.CloseWindow();
		Environment.Exit(0);
	}

	private void TitleMouseDown(object sender, MouseButtonEventArgs e)
	{
		if (e.LeftButton == MouseButtonState.Pressed)
		{
			DragMove();
		}
	}

	[NotifyPropertyChangedInvocator]
	protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
	{
		this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
	}

	private void RefreshAppListClick(object sender, RoutedEventArgs e)
	{
		InstalledApps = new ObservableCollection<SteamApp>(GetInstalledApps());
		AppList.ItemsSource = InstalledApps;
		OnPropertyChanged("InstalledApps");
	}

	private void AddClicked(object sender, RoutedEventArgs e)
	{
		if (Settings.OutputDirectory == string.Empty)
		{
			System.Windows.Forms.MessageBox.Show("Invalid output directory!");
		}
		else if (AppList.SelectedIndex != -1)
		{
			SteamApp steamApp = (SteamApp)AppList.SelectedItem;
			TaskList.AddTask(new PackageTask(steamApp));
		}
	}

	private void Button_Click(object sender, RoutedEventArgs e)
	{
		FolderBrowserDialog folderBrowserDialog = new FolderBrowserDialog();
		if (Directory.Exists(Settings.OutputDirectory))
		{
			folderBrowserDialog.SelectedPath = Settings.OutputDirectory;
		}
		if (folderBrowserDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
		{
			OutputDir_TB.Text = folderBrowserDialog.SelectedPath;
			Settings.OutputDirectory = OutputDir_TB.Text;
		}
	}

	private void LoginClicked(object sender, RoutedEventArgs e)
	{
		if (_loggedOn)
		{
			LoggedOn = false;
			SteamSession.Disconnect();
			LoginButton.Content = "LOGIN";
			return;
		}
		LoginWindow obj = new LoginWindow
		{
			WindowStartupLocation = WindowStartupLocation.CenterOwner,
			Owner = this
		};
		base.IsEnabled = false;
		obj.ShowDialog();
		base.IsEnabled = true;
		if (SteamSession.LoggedOn)
		{
			LoginButton.Content = "LOGOFF";
		}
	}

	private void QuickAddClicked(object sender, RoutedEventArgs e)
	{
		if (Settings.OutputDirectory == string.Empty)
		{
			System.Windows.Forms.MessageBox.Show("Invalid output directory!");
			return;
		}
		SteamApp steamApp = (SteamApp)FindParent<ComboBoxItem>((System.Windows.Controls.Button)e.Source).DataContext;
		TaskList.AddTask(new PackageTask(steamApp));
	}

	private static T FindParent<T>(DependencyObject child) where T : DependencyObject
	{
		DependencyObject parent = VisualTreeHelper.GetParent(child);
		if (parent == null)
		{
			return null;
		}
		return (parent as T) ?? FindParent<T>(parent);
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	internal Delegate _CreateDelegate(Type delegateType, string handler)
	{
		return Delegate.CreateDelegate(delegateType, this, handler);
	}
}
