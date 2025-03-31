using System.Collections;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using SteamContentPackager.UI.DragAndDrop.Utilities;

namespace SteamContentPackager.UI.DragAndDrop;

public class DragInfo : IDragInfo
{
	public object Data { get; set; }

	public Point DragStartPosition { get; private set; }

	public Point PositionInDraggedItem { get; private set; }

	public DragDropEffects Effects { get; set; }

	public MouseButton MouseButton { get; private set; }

	public IEnumerable SourceCollection { get; private set; }

	public int SourceIndex { get; private set; }

	public object SourceItem { get; private set; }

	public IEnumerable SourceItems { get; private set; }

	public CollectionViewGroup SourceGroup { get; private set; }

	public UIElement VisualSource { get; private set; }

	public UIElement VisualSourceItem { get; private set; }

	public FlowDirection VisualSourceFlowDirection { get; private set; }

	public IDataObject DataObject { get; set; }

	public DragDropKeyStates DragDropCopyKeyState { get; private set; }

	public DragInfo(object sender, MouseButtonEventArgs e)
	{
		DragStartPosition = e.GetPosition((IInputElement)sender);
		Effects = DragDropEffects.None;
		MouseButton = e.ChangedButton;
		VisualSource = sender as UIElement;
		DragDropCopyKeyState = DragDrop.GetDragDropCopyKeyState(VisualSource);
		UIElement uIElement = e.OriginalSource as UIElement;
		if (uIElement == null && e.OriginalSource is FrameworkContentElement)
		{
			uIElement = ((FrameworkContentElement)e.OriginalSource).Parent as UIElement;
		}
		if (sender is ItemsControl)
		{
			ItemsControl itemsControl = (ItemsControl)sender;
			SourceGroup = itemsControl.FindGroup(DragStartPosition);
			VisualSourceFlowDirection = itemsControl.GetItemsPanelFlowDirection();
			UIElement uIElement2 = null;
			if (uIElement != null)
			{
				uIElement2 = itemsControl.GetItemContainer(uIElement);
			}
			if (uIElement2 == null)
			{
				uIElement2 = ((!DragDrop.GetDragDirectlySelectedOnly(VisualSource)) ? itemsControl.GetItemContainerAt(e.GetPosition(itemsControl), itemsControl.GetItemsPanelOrientation()) : itemsControl.GetItemContainerAt(e.GetPosition(itemsControl)));
			}
			if (uIElement2 != null)
			{
				PositionInDraggedItem = e.GetPosition(uIElement2);
				ItemsControl itemsControl2 = ItemsControl.ItemsControlFromItemContainer(uIElement2);
				if (itemsControl2 != null)
				{
					SourceCollection = itemsControl2.ItemsSource ?? itemsControl2.Items;
					if (itemsControl2 != itemsControl)
					{
						if (uIElement2 is TreeViewItem d)
						{
							TreeView visualAncestor = d.GetVisualAncestor<TreeView>();
							if (visualAncestor != null && visualAncestor != itemsControl && !visualAncestor.IsDragSource())
							{
								return;
							}
						}
						else if (itemsControl.ItemContainerGenerator.IndexFromContainer(itemsControl2) < 0 && !itemsControl2.IsDragSource())
						{
							return;
						}
					}
					SourceIndex = itemsControl2.ItemContainerGenerator.IndexFromContainer(uIElement2);
					SourceItem = itemsControl2.ItemContainerGenerator.ItemFromContainer(uIElement2);
				}
				else
				{
					SourceIndex = -1;
				}
				SourceItems = itemsControl.GetSelectedItems();
				if (SourceItems.Cast<object>().Count() <= 1)
				{
					SourceItems = Enumerable.Repeat(SourceItem, 1);
				}
				VisualSourceItem = uIElement2;
			}
			else
			{
				SourceCollection = itemsControl.ItemsSource ?? itemsControl.Items;
			}
		}
		else
		{
			SourceItem = (sender as FrameworkElement)?.DataContext;
			if (SourceItem != null)
			{
				SourceItems = Enumerable.Repeat(SourceItem, 1);
			}
			VisualSourceItem = uIElement;
			PositionInDraggedItem = ((uIElement != null) ? e.GetPosition(uIElement) : DragStartPosition);
		}
		if (SourceItems == null)
		{
			SourceItems = Enumerable.Empty<object>();
		}
	}
}
