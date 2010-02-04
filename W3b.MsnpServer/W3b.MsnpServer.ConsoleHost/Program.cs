using System;
using System.IO;

using W3b.MsnpServer;

namespace W3b.MsnpServer.ConsoleHost {
	
	public static class Program {
		
		private static String             _dbPath;
		
		private static DispatchServer     _dispatchServer;
		private static NotificationServer _notificationServer;
		private static SwitchboardServer  _switchboardServer;
		
		public static void Main(String[] args) {
			
			WriteBanner();
			
			if( StartServers() ) {
				
				DoCommandLoop();
			}
			
			Console.WriteLine("Press any key to continue...");
			Console.ReadKey();
		}
		
		private static void WriteBanner() {
			
			Console.ForegroundColor = ConsoleColor.Magenta;
			Console.WriteLine( Banner );
			Console.ResetColor();
			
			Console.WriteLine();
			Console.WriteLine("A multi-version MSNP server, infrastructure, and proxy service.");
			Console.WriteLine("Copyright 2010 David Rees - http://www.w3bbo.com - Licensed under the GPL");
			Console.WriteLine();
			Console.WriteLine("To connect, modify your HOSTS file to point messenger.hotmail.com to {0}, then start MSN Messenger", "127.0.0.1"); // System.Net.Dns.GetHostEntry(System.Net.Dns.GetHostName()).AddressList[0] );
			
			Console.WriteLine();
			
			Console.ForegroundColor = ConsoleColor.Magenta;
			Console.WriteLine( Lyrics.GetRandomVerse() );
			Console.ResetColor();
			Console.WriteLine();
			
		}
		
