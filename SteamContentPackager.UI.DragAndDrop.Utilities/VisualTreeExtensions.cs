using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Media3D;

namespace SteamContentPackager.UI.DragAndDrop.Utilities;

public static class VisualTreeExtensions
{
	public static UIElement TryGetNextAncestorDropTargetElement(this UIElement element)
	{
		if (element == null)
		{
			return null;
		}
		for (UIElement visualAncestor = element.GetVisualAncestor<UIElement>(); visualAncestor != null; visualAncestor = visualAncestor.GetVisualAncestor<UIElement>())
		{
			if (visualAncestor.IsDropTarget())
			{
				return visualAncestor;
			}
		}
		return null;
	}

	internal static DependencyObject FindVisualTreeRoot(this DependencyObject d)
	{
		DependencyObject dependencyObject = d;
		DependencyObject result = d;
		while (dependencyObject != null)
		{
			result = dependencyObject;
			if (dependencyObject is Visual || dependencyObject is Visual3D)
			{
				break;
			}
			dependencyObject = LogicalTreeHelper.GetParent(dependencyObject);
		}
		return result;
	}

	public static T GetVisualAncestor<T>(this DependencyObject d) where T : class
	{
		for (DependencyObject parent = VisualTreeHelper.GetParent(d.FindVisualTreeRoot()); parent != null; parent = VisualTreeHelper.GetParent(parent))
		{
			if (parent is T result)
			{
				return result;
			}
		}
		return null;
	}

	public static DependencyObject GetVisualAncestor(this DependencyObject d, Type itemSearchType, Type itemContainerSearchType)
	{
		DependencyObject reference = d.FindVisualTreeRoot();
		DependencyObject parent = VisualTreeHelper.GetParent(reference);
		while (parent != null && itemSearchType != null)
		{
			Type type = parent.GetType();
			if (type == itemSearchType || type.IsSubclassOf(itemSearchType))
			{
				return parent;
			}
			if (itemContainerSearchType != null && itemContainerSearchType.IsAssignableFrom(type))
			{
				return null;
			}
			parent = VisualTreeHelper.GetParent(parent);
		}
		return null;
	}

	public static DependencyObject GetVisualAncestor(this DependencyObject d, Type itemSearchType, ItemsControl itemsControl)
	{
		DependencyObject reference = d.FindVisualTreeRoot();
		DependencyObject parent = VisualTreeHelper.GetParent(reference);
		DependencyObject result = null;
		while (parent != null && itemSearchType != null)
		{
			if (parent == itemsControl)
			{
				return result;
			}
			if ((parent.GetType() == itemSearchType || parent.GetType().IsSubclassOf(itemSearchType)) && (itemsControl == null || itemsControl.ItemContainerGenerator.IndexFromContainer(parent) != -1))
			{
				result = parent;
			}
			parent = VisualTreeHelper.GetParent(parent);
		}
		return result;
	}

	public static T GetVisualDescendent<T>(this DependencyObject d) where T : DependencyObject
	{
		return d.GetVisualDescendents<T>().FirstOrDefault();
	}

	public static IEnumerable<T> GetVisualDescendents<T>(this DependencyObject d) where T : DependencyObject
	{
		int childCount = VisualTreeHelper.GetChildrenCount(d);
		for (int n = 0; n < childCount; n++)
		{
			DependencyObject child = VisualTreeHelper.GetChild(d, n);
			if (child is T)
			{
				yield return (T)child;
			}
			foreach (T visualDescendent in child.GetVisualDescendents<T>())
			{
				yield return visualDescendent;
			}
		}
	}
}
