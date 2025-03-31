using System;
using System.Globalization;
using System.Windows.Data;
using SteamContentPackager.Steam;
using SteamKit2;

namespace SteamContentPackager.UI.Converters;

public class AccountIdToNameConverter : IValueConverter
{
	public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
	{
		if (value == null)
		{
			return "";
		}
		uint unAccountID = (uint)value;
		return SteamSession.SteamFriends.GetFriendPersonaName(new SteamID(unAccountID, EUniverse.Public, EAccountType.Individual));
	}

	public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
	{
		throw new NotImplementedException();
	}
}
