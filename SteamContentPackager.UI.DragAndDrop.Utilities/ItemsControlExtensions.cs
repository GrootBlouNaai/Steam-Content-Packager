using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Media;

namespace SteamContentPackager.UI.DragAndDrop.Utilities;

public static class ItemsControlExtensions
{
	public static CollectionViewGroup FindGroup(this ItemsControl itemsControl, Point position)
	{
		if (itemsControl.Items.Groups == null || itemsControl.Items.Groups.Count == 0)
		{
			return null;
		}
		if (itemsControl.InputHitTest(position) is DependencyObject d)
		{
			GroupItem groupItem = d.GetVisualAncestor<GroupItem>();
			if (groupItem == null && itemsControl.Items.Count > 0 && itemsControl.ItemContainerGenerator.ContainerFromItem(itemsControl.Items.GetItemAt(itemsControl.Items.Count - 1)) is FrameworkElement frameworkElement)
			{
				Point point = frameworkElement.PointToScreen(new Point(frameworkElement.ActualWidth, frameworkElement.ActualHeight));
				Point point2 = itemsControl.PointToScreen(position);
				switch (itemsControl.GetItemsPanelOrientation())
				{
				case Orientation.Horizontal:
					groupItem = ((point.X <= point2.X) ? frameworkElement.GetVisualAncestor<GroupItem>() : null);
					break;
				case Orientation.Vertical:
					groupItem = ((point.Y <= point2.Y) ? frameworkElement.GetVisualAncestor<GroupItem>() : null);
					break;
				}
			}
			if (groupItem != null)
			{
				return groupItem.Content as CollectionViewGroup;
			}
		}
		return null;
	}

