using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;
using SteamContentPackager.Annotations;

namespace SteamContentPackager.UI.Controls;

public partial class LogViewer : UserControl, INotifyPropertyChanged, IComponentConnector
{
	private ObservableCollection<LogEntry> _entries;

	public ObservableCollection<LogEntry> Entries
	{
		get
		{
			return _entries;
		}
		set
		{
			_entries = value;
			OnPropertyChanged("Entries");
		}
	}

	public event PropertyChangedEventHandler PropertyChanged;

	public LogViewer()
	{
		InitializeComponent();
		Logger.Init(this);
	}

	[NotifyPropertyChangedInvocator]
	protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
	{
		this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
	}

	private void LogViewer_OnIsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
	{
		if ((bool)e.NewValue && ListBox.Items.Count > 0)
		{
			ListBox.ScrollIntoView(ListBox.Items[ListBox.Items.Count - 1]);
		}
	}
}
