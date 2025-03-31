using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Threading;
using SteamContentPackager.Steam;
using SteamKit2;

namespace SteamContentPackager.UI.Controls;

public partial class AppInfoView : UserControl, IComponentConnector
{
	public static readonly DependencyProperty AppProperty = DependencyProperty.Register("App", typeof(SteamApp), typeof(AppInfoView), new PropertyMetadata(null, PropertyChangedCallback));

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

	private static async void PropertyChangedCallback(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs dependencyPropertyChangedEventArgs)
	{
		if (dependencyPropertyChangedEventArgs.NewValue != null)
		{
			await ((AppInfoView)dependencyObject).Refresh();
		}
	}

	public AppInfoView()
	{
		InitializeComponent();
	}

	public async Task Refresh()
	{
		KeyValue kv = SteamSession.AppInfo.Items[App.Appid].KeyValues;
		await Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.Render, (Action)delegate
		{
			TreeView.Items.Clear();
			foreach (KeyValue child in kv.Children)
			{
				TreeView.Items.Add(CreateTreeViewItem(child));
			}
		});
	}

	public static KeyValueTreeItem CreateTreeViewItem(KeyValue keyValue)
	{
		KeyValueTreeItem keyValueTreeItem = new KeyValueTreeItem(keyValue);
		if (keyValue.Children.Count > 0)
		{
			keyValueTreeItem.ItemsSource = new List<object> { null };
		}
		return keyValueTreeItem;
	}

	private void TreeView_OnPreviewMouseRightButtonDown(object sender, MouseButtonEventArgs e)
	{
		TreeViewItem treeViewItem = VisualUpwardSearch(e.OriginalSource as DependencyObject);
		if (treeViewItem != null)
		{
			treeViewItem.IsSelected = true;
			e.Handled = false;
		}
	}

	private static TreeViewItem VisualUpwardSearch(DependencyObject source)
	{
		while (source != null && !(source is TreeViewItem))
		{
			source = VisualTreeHelper.GetParent(source);
		}
		return source as TreeViewItem;
	}
}
