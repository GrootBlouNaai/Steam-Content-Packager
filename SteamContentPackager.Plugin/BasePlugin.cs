using System;
using SteamContentPackager.Packing;

namespace SteamContentPackager.Plugin;

public abstract class BasePlugin : SubTask
{
	public abstract string Name { get; }

	public PluginArgs Args { get; set; }

	public new PackageTask ParentTask { get; }

	public event EventHandler<float> ProgressChanged;

	protected BasePlugin(PluginArgs args, PackageTask parentTask)
		: base(parentTask)
	{
		Args = args;
		ParentTask = parentTask;
		ParentTask.TaskCancelled += OnTaskCancelled;
		ParentTask.StateChanged += OnStateChanged;
	}

	protected abstract void OnStateChanged(object sender, TaskState taskState);

	protected abstract void OnTaskCancelled(object sender, EventArgs eventArgs);

	protected BasePlugin()
	{
	}

	public new abstract bool Run();

	protected void SetProgress(float progress)
	{
		this.ProgressChanged?.Invoke(this, progress);
	}
}
