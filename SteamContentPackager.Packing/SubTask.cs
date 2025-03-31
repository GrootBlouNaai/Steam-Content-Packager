using System;
using System.Threading.Tasks;
using SteamContentPackager.UI;

namespace SteamContentPackager.Packing;

public class SubTask : BindableBase
{
	public class Result
	{
		public bool Success { get; set; }
	}

	private string _info;

	protected PackageTask ParentTask;

	public string TestName { get; }

	public string Info
	{
		get
		{
			return _info;
		}
		set
		{
			_info = value;
			OnPropertyChanged("Info");
		}
	}

	protected SubTask(PackageTask parentTask)
	{
		ParentTask = parentTask;
		Info = $"{TestName} - Progress 0%";
	}

	public SubTask()
	{
	}

	public virtual Task<Result> Run()
	{
		throw new NotImplementedException();
	}
}
