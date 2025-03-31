using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SteamContentPackager.Utils;

public static class EnumerableExtensions
{
	public static IEnumerable<IEnumerable<T>> Chunk<T>(this IEnumerable<T> source, int chunksize)
	{
		while (source.Any())
		{
			yield return source.Take(chunksize);
			source = source.Skip(chunksize);
		}
	}

	public static Task ParallelForEach<T>(this IEnumerable<T> source, Func<T, Task> action, int maxTasks = 1)
	{
		List<Task> list = new List<Task>();
		SemaphoreSlim semaphoreSlim = new SemaphoreSlim(maxTasks);
		foreach (T item in source)
		{
			list.Add(Task.Run(async delegate
			{
				await semaphoreSlim.WaitAsync();
				await action(item);
				semaphoreSlim.Release();
			}));
		}
		return Task.WhenAll(list.ToArray());
	}
}
