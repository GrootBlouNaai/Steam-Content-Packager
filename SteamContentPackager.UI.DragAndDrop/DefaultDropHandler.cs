using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using SteamContentPackager.UI.DragAndDrop.Utilities;

namespace SteamContentPackager.UI.DragAndDrop;

public class DefaultDropHandler : IDropTarget
{
	public static bool ShouldCopyData(IDropInfo dropInfo)
	{
		if (dropInfo == null || dropInfo.DragInfo == null)
		{
			return false;
		}
		return ((dropInfo.DragInfo.DragDropCopyKeyState != 0 && dropInfo.KeyStates.HasFlag(dropInfo.DragInfo.DragDropCopyKeyState)) || dropInfo.DragInfo.DragDropCopyKeyState.HasFlag(DragDropKeyStates.LeftMouseButton)) && !(dropInfo.DragInfo.SourceItem is HeaderedContentControl) && !(dropInfo.DragInfo.SourceItem is HeaderedItemsControl) && !(dropInfo.DragInfo.SourceItem is ListBoxItem);
	}

	public virtual void DragOver(IDropInfo dropInfo)
	{
		if (CanAcceptData(dropInfo))
		{
			dropInfo.Effects = (ShouldCopyData(dropInfo) ? DragDropEffects.Copy : DragDropEffects.Move);
			bool flag = dropInfo.InsertPosition.HasFlag(RelativeInsertPosition.TargetItemCenter) && dropInfo.VisualTargetItem is TreeViewItem;
			dropInfo.DropTargetAdorner = (flag ? DropTargetAdorners.Highlight : DropTargetAdorners.Insert);
		}
	}

	public virtual void Drop(IDropInfo dropInfo)
	{
		if (dropInfo == null || dropInfo.DragInfo == null)
		{
			return;
		}
		int num = ((dropInfo.InsertIndex != dropInfo.UnfilteredInsertIndex) ? dropInfo.UnfilteredInsertIndex : dropInfo.InsertIndex);
		if (dropInfo.VisualTarget is ItemsControl itemsControl)
		{
			IEditableCollectionView items = itemsControl.Items;
			if (items != null)
			{
				NewItemPlaceholderPosition newItemPlaceholderPosition = items.NewItemPlaceholderPosition;
				if (newItemPlaceholderPosition == NewItemPlaceholderPosition.AtBeginning && num == 0)
				{
					num++;
				}
				else if (newItemPlaceholderPosition == NewItemPlaceholderPosition.AtEnd && num == itemsControl.Items.Count)
				{
					num--;
				}
			}
		}
		IList list = dropInfo.TargetCollection.TryGetList();
		List<object> list2 = ExtractData(dropInfo.Data).OfType<object>().ToList();
		if (!ShouldCopyData(dropInfo))
		{
			IList list3 = dropInfo.DragInfo.SourceCollection.TryGetList();
			if (list3 != null)
			{
				foreach (object item in list2)
				{
					int num2 = list3.IndexOf(item);
					if (num2 != -1)
					{
						list3.RemoveAt(num2);
						if (list != null && object.Equals(list3, list) && num2 < num)
						{
							num--;
						}
					}
				}
			}
		}
		if (list == null)
		{
			return;
		}
		TabControl tabControl = dropInfo.VisualTarget as TabControl;
		bool flag = dropInfo.Effects.HasFlag(DragDropEffects.Copy) || dropInfo.Effects.HasFlag(DragDropEffects.Link);
		foreach (object item2 in list2)
		{
			object obj = item2;
			if (flag && item2 is ICloneable cloneable)
			{
				obj = cloneable.Clone();
			}
			list.Insert(num++, obj);
			if (tabControl != null)
			{
				if (tabControl.ItemContainerGenerator.ContainerFromItem(obj) is TabItem tabItem)
				{
					tabItem.ApplyTemplate();
				}
				tabControl.SetSelectedItem(obj);
			}
		}
	}

	public static bool CanAcceptData(IDropInfo dropInfo)
	{
		if (dropInfo == null || dropInfo.DragInfo == null)
		{
			return false;
		}
		if (!dropInfo.IsSameDragDropContextAsSource)
		{
			return false;
		}
		if (dropInfo.InsertPosition.HasFlag(RelativeInsertPosition.TargetItemCenter) && dropInfo.VisualTargetItem is TreeViewItem && dropInfo.VisualTargetItem == dropInfo.DragInfo.VisualSourceItem)
		{
			return false;
		}
		if (dropInfo.DragInfo.SourceCollection == dropInfo.TargetCollection)
		{
			IList list = dropInfo.TargetCollection.TryGetList();
			return list != null;
		}
		if (dropInfo.TargetCollection == null)
		{
			return false;
		}
		if (TestCompatibleTypes(dropInfo.TargetCollection, dropInfo.Data))
		{
			bool flag = IsChildOf(dropInfo.VisualTargetItem, dropInfo.DragInfo.VisualSourceItem);
			return !flag;
		}
		return false;
	}

	public static IEnumerable ExtractData(object data)
	{
		if (data is IEnumerable && !(data is string))
		{
			return (IEnumerable)data;
		}
		return Enumerable.Repeat(data, 1);
	}

	protected static bool IsChildOf(UIElement targetItem, UIElement sourceItem)
	{
		for (ItemsControl itemsControl = ItemsControl.ItemsControlFromItemContainer(targetItem); itemsControl != null; itemsControl = ItemsControl.ItemsControlFromItemContainer(itemsControl))
		{
			if (itemsControl == sourceItem)
			{
				return true;
			}
		}
		return false;
	}

	protected static bool TestCompatibleTypes(IEnumerable target, object data)
	{
		TypeFilter filter = (Type t, object o) => t.IsGenericType && t.GetGenericTypeDefinition() == typeof(IEnumerable<>);
		Type[] source = target.GetType().FindInterfaces(filter, null);
		IEnumerable<Type> source2 = source.Select((Type i) => i.GetGenericArguments().Single());
		if (source2.Count() > 0)
		{
			Type dataType = TypeUtilities.GetCommonBaseClass(ExtractData(data));
			return source2.Any((Type t) => t.IsAssignableFrom(dataType));
		}
		return target is IList;
	}
}
