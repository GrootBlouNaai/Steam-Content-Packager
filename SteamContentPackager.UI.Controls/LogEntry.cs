using System;
using System.ComponentModel;
using System.Globalization;
using System.Runtime.CompilerServices;
using SteamContentPackager.Annotations;

namespace SteamContentPackager.UI.Controls;

public class LogEntry : INotifyPropertyChanged
{
	private string _result;

	public int LineNumber;

	public string Message { get; set; }

	public string Time { get; set; }

	public LogLevel LogLevel { get; set; }

	public string Result
	{
		get
		{
			return _result;
		}
		set
		{
			_result = value;
			OnPropertyChanged("Result");
		}
	}

	public event PropertyChangedEventHandler PropertyChanged;

	public LogEntry(string message, LogLevel logLevel)
	{
		Message = message;
		Time = DateTime.Now.ToString(CultureInfo.InvariantCulture);
		LogLevel = logLevel;
	}

	[NotifyPropertyChangedInvocator]
	protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
	{
		this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
	}
}
