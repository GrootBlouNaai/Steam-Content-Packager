using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using SteamContentPackager.Annotations;

namespace SteamContentPackager.Tasks;

public abstract class TaskBase : INotifyPropertyChanged
{
	public delegate void ProgressChangedEvent(ulong value, ulong max, string status = null);

	public delegate void TaskCompletedEvent(TaskBase task);

	public ProgressChangedEvent ProgressChanged;

	public TaskCompletedEvent TaskCompleted;

	private ulong _progressMax = 100uL;

	private ulong _progressValue;

	private string _name;

	private string _status;

	private TaskState _state;

	public ulong ProgressMax
	{
		get
		{
			return _progressMax;
		}
		set
		{
			_progressMax = value;
			OnPropertyChanged("ProgressMax");
		}
	}

	public ulong ProgressValue
	{
		get
		{
			return _progressValue;
		}
		set
		{
			_progressValue = value;
			OnPropertyChanged("ProgressValue");
		}
	}

	public string Name
	{
		get
		{
			return _name;
		}
		set
		{
			_name = value;
			OnPropertyChanged("Name");
		}
	}

	public string Status
	{
		get
		{
			return _status;
		}
		set
		{
			_status = value;
			OnPropertyChanged("Status");
		}
	}

	public TaskState State
	{
		get
		{
			return _state;
		}
		set
		{
			_state = value;
			OnPropertyChanged("State");
		}
	}

	public event PropertyChangedEventHandler PropertyChanged;

	public abstract Task Run();

	public abstract void Pause();

	public abstract void Resume();

	public abstract void Cancel();

	[NotifyPropertyChangedInvocator]
	protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
	{
		this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
	}
}
