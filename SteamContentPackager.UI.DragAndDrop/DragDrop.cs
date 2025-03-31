using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using SteamContentPackager.UI.DragAndDrop.Icons;
using SteamContentPackager.UI.DragAndDrop.Utilities;

namespace SteamContentPackager.UI.DragAndDrop;

public static class DragDrop
{
	private static DragAdorner _DragAdorner;

	private static DragAdorner _EffectAdorner;

	private static DropTargetAdorner _DropTargetAdorner;

	private static DragInfo m_DragInfo;

	private static bool m_DragInProgress;

	private static object m_ClickSupressItem;

	private static Point _adornerPos;

	private static Size _adornerSize;

	public static readonly DependencyProperty IsDragSourceProperty = DependencyProperty.RegisterAttached("IsDragSource", typeof(bool), typeof(DragDrop), new UIPropertyMetadata(false, IsDragSourceChanged));

	public static readonly DependencyProperty IsDropTargetProperty = DependencyProperty.RegisterAttached("IsDropTarget", typeof(bool), typeof(DragDrop), new UIPropertyMetadata(false, IsDropTargetChanged));

	public static readonly DependencyProperty CanDragWithMouseRightButtonProperty = DependencyProperty.RegisterAttached("CanDragWithMouseRightButton", typeof(bool), typeof(DragDrop), new UIPropertyMetadata(false, CanDragWithMouseRightButtonChanged));

	public static readonly DependencyProperty DragHandlerProperty = DependencyProperty.RegisterAttached("DragHandler", typeof(IDragSource), typeof(DragDrop));

	public static readonly DependencyProperty DropHandlerProperty = DependencyProperty.RegisterAttached("DropHandler", typeof(IDropTarget), typeof(DragDrop));

	public static readonly DependencyProperty DropScrollingModeProperty = DependencyProperty.RegisterAttached("DropScrollingMode", typeof(ScrollingMode), typeof(DragDrop), new PropertyMetadata(ScrollingMode.Both));

	public static readonly DependencyProperty DropTargetAdornerBrushProperty = DependencyProperty.RegisterAttached("DropTargetAdornerBrush", typeof(Brush), typeof(DragDrop), new PropertyMetadata((object)null));

	public static readonly DependencyProperty DragDropContextProperty = DependencyProperty.RegisterAttached("DragDropContext", typeof(string), typeof(DragDrop), new UIPropertyMetadata(string.Empty));

