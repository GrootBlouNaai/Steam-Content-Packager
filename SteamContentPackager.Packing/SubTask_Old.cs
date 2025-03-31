using System.Threading;
using System.Threading.Tasks;

namespace SteamContentPackager.Packing;

public abstract class SubTask_Old
{
	public class Result
	{
		public bool Success;
	}

	protected PackageTask_OLD ParentTaskOld;

	public TaskState State
	{
		get
		{
			return ParentTaskOld.State;
		}
		set
		{
			ParentTaskOld.State = value;
		}
	}

	public AppConfig AppConfig => ParentTaskOld.AppConfig;

	public CancellationTokenSource CancellationTokenSource => ParentTaskOld.CancellationTokenSource;

	public float Progress
	{
		set
		{
			ParentTaskOld.Progress = value;
		}
	}

	protected SubTask_Old(PackageTask_OLD parentTaskOld)
	{
		ParentTaskOld = parentTaskOld;
	}

	public abstract Task<Result> RunTask();
}
