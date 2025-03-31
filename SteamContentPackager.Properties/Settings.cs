using System.CodeDom.Compiler;
using System.Configuration;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Windows;

namespace SteamContentPackager.Properties;

[CompilerGenerated]
[GeneratedCode("Microsoft.VisualStudio.Editors.SettingsDesigner.SettingsSingleFileGenerator", "15.3.0.0")]
internal sealed class Settings : ApplicationSettingsBase
{
	private static Settings defaultInstance = (Settings)SettingsBase.Synchronized(new Settings());

	public static Settings Default => defaultInstance;

	[UserScopedSetting]
	[DebuggerNonUserCode]
	[DefaultSettingValue("600")]
	public double Width
	{
		get
		{
			return (double)this["Width"];
		}
		set
		{
			this["Width"] = value;
		}
	}

	[UserScopedSetting]
	[DebuggerNonUserCode]
	[DefaultSettingValue("500")]
	public double Height
	{
		get
		{
			return (double)this["Height"];
		}
		set
		{
			this["Height"] = value;
		}
	}

	[UserScopedSetting]
	[DebuggerNonUserCode]
	[DefaultSettingValue("200")]
	public double Top
	{
		get
		{
			return (double)this["Top"];
		}
		set
		{
			this["Top"] = value;
		}
	}

	[UserScopedSetting]
	[DebuggerNonUserCode]
	[DefaultSettingValue("200")]
	public double Left
	{
		get
		{
			return (double)this["Left"];
		}
		set
		{
			this["Left"] = value;
		}
	}

	[UserScopedSetting]
	[DebuggerNonUserCode]
	[DefaultSettingValue("Normal")]
	public WindowState WindowState
	{
		get
		{
			return (WindowState)this["WindowState"];
		}
		set
		{
			this["WindowState"] = value;
		}
	}
}
