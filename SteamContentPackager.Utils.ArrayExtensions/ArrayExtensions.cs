using System;

namespace SteamContentPackager.Utils.ArrayExtensions;

public static class ArrayExtensions
{
	public static void ForEach(this Array array, Action<Array, int[]> action)
	{
		if (array.LongLength != 0)
		{
			ArrayTraverse arrayTraverse = new ArrayTraverse(array);
			do
			{
				action(array, arrayTraverse.Position);
			}
			while (arrayTraverse.Step());
		}
	}
}
