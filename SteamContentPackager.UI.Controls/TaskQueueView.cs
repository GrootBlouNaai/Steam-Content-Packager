using System;
using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;
using SteamContentPackager.Annotations;
using SteamContentPackager.Packing;
using SteamContentPackager.UI.DragAndDrop;
using SteamContentPackager.UI.DragAndDrop.Utilities;

namespace SteamContentPackager.UI.Controls;

public partial class TaskQueueView : UserControl, INotifyPropertyChanged, IDropTarget, IDragSource, IComponentConnector
{
	private string _pauseResumeString = "Pause";

	public TaskQueue TaskQueue { get; } = new TaskQueue();

	public string PauseResumeString
	{
		get
		{
			return _pauseResumeString;
		}
		set
		{
			_pauseResumeString = value;
			OnPropertyChanged("PauseResumeString");
		}
	}

	public RelayCommand AddTaskCommand => new RelayCommand(AddTask);

	public event PropertyChangedEventHandler PropertyChanged;

	public TaskQueueView()
	{
		InitializeComponent();
	}

	private void AddTask(object o)
	{
		AppConfig config = (AppConfig)o;
		PackageTask item = new PackageTask(config);
		TaskQueue.Add(item);
	}

	public void AddTask(PackageTask taskOld)
	{
		TaskQueue.Add(taskOld);
	}

	private void ClearCompleted_Clicked(object sender, RoutedEventArgs e)
	{
		foreach (PackageTask item in TaskQueue.Tasks.ToList())
		{
			if (item.State == TaskState.Complete || item.State == TaskState.Failed)
			{
				TaskQueue.Tasks.Remove(item);
			}
		}
	}

	private void TogglePause_Clicked(object sender, RoutedEventArgs e)
	{
		if (TaskQueue.Paused)
		{
			TaskQueue.Resume();
			PauseResumeString = "Pause";
		}
		else
		{
			TaskQueue.Pause();
			PauseResumeString = "Resume";
		}
	}

	public new void DragOver(IDropInfo dropInfo)
	{
		PackageTask packageTask = dropInfo.Data as PackageTask;
		if (dropInfo.TargetItem is PackageTask packageTask2 && packageTask != null && packageTask2.State != TaskState.Complete && packageTask2.State != TaskState.Running && (TaskQueue.Tasks.IndexOf(packageTask2) != 0 || dropInfo.InsertPosition != RelativeInsertPosition.BeforeTargetItem))
		{
			dropInfo.DropTargetAdorner = DropTargetAdorners.Insert;
			dropInfo.Effects = DragDropEffects.Move;
		}
	}

	public new void Drop(IDropInfo dropInfo)
	{
		PackageTask packageTask = dropInfo.Data as PackageTask;
		PackageTask packageTask2 = dropInfo.TargetItem as PackageTask;
		int num = TaskQueue.Tasks.IndexOf(packageTask);
		int num2 = TaskQueue.Tasks.IndexOf(packageTask2);
		if (num == num2)
		{
			return;
		}
		if (num2 < num && dropInfo.InsertPosition == RelativeInsertPosition.AfterTargetItem)
		{
			num2++;
		}
		if (packageTask2.State == TaskState.Complete)
		{
			return;
		}
		try
		{
			TaskQueue.Tasks.Move(num, num2);
		}
		catch (Exception)
		{
			TaskQueue.Tasks.Remove(packageTask);
			AddTask(packageTask);
		}
	}

	public void StartDrag(IDragInfo dragInfo)
	{
		int num = dragInfo.SourceItems.Cast<object>().Count();
		if (num == 1)
		{
			dragInfo.Data = dragInfo.SourceItems.Cast<object>().First();
		}
		else if (num > 1)
		{
			dragInfo.Data = TypeUtilities.CreateDynamicallyTypedList(dragInfo.SourceItems);
		}
		dragInfo.Effects = ((dragInfo.Data != null) ? (DragDropEffects.Copy | DragDropEffects.Move) : DragDropEffects.None);
	}

	public bool CanStartDrag(IDragInfo dragInfo)
	{
		if (!(dragInfo.SourceItem is PackageTask packageTask))
		{
			return false;
		}
		if (packageTask.State == TaskState.Complete || packageTask.State == TaskState.Running)
		{
			return false;
		}
		return true;
	}

	public void Dropped(IDropInfo dropInfo)
	{
	}

	public void DragCancelled()
	{
	}

	public bool TryCatchOccurredException(Exception exception)
	{
		return false;
	}

	[NotifyPropertyChangedInvocator]
	protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
	{
		this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	internal Delegate _CreateDelegate(Type delegateType, string handler)
	{
		return Delegate.CreateDelegate(delegateType, this, handler);
	}
}
