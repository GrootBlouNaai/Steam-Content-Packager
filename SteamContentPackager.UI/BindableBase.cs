using System.ComponentModel;
using System.Runtime.CompilerServices;
using SteamContentPackager.Annotations;

namespace SteamContentPackager.UI;

public abstract class BindableBase : INotifyPropertyChanged
{
	public event PropertyChangedEventHandler PropertyChanged;

	[NotifyPropertyChangedInvocator]
	protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
	{
		this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
	}
}
