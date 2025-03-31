using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;

namespace SteamContentPackager.UI.DragAndDrop.Utilities;

public static class HitTestUtilities
{
	public static bool HitTest4Type<T>(object sender, Point elementPosition) where T : UIElement
	{
		T hitTestElement4Type = GetHitTestElement4Type<T>(sender, elementPosition);
		return hitTestElement4Type != null && hitTestElement4Type.Visibility == Visibility.Visible;
	}

	private static T GetHitTestElement4Type<T>(object sender, Point elementPosition) where T : UIElement
	{
		if (!(sender is Visual reference))
		{
			return null;
		}
		HitTestResult hitTestResult = VisualTreeHelper.HitTest(reference, elementPosition);
		if (hitTestResult == null)
		{
			return null;
		}
		return hitTestResult.VisualHit.GetVisualAncestor<T>();
	}

	public static bool HitTest4GridViewColumnHeader(object sender, Point elementPosition)
	{
		if (sender is ListView)
		{
			GridViewColumnHeader hitTestElement4Type = GetHitTestElement4Type<GridViewColumnHeader>(sender, elementPosition);
			if (hitTestElement4Type != null && (hitTestElement4Type.Role == GridViewColumnHeaderRole.Floating || hitTestElement4Type.Visibility == Visibility.Visible))
			{
				return true;
			}
		}
		return false;
	}

	public static bool HitTest4DataGridTypes(object sender, Point elementPosition)
	{
		if (sender is DataGrid)
		{
			DataGridColumnHeader hitTestElement4Type = GetHitTestElement4Type<DataGridColumnHeader>(sender, elementPosition);
			if (hitTestElement4Type != null && hitTestElement4Type.Visibility == Visibility.Visible)
			{
				return true;
			}
			DataGridRowHeader hitTestElement4Type2 = GetHitTestElement4Type<DataGridRowHeader>(sender, elementPosition);
			if (hitTestElement4Type2 != null && hitTestElement4Type2.Visibility == Visibility.Visible)
			{
				return true;
			}
			DataGridRow hitTestElement4Type3 = GetHitTestElement4Type<DataGridRow>(sender, elementPosition);
			return hitTestElement4Type3 == null || object.Equals(hitTestElement4Type3.DataContext, CollectionView.NewItemPlaceholder);
		}
		return false;
	}

	public static bool HitTest4DataGridTypesOnDragOver(object sender, Point elementPosition)
	{
		if (sender is DataGrid)
		{
			DataGridColumnHeader hitTestElement4Type = GetHitTestElement4Type<DataGridColumnHeader>(sender, elementPosition);
			if (hitTestElement4Type != null && hitTestElement4Type.Visibility == Visibility.Visible)
			{
				return true;
			}
		}
		return false;
	}

	public static bool IsNotPartOfSender(object sender, MouseButtonEventArgs e)
	{
		if (!(e.OriginalSource is Visual visual))
		{
			return false;
		}
		HitTestResult hitTestResult = VisualTreeHelper.HitTest(visual, e.GetPosition((IInputElement)visual));
		if (hitTestResult == null)
		{
			return false;
		}
		if (!(e.OriginalSource is DependencyObject dependencyObject))
		{
			return false;
		}
		if (object.Equals(dependencyObject, sender))
		{
			return false;
		}
		DependencyObject parent = VisualTreeHelper.GetParent(dependencyObject.FindVisualTreeRoot());
		while (parent != null && !object.Equals(parent, sender))
		{
			parent = VisualTreeHelper.GetParent(parent);
		}
		return !object.Equals(parent, sender);
	}
}
