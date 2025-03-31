using System;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;

namespace SteamContentPackager.UI.DragAndDrop;

public abstract class DropTargetAdorner : Adorner
{
	private readonly AdornerLayer m_AdornerLayer;

	public DropInfo DropInfo { get; set; }

	public Pen Pen { get; set; } = new Pen(Brushes.Gray, 2.0);

	[Obsolete("This constructor is obsolete and will be deleted in next major release.")]
	public DropTargetAdorner(UIElement adornedElement)
		: this(adornedElement, null)
	{
	}

	public DropTargetAdorner(UIElement adornedElement, DropInfo dropInfo)
		: base(adornedElement)
	{
		DropInfo = dropInfo;
		base.IsHitTestVisible = false;
		base.AllowDrop = false;
		base.SnapsToDevicePixels = true;
		m_AdornerLayer = AdornerLayer.GetAdornerLayer(adornedElement);
		m_AdornerLayer.Add(this);
	}

	public void Detatch()
	{
		m_AdornerLayer.Remove(this);
	}

	internal static DropTargetAdorner Create(Type type, UIElement adornedElement, IDropInfo dropInfo)
	{
		if (!typeof(DropTargetAdorner).IsAssignableFrom(type))
		{
			throw new InvalidOperationException("The requested adorner class does not derive from DropTargetAdorner.");
		}
		return type.GetConstructor(new Type[2]
		{
			typeof(UIElement),
			typeof(DropInfo)
		})?.Invoke(new object[2] { adornedElement, dropInfo }) as DropTargetAdorner;
	}
}
