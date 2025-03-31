using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Resources;
using System.Runtime.CompilerServices;

namespace SteamContentPackager.Properties;

[GeneratedCode("System.Resources.Tools.StronglyTypedResourceBuilder", "4.0.0.0")]
[DebuggerNonUserCode]
[CompilerGenerated]
internal class Resources
{
	private static ResourceManager resourceMan;

	private static CultureInfo resourceCulture;

	[EditorBrowsable(EditorBrowsableState.Advanced)]
	internal static ResourceManager ResourceManager
	{
		get
		{
			if (resourceMan == null)
			{
				resourceMan = new ResourceManager("SteamContentPackager.Properties.Resources", typeof(Resources).Assembly);
			}
			return resourceMan;
		}
	}

	[EditorBrowsable(EditorBrowsableState.Advanced)]
	internal static CultureInfo Culture
	{
		get
		{
			return resourceCulture;
		}
		set
		{
			resourceCulture = value;
		}
	}

	internal static byte[] GongSolutions_Wpf_DragDrop => (byte[])ResourceManager.GetObject("GongSolutions_Wpf_DragDrop", resourceCulture);

	internal static byte[] Newtonsoft_Json => (byte[])ResourceManager.GetObject("Newtonsoft_Json", resourceCulture);

	internal static byte[] SteamKit2 => (byte[])ResourceManager.GetObject("SteamKit2", resourceCulture);

	internal Resources()
	{
	}
}
