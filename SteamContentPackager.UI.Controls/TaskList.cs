using System;
using System.CodeDom.Compiler;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;
using System.Windows.Media;
using GongSolutions.Wpf.DragDrop;
using SteamContentPackager.Annotations;
using SteamContentPackager.Tasks;
using SteamContentPackager.Utils;

namespace SteamContentPackager.UI.Controls;

public partial class TaskList : UserControl, INotifyPropertyChanged, IDropTarget, IComponentConnector, IStyleConnector
{
	private ObservableCollection<PackageTask> _tasks;

	public bool Running;

	public bool Paused;

	private bool _slimTasks;

	public ObservableCollection<PackageTask> Tasks
	{
		get
		{
			return _tasks;
		}
		set
		{
			_tasks = value;
			OnPropertyChanged("Tasks");
		}
	}

	public bool SlimTasks
	{
		get
		{
			return _slimTasks;
		}
		set
		{
			_slimTasks = value;
			OnPropertyChanged("SlimTasks");
		}
	}

	public event PropertyChangedEventHandler PropertyChanged;

	public void Start()
	{
		if (!Running && !Paused)
		{
			Running = true;
			Thread thread = new Thread(TaskLoop);
			thread.IsBackground = true;
			thread.SetApartmentState(ApartmentState.STA);
			thread.Start();
		}
	}

	public void Pause()
	{
		if (Running)
		{
			if (Tasks.Count > 0 && Tasks[0].State == TaskState.Running)
			{
				Tasks[0].Pause();
			}
			Paused = true;
		}
	}

	public void Resume()
	{
		if (!Running)
		{
			Start();
			return;
		}
		if (Tasks.Count > 0 && Tasks[0].State == TaskState.Paused)
		{
			Tasks[0].Resume();
		}
		Paused = false;
	}

	private void TaskLoop()
	{
		while (true)
		{
			Thread.Sleep(500);
			if (Tasks.Count != 0 && Tasks[0].State != TaskState.WaitingForLogin)
			{
				if (Tasks[0].State == TaskState.Idle && !Paused)
				{
					Thread thread = new Thread(RunTask);
					thread.SetApartmentState(ApartmentState.STA);
					thread.Start();
				}
				else if (Tasks[0].State == TaskState.Paused && !Paused)
				{
					Tasks[0].Resume();
				}
			}
		}
	}

	public void AddTask(PackageTask task)
	{
		task.Name = $"Task {Tasks.Count}";
		if (Tasks.Count > 0 && Tasks[0].State == TaskState.Completed)
		{
			Tasks.Add(task);
			Tasks.Move(Tasks.IndexOf(task), 0);
			return;
		}
		for (int num = Tasks.Count - 1; num >= 0; num--)
		{
			if (Tasks[num].State != TaskState.Completed)
			{
				Tasks.Insert(num + 1, task);
				return;
			}
		}
		for (int i = 0; i < Tasks.Count; i++)
		{
			if (Tasks[i].State != TaskState.Running)
			{
				Tasks.Insert(i - 1, task);
				return;
			}
		}
		Tasks.Add(task);
	}

	private async void RunTask()
	{
		PackageTask packageTask = Tasks[0];
		packageTask.TaskCompleted = (TaskBase.TaskCompletedEvent)Delegate.Combine(packageTask.TaskCompleted, new TaskBase.TaskCompletedEvent(TaskCompleted));
		await Tasks[0].Run();
	}

	private void TaskCompleted(TaskBase t)
	{
		Application.Current.Dispatcher.Invoke(delegate
		{
			if (Tasks[0].State != TaskState.Cancelled)
			{
				PackageTask packageTask = Tasks[0];
				Tasks.Move(0, Tasks.Count - 1);
				OnPropertyChanged("Tasks");
				t.ProgressValue = t.ProgressMax;
				Logger.WriteEntry($"Task Completed ({packageTask.SteamApp.Name})");
			}
		});
	}

	public new void DragOver(IDropInfo dropInfo)
	{
		//IL_0015: Unknown result type (might be due to invalid IL or missing references)
		//IL_001b: Invalid comparison between Unknown and I4
		if (dropInfo.InsertIndex != dropInfo.DragInfo.SourceIndex && ((int)dropInfo.InsertPosition != 2 || dropInfo.DragInfo.SourceIndex != Tasks.Count - 1))
		{
			PackageTask packageTask = dropInfo.Data as PackageTask;
			PackageTask packageTask2 = dropInfo.TargetItem as PackageTask;
			if (packageTask != null && packageTask2 != null && packageTask.State != TaskState.Completed && packageTask.State != TaskState.Running && packageTask2.State != TaskState.Completed && packageTask2.State != TaskState.Running)
			{
				dropInfo.DropTargetAdorner = DropTargetAdorners.Insert;
				dropInfo.Effects = DragDropEffects.Copy;
			}
		}
	}

	public new void Drop(IDropInfo dropInfo)
	{
		PackageTask packageTask = dropInfo.Data as PackageTask;
		PackageTask packageTask2 = dropInfo.TargetItem as PackageTask;
		if (packageTask != null && packageTask2 != null && packageTask.State != TaskState.Completed && packageTask.State != TaskState.Running && packageTask2.State != TaskState.Completed && packageTask2.State != TaskState.Running)
		{
			Tasks.Move(Tasks.IndexOf(packageTask), Tasks.IndexOf(packageTask2));
			OnPropertyChanged("Tasks");
		}
	}

	public void Sort()
	{
		OnPropertyChanged("Tasks");
	}

	public TaskList()
	{
		_tasks = new ObservableCollection<PackageTask>();
		SlimTasks = Settings.SlimTasks;
		InitializeComponent();
	}

	[NotifyPropertyChangedInvocator]
	protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
	{
		this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
	}

	private void TaskCancelButtonClicked(object sender, RoutedEventArgs e)
	{
		PackageTask packageTask = (PackageTask)FindParent<ListBoxItem>((Label)e.Source).DataContext;
		packageTask.Cancel();
		Tasks.Remove(packageTask);
		OnPropertyChanged("Tasks");
	}

	private static T FindParent<T>(DependencyObject child) where T : DependencyObject
	{
		DependencyObject parent = VisualTreeHelper.GetParent(child);
		if (parent == null)
		{
			return null;
		}
		if (parent is T result)
		{
			return result;
		}
		return FindParent<T>(parent);
	}

	private void QueueStateToggleClicked(object sender, RoutedEventArgs e)
	{
		if (!Running)
		{
			Start();
			QueueToggle.Content = "PAUSE QUEUE";
		}
		else if (!Paused)
		{
			Pause();
			QueueToggle.Content = "RESUME QUEUE";
		}
		else
		{
			Resume();
			QueueToggle.Content = "PAUSE QUEUE";
		}
	}

	private void ClearCompletedClicked(object sender, RoutedEventArgs e)
	{
		if (Tasks.Count == 0)
		{
			return;
		}
		while (Tasks[Tasks.Count - 1].State == TaskState.Completed)
		{
			Tasks.RemoveAt(Tasks.Count - 1);
			if (Tasks.Count == 0)
			{
				break;
			}
		}
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	internal Delegate _CreateDelegate(Type delegateType, string handler)
	{
		return Delegate.CreateDelegate(delegateType, this, handler);
	}
}
