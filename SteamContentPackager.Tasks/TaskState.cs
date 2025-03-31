namespace SteamContentPackager.Tasks;

public enum TaskState
{
	Idle,
	Running,
	Paused,
	Cancelled,
	Completed,
	Failed,
	WaitingForLogin
}
