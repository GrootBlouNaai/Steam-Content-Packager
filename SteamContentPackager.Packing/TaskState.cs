namespace SteamContentPackager.Packing;

public enum TaskState
{
	Idle,
	Running,
	Paused,
	Complete,
	Cancelled,
	Failed
}
