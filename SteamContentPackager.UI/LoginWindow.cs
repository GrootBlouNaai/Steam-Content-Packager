using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Markup;
using SteamContentPackager.Steam;
using SteamContentPackager.Utils;
using SteamKit2;

namespace SteamContentPackager.UI;

public partial class LoginWindow : Window, INotifyPropertyChanged, IComponentConnector
{
	private bool _twoFactorAuth;

	private bool _steamGuardAuth;

	private string _errorMessage;

	public bool Cancelled;

	public string ErrorMessage
	{
		get
		{
			return _errorMessage;
		}
		set
		{
			_errorMessage = value;
			OnPropertyChanged("ErrorMessage");
		}
	}

	public event PropertyChangedEventHandler PropertyChanged;

	public LoginWindow()
	{
		ErrorMessage = "";
		InitializeComponent();
		SteamSession.Init();
		UsernameTextBox.Text = Settings.Username;
		PasswordTextBox.Password = Settings.Password;
		SteamSession.CallbackManager.Subscribe<ConnectedCallback>((Action<ConnectedCallback>)OnConnected);
		SteamSession.CallbackManager.Subscribe<LoggedOnCallback>((Action<LoggedOnCallback>)OnLoggedOnCallback);
	}

	protected void OnPropertyChanged(string name)
	{
		this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
	}

	private void OnLoggedOnCallback(LoggedOnCallback loggedOnCallback)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Invalid comparison between Unknown and I4
		//IL_000f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0014: Unknown result type (might be due to invalid IL or missing references)
		//IL_0015: Unknown result type (might be due to invalid IL or missing references)
		//IL_0017: Invalid comparison between Unknown and I4
		//IL_0029: Unknown result type (might be due to invalid IL or missing references)
		//IL_002c: Invalid comparison between Unknown and I4
		//IL_0019: Unknown result type (might be due to invalid IL or missing references)
		//IL_001b: Invalid comparison between Unknown and I4
		//IL_002e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0031: Invalid comparison between Unknown and I4
		//IL_001d: Unknown result type (might be due to invalid IL or missing references)
		//IL_001f: Invalid comparison between Unknown and I4
		//IL_0174: Unknown result type (might be due to invalid IL or missing references)
		//IL_0185: Unknown result type (might be due to invalid IL or missing references)
		//IL_018b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0111: Unknown result type (might be due to invalid IL or missing references)
		//IL_0122: Unknown result type (might be due to invalid IL or missing references)
		//IL_0128: Unknown result type (might be due to invalid IL or missing references)
		//IL_0199: Unknown result type (might be due to invalid IL or missing references)
		//IL_0136: Unknown result type (might be due to invalid IL or missing references)
		if ((int)loggedOnCallback.Result != 1)
		{
			SteamSession.Disconnect();
		}
		EResult result = loggedOnCallback.Result;
		if ((int)result <= 5)
		{
			if ((int)result == 1)
			{
				Cancelled = false;
				Application.Current.Dispatcher.Invoke(delegate
				{
					MainWindow obj = (MainWindow)base.Owner;
					obj.LoggedOn = true;
					obj.LoginButton.Content = "LOGOFF";
					Close();
				});
				return;
			}
			if ((int)result == 5)
			{
				if (!string.IsNullOrEmpty(Settings.LoginKey))
				{
					Settings.LoginKey = null;
					Application.Current.Dispatcher.Invoke(delegate
					{
						LoginButton.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent));
					});
					return;
				}
				Application.Current.Dispatcher.Invoke(delegate
				{
					base.IsEnabled = true;
				});
				string text = $"Login Failed: {loggedOnCallback.Result}";
				if (loggedOnCallback.Result != loggedOnCallback.ExtendedResult)
				{
					text += $" {loggedOnCallback.ExtendedResult}";
				}
				ErrorMessage = text;
				return;
			}
		}
		else
		{
			if ((int)result == 63)
			{
				_steamGuardAuth = true;
				ErrorMessage = $"Login Failed: Account has steamguard enabled. Please enter the code from your e-mail address ({loggedOnCallback.EmailDomain})";
				Application.Current.Dispatcher.Invoke(delegate
				{
					base.IsEnabled = true;
					AuthTextBox.IsEnabled = true;
				});
				return;
			}
			if ((int)result == 85)
			{
				_twoFactorAuth = true;
				ErrorMessage = "Login Failed: Account has two factor authentication enabled: Enter the code from your authenticator";
				Application.Current.Dispatcher.Invoke(delegate
				{
					base.IsEnabled = true;
					AuthTextBox.IsEnabled = true;
				});
				return;
			}
		}
		Application.Current.Dispatcher.Invoke(delegate
		{
			base.IsEnabled = true;
		});
		string text2 = $"Login Failed: {loggedOnCallback.Result}";
		if (loggedOnCallback.Result != loggedOnCallback.ExtendedResult)
		{
			text2 += $" {loggedOnCallback.ExtendedResult}";
		}
		ErrorMessage = text2;
	}

	private void OnConnected(ConnectedCallback connectedCallback)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Invalid comparison between Unknown and I4
		if ((int)connectedCallback.Result != 1)
		{
			ErrorMessage = "Failed to connect to steam.";
		}
		else
		{
			SteamSession.Logon(Settings.Username, Settings.Password);
		}
	}

	private void CancelClick(object sender, RoutedEventArgs e)
	{
		Close();
	}

	private void LoginClick(object sender, RoutedEventArgs e)
	{
		if (string.IsNullOrEmpty(UsernameTextBox.Text) || string.IsNullOrEmpty(PasswordTextBox.Password))
		{
			MessageBox.Show("You must enter a username and password");
			return;
		}
		base.IsEnabled = false;
		if (_steamGuardAuth)
		{
			Settings.SteamGuardCode = AuthTextBox.Text;
		}
		if (_twoFactorAuth)
		{
			Settings.TwoFactorCode = AuthTextBox.Text;
		}
		Settings.Username = UsernameTextBox.Text;
		Settings.Password = PasswordTextBox.Password;
		SteamSession.Connect();
	}

	private void PasswordTextBox_OnPasswordChanged(object sender, RoutedEventArgs e)
	{
		if (PasswordTextBox.Password.Length == 0)
		{
			PasswordTextBox.Tag = "Password";
		}
		else
		{
			PasswordTextBox.Tag = "";
		}
	}
}
