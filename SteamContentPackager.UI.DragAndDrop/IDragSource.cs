using System;

namespace SteamContentPackager.UI.DragAndDrop;

public interface IDragSource
{
	void StartDrag(IDragInfo dragInfo);

	bool CanStartDrag(IDragInfo dragInfo);

	void Dropped(IDropInfo dropInfo);

	void DragCancelled();

	bool TryCatchOccurredException(Exception exception);
}
