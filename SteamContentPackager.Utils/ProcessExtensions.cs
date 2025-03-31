using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace SteamContentPackager.Utils;

public static class ProcessExtensions
{
	public static Task WaitForExitAsync(this Process process, CancellationToken cancellationToken = default(CancellationToken))
	{
		TaskCompletionSource<object> tcs = new TaskCompletionSource<object>();
		process.EnableRaisingEvents = true;
		process.Exited += delegate
		{
			tcs.TrySetResult(null);
		};
		if (cancellationToken != default(CancellationToken))
		{
			cancellationToken.Register(tcs.SetCanceled);
		}
		return tcs.Task;
	}
}
