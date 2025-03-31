using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace SteamContentPackager.UI.DragAndDrop;

public class DropTargetHighlightAdorner : DropTargetAdorner
{
	[Obsolete("This constructor is obsolete and will be deleted in next major release.")]
	public DropTargetHighlightAdorner(UIElement adornedElement)
		: base(adornedElement, null)
	{
	}

	public DropTargetHighlightAdorner(UIElement adornedElement, DropInfo dropInfo)
		: base(adornedElement, dropInfo)
	{
	}

	protected override void OnRender(DrawingContext drawingContext)
	{
		DropInfo dropInfo = base.DropInfo;
		UIElement visualTargetItem = dropInfo.VisualTargetItem;
		if (visualTargetItem != null)
		{
			Rect rectangle = Rect.Empty;
			if (visualTargetItem is TreeViewItem treeViewItem && VisualTreeHelper.GetChildrenCount(treeViewItem) > 0)
			{
				Rect descendantBounds = VisualTreeHelper.GetDescendantBounds(treeViewItem);
				Point location = treeViewItem.TranslatePoint(default(Point), base.AdornedElement);
				Rect rect = new Rect(location, treeViewItem.RenderSize);
				descendantBounds.Union(rect);
				location.Offset(1.0, 0.0);
				rectangle = new Rect(location, new Size(descendantBounds.Width - location.X - 1.0, treeViewItem.ActualHeight));
			}
			if (rectangle.IsEmpty)
			{
				rectangle = new Rect(visualTargetItem.TranslatePoint(default(Point), base.AdornedElement), VisualTreeHelper.GetDescendantBounds(visualTargetItem).Size);
			}
			drawingContext.DrawRoundedRectangle(null, base.Pen, rectangle, 2.0, 2.0);
		}
	}
}
