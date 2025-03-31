namespace SteamContentPackager.UI.DragAndDrop;

public interface IDropTarget
{
	void DragOver(IDropInfo dropInfo);

	void Drop(IDropInfo dropInfo);
}
