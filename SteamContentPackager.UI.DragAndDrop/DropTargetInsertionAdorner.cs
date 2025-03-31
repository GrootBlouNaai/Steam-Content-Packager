using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;

namespace SteamContentPackager.UI.DragAndDrop;

public class DropTargetInsertionAdorner : DropTargetAdorner
{
	private static readonly PathGeometry m_Triangle;

	[Obsolete("This constructor is obsolete and will be deleted in next major release.")]
	public DropTargetInsertionAdorner(UIElement adornedElement)
		: base(adornedElement, null)
	{
	}

	public DropTargetInsertionAdorner(UIElement adornedElement, DropInfo dropInfo)
		: base(adornedElement, dropInfo)
	{
	}

	protected override void OnRender(DrawingContext drawingContext)
	{
		DropInfo dropInfo = base.DropInfo;
		if (!(dropInfo.VisualTarget is ItemsControl itemsControl))
		{
			return;
		}
		UIElement visualTargetItem = dropInfo.VisualTargetItem;
		ItemsControl itemsControl2 = ((visualTargetItem == null) ? itemsControl : ItemsControl.ItemsControlFromItemContainer(visualTargetItem));
		if (itemsControl2 == null)
		{
			return;
		}
		int count = itemsControl2.Items.Count;
		int num = Math.Min(dropInfo.InsertIndex, count - 1);
		bool flag = false;
		CollectionViewGroup targetGroup = dropInfo.TargetGroup;
		if (targetGroup != null && targetGroup.IsBottomLevel && dropInfo.InsertPosition.HasFlag(RelativeInsertPosition.AfterTargetItem))
		{
			int num2 = targetGroup.Items.IndexOf(dropInfo.TargetItem);
			flag = num2 == targetGroup.ItemCount - 1;
			if (flag && dropInfo.InsertIndex != count)
			{
				num--;
			}
		}
		UIElement uIElement = (UIElement)itemsControl2.ItemContainerGenerator.ContainerFromIndex(num);
		if (uIElement == null)
		{
			return;
		}
		Rect rect = new Rect(uIElement.TranslatePoint(default(Point), base.AdornedElement), uIElement.RenderSize);
		double num3 = 0.0;
		double val = base.DropInfo.TargetScrollViewer?.ViewportWidth ?? double.MaxValue;
		double num4 = base.DropInfo.TargetScrollViewer?.ViewportHeight ?? double.MaxValue;
		Point point;
		Point point2;
		if (dropInfo.VisualTargetOrientation == Orientation.Vertical)
		{
			if (dropInfo.InsertIndex == count || flag)
			{
				rect.Y += uIElement.RenderSize.Height;
			}
			double x = Math.Min(rect.Right, val);
			double x2 = ((rect.X < 0.0) ? 0.0 : rect.X);
			point = new Point(x2, rect.Y);
			point2 = new Point(x, rect.Y);
		}
		else
		{
			double num5 = rect.X;
			if (dropInfo.VisualTargetFlowDirection == FlowDirection.LeftToRight && dropInfo.InsertIndex == count)
			{
				num5 += uIElement.RenderSize.Width;
			}
			else if (dropInfo.VisualTargetFlowDirection == FlowDirection.RightToLeft && dropInfo.InsertIndex != count)
			{
				num5 += uIElement.RenderSize.Width;
			}
			point = new Point(num5, rect.Y);
			point2 = new Point(num5, rect.Bottom);
			num3 = 90.0;
		}
		drawingContext.DrawLine(base.Pen, point, point2);
		DrawTriangle(drawingContext, point, num3);
		DrawTriangle(drawingContext, point2, 180.0 + num3);
	}

	private void DrawTriangle(DrawingContext drawingContext, Point origin, double rotation)
	{
		drawingContext.PushTransform(new TranslateTransform(origin.X, origin.Y));
		drawingContext.PushTransform(new RotateTransform(rotation));
		drawingContext.DrawGeometry(base.Pen.Brush, null, m_Triangle);
		drawingContext.Pop();
		drawingContext.Pop();
	}

	static DropTargetInsertionAdorner()
	{
		LineSegment lineSegment = new LineSegment(new Point(0.0, -5.0), isStroked: false);
		lineSegment.Freeze();
		LineSegment lineSegment2 = new LineSegment(new Point(0.0, 5.0), isStroked: false);
		lineSegment2.Freeze();
		PathFigure pathFigure = new PathFigure
		{
			StartPoint = new Point(5.0, 0.0)
		};
		pathFigure.Segments.Add(lineSegment);
		pathFigure.Segments.Add(lineSegment2);
		pathFigure.Freeze();
		m_Triangle = new PathGeometry();
		m_Triangle.Figures.Add(pathFigure);
		m_Triangle.Freeze();
	}
}
