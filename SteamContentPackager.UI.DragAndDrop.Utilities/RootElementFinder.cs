using System.Windows;
using System.Windows.Controls;

namespace SteamContentPackager.UI.DragAndDrop.Utilities;

public static class RootElementFinder
{
	public static UIElement FindRoot(DependencyObject visual)
	{
		Window window = Window.GetWindow(visual);
		UIElement uIElement = ((window != null) ? (window.Content as UIElement) : null);
		if (uIElement == null)
		{
			if (Application.Current != null && Application.Current.MainWindow != null)
			{
				uIElement = Application.Current.MainWindow.Content as UIElement;
			}
			if (uIElement == null)
			{
				uIElement = (UIElement)(((object)visual.GetVisualAncestor<Page>()) ?? ((object)visual.GetVisualAncestor<UserControl>()));
			}
		}
		return uIElement;
	}
}
