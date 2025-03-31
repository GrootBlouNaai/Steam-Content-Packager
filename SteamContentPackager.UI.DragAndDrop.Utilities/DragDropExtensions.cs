using System.Windows;

namespace SteamContentPackager.UI.DragAndDrop.Utilities;

public static class DragDropExtensions
{
	public static bool IsDragSourceIgnored(this UIElement element)
	{
		return false;
	}

	public static bool IsDragSource(this UIElement element)
	{
		return element != null && DragDrop.GetIsDragSource(element);
	}

	public static bool IsDropTarget(this UIElement element)
	{
		return element != null && DragDrop.GetIsDropTarget(element);
	}
}
