using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Collections.Generic;

namespace W3b.MsnpServer.ConsoleHost {
	
	
#region Old Attempt
#if NEVER
	
	public static class Utility {
		
		// mmmmm, subversive behaviour!
		
		public static bool ClearMsmsgsEvent() {
			
			// the only way to clear the event is to close all handles to it
			// Messenger is using MSMSGS as a mutex rather than a signaled event (or if the event does have significance, I don't know it)
			
			
			
			IntPtr messengerEvent = NativeMethods.OpenEvent("MSMSGS");
			if( messengerEvent == IntPtr.Zero ) {
				
				
				return false;
				
			} else {
				
				//NativeMethods.ResetEvent( messengerEvent );
				NativeMethods.SetEvent( messengerEvent );
				
				while( NativeMethods.CloseHandle( messengerEvent ) ) {
				}
				
				return true;
			}
			
		}
		
		public enum ClearMsmsgsResult {
			Success,
			EventDoesntExist,
			
		}
		
	}
	
	internal static class NativeMethods {
		
		public static IntPtr OpenEvent(String name) {
			
			return OpenEvent(SyncObjectAccess.EventAllAccess | SyncObjectAccess.EventModifyState, false, name);
		}
		
		[DllImport("Kernel32.dll", SetLastError=true)]
		public static extern IntPtr OpenEvent(SyncObjectAccess desiredAccess, Boolean inheritHandle, String name);
		
		/// <summary>Sets an event to 'unsignaled'</summary>
		[DllImport("kernel32.dll", SetLastError=true)]
		public static extern bool ResetEvent(IntPtr hEvent);
		
		/// <summary>Sets an event to 'signaled'</summary>
		[DllImport("kernel32.dll", SetLastError=true)]
		public static extern bool SetEvent(IntPtr hEvent);
		
		[DllImport("Kernel32.dll", SetLastError=true)]
		public static extern bool CloseHandle(IntPtr handle);
	}
#endif
#endregion
	
#region Subversive CreateRemoteThread
	
	public static class Utility {
		
		public static void ClearMsmsgsEvent() {
			
			return;
			
			// find all MSMSGS.exe instances
			// inject my code into them, my code finds and closes the handle to the 'MSMSGS' event
			// then it should be all done
			
			NativeMethods.ThreadProc proc = new NativeMethods.ThreadProc(  CloseMsmsgsEvent );
			IntPtr threadProc = Marshal.GetFunctionPointerForDelegate(proc);
			
			Process[] processes = Process.GetProcessesByName("msmsgs"); // the name does not include the extension
			foreach(Process p in processes) {
				
				uint threadId;
				
				IntPtr hProcess = NativeMethods.OpenProcess( NativeMethods.PROCESS_ALL_ACCESS, false, (uint)p.Id );
				
				IntPtr thread = NativeMethods.CreateRemoteThread( hProcess, IntPtr.Zero, 0, threadProc, IntPtr.Zero, 0, out threadId);
				
				WaitForThreadToExit( thread );
			}
			
		}
		
		/// <summary>Helper to wait for a thread to exit and print its exit code</summary>
		private static void WaitForThreadToExit(IntPtr hThread) {
			
			NativeMethods.WaitForSingleObject(hThread, unchecked((uint)-1));
			
			uint exitCode;
			NativeMethods.GetExitCodeThread(hThread, out exitCode);
			
			int pid = Process.GetCurrentProcess().Id;
			Console.WriteLine("Pid {0}: Thread exited with code: {1}", pid, exitCode);
		}
		
		private static int CloseMsmsgsEvent(IntPtr arg) {
			
			// this code will execute within the MSMSGS.exe process instances
			
			IntPtr hEvent = NativeMethods.OpenEvent("MSMSGS");
			if( hEvent == IntPtr.Zero ) {
				// it's already been closed
				return 1;
			}
			
			// hopefully, it should be this simple
			NativeMethods.CloseHandle( hEvent );
			
			return 0;
		}
		
	}
	
