using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Markup;
using SteamContentPackager.Annotations;
using SteamContentPackager.Steam;
using SteamKit2;

namespace SteamContentPackager.UI.Controls;

public partial class LoginPanel : UserControl, INotifyPropertyChanged, IComponentConnector
{
	private Visibility _authBoxVisibility = Visibility.Collapsed;

	private Visibility _errorMessageVisibility = Visibility.Collapsed;

	private bool _connected;

	private string _username;

	private string _authCode;

	private string _errorMessage;

	private bool _controlsEnabled = true;

	private bool _useTwoFactorAuth;

	private bool _useSteamGuard;

	public UserData.User User;

	public bool Connected
	{
		get
		{
			return _connected;
		}
		set
		{
			_connected = value;
			OnPropertyChanged("Connected");
		}
	}

	public Visibility AuthBoxVisibility
	{
		get
		{
			return _authBoxVisibility;
		}
		set
		{
			_authBoxVisibility = value;
			OnPropertyChanged("AuthBoxVisibility");
		}
	}

	public Visibility ErrorMessageVisibility
	{
		get
		{
			return _errorMessageVisibility;
		}
		set
		{
			_errorMessageVisibility = value;
			OnPropertyChanged("ErrorMessageVisibility");
		}
	}

	public string Username
	{
		get
		{
			return _username;
		}
		set
		{
			_username = value;
			OnPropertyChanged("Username");
			TryLoadUserData(_username);
		}
	}

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
			ErrorMessageVisibility = (string.IsNullOrEmpty(_errorMessage) ? Visibility.Collapsed : Visibility.Visible);
		}
	}

	public string AuthCode
	{
		get
		{
			return _authCode;
		}
		set
		{
			_authCode = value;
			OnPropertyChanged("AuthCode");
		}
	}

	public bool ControlsEnabled
	{
		get
		{
			return _controlsEnabled;
		}
		set
		{
			_controlsEnabled = value;
			OnPropertyChanged("ControlsEnabled");
		}
	}

	public RelayCommand LoginCommand => new RelayCommand(Login, CanLogin);

	public event EventHandler LoggedIn;

	public event PropertyChangedEventHandler PropertyChanged;

	public LoginPanel()
	{
		if (DesignerProperties.GetIsInDesignMode(this))
		{
			base.Visibility = Visibility.Collapsed;
			InitializeComponent();
			return;
		}
		InitializeComponent();
		SteamSession.CallbackManager.Subscribe<SteamUser.LoggedOnCallback>(OnLoggedOnCallbackReceived);
		SteamSession.CallbackManager.Subscribe<SteamClient.ConnectedCallback>(OnConnected);
		SteamSession.CallbackManager.Subscribe<SteamClient.DisconnectedCallback>(OnDisconnected);
		LoggedIn += delegate
		{
			Application.Current.Dispatcher.Invoke(delegate
			{
				base.Visibility = Visibility.Collapsed;
			});
		};
		SteamSession.Connect();
	}

	private void TryLoadUserData(string username)
	{
		UserData.User user = UserData.Instance.users.Find((UserData.User x) => x.Username.ToLower() == username.ToLower());
		if (user == null)
		{
			return;
		}
		if (!string.IsNullOrEmpty(user.LoginKey))
		{
			Application.Current.Dispatcher.Invoke(() => PassBox.Password = user.LoginKey);
			User = user;
		}
		Application.Current.Dispatcher.Invoke(delegate
		{
			LoginButton.Focus();
		});
	}

	private bool CanLogin(object o)
	{
		if (!Connected || string.IsNullOrEmpty(Username) || string.IsNullOrEmpty(PassBox.Password))
		{
			return false;
		}
		if ((_useSteamGuard || _useTwoFactorAuth) && string.IsNullOrEmpty(AuthCode))
		{
			return false;
		}
		return true;
	}

	private void Login(object o)
	{
		User = ((o == null) ? UserData.GetUser(Username) : ((UserData.User)o));
		if (o == null)
		{
			User.AuthCode = (_useSteamGuard ? AuthCode : null);
		}
		User.TwoFactorCode = (_useTwoFactorAuth ? AuthCode : null);
		if (PassBox.Password != User.LoginKey)
		{
			User.Password = PassBox.Password;
		}
		UserData.Save();
		SteamSession.Logon(User);
		ControlsEnabled = false;
	}

	private void OnLoggedOnCallbackReceived(SteamUser.LoggedOnCallback callback)
	{
		switch (callback.Result)
		{
		case EResult.AccountLogonDenied:
			ErrorMessage = "SteamGuard Code Required";
			_useSteamGuard = true;
			AuthBoxVisibility = Visibility.Visible;
			break;
		case EResult.AccountLoginDeniedNeedTwoFactor:
			ErrorMessage = "Two Factor Code Required";
			_useTwoFactorAuth = true;
			AuthBoxVisibility = Visibility.Visible;
			break;
		case EResult.TwoFactorCodeMismatch:
			ErrorMessage = "Two Factor Code Invalid";
			_useTwoFactorAuth = true;
			AuthBoxVisibility = Visibility.Visible;
			break;
		case EResult.OK:
			Config.LastUser = Username;
			Config.Save();
			this.LoggedIn?.Invoke(this, null);
			return;
		case EResult.PasswordRequiredToKickSession:
			ErrorMessage = "Password Required";
			User.Password = "";
			break;
		case EResult.InvalidPassword:
			ErrorMessage = ((SteamSession.User.LoginKey != null) ? "Login Key Expired. Password Required" : "Invalid Password");
			if (SteamSession.User.LoginKey != null)
			{
				ErrorMessage = "LoginKey Expired. Password Required";
				User.LoginKey = null;
				UserData.Save();
			}
			else
			{
				ErrorMessage = "Invalid Password";
				User.Password = "";
			}
			Application.Current.Dispatcher.Invoke(delegate
			{
				PassBox.Password = "";
			});
			break;
		default:
			ErrorMessage = $"LogonError: {callback.Result}";
			SteamSession.Disconnect();
			return;
		}
		SteamSession.Connect();
		ControlsEnabled = true;
	}

	private void OnDisconnected(SteamClient.DisconnectedCallback disconnectedCallback)
	{
		Connected = false;
	}

	private void OnConnected(SteamClient.ConnectedCallback connectedCallback)
	{
		Connected = true;
		if (!string.IsNullOrEmpty(Config.LastUser) && User == null)
		{
			User = UserData.GetUser(Config.LastUser);
			Username = User.Username;
			if (!string.IsNullOrEmpty(User.LoginKey))
			{
				Application.Current.Dispatcher.Invoke(() => PassBox.Password = User.LoginKey);
				Login(User);
			}
		}
		Application.Current.Dispatcher.Invoke(delegate
		{
			UsernameTextBox.Focus();
		});
	}

	private void Cancel_Clicked(object sender, RoutedEventArgs e)
	{
		SteamSession.Disconnect();
		Config.LastUser = null;
		Config.Save();
		Environment.Exit(0);
	}

	[NotifyPropertyChangedInvocator]
	protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
	{
		this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
	}

	private void PassBox_OnKeyDown(object sender, KeyEventArgs e)
	{
		if (e.Key == Key.Return)
		{
			Application.Current.Dispatcher.Invoke(delegate
			{
				LoginButton.Focus();
				LoginButton.Command.Execute(null);
			});
		}
	}
}
