using System;
using System.CodeDom.Compiler;
using System.Diagnostics;
using System.Windows;
using System.Windows.Markup;
using SteamContentPackager.Steam;
using SteamContentPackager.Utils;

namespace SteamContentPackager;

public partial class MainWindow : Window, IComponentConnector
{
	public MainWindow()
	{
		PICSUpdater.UpdateComplete += PICSUpdaterOnUpdateComplete;
		InitializeComponent();
		Log.GetHandler<ListBoxLogHandler>().SetListBox(LogBox);
	}

	private void PICSUpdaterOnUpdateComplete(object sender, PICSUpdater.UpdateCompleteEventArgs eventArgs)
	{
		Application.Current.Dispatcher.Invoke(() => MainGrid.IsEnabled = true);
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	internal Delegate _CreateDelegate(Type delegateType, string handler)
	{
		return Delegate.CreateDelegate(delegateType, this, handler);
	}
}
