using System.Windows.Data;
using SteamContentPackager.Properties;

namespace SteamContentPackager;

public class SettingBindingExtension : Binding
{
	public SettingBindingExtension()
	{
		Initialize();
	}

	public SettingBindingExtension(string path)
		: base(path)
	{
		Initialize();
	}

	private void Initialize()
	{
		base.Source = Settings.Default;
		base.Mode = BindingMode.TwoWay;
	}
}
