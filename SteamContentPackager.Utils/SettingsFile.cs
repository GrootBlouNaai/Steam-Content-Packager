using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Xml.Serialization;
using SteamContentPackager.Annotations;

namespace SteamContentPackager.Utils;

[Serializable]
public class SettingsFile : INotifyPropertyChanged
{
	[XmlElement]
	private SteamSettings _steam;

	[XmlElement]
	private OutputSettings _output;

	[XmlIgnore]
	public List<string> FailedServerList = new List<string>();

	public bool ShowLog;

	public bool ShowAppSettings;

	public bool ShowNotifications;

	public bool SlimTasks;

	public int WindowWidth = 650;

	public int WindowHeight = 600;

	public SteamSettings Steam
	{
		get
		{
			return _steam;
		}
		set
		{
			_steam = value;
			OnPropertyChanged("Steam");
		}
	}

	public OutputSettings Output
	{
		get
		{
			return _output;
		}
		set
		{
			_output = value;
			OnPropertyChanged("Output");
		}
	}

	public event PropertyChangedEventHandler PropertyChanged;

	public SettingsFile()
	{
		Steam = new SteamSettings();
		Output = new OutputSettings();
	}

	[NotifyPropertyChangedInvocator]
	protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
	{
		this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
	}
}
