using System;
using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Markup;
using SteamContentPackager.Annotations;
using SteamContentPackager.Utils;

namespace SteamContentPackager;

public partial class LogWindow : Window, INotifyPropertyChanged, IComponentConnector
{
	private bool _quit;

	private float _windowOpacity;

	public bool ShowWindow
	{
		get
		{
			return Settings.ShowLog;
		}
		set
		{
			Settings.ShowLog = value;
			OnPropertyChanged("ShowWindow");
			base.Visibility = ((!Settings.ShowLog) ? Visibility.Hidden : Visibility.Visible);
		}
	}

	public float WindowOpacity
	{
		get
		{
			return _windowOpacity;
		}
		set
		{
			_windowOpacity = value;
			base.Opacity = value;
			OnPropertyChanged("WindowOpacity");
		}
	}

	public event PropertyChangedEventHandler PropertyChanged;

	public LogWindow()
	{
		WindowOpacity = 1f;
		InitializeComponent();
		base.Owner = null;
	}

	private void MinimizeButton_OnClick(object sender, RoutedEventArgs e)
	{
		base.WindowState = WindowState.Minimized;
	}

	private void CloseButton_OnClick(object sender, RoutedEventArgs e)
	{
		Close();
	}

	private void TitleMouseDown(object sender, MouseButtonEventArgs e)
	{
		DragMove();
	}

	public void CloseWindow()
	{
		_quit = true;
		Close();
	}

	protected override void OnClosing(CancelEventArgs e)
	{
		if (_quit)
		{
			base.OnClosing(e);
			return;
		}
		e.Cancel = true;
		ShowWindow = false;
		Hide();
	}

	[NotifyPropertyChangedInvocator]
	protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
	{
		this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	internal Delegate _CreateDelegate(Type delegateType, string handler)
	{
		return Delegate.CreateDelegate(delegateType, this, handler);
	}
}
