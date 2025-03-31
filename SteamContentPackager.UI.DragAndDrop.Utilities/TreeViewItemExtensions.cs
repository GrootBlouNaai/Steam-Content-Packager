using System.Windows;
using System.Windows.Controls;

namespace SteamContentPackager.UI.DragAndDrop.Utilities;

public static class TreeViewItemExtensions
{
	public static Size GetHeaderSize(this TreeViewItem item)
	{
		if (item == null)
		{
			return Size.Empty;
		}
		FrameworkElement headerControl = item.GetHeaderControl();
		return (headerControl != null) ? new Size(headerControl.ActualWidth, headerControl.ActualHeight) : item.RenderSize;
	}

	public static FrameworkElement GetHeaderControl(this TreeViewItem item)
	{
		return item?.Template?.FindName("PART_Header", item) as FrameworkElement;
	}
}