		private static String Banner {
			get {
				if( Console.WindowWidth >= 85 ) {
					
					return // 85 col
@"  ____               __          ____                       ____                    
 /\  _`\  __        /\ \        /\  _`\                    /\  _`\                  
 \ \ \L\ \\_\    ___\ \ \/'\    \ \ \L\_\    __     ___    \ \ \L\ \   ___   __  _  
  \ \ ,__//\ \ /' _ `\ \ , <     \ \  _\L  /'_ `\  / __`\   \ \  _ <' / __`\/\ \/'\ 
   \ \ \/ \ \ \/\ \/\ \ \ \\`\    \ \ \L\ \\ \L\ \/\ \L\ \   \ \ \L\ \\ \L\ \/>  </ 
    \ \_\  \ \_\ \_\ \_\ \_\ \_\   \ \____/ \____ \ \____/    \ \____/ \____//\_/\_\
     \/_/   \/_/\/_/\/_/\/_/\/_/    \/___/ \/___L\ \/___/      \/___/ \/___/ \//\/_/
                                            /\____/                                
                                            \/___/                                 ";
					
				} else if( Console.WindowWidth >= 67 ) {
					
					return // 67 col
@" ______ _       _        _______               ______             
(_____ (_)     | |      (_______)             (____  \            
 _____) ) ____ | |  _    _____   ____  ___     ____)  ) ___ _   _ 
|  ____/ |  _ \| |_/ )  |  ___) / _  |/ _ \   |  __  ( / _ ( \ / )
| |    | | | | |  _ (   | |____( (_| | |_| |  | |__)  ) |_| ) X ( 
|_|    |_|_| |_|_| \_)  |_______)___ |\___/   |______/ \___(_/ \_)
                               (_____|                            ";
					
				} else {
					
					return // 47 col
@" ___ _      _     ___            ___          
| _ (_)_ _ | |__ | __|__ _ ___  | _ ) _____ __
|  _/ | ' \| / / | _|/ _` / _ \ | _ \/ _ \ \ /
|_| |_|_||_|_\_\ |___\__, \___/ |___/\___/_\_\
                     |___/                    ";
					
				}
			}
		}
		
		private static bool StartServers() {
			
			Console.Write("Loading User Database...");
			
			//String path = @"C:\Documents and Settings\David\My Documents\Visual Studio Projects\W3b.MsnpServer\Data\PinkEgoBox.sqlite";
			//String path = @"D:\Users\David\My Documents\Visual Studio Projects\Solutions\W3b.MsnpServer\Data\PinkEgoBox.sqlite";
			_dbPath = UtilityMethods.FindFile( new DirectoryInfo( Environment.CurrentDirectory ), "PinkEgoBox.sqlite", 5 );
			if( _dbPath == null ) {
				Console.WriteLine();
				Console.ForegroundColor = ConsoleColor.Red;
				Console.WriteLine("Unable to locate database file");
				Console.ResetColor();
				return false;
			}
			
			User.LoadDatabase( _dbPath );
			
			Console.WriteLine("Done");
			
			Console.WriteLine("Starting Servers...");
			
			_dispatchServer = DispatchServer.Instance;
			_dispatchServer.Start();
			
			Console.ForegroundColor = _dispatchServer.ConsoleColor;
			Console.WriteLine("Dispatch Server started, listening on " + _dispatchServer.EndPoint.ToString2() );
			
			//////////////////////
			
			_notificationServer = NotificationServer.Instance;
			_notificationServer.Start();
			
			Console.ForegroundColor = _notificationServer.ConsoleColor;
			Console.WriteLine("Notification Server started, listening on " + _notificationServer.EndPoint.ToString2() );
			
			//////////////////////
			
			_switchboardServer = SwitchboardServer.Instance;
			_switchboardServer.Start();
			
			Console.ForegroundColor = _switchboardServer.ConsoleColor;
			Console.WriteLine("Switchboard Server started, listening on " + _switchboardServer.EndPoint.ToString2() );
			Console.ResetColor();
			
			return true;
		}
		
		private static void StopServers() {
			
			Console.WriteLine("Stopping servers...");
			
			_dispatchServer    .Stop();
			_notificationServer.Stop();
			_switchboardServer .Stop();
			
			Console.WriteLine("Save database? Y / D %filename% / N");
			String line = Console.ReadLine();
			if( line.ToUpperInvariant() == "Y" ) {
				
				Console.WriteLine("Saving Database...");
				
				User.SaveDatabase( _dbPath );
				
			} else if( line.StartsWith("D", StringComparison.OrdinalIgnoreCase) ) {
				
				String path = Path.Combine( Path.GetDirectoryName( _dbPath ), line.Substring(2).Trim() );
				
				Console.WriteLine("Saving Database to \"{0}\"...", path);
				
				User.SaveDatabase( path );
				
			} else {
				
				Console.WriteLine("Not saving changes");
				
			}
			
			Console.WriteLine("Terminating");
		}
		
		private static void DoCommandLoop() {
			
			WriteCommands();
			
			while(true) {
				String line;
				switch( line = Console.ReadLine().ToUpperInvariant() ) {
					case "M":
						ClearMsmgsEvent();
						break;
					case "H":
						HostsFile.AddHostsFileEntry("messenger.hotmail.com", "127.0.0.1");
						break;
					case "R":
						HostsFile.RemoveHostsFileEntry("messenger.hotmail.com");
						break;
					case "Q":
						StopServers();
						return;
					case "Z":
						return;
					case "C":
						Console.Clear();
						break;
					case "?":
					case "HELP":
					case "h":
						WriteCommands();
						break;
					default:
						Console.WriteLine("Unrecognised Command: " + line );
						break;
				}
			}
			
		}
		
		private static void ClearMsmgsEvent() {
			
			Console.WriteLine("Not yet implemented. Use Sysinternals Process Explorer to close the 'MSMSGS' event instead.");
			
//			Utility.ClearMsmsgsEvent();
//			
//			if( Utility.ClearMsmsgsEvent() ) {
//				
//				Console.WriteLine("MSMSGS Event Cleared");
//				
//			} else {
//				
//				Console.WriteLine("MSMSGS Event does not exist");
//			}
			
		}
		
		private static void WriteCommands() {
			
			Console.WriteLine("Commands:");
			Console.WriteLine("\tm - Close MSMSGS event (so you can open multiple copies of msmsgs.exe)");
			Console.WriteLine("\th - Add messenger.hotmail.com entry to hosts file");
			Console.WriteLine("\tr - Remove messenger.hotmail.com entry from hosts file");
			Console.WriteLine("\tq - Quit (Stop servers first)");
			Console.WriteLine("\tz - Quit (Stop immediately)");
			Console.WriteLine("\tc - Clear history");
			Console.WriteLine("\t? - Show this message");
			
		}
		
	}
	
}
