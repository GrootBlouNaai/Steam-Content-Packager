using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using SteamContentPackager.Properties;
using SteamContentPackager.Steam;
using SteamContentPackager.Utils;

namespace SteamContentPackager;

public partial class App : Application
{
	static App()
	{
		Log.AddHandler<ConsoleLogHandler>();
		Log.AddHandler<ListBoxLogHandler>(new ListBox());
		Log.AddHandler<FileLogHandler>();
		AppDomain.CurrentDomain.UnhandledException += CurrentDomainOnUnhandledException;
		CreateDirectories();
	}

	private static void CreateDirectories()
	{
		string baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
		if (!Directory.Exists($"{baseDirectory}\\UserData"))
		{
			Directory.CreateDirectory($"{baseDirectory}\\UserData");
		}
	}

	private static void CurrentDomainOnUnhandledException(object sender, UnhandledExceptionEventArgs args)
	{
		Log.WriteException((Exception)args.ExceptionObject);
		if (args.IsTerminating)
		{
			SteamSession.Disconnect();
		}
	}

	private void App_OnExit(object sender, ExitEventArgs e)
	{
		Settings.Default.Save();
		SteamSession.Disconnect();
	}
}
