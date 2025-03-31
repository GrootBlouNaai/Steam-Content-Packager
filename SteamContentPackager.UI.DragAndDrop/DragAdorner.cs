using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;

namespace SteamContentPackager.UI.DragAndDrop;

internal class DragAdorner : Adorner
{
	private readonly AdornerLayer m_AdornerLayer;

	private readonly UIElement m_Adornment;

	private Point m_MousePosition;

	public DragDropEffects Effects { get; private set; }

	public Point MousePosition
	{
		get
		{
			return m_MousePosition;
		}
		set
		{
			if (m_MousePosition != value)
			{
				m_MousePosition = value;
				m_AdornerLayer.Update(base.AdornedElement);
			}
		}
	}

	protected override int VisualChildrenCount => 1;

	public DragAdorner(UIElement adornedElement, UIElement adornment, DragDropEffects effects = DragDropEffects.None)
		: base(adornedElement)
	{
		m_AdornerLayer = AdornerLayer.GetAdornerLayer(adornedElement);
		m_AdornerLayer.Add(this);
		m_Adornment = adornment;
		base.IsHitTestVisible = false;
		Effects = effects;
	}

	public void Detatch()
	{
		m_AdornerLayer.Remove(this);
	}

	protected override Size ArrangeOverride(Size finalSize)
	{
		m_Adornment.Arrange(new Rect(finalSize));
		return finalSize;
	}

	public override GeneralTransform GetDesiredTransform(GeneralTransform transform)
	{
		GeneralTransformGroup generalTransformGroup = new GeneralTransformGroup();
		generalTransformGroup.Children.Add(base.GetDesiredTransform(transform));
		generalTransformGroup.Children.Add(new TranslateTransform(MousePosition.X - 4.0, MousePosition.Y - 4.0));
		return generalTransformGroup;
	}

	protected override Visual GetVisualChild(int index)
	{
		return m_Adornment;
	}

	protected override Size MeasureOverride(Size constraint)
	{
		m_Adornment.Measure(constraint);
		return m_Adornment.DesiredSize;
	}
}
