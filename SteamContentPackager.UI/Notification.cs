using System;
using System.ComponentModel;
using System.Drawing;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Threading;
using SteamContentPackager.Annotations;
using SteamContentPackager.Steam;

namespace SteamContentPackager.UI;

public partial class Notification : Window, INotifyPropertyChanged, IComponentConnector
{
	private SteamApp _steamApp;

	public SteamApp SteamApp
	{
		get
		{
			return _steamApp;
		}
		set
		{
			_steamApp = value;
			OnPropertyChanged("SteamApp");
		}
	}

	public event PropertyChangedEventHandler PropertyChanged;

	public Notification(SteamApp app)
	{
		SteamApp = app;
		InitializeComponent();
		base.Dispatcher.BeginInvoke(DispatcherPriority.ApplicationIdle, (Action)delegate
		{
			Rectangle bounds = Screen.PrimaryScreen.Bounds;
			base.Left = (double)bounds.Width - base.ActualWidth - 100.0;
			base.Top = (double)bounds.Height - base.ActualHeight - 3.0;
		});
		Thread thread = new Thread(Destroy);
		thread.SetApartmentState(ApartmentState.STA);
		thread.Start(4500);
	}

	private void Destroy(object timeout)
	{
		for (int num = (int)timeout; num > 0; num -= 20)
		{
			Thread.Sleep(20);
		}
		base.Dispatcher.Invoke(base.Close);
	}

	[NotifyPropertyChangedInvocator]
	protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
	{
		this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
	}

	private void Notification_OnMouseUp(object sender, MouseButtonEventArgs e)
	{
		base.Dispatcher.Invoke(base.Close);
	}
}
