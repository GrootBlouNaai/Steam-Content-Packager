using System;
using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Forms;
using SteamContentPackager.Properties;
using SteamContentPackager.Utils;

namespace SteamContentPackager;

public partial class App : System.Windows.Application
{
	private App()
	{
		AppDomain.CurrentDomain.AssemblyResolve += OnAssemblyResolve;
		SteamContentPackager.Utils.Settings.Load();
	}

	private bool GetSteamPath()
	{
		FolderBrowserDialog folderBrowserDialog = new FolderBrowserDialog();
		folderBrowserDialog.Description = "Select Steam directory";
		folderBrowserDialog.ShowNewFolderButton = false;
		while (string.IsNullOrEmpty(SteamContentPackager.Utils.Settings.SteamPath))
		{
			if (folderBrowserDialog.ShowDialog() == DialogResult.OK)
			{
				if (!File.Exists($"{folderBrowserDialog.SelectedPath}\\steam.exe"))
				{
					System.Windows.Forms.MessageBox.Show("Invalid Steam Directory");
					continue;
				}
				SteamContentPackager.Utils.Settings.SteamPath = folderBrowserDialog.SelectedPath;
				return true;
			}
			return false;
		}
		return false;
	}

	private static Assembly OnAssemblyResolve(object sender, ResolveEventArgs args)
	{
		if ((args.Name.Contains(",") ? args.Name.Substring(0, args.Name.IndexOf(",", StringComparison.InvariantCultureIgnoreCase)) : args.Name.Replace(".dll", "")).ToLower().EndsWith(".resources"))
		{
			return null;
		}
		Assembly.GetExecutingAssembly().GetManifestResourceNames();
		string text = $"{Assembly.GetExecutingAssembly().EntryPoint.DeclaringType?.Namespace}.Embedded.{new AssemblyName(args.Name).Name}.dll";
		if (text.Contains("SteamKit2"))
		{
			return Assembly.Load(SteamContentPackager.Properties.Resources.SteamKit2);
		}
		if (text.Contains("Gong"))
		{
			return Assembly.Load(SteamContentPackager.Properties.Resources.GongSolutions_Wpf_DragDrop);
		}
		if (text.Contains("Json"))
		{
			return Assembly.Load(SteamContentPackager.Properties.Resources.Newtonsoft_Json);
		}
		using Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(text);
		if (stream == null)
		{
			return null;
		}
		byte[] array = new byte[stream.Length];
		stream.Read(array, 0, (int)stream.Length);
		return Assembly.Load(array);
	}
}
