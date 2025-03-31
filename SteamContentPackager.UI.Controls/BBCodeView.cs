using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;
using SteamContentPackager.Annotations;
using SteamContentPackager.Steam;
using SteamContentPackager.Utils;

namespace SteamContentPackager.UI.Controls;

public partial class BBCodeView : UserControl, INotifyPropertyChanged, IComponentConnector
{
	public static readonly DependencyProperty AppProperty = DependencyProperty.Register("App", typeof(SteamApp), typeof(BBCodeView), new PropertyMetadata(null, PropertyChangedCallback));

	private string _bbCode;

	public SteamApp App
	{
		get
		{
			return (SteamApp)GetValue(AppProperty);
		}
		set
		{
			SetValue(AppProperty, value);
		}
	}

	public string BBCode
	{
		get
		{
			return _bbCode;
		}
		set
		{
			_bbCode = value;
			OnPropertyChanged("BBCode");
		}
	}

	public event PropertyChangedEventHandler PropertyChanged;

	private static void PropertyChangedCallback(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs dependencyPropertyChangedEventArgs)
	{
		BBCodeView bBCodeView = (BBCodeView)dependencyObject;
		bBCodeView.BBCode = "";
	}

	public BBCodeView()
	{
		InitializeComponent();
	}

	[NotifyPropertyChangedInvocator]
	protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
	{
		this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
	}

	private async void Generate_Clicked(object sender, RoutedEventArgs e)
	{
		BBCode = await BbCode.Generate(App);
	}

	private void CopyToClipboard_Clicked(object sender, RoutedEventArgs e)
	{
		Clipboard.SetText(BBCode);
		Log.Write($"Copied BBCode for {App.Name} to clipboard");
	}
}