	internal static class NativeMethods {
		
		////////////////////////////////////////
		// Thread Injection
		
		// Some source from: http://blogs.msdn.com/jmstall/articles/sample_create_remote_thread.aspx
		
		// Thread proc, to be used with Create*Thread
		public delegate int ThreadProc(IntPtr param);
		
		/// <summary>Friendly version, marshals thread-proc as friendly delegate</summary>
		/// <param name="lpStartAddress">ThreadProc as friendly delegate</param>
		/// <returns></returns>
		[DllImport("kernel32")]
		public static extern IntPtr CreateThread(IntPtr lpThreadAttributes, uint dwStackSize, ThreadProc lpStartAddress, IntPtr lpParameter, uint dwCreationFlags, out uint dwThreadId);
		
		/// <summary>Marshal with ThreadProc's function pointer as a raw IntPtr.</summary>
		/// <param name="lpStartAddress">ThreadProc as raw IntPtr</param>
		[DllImport("kernel32", EntryPoint="CreateThread")]
		public static extern IntPtr CreateThreadRaw(IntPtr lpThreadAttributes, uint dwStackSize, IntPtr lpStartAddress, IntPtr lpParameter, uint dwCreationFlags, out uint dwThreadId);
		
		/// <summary>CreateRemoteThread, since ThreadProc is in remote process, we must use a raw function-pointer.</summary>
		/// <param name="lpStartAddress">raw Pointer into remote process</param>
		[DllImport("kernel32")]
		public static extern IntPtr CreateRemoteThread(IntPtr hProcess, IntPtr lpThreadAttributes, uint dwStackSize, IntPtr lpStartAddress, IntPtr lpParameter, uint dwCreationFlags, out uint lpThreadId);
		
		[DllImport("kernel32")]
		public static extern IntPtr GetCurrentProcess();
		
		public const uint PROCESS_ALL_ACCESS = 0x000F0000 | 0x00100000 | 0xFFF;
		
		[DllImport("kernel32")]
		public static extern IntPtr OpenProcess(uint dwDesiredAccess, bool bInheritHandle, uint dwProcessId);
		
		[DllImport("kernel32")]
		[return: MarshalAs(UnmanagedType.Bool)]
		public static extern bool CloseHandle(IntPtr hObject);
		
		[DllImport("kernel32")]
		public static extern uint WaitForSingleObject(IntPtr hHandle, uint dwMilliseconds);
		
		[DllImport("kernel32")]
		[return: MarshalAs(UnmanagedType.Bool)]
		public static extern bool GetExitCodeThread(IntPtr hThread, out uint lpExitCode);
		
		////////////////////////////////////////
		// Events
		
		public static IntPtr OpenEvent(String name) {
			
			return OpenEvent(SyncObjectAccess.EventAllAccess | SyncObjectAccess.EventModifyState, false, name);
		}
		
		[DllImport("Kernel32.dll", SetLastError=true)]
		public static extern IntPtr OpenEvent(SyncObjectAccess desiredAccess, Boolean inheritHandle, String name);
		
	}
	
	/// <summary>Security enumeration from: http://msdn.microsoft.com/library/default.asp?url=/library/en-us/dllproc/base/synchronization_object_security_and_access_rights.asp</summary>
	[Flags]
	public enum SyncObjectAccess : uint {
		Delete               = 0x00010000,
		ReadControl          = 0x00020000,
		WriteDacl            = 0x00040000,
		WriteOwner           = 0x00080000,
		Synchronize          = 0x00100000,
		EventAllAccess       = 0x001F0003,
		EventModifyState     = 0x00000002,
		MutexAllAccess       = 0x001F0001,
		MutexModifyState     = 0x00000001,
		SemaphoreAllAccess   = 0x001F0003,
		SemoaphorModifyState = 0x00000002,
		TimerAllAccess       = 0x001F0003,
		TimerModifyState     = 0x00000002,
		TimerQueryState      = 0x00000001
	}
	
#endregion
	
	
}