	public static readonly DependencyProperty DragSourceIgnoreProperty = DependencyProperty.RegisterAttached("DragSourceIgnore", typeof(bool), typeof(DragDrop), new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.Inherits));

	public static readonly DependencyProperty DragDirectlySelectedOnlyProperty = DependencyProperty.RegisterAttached("DragDirectlySelectedOnly", typeof(bool), typeof(DragDrop), new PropertyMetadata(false));

	public static readonly DependencyProperty DragDropCopyKeyStateProperty = DependencyProperty.RegisterAttached("DragDropCopyKeyState", typeof(DragDropKeyStates), typeof(DragDrop), new PropertyMetadata(DragDropKeyStates.None));

	public static readonly DependencyProperty UseDefaultDragAdornerProperty = DependencyProperty.RegisterAttached("UseDefaultDragAdorner", typeof(bool), typeof(DragDrop), new PropertyMetadata(false));

	public static readonly DependencyProperty DefaultDragAdornerOpacityProperty = DependencyProperty.RegisterAttached("DefaultDragAdornerOpacity", typeof(double), typeof(DragDrop), new PropertyMetadata(0.8));

	public static readonly DependencyProperty DragMouseAnchorPointProperty = DependencyProperty.RegisterAttached("DragMouseAnchorPoint", typeof(Point), typeof(DragDrop), new PropertyMetadata(new Point(0.0, 1.0)));

	public static readonly DependencyProperty DragAdornerTemplateProperty = DependencyProperty.RegisterAttached("DragAdornerTemplate", typeof(DataTemplate), typeof(DragDrop));

	public static readonly DependencyProperty DragAdornerTemplateSelectorProperty = DependencyProperty.RegisterAttached("DragAdornerTemplateSelector", typeof(DataTemplateSelector), typeof(DragDrop), new PropertyMetadata((object)null));

	public static readonly DependencyProperty UseVisualSourceItemSizeForDragAdornerProperty = DependencyProperty.RegisterAttached("UseVisualSourceItemSizeForDragAdorner", typeof(bool), typeof(DragDrop), new PropertyMetadata(false));

	public static readonly DependencyProperty UseDefaultEffectDataTemplateProperty = DependencyProperty.RegisterAttached("UseDefaultEffectDataTemplate", typeof(bool), typeof(DragDrop), new PropertyMetadata(false));

	public static readonly DependencyProperty EffectNoneAdornerTemplateProperty = DependencyProperty.RegisterAttached("EffectNoneAdornerTemplate", typeof(DataTemplate), typeof(DragDrop), new PropertyMetadata((object)null));

	public static readonly DependencyProperty EffectCopyAdornerTemplateProperty = DependencyProperty.RegisterAttached("EffectCopyAdornerTemplate", typeof(DataTemplate), typeof(DragDrop), new PropertyMetadata((object)null));

	public static readonly DependencyProperty EffectMoveAdornerTemplateProperty = DependencyProperty.RegisterAttached("EffectMoveAdornerTemplate", typeof(DataTemplate), typeof(DragDrop), new PropertyMetadata((object)null));

	public static readonly DependencyProperty EffectLinkAdornerTemplateProperty = DependencyProperty.RegisterAttached("EffectLinkAdornerTemplate", typeof(DataTemplate), typeof(DragDrop), new PropertyMetadata((object)null));

	public static readonly DependencyProperty EffectAllAdornerTemplateProperty = DependencyProperty.RegisterAttached("EffectAllAdornerTemplate", typeof(DataTemplate), typeof(DragDrop), new PropertyMetadata((object)null));

	public static readonly DependencyProperty EffectScrollAdornerTemplateProperty = DependencyProperty.RegisterAttached("EffectScrollAdornerTemplate", typeof(DataTemplate), typeof(DragDrop), new PropertyMetadata((object)null));

	public static readonly DependencyProperty ItemsPanelOrientationProperty = DependencyProperty.RegisterAttached("ItemsPanelOrientation", typeof(Orientation?), typeof(DragDrop), new PropertyMetadata(null));

	private static DragAdorner DragAdorner
	{
		get
		{
			return _DragAdorner;
		}
		set
		{
			_DragAdorner?.Detatch();
			_DragAdorner = value;
		}
	}

	private static DragAdorner EffectAdorner
	{
		get
		{
			return _EffectAdorner;
		}
		set
		{
			_EffectAdorner?.Detatch();
			_EffectAdorner = value;
		}
	}

	private static DropTargetAdorner DropTargetAdorner
	{
		get
		{
			return _DropTargetAdorner;
		}
		set
		{
			_DropTargetAdorner?.Detatch();
			_DropTargetAdorner = value;
		}
	}

	public static DataFormat DataFormat { get; } = DataFormats.GetDataFormat("GongSolutions.Wpf.DragDrop");

	public static IDragSource DefaultDragHandler { get; } = new DefaultDragHandler();

	public static IDropTarget DefaultDropHandler { get; } = new DefaultDropHandler();

	private static void CreateDragAdorner(DropInfo dropInfo)
	{
		DataTemplate dataTemplate = GetDragAdornerTemplate(m_DragInfo.VisualSource);
		DataTemplateSelector dragAdornerTemplateSelector = GetDragAdornerTemplateSelector(m_DragInfo.VisualSource);
		UIElement uIElement = null;
		bool useDefaultDragAdorner = GetUseDefaultDragAdorner(m_DragInfo.VisualSource);
		bool useVisualSourceItemSizeForDragAdorner = GetUseVisualSourceItemSizeForDragAdorner(m_DragInfo.VisualSource);
		if (dataTemplate == null && dragAdornerTemplateSelector == null && useDefaultDragAdorner)
		{
			BitmapSource bitmapSource = CaptureScreen(m_DragInfo.VisualSourceItem, m_DragInfo.VisualSourceFlowDirection);
			if (bitmapSource != null)
			{
				FrameworkElementFactory frameworkElementFactory = new FrameworkElementFactory(typeof(Image));
				frameworkElementFactory.SetValue(Image.SourceProperty, bitmapSource);
				frameworkElementFactory.SetValue(RenderOptions.EdgeModeProperty, EdgeMode.Aliased);
				frameworkElementFactory.SetValue(RenderOptions.BitmapScalingModeProperty, BitmapScalingMode.HighQuality);
				frameworkElementFactory.SetValue(FrameworkElement.WidthProperty, bitmapSource.Width);
				frameworkElementFactory.SetValue(FrameworkElement.HeightProperty, bitmapSource.Height);
				frameworkElementFactory.SetValue(FrameworkElement.HorizontalAlignmentProperty, HorizontalAlignment.Left);
				frameworkElementFactory.SetValue(FrameworkElement.VerticalAlignmentProperty, VerticalAlignment.Top);
				dataTemplate = new DataTemplate
				{
					VisualTree = frameworkElementFactory
				};
			}
		}
		if (dataTemplate != null || dragAdornerTemplateSelector != null)
		{
			if (m_DragInfo.Data is IEnumerable && !(m_DragInfo.Data is string))
			{
				if (!useDefaultDragAdorner && ((IEnumerable)m_DragInfo.Data).Cast<object>().Count() <= 10)
				{
					ItemsControl itemsControl = new ItemsControl();
					itemsControl.ItemsSource = (IEnumerable)m_DragInfo.Data;
					itemsControl.ItemTemplate = dataTemplate;
					itemsControl.ItemTemplateSelector = dragAdornerTemplateSelector;
					if (useVisualSourceItemSizeForDragAdorner)
					{
						Rect descendantBounds = VisualTreeHelper.GetDescendantBounds(m_DragInfo.VisualSourceItem);
						itemsControl.SetValue(FrameworkElement.MinWidthProperty, descendantBounds.Width);
					}
					Grid grid = new Grid();
					grid.Children.Add(itemsControl);
					uIElement = grid;
				}
			}
			else
			{
				ContentPresenter contentPresenter = new ContentPresenter();
				contentPresenter.Content = m_DragInfo.Data;
				contentPresenter.ContentTemplate = dataTemplate;
				contentPresenter.ContentTemplateSelector = dragAdornerTemplateSelector;
				if (useVisualSourceItemSizeForDragAdorner)
				{
					Rect descendantBounds2 = VisualTreeHelper.GetDescendantBounds(m_DragInfo.VisualSourceItem);
					contentPresenter.SetValue(FrameworkElement.MinWidthProperty, descendantBounds2.Width);
					contentPresenter.SetValue(FrameworkElement.MinHeightProperty, descendantBounds2.Height);
				}
				uIElement = contentPresenter;
			}
		}
		if (uIElement != null)
		{
			if (useDefaultDragAdorner)
			{
				uIElement.Opacity = GetDefaultDragAdornerOpacity(m_DragInfo.VisualSource);
			}
			UIElement adornedElement = RootElementFinder.FindRoot(dropInfo.VisualTarget ?? m_DragInfo.VisualSource);
			DragAdorner = new DragAdorner(adornedElement, uIElement);
		}
	}

	private static BitmapSource CaptureScreen(Visual target, FlowDirection flowDirection)
	{
		if (target == null)
		{
			return null;
		}
		double dpiX = DpiHelper.DpiX;
		double dpiY = DpiHelper.DpiY;
		Rect descendantBounds = VisualTreeHelper.GetDescendantBounds(target);
		Rect rect = DpiHelper.LogicalRectToDevice(descendantBounds);
		int num = (int)Math.Ceiling(rect.Width);
		int num2 = (int)Math.Ceiling(rect.Height);
		if (num < 0 || num2 < 0)
		{
			return null;
		}
		RenderTargetBitmap renderTargetBitmap = new RenderTargetBitmap(num, num2, dpiX, dpiY, PixelFormats.Pbgra32);
		DrawingVisual drawingVisual = new DrawingVisual();
		using (DrawingContext drawingContext = drawingVisual.RenderOpen())
		{
			VisualBrush brush = new VisualBrush(target);
			if (flowDirection == FlowDirection.RightToLeft)
			{
				TransformGroup transformGroup = new TransformGroup();
				transformGroup.Children.Add(new ScaleTransform(-1.0, 1.0));
				transformGroup.Children.Add(new TranslateTransform(descendantBounds.Size.Width - 1.0, 0.0));
				drawingContext.PushTransform(transformGroup);
			}
			drawingContext.DrawRectangle(brush, null, new Rect(default(Point), descendantBounds.Size));
		}
		renderTargetBitmap.Render(drawingVisual);
		return renderTargetBitmap;
	}

	private static void CreateEffectAdorner(DropInfo dropInfo)
	{
		DataTemplate effectAdornerTemplate = GetEffectAdornerTemplate(m_DragInfo.VisualSource, dropInfo.Effects, dropInfo.DestinationText);
		if (effectAdornerTemplate != null)
		{
			UIElement adornedElement = RootElementFinder.FindRoot(dropInfo.VisualTarget ?? m_DragInfo.VisualSource);
			ContentPresenter contentPresenter = new ContentPresenter();
			contentPresenter.Content = m_DragInfo.Data;
			contentPresenter.ContentTemplate = effectAdornerTemplate;
			EffectAdorner = new DragAdorner(adornedElement, contentPresenter, dropInfo.Effects);
		}
	}

	private static DataTemplate GetEffectAdornerTemplate(UIElement target, DragDropEffects effect, string destinationText)
	{
		return effect switch
		{
			DragDropEffects.All => GetEffectAllAdornerTemplate(target), 
			DragDropEffects.Copy => GetEffectCopyAdornerTemplate(target) ?? CreateDefaultEffectDataTemplate(target, IconFactory.EffectCopy, "Copy to", destinationText), 
			DragDropEffects.Link => GetEffectLinkAdornerTemplate(target) ?? CreateDefaultEffectDataTemplate(target, IconFactory.EffectLink, "Link to", destinationText), 
			DragDropEffects.Move => GetEffectMoveAdornerTemplate(target) ?? CreateDefaultEffectDataTemplate(target, IconFactory.EffectMove, "Move to", destinationText), 
			DragDropEffects.None => GetEffectNoneAdornerTemplate(target) ?? CreateDefaultEffectDataTemplate(target, IconFactory.EffectNone, "None", destinationText), 
			DragDropEffects.Scroll => GetEffectScrollAdornerTemplate(target), 
			_ => null, 
		};
	}

	private static DataTemplate CreateDefaultEffectDataTemplate(UIElement target, BitmapImage effectIcon, string effectText, string destinationText)
	{
		if (!GetUseDefaultEffectDataTemplate(target))
		{
			return null;
		}
		double messageFontSize = SystemFonts.MessageFontSize;
		FrameworkElementFactory frameworkElementFactory = new FrameworkElementFactory(typeof(Image));
		frameworkElementFactory.SetValue(Image.SourceProperty, effectIcon);
		frameworkElementFactory.SetValue(FrameworkElement.HeightProperty, 12.0);
		frameworkElementFactory.SetValue(FrameworkElement.WidthProperty, 12.0);
		if (object.Equals(effectIcon, IconFactory.EffectNone))
		{
			return new DataTemplate
			{
				VisualTree = frameworkElementFactory
			};
		}
		frameworkElementFactory.SetValue(FrameworkElement.MarginProperty, new Thickness(0.0, 0.0, 3.0, 0.0));
		frameworkElementFactory.SetValue(FrameworkElement.VerticalAlignmentProperty, VerticalAlignment.Center);
		FrameworkElementFactory frameworkElementFactory2 = new FrameworkElementFactory(typeof(TextBlock));
		frameworkElementFactory2.SetValue(TextBlock.TextProperty, effectText);
		frameworkElementFactory2.SetValue(TextBlock.FontSizeProperty, messageFontSize);
		frameworkElementFactory2.SetValue(TextBlock.ForegroundProperty, Brushes.Blue);
		frameworkElementFactory2.SetValue(FrameworkElement.VerticalAlignmentProperty, VerticalAlignment.Center);
		FrameworkElementFactory frameworkElementFactory3 = new FrameworkElementFactory(typeof(TextBlock));
		frameworkElementFactory3.SetValue(TextBlock.TextProperty, destinationText);
		frameworkElementFactory3.SetValue(TextBlock.FontSizeProperty, messageFontSize);
		frameworkElementFactory3.SetValue(TextBlock.ForegroundProperty, Brushes.DarkBlue);
		frameworkElementFactory3.SetValue(FrameworkElement.MarginProperty, new Thickness(3.0, 0.0, 0.0, 0.0));
		frameworkElementFactory3.SetValue(TextBlock.FontWeightProperty, FontWeights.DemiBold);
		frameworkElementFactory3.SetValue(FrameworkElement.VerticalAlignmentProperty, VerticalAlignment.Center);
		FrameworkElementFactory frameworkElementFactory4 = new FrameworkElementFactory(typeof(StackPanel));
		frameworkElementFactory4.SetValue(StackPanel.OrientationProperty, Orientation.Horizontal);
		frameworkElementFactory4.SetValue(FrameworkElement.MarginProperty, new Thickness(2.0));
		frameworkElementFactory4.SetValue(FrameworkElement.VerticalAlignmentProperty, VerticalAlignment.Center);
		frameworkElementFactory4.AppendChild(frameworkElementFactory);
		frameworkElementFactory4.AppendChild(frameworkElementFactory2);
		frameworkElementFactory4.AppendChild(frameworkElementFactory3);
		FrameworkElementFactory frameworkElementFactory5 = new FrameworkElementFactory(typeof(Border));
		GradientStopCollection gradientStopCollection = new GradientStopCollection
		{
			new GradientStop(Colors.White, 0.0),
			new GradientStop(Colors.AliceBlue, 1.0)
		};
		LinearGradientBrush value = new LinearGradientBrush(gradientStopCollection)
		{
			StartPoint = new Point(0.0, 0.0),
			EndPoint = new Point(0.0, 1.0)
		};
		frameworkElementFactory5.SetValue(Panel.BackgroundProperty, value);
		frameworkElementFactory5.SetValue(Border.BorderBrushProperty, Brushes.DimGray);
		frameworkElementFactory5.SetValue(Border.CornerRadiusProperty, new CornerRadius(3.0));
		frameworkElementFactory5.SetValue(Border.BorderThicknessProperty, new Thickness(1.0));
		frameworkElementFactory5.SetValue(UIElement.SnapsToDevicePixelsProperty, true);
		frameworkElementFactory5.SetValue(TextOptions.TextFormattingModeProperty, TextFormattingMode.Display);
		frameworkElementFactory5.SetValue(TextOptions.TextRenderingModeProperty, TextRenderingMode.ClearType);
		frameworkElementFactory5.SetValue(TextOptions.TextHintingModeProperty, TextHintingMode.Fixed);
		frameworkElementFactory5.AppendChild(frameworkElementFactory4);
		return new DataTemplate
		{
			VisualTree = frameworkElementFactory5
		};
	}

	private static void Scroll(DropInfo dropInfo, DragEventArgs e)
	{
		if (dropInfo == null || dropInfo.TargetScrollViewer == null)
		{
			return;
		}
		ScrollViewer targetScrollViewer = dropInfo.TargetScrollViewer;
		ScrollingMode targetScrollingMode = dropInfo.TargetScrollingMode;
		Point position = e.GetPosition(targetScrollViewer);
		double num = Math.Min(targetScrollViewer.FontSize * 2.0, targetScrollViewer.ActualHeight / 2.0);
		if (targetScrollingMode == ScrollingMode.Both || targetScrollingMode == ScrollingMode.HorizontalOnly)
		{
			if (position.X >= targetScrollViewer.ActualWidth - num && targetScrollViewer.HorizontalOffset < targetScrollViewer.ExtentWidth - targetScrollViewer.ViewportWidth)
			{
				targetScrollViewer.LineRight();
			}
			else if (position.X < num && targetScrollViewer.HorizontalOffset > 0.0)
			{
				targetScrollViewer.LineLeft();
			}
		}
		if (targetScrollingMode == ScrollingMode.Both || targetScrollingMode == ScrollingMode.VerticalOnly)
		{
			if (position.Y >= targetScrollViewer.ActualHeight - num && targetScrollViewer.VerticalOffset < targetScrollViewer.ExtentHeight - targetScrollViewer.ViewportHeight)
			{
				targetScrollViewer.LineDown();
			}
			else if (position.Y < num && targetScrollViewer.VerticalOffset > 0.0)
			{
				targetScrollViewer.LineUp();
			}
		}
	}

	private static IDragSource TryGetDragHandler(DragInfo dragInfo, UIElement sender)
	{
		IDragSource dragSource = null;
		if (dragInfo != null && dragInfo.VisualSource != null)
		{
			dragSource = GetDragHandler(dragInfo.VisualSource);
		}
		if (dragSource == null && sender != null)
		{
			dragSource = GetDragHandler(sender);
		}
		return dragSource ?? DefaultDragHandler;
	}

	private static IDropTarget TryGetDropHandler(DropInfo dropInfo, UIElement sender)
	{
		IDropTarget dropTarget = null;
		if (dropInfo != null && dropInfo.VisualTarget != null)
		{
			dropTarget = GetDropHandler(dropInfo.VisualTarget);
		}
		if (dropTarget == null && sender != null)
		{
			dropTarget = GetDropHandler(sender);
		}
		return dropTarget ?? DefaultDropHandler;
	}

	private static void DragSourceOnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
	{
		DoMouseButtonDown(sender, e);
	}

	private static void DragSourceOnMouseRightButtonDown(object sender, MouseButtonEventArgs e)
	{
		DoMouseButtonDown(sender, e);
	}

	private static void DoMouseButtonDown(object sender, MouseButtonEventArgs e)
	{
		Point position = e.GetPosition((IInputElement)sender);
		if (e.ClickCount != 1 || (sender as UIElement).IsDragSourceIgnored() || sender is ProgressBar || (e.Source as UIElement).IsDragSourceIgnored() || (e.OriginalSource as UIElement).IsDragSourceIgnored() || (sender is TabControl && !HitTestUtilities.HitTest4Type<TabPanel>(sender, position)) || HitTestUtilities.HitTest4Type<RangeBase>(sender, position) || HitTestUtilities.HitTest4Type<TextBoxBase>(sender, position) || HitTestUtilities.HitTest4Type<PasswordBox>(sender, position) || HitTestUtilities.HitTest4Type<ComboBox>(sender, position) || HitTestUtilities.HitTest4GridViewColumnHeader(sender, position) || HitTestUtilities.HitTest4DataGridTypes(sender, position) || HitTestUtilities.IsNotPartOfSender(sender, e))
		{
			m_DragInfo = null;
			return;
		}
		m_DragInfo = new DragInfo(sender, e);
		if (m_DragInfo.VisualSourceItem == null)
		{
			m_DragInfo = null;
			return;
		}
		IDragSource dragSource = TryGetDragHandler(m_DragInfo, sender as UIElement);
		if (!dragSource.CanStartDrag(m_DragInfo))
		{
			m_DragInfo = null;
			return;
		}
		ItemsControl itemsControl = sender as ItemsControl;
		if ((Keyboard.Modifiers & ModifierKeys.Shift) == 0 && (Keyboard.Modifiers & ModifierKeys.Control) == 0 && m_DragInfo.VisualSourceItem != null && itemsControl != null && itemsControl.CanSelectMultipleItems())
		{
			List<object> list = itemsControl.GetSelectedItems().OfType<object>().ToList();
			if (list.Count > 1 && list.Contains(m_DragInfo.SourceItem))
			{
				m_ClickSupressItem = m_DragInfo.SourceItem;
				e.Handled = true;
			}
		}
	}

	private static void DragSourceOnMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
	{
		DoMouseButtonUp(sender, e);
	}

	private static void DragSourceOnMouseRightButtonUp(object sender, MouseButtonEventArgs e)
	{
		DoMouseButtonUp(sender, e);
	}

	private static void DoMouseButtonUp(object sender, MouseButtonEventArgs e)
	{
		Point position = e.GetPosition((IInputElement)sender);
		if (sender is TabControl && !HitTestUtilities.HitTest4Type<TabPanel>(sender, position))
		{
			m_DragInfo = null;
			m_ClickSupressItem = null;
			return;
		}
		if (sender is ItemsControl itemsControl && m_DragInfo != null && m_ClickSupressItem != null && m_ClickSupressItem == m_DragInfo.SourceItem)
		{
			if ((Keyboard.Modifiers & ModifierKeys.Control) != 0)
			{
				itemsControl.SetItemSelected(m_DragInfo.SourceItem, value: false);
			}
			else if ((Keyboard.Modifiers & ModifierKeys.Shift) == 0)
			{
				itemsControl.SetSelectedItem(m_DragInfo.SourceItem);
			}
		}
		m_DragInfo = null;
		m_ClickSupressItem = null;
	}

	private static void DragSourceOnMouseMove(object sender, MouseEventArgs e)
	{
		if (m_DragInfo == null || m_DragInProgress)
		{
			return;
		}
		Point dragStartPosition = m_DragInfo.DragStartPosition;
		if (m_DragInfo.MouseButton == MouseButton.Left && e.LeftButton == MouseButtonState.Released)
		{
			m_DragInfo = null;
			return;
		}
		if (GetCanDragWithMouseRightButton(m_DragInfo.VisualSource) && m_DragInfo.MouseButton == MouseButton.Right && e.RightButton == MouseButtonState.Released)
		{
			m_DragInfo = null;
			return;
		}
		Point position = e.GetPosition((IInputElement)sender);
		m_DragInfo.VisualSource?.ReleaseMouseCapture();
		if (m_DragInfo.VisualSource != sender || (!(Math.Abs(position.X - dragStartPosition.X) > SystemParameters.MinimumHorizontalDragDistance) && !(Math.Abs(position.Y - dragStartPosition.Y) > SystemParameters.MinimumVerticalDragDistance)))
		{
			return;
		}
		IDragSource dragSource = TryGetDragHandler(m_DragInfo, sender as UIElement);
		if (!dragSource.CanStartDrag(m_DragInfo))
		{
			return;
		}
		dragSource.StartDrag(m_DragInfo);
		if (m_DragInfo.Effects == DragDropEffects.None || m_DragInfo.Data == null)
		{
			return;
		}
		IDataObject dataObject = m_DragInfo.DataObject;
		if (dataObject == null)
		{
			dataObject = new DataObject(DataFormat.Name, m_DragInfo.Data);
		}
		else
		{
			dataObject.SetData(DataFormat.Name, m_DragInfo.Data);
		}
		try
		{
			m_DragInProgress = true;
			if (System.Windows.DragDrop.DoDragDrop(m_DragInfo.VisualSource, dataObject, m_DragInfo.Effects) == DragDropEffects.None)
			{
				dragSource.DragCancelled();
			}
		}
		catch (Exception exception)
		{
			if (!dragSource.TryCatchOccurredException(exception))
			{
				throw;
			}
		}
		finally
		{
			m_DragInProgress = false;
		}
		m_DragInfo = null;
	}

	private static void DragSourceOnQueryContinueDrag(object sender, QueryContinueDragEventArgs e)
	{
		if (e.Action == DragAction.Cancel || e.EscapePressed)
		{
			DragAdorner = null;
			EffectAdorner = null;
			DropTargetAdorner = null;
			Mouse.OverrideCursor = null;
		}
	}

	private static void DropTargetOnDragEnter(object sender, DragEventArgs e)
	{
		DropTargetOnDragOver(sender, e);
	}

	private static void DropTargetOnDragLeave(object sender, DragEventArgs e)
	{
		DragAdorner = null;
		EffectAdorner = null;
		DropTargetAdorner = null;
	}

	private static void DropTargetOnDragOver(object sender, DragEventArgs e)
	{
		Point position = e.GetPosition((IInputElement)sender);
		DropInfo dropInfo = new DropInfo(sender, e, m_DragInfo);
		IDropTarget dropTarget = TryGetDropHandler(dropInfo, sender as UIElement);
		UIElement visualTarget = dropInfo.VisualTarget;
		dropTarget.DragOver(dropInfo);
		if (DragAdorner == null && m_DragInfo != null)
		{
			CreateDragAdorner(dropInfo);
		}
		if (DragAdorner != null)
		{
			Point position2 = e.GetPosition(DragAdorner.AdornedElement);
			if (position2.X >= 0.0 && position2.Y >= 0.0)
			{
				_adornerPos = position2;
			}
			if (DragAdorner.RenderSize.Width > 0.0 && DragAdorner.RenderSize.Height > 0.0)
			{
				_adornerSize = DragAdorner.RenderSize;
			}
			if (m_DragInfo != null)
			{
				double offsetX = _adornerSize.Width * (0.0 - GetDragMouseAnchorPoint(m_DragInfo.VisualSource).X);
				double offsetY = _adornerSize.Height * (0.0 - GetDragMouseAnchorPoint(m_DragInfo.VisualSource).Y);
				_adornerPos.Offset(offsetX, offsetY);
				double width = DragAdorner.AdornedElement.RenderSize.Width;
				double num = _adornerPos.X + _adornerSize.Width;
				if (num > width)
				{
					_adornerPos.Offset(0.0 - num + width, 0.0);
				}
				if (_adornerPos.Y < 0.0)
				{
					_adornerPos.Y = 0.0;
				}
			}
			DragAdorner.MousePosition = _adornerPos;
			DragAdorner.InvalidateVisual();
		}
		if (HitTestUtilities.HitTest4Type<ScrollBar>(sender, position) || HitTestUtilities.HitTest4GridViewColumnHeader(sender, position) || HitTestUtilities.HitTest4DataGridTypesOnDragOver(sender, position))
		{
			e.Effects = DragDropEffects.None;
			e.Handled = true;
			return;
		}
		if (visualTarget != null)
		{
			UIElement uIElement = null;
			uIElement = ((visualTarget is TabControl) ? visualTarget.GetVisualDescendent<TabPanel>() : ((!(visualTarget is DataGrid)) ? (visualTarget.GetVisualDescendent<ItemsPresenter>() ?? visualTarget.GetVisualDescendent<ScrollContentPresenter>() ?? visualTarget) : (visualTarget.GetVisualDescendent<ScrollContentPresenter>() ?? visualTarget.GetVisualDescendent<ItemsPresenter>() ?? visualTarget)));
			if (uIElement != null)
			{
				if (dropInfo.DropTargetAdorner == null)
				{
					DropTargetAdorner = null;
				}
				else if (!dropInfo.DropTargetAdorner.IsInstanceOfType(DropTargetAdorner))
				{
					DropTargetAdorner = DropTargetAdorner.Create(dropInfo.DropTargetAdorner, uIElement, dropInfo);
				}
				DropTargetAdorner dropTargetAdorner = DropTargetAdorner;
				if (dropTargetAdorner != null)
				{
					Brush dropTargetAdornerBrush = GetDropTargetAdornerBrush(dropInfo.VisualTarget);
					if (dropTargetAdornerBrush != null)
					{
						dropTargetAdorner.Pen.Brush = dropTargetAdornerBrush;
					}
					dropTargetAdorner.DropInfo = dropInfo;
					dropTargetAdorner.InvalidateVisual();
				}
			}
		}
		if (m_DragInfo != null && (EffectAdorner == null || EffectAdorner.Effects != dropInfo.Effects))
		{
			CreateEffectAdorner(dropInfo);
		}
		if (EffectAdorner != null)
		{
			Point position3 = e.GetPosition(EffectAdorner.AdornedElement);
			position3.Offset(20.0, 20.0);
			EffectAdorner.MousePosition = position3;
			EffectAdorner.InvalidateVisual();
		}
		e.Effects = dropInfo.Effects;
		e.Handled = !dropInfo.NotHandled;
		if (!dropInfo.IsSameDragDropContextAsSource)
		{
			e.Effects = DragDropEffects.None;
		}
		Scroll(dropInfo, e);
	}

	private static void DropTargetOnDrop(object sender, DragEventArgs e)
	{
		DropInfo dropInfo = new DropInfo(sender, e, m_DragInfo);
		IDropTarget dropTarget = TryGetDropHandler(dropInfo, sender as UIElement);
		IDragSource dragSource = TryGetDragHandler(m_DragInfo, sender as UIElement);
		DragAdorner = null;
		EffectAdorner = null;
		DropTargetAdorner = null;
		dropTarget.DragOver(dropInfo);
		dropTarget.Drop(dropInfo);
		dragSource.Dropped(dropInfo);
		Mouse.OverrideCursor = null;
		e.Handled = !dropInfo.NotHandled;
	}

	private static void DropTargetOnGiveFeedback(object sender, GiveFeedbackEventArgs e)
	{
		if (EffectAdorner != null)
		{
			e.UseDefaultCursors = false;
			e.Handled = true;
			if (Mouse.OverrideCursor != Cursors.Arrow)
			{
				Mouse.OverrideCursor = Cursors.Arrow;
			}
		}
		else
		{
			e.UseDefaultCursors = true;
			e.Handled = true;
			if (Mouse.OverrideCursor != null)
			{
				Mouse.OverrideCursor = null;
			}
		}
	}

	public static bool GetIsDragSource(UIElement target)
	{
		return (bool)target.GetValue(IsDragSourceProperty);
	}

	public static void SetIsDragSource(UIElement target, bool value)
	{
		target.SetValue(IsDragSourceProperty, value);
	}

	private static void IsDragSourceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
	{
		UIElement uIElement = (UIElement)d;
		if ((bool)e.NewValue)
		{
			uIElement.PreviewMouseLeftButtonDown += DragSourceOnMouseLeftButtonDown;
			uIElement.PreviewMouseLeftButtonUp += DragSourceOnMouseLeftButtonUp;
			uIElement.PreviewMouseMove += DragSourceOnMouseMove;
			uIElement.QueryContinueDrag += DragSourceOnQueryContinueDrag;
		}
		else
		{
			uIElement.PreviewMouseLeftButtonDown -= DragSourceOnMouseLeftButtonDown;
			uIElement.PreviewMouseLeftButtonUp -= DragSourceOnMouseLeftButtonUp;
			uIElement.PreviewMouseMove -= DragSourceOnMouseMove;
			uIElement.QueryContinueDrag -= DragSourceOnQueryContinueDrag;
		}
	}

	public static bool GetIsDropTarget(UIElement target)
	{
		return (bool)target.GetValue(IsDropTargetProperty);
	}

	public static void SetIsDropTarget(UIElement target, bool value)
	{
		target.SetValue(IsDropTargetProperty, value);
	}

	private static void IsDropTargetChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
	{
		UIElement uIElement = (UIElement)d;
		if ((bool)e.NewValue)
		{
			uIElement.AllowDrop = true;
			if (uIElement is ItemsControl)
			{
				uIElement.DragEnter += DropTargetOnDragEnter;
				uIElement.DragLeave += DropTargetOnDragLeave;
				uIElement.DragOver += DropTargetOnDragOver;
				uIElement.Drop += DropTargetOnDrop;
				uIElement.GiveFeedback += DropTargetOnGiveFeedback;
			}
			else
			{
				uIElement.PreviewDragEnter += DropTargetOnDragEnter;
				uIElement.PreviewDragLeave += DropTargetOnDragLeave;
				uIElement.PreviewDragOver += DropTargetOnDragOver;
				uIElement.PreviewDrop += DropTargetOnDrop;
				uIElement.PreviewGiveFeedback += DropTargetOnGiveFeedback;
			}
			return;
		}
		uIElement.AllowDrop = false;
		if (uIElement is ItemsControl)
		{
			uIElement.DragEnter -= DropTargetOnDragEnter;
			uIElement.DragLeave -= DropTargetOnDragLeave;
			uIElement.DragOver -= DropTargetOnDragOver;
			uIElement.Drop -= DropTargetOnDrop;
			uIElement.GiveFeedback -= DropTargetOnGiveFeedback;
		}
		else
		{
			uIElement.PreviewDragEnter -= DropTargetOnDragEnter;
			uIElement.PreviewDragLeave -= DropTargetOnDragLeave;
			uIElement.PreviewDragOver -= DropTargetOnDragOver;
			uIElement.PreviewDrop -= DropTargetOnDrop;
			uIElement.PreviewGiveFeedback -= DropTargetOnGiveFeedback;
		}
		Mouse.OverrideCursor = null;
	}

	public static bool GetCanDragWithMouseRightButton(UIElement target)
	{
		return (bool)target.GetValue(CanDragWithMouseRightButtonProperty);
	}

	public static void SetCanDragWithMouseRightButton(UIElement target, bool value)
	{
		target.SetValue(CanDragWithMouseRightButtonProperty, value);
	}

	private static void CanDragWithMouseRightButtonChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
	{
		UIElement uIElement = (UIElement)d;
		if ((bool)e.NewValue)
		{
			uIElement.PreviewMouseRightButtonDown += DragSourceOnMouseRightButtonDown;
			uIElement.PreviewMouseRightButtonUp += DragSourceOnMouseRightButtonUp;
		}
		else
		{
			uIElement.PreviewMouseRightButtonDown -= DragSourceOnMouseRightButtonDown;
			uIElement.PreviewMouseRightButtonUp -= DragSourceOnMouseRightButtonUp;
		}
	}

	public static IDragSource GetDragHandler(UIElement target)
	{
		return (IDragSource)target.GetValue(DragHandlerProperty);
	}

	public static void SetDragHandler(UIElement target, IDragSource value)
	{
		target.SetValue(DragHandlerProperty, value);
	}

	public static IDropTarget GetDropHandler(UIElement target)
	{
		return (IDropTarget)target.GetValue(DropHandlerProperty);
	}

	public static void SetDropHandler(UIElement target, IDropTarget value)
	{
		target.SetValue(DropHandlerProperty, value);
	}

	public static ScrollingMode GetDropScrollingMode(UIElement target)
	{
		return (ScrollingMode)target.GetValue(DropScrollingModeProperty);
	}

	public static void SetDropScrollingMode(UIElement target, ScrollingMode value)
	{
		target.SetValue(DropScrollingModeProperty, value);
	}

	public static Brush GetDropTargetAdornerBrush(UIElement target)
	{
		return (Brush)target.GetValue(DropTargetAdornerBrushProperty);
	}

	public static void SetDropTargetAdornerBrush(UIElement target, Brush value)
	{
		target.SetValue(DropTargetAdornerBrushProperty, value);
	}

	public static string GetDragDropContext(UIElement target)
	{
		return (string)target.GetValue(DragDropContextProperty);
	}

	public static void SetDragDropContext(UIElement target, string value)
	{
		target.SetValue(DragDropContextProperty, value);
	}

	public static bool GetDragSourceIgnore(UIElement source)
	{
		return (bool)source.GetValue(DragSourceIgnoreProperty);
	}

	public static void SetDragSourceIgnore(UIElement source, bool value)
	{
		source.SetValue(DragSourceIgnoreProperty, value);
	}

	public static bool GetDragDirectlySelectedOnly(DependencyObject obj)
	{
		return (bool)obj.GetValue(DragDirectlySelectedOnlyProperty);
	}

	public static void SetDragDirectlySelectedOnly(DependencyObject obj, bool value)
	{
		obj.SetValue(DragDirectlySelectedOnlyProperty, value);
	}

	public static DragDropKeyStates GetDragDropCopyKeyState(UIElement target)
	{
		return (DragDropKeyStates)target.GetValue(DragDropCopyKeyStateProperty);
	}

	public static void SetDragDropCopyKeyState(UIElement target, DragDropKeyStates value)
	{
		target.SetValue(DragDropCopyKeyStateProperty, value);
	}

	public static bool GetUseDefaultDragAdorner(UIElement target)
	{
		return (bool)target.GetValue(UseDefaultDragAdornerProperty);
	}

	public static void SetUseDefaultDragAdorner(UIElement target, bool value)
	{
		target.SetValue(UseDefaultDragAdornerProperty, value);
	}

	public static double GetDefaultDragAdornerOpacity(UIElement target)
	{
		return (double)target.GetValue(DefaultDragAdornerOpacityProperty);
	}

	public static void SetDefaultDragAdornerOpacity(UIElement target, double value)
	{
		target.SetValue(DefaultDragAdornerOpacityProperty, value);
	}

	public static Point GetDragMouseAnchorPoint(UIElement target)
	{
		return (Point)target.GetValue(DragMouseAnchorPointProperty);
	}

	public static void SetDragMouseAnchorPoint(UIElement target, Point value)
	{
		target.SetValue(DragMouseAnchorPointProperty, value);
	}

	public static DataTemplate GetDragAdornerTemplate(UIElement target)
	{
		return (DataTemplate)target.GetValue(DragAdornerTemplateProperty);
	}

	public static void SetDragAdornerTemplate(UIElement target, DataTemplate value)
	{
		target.SetValue(DragAdornerTemplateProperty, value);
	}

	public static void SetDragAdornerTemplateSelector(DependencyObject element, DataTemplateSelector value)
	{
		element.SetValue(DragAdornerTemplateSelectorProperty, value);
	}

	public static DataTemplateSelector GetDragAdornerTemplateSelector(DependencyObject element)
	{
		return (DataTemplateSelector)element.GetValue(DragAdornerTemplateSelectorProperty);
	}

	public static bool GetUseVisualSourceItemSizeForDragAdorner(UIElement target)
	{
		return (bool)target.GetValue(UseVisualSourceItemSizeForDragAdornerProperty);
	}

	public static void SetUseVisualSourceItemSizeForDragAdorner(UIElement target, bool value)
	{
		target.SetValue(UseVisualSourceItemSizeForDragAdornerProperty, value);
	}

	public static bool GetUseDefaultEffectDataTemplate(UIElement target)
	{
		return (bool)target.GetValue(UseDefaultEffectDataTemplateProperty);
	}

	public static void SetUseDefaultEffectDataTemplate(UIElement target, bool value)
	{
		target.SetValue(UseDefaultEffectDataTemplateProperty, value);
	}

	public static DataTemplate GetEffectNoneAdornerTemplate(UIElement target)
	{
		return (DataTemplate)target.GetValue(EffectNoneAdornerTemplateProperty);
	}

	public static void SetEffectNoneAdornerTemplate(UIElement target, DataTemplate value)
	{
		target.SetValue(EffectNoneAdornerTemplateProperty, value);
	}

	public static DataTemplate GetEffectCopyAdornerTemplate(UIElement target)
	{
		return (DataTemplate)target.GetValue(EffectCopyAdornerTemplateProperty);
	}

	public static void SetEffectCopyAdornerTemplate(UIElement target, DataTemplate value)
	{
		target.SetValue(EffectCopyAdornerTemplateProperty, value);
	}

	public static DataTemplate GetEffectMoveAdornerTemplate(UIElement target)
	{
		return (DataTemplate)target.GetValue(EffectMoveAdornerTemplateProperty);
	}

	public static void SetEffectMoveAdornerTemplate(UIElement target, DataTemplate value)
	{
		target.SetValue(EffectMoveAdornerTemplateProperty, value);
	}

	public static DataTemplate GetEffectLinkAdornerTemplate(UIElement target)
	{
		return (DataTemplate)target.GetValue(EffectLinkAdornerTemplateProperty);
	}

	public static void SetEffectLinkAdornerTemplate(UIElement target, DataTemplate value)
	{
		target.SetValue(EffectLinkAdornerTemplateProperty, value);
	}

	public static DataTemplate GetEffectAllAdornerTemplate(UIElement target)
	{
		return (DataTemplate)target.GetValue(EffectAllAdornerTemplateProperty);
	}

	public static void SetEffectAllAdornerTemplate(UIElement target, DataTemplate value)
	{
		target.SetValue(EffectAllAdornerTemplateProperty, value);
	}

	public static DataTemplate GetEffectScrollAdornerTemplate(UIElement target)
	{
		return (DataTemplate)target.GetValue(EffectScrollAdornerTemplateProperty);
	}

	public static void SetEffectScrollAdornerTemplate(UIElement target, DataTemplate value)
	{
		target.SetValue(EffectScrollAdornerTemplateProperty, value);
	}

	public static Orientation? GetItemsPanelOrientation(UIElement source)
	{
		return (Orientation?)source.GetValue(ItemsPanelOrientationProperty);
	}

	public static void SetItemsPanelOrientation(UIElement source, Orientation? value)
	{
		source.SetValue(ItemsPanelOrientationProperty, value);
	}
}
