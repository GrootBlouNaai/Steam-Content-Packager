using System;
using System.Linq;
using System.Windows;
using SteamContentPackager.UI.DragAndDrop.Utilities;

namespace SteamContentPackager.UI.DragAndDrop;

public class DefaultDragHandler : IDragSource
{
	public virtual void StartDrag(IDragInfo dragInfo)
	{
		int num = dragInfo.SourceItems.Cast<object>().Count();
		if (num == 1)
		{
			dragInfo.Data = dragInfo.SourceItems.Cast<object>().First();
		}
		else if (num > 1)
		{
			dragInfo.Data = TypeUtilities.CreateDynamicallyTypedList(dragInfo.SourceItems);
		}
		dragInfo.Effects = ((dragInfo.Data != null) ? (DragDropEffects.Copy | DragDropEffects.Move) : DragDropEffects.None);
	}

	public virtual bool CanStartDrag(IDragInfo dragInfo)
	{
		return true;
	}

	public virtual void Dropped(IDropInfo dropInfo)
	{
	}

	public virtual void DragCancelled()
	{
	}

	public virtual bool TryCatchOccurredException(Exception exception)
	{
		return false;
	}
}
