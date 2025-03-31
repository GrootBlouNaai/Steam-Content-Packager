using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;

namespace SteamContentPackager.Utils;

internal class ListBoxLogHandler : Log.Handler
{
	public ObservableCollection<string> LogMessages = new ObservableCollection<string>();

	private ListBox _listBox;

	public ListBoxLogHandler(ListBox listBox)
	{
		_listBox = listBox;
		listBox.ItemsSource = LogMessages;
	}

	public void ClearLog()
	{
		LogMessages.Clear();
	}

	public void SetListBox(ListBox listBox)
	{
		_listBox = listBox;
		listBox.ItemsSource = LogMessages;
	}

	public override void OnMessageReceived(Log.Message message)
	{
		Application.Current?.Dispatcher?.Invoke(delegate
		{
			LogMessages.Add(FormatMessage(message));
			_listBox.SelectedIndex = _listBox.Items.Count - 1;
			_listBox.ScrollIntoView(_listBox.SelectedItem);
		});
	}
}
