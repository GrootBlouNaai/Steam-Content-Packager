using System;

namespace SteamContentPackager.UI.DragAndDrop;

public class DropTargetAdorners
{
	public static Type Highlight { get; } = typeof(DropTargetHighlightAdorner);

	public static Type Insert { get; } = typeof(DropTargetInsertionAdorner);
}
