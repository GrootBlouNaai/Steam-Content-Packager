using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Threading;
using SteamContentPackager.UI;
using SteamContentPackager.Utils;

namespace SteamContentPackager.Packing;

public class TaskQueue : BindableBase
{
	public PackageTask CurrentTask;

	private bool _paused;

	public ObservableCollection<PackageTask> Tasks { get; } = new ObservableCollection<PackageTask>();

	public bool Paused
	{
		get
		{
			return _paused;
		}
		set
		{
			_paused = value;
			OnPropertyChanged("Paused");
		}
	}

	public void Pause()
	{
		Paused = true;
		if (CurrentTask != null)
		{
			CurrentTask.State = TaskState.Paused;
		}
	}

	public void Resume()
	{
		Paused = false;
		PackageTask currentTask = CurrentTask;
		if (currentTask != null && currentTask.State == TaskState.Paused)
		{
			CurrentTask.State = TaskState.Running;
		}
	}

	public void Add(PackageTask item)
	{
		item.TaskCancelled += OnTaskCancelled;
		for (int num = Tasks.Count - 1; num >= 0; num--)
		{
			if (Tasks[num].State != TaskState.Complete && Tasks[num].State != TaskState.Failed)
			{
				Tasks.Insert(num + 1, item);
				return;
			}
		}
		Tasks.Insert(0, item);
	}

	private void OnTaskCancelled(object sender, EventArgs eventArgs)
	{
		PackageTask packageTask = (PackageTask)sender;
		if (Tasks.IndexOf(packageTask) > -1)
		{
			RemoveTask(packageTask);
		}
	}

	public TaskQueue()
	{
		Thread thread = new Thread(ProcessQueue);
		thread.IsBackground = true;
		thread.Start();
	}

	private async void ProcessQueue()
	{
		while (true)
		{
			Thread.Sleep(2);
			if (Tasks.Count == 0)
			{
				continue;
			}
			if (Tasks[0].State != 0)
			{
				PackageTask[] array = Tasks.ToArray();
				foreach (PackageTask packageTask in array)
				{
					if (packageTask.State == TaskState.Idle)
					{
						MoveToStart(packageTask);
					}
				}
			}
			if (Tasks[0].State != 0)
			{
				continue;
			}
			while (Paused)
			{
				Thread.Sleep(10);
			}
			CurrentTask = Tasks[0];
			try
			{
				await CurrentTask.Run();
				if (CurrentTask.State == TaskState.Cancelled)
				{
					CurrentTask.Cleanup();
					Log.Write("Task Cancelled");
					RemoveTask(CurrentTask);
				}
				else
				{
					MoveToEnd(CurrentTask);
				}
			}
			catch (Exception ex)
			{
				Exception e = ex;
				Log.Write($"Task Failed: {e.Message}");
			}
			CurrentTask.CurrentSubTask = null;
			CurrentTask = null;
		}
	}

	public void RemoveTask(PackageTask task)
	{
		BeginInvoke(delegate
		{
			Tasks.Remove(task);
		});
	}

	public void MoveToEnd(PackageTask task)
	{
		BeginInvoke(delegate
		{
			Tasks.Move(0, Tasks.Count - 1);
		});
	}

	public void MoveToStart(PackageTask task)
	{
		BeginInvoke(delegate
		{
			Tasks.Move(Tasks.IndexOf(task), 0);
		});
	}

	private void BeginInvoke(Action action)
	{
		Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.Render, (Action)delegate
		{
			action();
		});
	}
}
