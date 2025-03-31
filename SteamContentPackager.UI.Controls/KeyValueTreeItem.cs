using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using SteamKit2;

namespace SteamContentPackager.UI.Controls;

public class KeyValueTreeItem : TreeViewItem
{
	public static readonly DependencyProperty KeyValueProperty = DependencyProperty.Register("KeyValue", typeof(KeyValue), typeof(KeyValueTreeItem), new PropertyMetadata(null));

	public KeyValue KeyValue
	{
		get
		{
			return (KeyValue)GetValue(KeyValueProperty);
		}
		set
		{
			SetValue(KeyValueProperty, value);
		}
	}

	public RelayCommand CopyToClipboardCommand => new RelayCommand(Execute);

	private void Execute(object o)
	{
		Clipboard.SetText(KeyValue.ToText());
	}

	public KeyValueTreeItem(KeyValue keyValue)
	{
		KeyValue = keyValue;
		base.Style = Application.Current.FindResource("KeyValueTreeItemStyle") as Style;
	}

	protected override void OnExpanded(RoutedEventArgs e)
	{
		base.ItemsSource = KeyValue.Children.Select(Create);
		base.OnExpanded(e);
	}

	public static KeyValueTreeItem Create(KeyValue kv)
	{
		KeyValueTreeItem keyValueTreeItem = new KeyValueTreeItem(kv);
		if (kv.Children.Count > 0)
		{
			keyValueTreeItem.ItemsSource = new List<object> { null };
		}
		return keyValueTreeItem;
	}
}
