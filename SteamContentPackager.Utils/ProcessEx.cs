using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace SteamContentPackager.Utils;

public class ProcessEx : Process
{
	[Flags]
	public enum ThreadAccess
	{
		TERMINATE = 1,
		SUSPEND_RESUME = 2,
		GET_CONTEXT = 8,
		SET_CONTEXT = 0x10,
		SET_INFORMATION = 0x20,
		QUERY_INFORMATION = 0x40,
		SET_THREAD_TOKEN = 0x80,
		IMPERSONATE = 0x100,
		DIRECT_IMPERSONATION = 0x200
	}

	[DllImport("kernel32.dll")]
	private static extern IntPtr OpenThread(ThreadAccess dwDesiredAccess, bool bInheritHandle, uint dwThreadId);

	[DllImport("kernel32.dll")]
	private static extern uint SuspendThread(IntPtr hThread);

	[DllImport("kernel32.dll")]
	private static extern int ResumeThread(IntPtr hThread);

	[DllImport("kernel32", CharSet = CharSet.Auto, SetLastError = true)]
	private static extern bool CloseHandle(IntPtr handle);

	public void SuspendProcess()
	{
		foreach (ProcessThread thread in base.Threads)
		{
			IntPtr intPtr = OpenThread(ThreadAccess.SUSPEND_RESUME, bInheritHandle: false, (uint)thread.Id);
			if (!(intPtr == IntPtr.Zero))
			{
				SuspendThread(intPtr);
				CloseHandle(intPtr);
			}
		}
	}

	public void ResumeProcess()
	{
		foreach (ProcessThread thread in base.Threads)
		{
			IntPtr intPtr = OpenThread(ThreadAccess.SUSPEND_RESUME, bInheritHandle: false, (uint)thread.Id);
			if (!(intPtr == IntPtr.Zero))
			{
				int num = 0;
				do
				{
					num = ResumeThread(intPtr);
				}
				while (num > 0);
				CloseHandle(intPtr);
			}
		}
	}
}