	public static bool CanSelectMultipleItems(this ItemsControl itemsControl)
	{
		if (itemsControl is MultiSelector)
		{
			return (bool)itemsControl.GetType().GetProperty("CanSelectMultipleItems", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(itemsControl, null);
		}
		if (itemsControl is ListBox)
		{
			return ((ListBox)itemsControl).SelectionMode != SelectionMode.Single;
		}
		return false;
	}

	public static UIElement GetItemContainer(this ItemsControl itemsControl, DependencyObject child)
	{
		bool isItemContainer;
		Type itemContainerType = itemsControl.GetItemContainerType(out isItemContainer);
		if (itemContainerType != null)
		{
			return isItemContainer ? ((UIElement)child.GetVisualAncestor(itemContainerType, itemsControl)) : ((UIElement)child.GetVisualAncestor(itemContainerType, itemsControl.GetType()));
		}
		return null;
	}

	public static UIElement GetItemContainerAt(this ItemsControl itemsControl, Point position)
	{
		IInputElement inputElement = itemsControl.InputHitTest(position);
		if (inputElement is UIElement child)
		{
			return itemsControl.GetItemContainer(child);
		}
		if (inputElement is ContentElement child2)
		{
			return itemsControl.GetItemContainer(child2);
		}
		return null;
	}

	public static UIElement GetItemContainerAt(this ItemsControl itemsControl, Point position, Orientation searchDirection)
	{
		bool isItemContainer;
		Type itemContainerType = itemsControl.GetItemContainerType(out isItemContainer);
		Geometry geometry;
		if (typeof(TreeViewItem).IsAssignableFrom(itemContainerType))
		{
			geometry = new LineGeometry(new Point(0.0, position.Y), new Point(itemsControl.RenderSize.Width, position.Y));
		}
		else
		{
			GeometryGroup geometryGroup = new GeometryGroup();
			geometryGroup.Children.Add(new LineGeometry(new Point(0.0, position.Y), new Point(itemsControl.RenderSize.Width, position.Y)));
			geometryGroup.Children.Add(new LineGeometry(new Point(position.X, 0.0), new Point(position.X, itemsControl.RenderSize.Height)));
			geometry = geometryGroup;
		}
		HashSet<DependencyObject> hits = new HashSet<DependencyObject>();
		VisualTreeHelper.HitTest(itemsControl, (DependencyObject obj) => (obj is Viewport3D || (itemsControl is DataGrid && obj is ScrollBar)) ? HitTestFilterBehavior.Stop : HitTestFilterBehavior.Continue, delegate(HitTestResult result)
		{
			DependencyObject dependencyObject = (isItemContainer ? result.VisualHit.GetVisualAncestor(itemContainerType, itemsControl) : result.VisualHit.GetVisualAncestor(itemContainerType, itemsControl.GetType()));
			if (dependencyObject != null && ((UIElement)dependencyObject).IsVisible)
			{
				if (dependencyObject is TreeViewItem d)
				{
					TreeView visualAncestor = d.GetVisualAncestor<TreeView>();
					if (visualAncestor == itemsControl)
					{
						hits.Add(dependencyObject);
					}
				}
				else if (itemsControl.ItemContainerGenerator.IndexFromContainer(dependencyObject) >= 0)
				{
					hits.Add(dependencyObject);
				}
			}
			return HitTestResultBehavior.Continue;
		}, new GeometryHitTestParameters(geometry));
		return GetClosest(itemsControl, hits, position, searchDirection);
	}

	public static Type GetItemContainerType(this ItemsControl itemsControl, out bool isItemContainer)
	{
		isItemContainer = false;
		if (typeof(TabControl).IsAssignableFrom(itemsControl.GetType()))
		{
			return typeof(TabItem);
		}
		if (typeof(DataGrid).IsAssignableFrom(itemsControl.GetType()))
		{
			return typeof(DataGridRow);
		}
		if (typeof(ListView).IsAssignableFrom(itemsControl.GetType()))
		{
			return typeof(ListViewItem);
		}
		if (typeof(ListBox).IsAssignableFrom(itemsControl.GetType()))
		{
			return typeof(ListBoxItem);
		}
		if (typeof(TreeView).IsAssignableFrom(itemsControl.GetType()))
		{
			return typeof(TreeViewItem);
		}
		if (itemsControl.Items.Count > 0)
		{
			IEnumerable<ItemsPresenter> visualDescendents = itemsControl.GetVisualDescendents<ItemsPresenter>();
			foreach (ItemsPresenter item in visualDescendents)
			{
				if (VisualTreeHelper.GetChildrenCount(item) > 0)
				{
					DependencyObject child = VisualTreeHelper.GetChild(item, 0);
					DependencyObject dependencyObject = ((VisualTreeHelper.GetChildrenCount(child) > 0) ? VisualTreeHelper.GetChild(child, 0) : null);
					if (dependencyObject != null && itemsControl.ItemContainerGenerator.IndexFromContainer(dependencyObject) != -1)
					{
						isItemContainer = true;
						return dependencyObject.GetType();
					}
				}
			}
		}
		return null;
	}

	public static Orientation GetItemsPanelOrientation(this ItemsControl itemsControl)
	{
		Orientation? itemsPanelOrientation = DragDrop.GetItemsPanelOrientation(itemsControl);
		if (itemsPanelOrientation.HasValue)
		{
			return itemsPanelOrientation.Value;
		}
		if (itemsControl is TabControl)
		{
			TabControl tabControl = (TabControl)itemsControl;
			return (tabControl.TabStripPlacement == Dock.Left || tabControl.TabStripPlacement == Dock.Right) ? Orientation.Vertical : Orientation.Horizontal;
		}
		UIElement uIElement = (UIElement)(((object)itemsControl.GetVisualDescendent<ItemsPresenter>()) ?? ((object)itemsControl.GetVisualDescendent<ScrollContentPresenter>()));
		if (uIElement != null && VisualTreeHelper.GetChildrenCount(uIElement) > 0)
		{
			DependencyObject child = VisualTreeHelper.GetChild(uIElement, 0);
			PropertyInfo property = child.GetType().GetProperty("Orientation", typeof(Orientation));
			if (property != null)
			{
				return (Orientation)property.GetValue(child, null);
			}
		}
		return Orientation.Vertical;
	}

	public static FlowDirection GetItemsPanelFlowDirection(this ItemsControl itemsControl)
	{
		UIElement uIElement = (UIElement)(((object)itemsControl.GetVisualDescendent<ItemsPresenter>()) ?? ((object)itemsControl.GetVisualDescendent<ScrollContentPresenter>()));
		if (uIElement != null && VisualTreeHelper.GetChildrenCount(uIElement) > 0)
		{
			DependencyObject child = VisualTreeHelper.GetChild(uIElement, 0);
			PropertyInfo property = child.GetType().GetProperty("FlowDirection", typeof(FlowDirection));
			if (property != null)
			{
				return (FlowDirection)property.GetValue(child, null);
			}
		}
		return FlowDirection.LeftToRight;
	}

	public static void SetSelectedItem(this ItemsControl itemsControl, object item)
	{
		if (itemsControl is MultiSelector)
		{
			((MultiSelector)itemsControl).SelectedItem = null;
			((MultiSelector)itemsControl).SelectedItem = item;
			return;
		}
		if (itemsControl is ListBox)
		{
			SelectionMode selectionMode = ((ListBox)itemsControl).SelectionMode;
			try
			{
				((ListBox)itemsControl).SelectionMode = SelectionMode.Single;
				((ListBox)itemsControl).SelectedItem = null;
				((ListBox)itemsControl).SelectedItem = item;
				return;
			}
			finally
			{
				((ListBox)itemsControl).SelectionMode = selectionMode;
			}
		}
		if (itemsControl is TreeView)
		{
			object value = ((TreeView)itemsControl).GetValue(TreeView.SelectedItemProperty);
			if (value != null && ((TreeView)itemsControl).ItemContainerGenerator.ContainerFromItem(value) is TreeViewItem treeViewItem)
			{
				treeViewItem.IsSelected = false;
			}
			if (((TreeView)itemsControl).ItemContainerGenerator.ContainerFromItem(item) is TreeViewItem treeViewItem2)
			{
				treeViewItem2.IsSelected = true;
			}
		}
		else if (itemsControl is Selector)
		{
			((Selector)itemsControl).SelectedItem = null;
			((Selector)itemsControl).SelectedItem = item;
		}
	}

	public static object GetSelectedItem(this ItemsControl itemsControl)
	{
		if (itemsControl is MultiSelector)
		{
			return ((MultiSelector)itemsControl).SelectedItem;
		}
		if (itemsControl is ListBox)
		{
			return ((ListBox)itemsControl).SelectedItem;
		}
		if (itemsControl is TreeView)
		{
			return ((TreeView)itemsControl).GetValue(TreeView.SelectedItemProperty);
		}
		if (itemsControl is Selector)
		{
			return ((Selector)itemsControl).SelectedItem;
		}
		return null;
	}

	public static IEnumerable GetSelectedItems(this ItemsControl itemsControl)
	{
		if (typeof(MultiSelector).IsAssignableFrom(itemsControl.GetType()))
		{
			return ((MultiSelector)itemsControl).SelectedItems;
		}
		if (itemsControl is ListBox)
		{
			ListBox listBox = (ListBox)itemsControl;
			if (listBox.SelectionMode == SelectionMode.Single)
			{
				return Enumerable.Repeat(listBox.SelectedItem, 1);
			}
			return listBox.SelectedItems;
		}
		if (typeof(TreeView).IsAssignableFrom(itemsControl.GetType()))
		{
			return Enumerable.Repeat(((TreeView)itemsControl).SelectedItem, 1);
		}
		if (typeof(Selector).IsAssignableFrom(itemsControl.GetType()))
		{
			return Enumerable.Repeat(((Selector)itemsControl).SelectedItem, 1);
		}
		return Enumerable.Empty<object>();
	}

	public static bool GetItemSelected(this ItemsControl itemsControl, object item)
	{
		if (itemsControl is MultiSelector)
		{
			return ((MultiSelector)itemsControl).SelectedItems.Contains(item);
		}
		if (itemsControl is ListBox)
		{
			return ((ListBox)itemsControl).SelectedItems.Contains(item);
		}
		if (itemsControl is TreeView)
		{
			return ((TreeView)itemsControl).SelectedItem == item;
		}
		if (itemsControl is Selector)
		{
			return ((Selector)itemsControl).SelectedItem == item;
		}
		return false;
	}

	public static void SetItemSelected(this ItemsControl itemsControl, object item, bool value)
	{
		if (itemsControl is MultiSelector)
		{
			MultiSelector multiSelector = (MultiSelector)itemsControl;
			if (value)
			{
				if (multiSelector.CanSelectMultipleItems())
				{
					multiSelector.SelectedItems.Add(item);
				}
				else
				{
					multiSelector.SelectedItem = item;
				}
			}
			else
			{
				multiSelector.SelectedItems.Remove(item);
			}
		}
		else if (itemsControl is ListBox)
		{
			ListBox listBox = (ListBox)itemsControl;
			if (value)
			{
				if (listBox.SelectionMode != 0)
				{
					listBox.SelectedItems.Add(item);
				}
				else
				{
					listBox.SelectedItem = item;
				}
			}
			else
			{
				listBox.SelectedItems.Remove(item);
			}
		}
		else if (value)
		{
			itemsControl.SetSelectedItem(item);
		}
		else
		{
			itemsControl.SetSelectedItem(null);
		}
	}

	private static UIElement GetClosest(ItemsControl itemsControl, IEnumerable<DependencyObject> items, Point position, Orientation searchDirection)
	{
		UIElement result = null;
		double num = double.MaxValue;
		foreach (DependencyObject item in items)
		{
			if (!(item is UIElement uIElement))
			{
				continue;
			}
			Point point = uIElement.TransformToAncestor(itemsControl).Transform(new Point(0.0, 0.0));
			double num2 = double.MaxValue;
			if (itemsControl is TreeView)
			{
				double x = position.X - point.X;
				double x2 = position.Y - point.Y;
				double value = Math.Sqrt(Math.Pow(x, 2.0) + Math.Pow(x2, 2.0));
				num2 = Math.Abs(value);
			}
			else
			{
				ItemsControl itemsControl2 = ItemsControl.ItemsControlFromItemContainer(uIElement);
				if (itemsControl2 != null && itemsControl2 != itemsControl)
				{
					searchDirection = itemsControl2.GetItemsPanelOrientation();
				}
				switch (searchDirection)
				{
				case Orientation.Horizontal:
					num2 = ((position.X <= point.X) ? (point.X - position.X) : (position.X - uIElement.RenderSize.Width - point.X));
					break;
				case Orientation.Vertical:
					num2 = ((position.Y <= point.Y) ? (point.Y - position.Y) : (position.Y - uIElement.RenderSize.Height - point.Y));
					break;
				}
			}
			if (num2 < num)
			{
				result = uIElement;
				num = num2;
			}
		}
		return result;
	}
}
