using System;
using System.Collections;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using SteamContentPackager.UI.DragAndDrop.Utilities;

namespace SteamContentPackager.UI.DragAndDrop;

public class DropInfo : IDropInfo
{
	private ItemsControl itemParent = null;

	private UIElement item = null;

	public object Data { get; private set; }

	public IDragInfo DragInfo { get; private set; }

	public Point DropPosition { get; private set; }

	public Type DropTargetAdorner { get; set; }

	public DragDropEffects Effects { get; set; }

	public int InsertIndex { get; private set; }

	public int UnfilteredInsertIndex
	{
		get
		{
			int insertIndex = InsertIndex;
			if (itemParent != null)
			{
				IList list = itemParent.ItemsSource.TryGetList();
				if (list != null && itemParent.Items != null && itemParent.Items.Count != list.Count && insertIndex >= 0 && insertIndex < itemParent.Items.Count)
				{
					int num = list.IndexOf(itemParent.Items[insertIndex]);
					if (num >= 0)
					{
						return num;
					}
				}
			}
			return insertIndex;
		}
	}

	public IEnumerable TargetCollection { get; private set; }

	public object TargetItem { get; private set; }

	public CollectionViewGroup TargetGroup { get; private set; }

	public ScrollViewer TargetScrollViewer { get; private set; }

	public ScrollingMode TargetScrollingMode { get; set; }

	public UIElement VisualTarget { get; private set; }

	public UIElement VisualTargetItem { get; private set; }

	public Orientation VisualTargetOrientation { get; private set; }

	public FlowDirection VisualTargetFlowDirection { get; private set; }

	public string DestinationText { get; set; }

	public RelativeInsertPosition InsertPosition { get; private set; }

	public DragDropKeyStates KeyStates { get; private set; }

	public bool NotHandled { get; set; }

	public bool IsSameDragDropContextAsSource
	{
		get
		{
			if (DragInfo == null || DragInfo.VisualSource == null)
			{
				return true;
			}
			if (VisualTarget == null)
			{
				return true;
			}
			string text = DragInfo.VisualSource.GetValue(DragDrop.DragDropContextProperty) as string;
			if (string.IsNullOrEmpty(text))
			{
				return true;
			}
			string b = VisualTarget.GetValue(DragDrop.DragDropContextProperty) as string;
			return string.Equals(text, b);
		}
	}

	public DropInfo(object sender, DragEventArgs e, DragInfo dragInfo)
	{
		string name = DragDrop.DataFormat.Name;
		Data = (e.Data.GetDataPresent(name) ? e.Data.GetData(name) : e.Data);
		DragInfo = dragInfo;
		KeyStates = e.KeyStates;
		VisualTarget = sender as UIElement;
		if (!VisualTarget.IsDropTarget())
		{
			UIElement uIElement = VisualTarget.TryGetNextAncestorDropTargetElement();
			if (uIElement != null)
			{
				VisualTarget = uIElement;
			}
		}
		if (VisualTarget is TabControl)
		{
			TargetScrollViewer = VisualTarget.GetVisualDescendent<TabPanel>()?.GetVisualAncestor<ScrollViewer>();
		}
		else
		{
			TargetScrollViewer = VisualTarget?.GetVisualDescendent<ScrollViewer>();
		}
		TargetScrollingMode = ((VisualTarget != null) ? DragDrop.GetDropScrollingMode(VisualTarget) : ScrollingMode.Both);
		DropPosition = ((VisualTarget != null) ? e.GetPosition(VisualTarget) : default(Point));
		if (VisualTarget is TabControl && !HitTestUtilities.HitTest4Type<TabPanel>(VisualTarget, DropPosition))
		{
			return;
		}
		if (VisualTarget is ItemsControl)
		{
			ItemsControl itemsControl = (ItemsControl)VisualTarget;
			item = itemsControl.GetItemContainerAt(DropPosition);
			bool flag = item != null;
			TargetGroup = itemsControl.FindGroup(DropPosition);
			VisualTargetOrientation = itemsControl.GetItemsPanelOrientation();
			VisualTargetFlowDirection = itemsControl.GetItemsPanelFlowDirection();
			if (item == null)
			{
				item = itemsControl.GetItemContainerAt(DropPosition, VisualTargetOrientation);
				flag = false;
			}
			if (item == null && TargetGroup != null && TargetGroup.IsBottomLevel)
			{
				object obj = TargetGroup.Items.FirstOrDefault();
				if (obj != null)
				{
					item = itemsControl.ItemContainerGenerator.ContainerFromItem(obj) as UIElement;
					flag = false;
				}
			}
			if (item != null)
			{
				itemParent = ItemsControl.ItemsControlFromItemContainer(item);
				VisualTargetOrientation = itemParent.GetItemsPanelOrientation();
				VisualTargetFlowDirection = itemParent.GetItemsPanelFlowDirection();
				InsertIndex = itemParent.ItemContainerGenerator.IndexFromContainer(item);
				TargetCollection = itemParent.ItemsSource ?? itemParent.Items;
				TreeViewItem treeViewItem = item as TreeViewItem;
				if (flag || treeViewItem != null)
				{
					VisualTargetItem = item;
					TargetItem = itemParent.ItemContainerGenerator.ItemFromContainer(item);
				}
				bool flag2 = treeViewItem != null && treeViewItem.HasHeader && treeViewItem.HasItems && treeViewItem.IsExpanded;
				Size size = (flag2 ? treeViewItem.GetHeaderSize() : item.RenderSize);
				if (VisualTargetOrientation == Orientation.Vertical)
				{
					double y = e.GetPosition(item).Y;
					double height = size.Height;
					double num = height * 0.25;
					double num2 = height * 0.75;
					if (y > height / 2.0)
					{
						if (flag2 && (y < num || y > num2))
						{
							VisualTargetItem = treeViewItem.ItemContainerGenerator.ContainerFromIndex(0) as UIElement;
							TargetItem = ((VisualTargetItem != null) ? treeViewItem.ItemContainerGenerator.ItemFromContainer(VisualTargetItem) : null);
							TargetCollection = treeViewItem.ItemsSource ?? treeViewItem.Items;
							InsertIndex = 0;
							InsertPosition = RelativeInsertPosition.BeforeTargetItem;
						}
						else
						{
							InsertIndex++;
							InsertPosition = RelativeInsertPosition.AfterTargetItem;
						}
					}
					else
					{
						InsertPosition = RelativeInsertPosition.BeforeTargetItem;
					}
					return;
				}
				double x = e.GetPosition(item).X;
				double width = size.Width;
				if (VisualTargetFlowDirection == FlowDirection.RightToLeft)
				{
					if (x > width / 2.0)
					{
						InsertPosition = RelativeInsertPosition.BeforeTargetItem;
						return;
					}
					InsertIndex++;
					InsertPosition = RelativeInsertPosition.AfterTargetItem;
				}
				else if (VisualTargetFlowDirection == FlowDirection.LeftToRight)
				{
					if (x > width / 2.0)
					{
						InsertIndex++;
						InsertPosition = RelativeInsertPosition.AfterTargetItem;
					}
					else
					{
						InsertPosition = RelativeInsertPosition.BeforeTargetItem;
					}
				}
			}
			else
			{
				TargetCollection = itemsControl.ItemsSource ?? itemsControl.Items;
				InsertIndex = itemsControl.Items.Count;
			}
		}
		else
		{
			VisualTargetItem = VisualTarget;
		}
	}
}
