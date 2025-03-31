using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;

namespace SteamContentPackager.UI.Controls;

public partial class SettingView : UserControl, IComponentConnector
{
	public int MaxConnections
	{
		get
		{
			return Config.MaxConnections;
		}
		set
		{
			Config.MaxConnections = value;
		}
	}

	public SettingView()
	{
		InitializeComponent();
	}

	private void Save_Clicked(object sender, RoutedEventArgs e)
	{
		Config.Save();
	}
}
