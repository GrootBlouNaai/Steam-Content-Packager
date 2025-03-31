using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows.Controls;
using SteamContentPackager.Utils;

namespace SteamContentPackager.Plugin;

public class PluginManager
{
	public class PluginInfo
	{
		public Type PluginType;

		public Type ArgType;

		public Type ControlType;

		public string Name;

		public override string ToString()
		{
			return Name;
		}
	}

	public static List<PluginInfo> LoadPlugins()
	{
		List<PluginInfo> list = new List<PluginInfo>();
		if (Directory.Exists($"{AppDomain.CurrentDomain.BaseDirectory}\\Plugins"))
		{
			string[] files = Directory.GetFiles($"{AppDomain.CurrentDomain.BaseDirectory}\\Plugins", "*.dll");
			foreach (string path in files)
			{
				try
				{
					Assembly assembly = Assembly.LoadFile(path);
					PluginInfo pluginInfo = new PluginInfo();
					BasePlugin basePlugin = (BasePlugin)Activator.CreateInstance(assembly.GetTypes().First((Type x) => typeof(BasePlugin).IsAssignableFrom(x)));
					Type argType = assembly.GetTypes().First((Type x) => typeof(PluginArgs).IsAssignableFrom(x));
					Type controlType = assembly.GetTypes().First((Type x) => typeof(UserControl).IsAssignableFrom(x));
					pluginInfo.PluginType = basePlugin.GetType();
					pluginInfo.ArgType = argType;
					pluginInfo.ControlType = controlType;
					pluginInfo.Name = basePlugin.Name;
					list.Add(pluginInfo);
				}
				catch (Exception arg)
				{
					Log.Write($"Failed to load plugin:\n {arg}");
				}
			}
		}
		return list;
	}
}
