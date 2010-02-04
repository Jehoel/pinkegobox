using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

namespace W3b.MsnpServer.ConsoleHost {
	
	public class HostsFile {
		
		public static void AddHostsFileEntry(String hostName, String ipAddress) {
			
			String path = Environment.GetFolderPath(Environment.SpecialFolder.System);
			path = Path.Combine( path, @"drivers\etc\hosts" );
			
			HostsFile file = new HostsFile( path );
			
			HostsFile.HostEntry e = new HostsFile.HostEntry() {
				IPAddress = ipAddress,
				HostName = hostName,
				Comment = "Added by PinkEgoBox"
			};
			
			file.Elements.Add( e );
			
			file.Save( path );
			
		}
		
		public static void RemoveHostsFileEntry(String hostName) {
			
			String path = Environment.GetFolderPath(Environment.SpecialFolder.System);
			path = Path.Combine( path, @"drivers\etc\hosts" );
			
			HostsFile file = new HostsFile( path );
			
			HostsFile.HostEntry entry = file.HostEntries.Find( e => e.HostName.Equals( hostName, StringComparison.OrdinalIgnoreCase ) );
			if( entry != null ) file.Elements.Remove( entry );
			
			file.Save( path );
			
		}
		
		// unlike HostFile_Old, this uses a simpler FSM parser. It's less robust, but good enough
		
		// assuming a verrrry simple format for hosts files:
		// 
		// file    : { [ <comment> ] | [ host ] }*
		// comment : <hash> <anything> <nl>
		// hash    : "#"
		// host    : <ipAddress> <whitespace> <hostName> [ <comment> ] <nl>
		// nl      : "\r\n"
		
		public HostsFile(String fileName) {
			
			Load( fileName );
		}
		
		public List<Element> Elements {
			get; private set;
		}
		
		public IEnumerable<HostEntry> HostEntries {
			get {
				foreach(Element e in Elements) {
					HostEntry he = e as HostEntry;
					if( he != null ) yield return he;
				}
			}
		}
		
		public abstract class Element {
			public abstract override String ToString();
		}
		
		public class Comment : Element {
			
			public String Text { get; set; }
			
			public override String ToString() {
				return "# " + Text;
			}
		}
		
		public class HostEntry : Element {
			public String IPAddress { get; set; }
			public String HostName  { get; set; }
			public String Comment   { get; set; }
			
			public override String ToString() {
				return IPAddress.PadRight( 16 ) + HostName + ( String.IsNullOrEmpty( Comment ) ? String.Empty : " # " + Comment );
				// 16 = 15 + 1 (extra space betwee IPAddr and HostName)
				// 15 = 255.255.255.255 (4*3 + 3)
			}
		}
		
		private void Load(String fileName) {
			
			Elements = new List<Element>();
			
			using(StreamReader rdr = new StreamReader(fileName)) {
				
				RdrState state = RdrState.InNLWhitespace;
				StringBuilder sb = new StringBuilder();
				
				Element current = null;
				
				int nc; char c;
				while( (nc = rdr.Read()) != -1 ) {
					c = (char)nc;
					
					switch(c) {
						case '#':
							
							switch(state) {
								case RdrState.InNLWhitespace:
									
									state = RdrState.InNLComment;
									
									if( current != null ) Elements.Add( current );
									current = new Comment();
									
									break;
								case RdrState.InHostComment:
								case RdrState.InNLComment:
									sb.Append( c );
									break;
								case RdrState.InHostIPAddress:
								case RdrState.InHostWS1:
									throw new Exception("Unexpected comment");
								case RdrState.InHostName:
								case RdrState.InHostWS2:
									
									(current as HostEntry).Comment = rdr.ReadLine();
//									state = RdrState.InHostComment;
									state = RdrState.InNLWhitespace; // because .ReadLine() causes it to jump to the start of the next line, skipping the '\r\n' characters
									
									Elements.Add( current );
									current = null;
									
									break;
							}
							
							break;
						
						case '\r':
						case '\n':
							
							switch(state) {
								case RdrState.InHostComment:
									
									(current as HostEntry).Comment = sb.ToString();
									sb.Length = 0;
									
									break;
								case RdrState.InNLComment:
									
									(current as Comment).Text = sb.ToString();
									sb.Length = 0;
									
									break;
								case RdrState.InNLWhitespace:
									// NOOP, state changes anyway
									break;
								case RdrState.InHostIPAddress:
								case RdrState.InHostWS1:
									throw new Exception("Unexpected newline");
								case RdrState.InHostWS2:
									
									break;
								case RdrState.InHostName:
									
									(current as HostEntry).HostName = sb.ToString();
									sb.Length = 0;
									
									break;
							}
							
							if( current != null ) Elements.Add( current );
							current = null;
							
							state = RdrState.InNLWhitespace;
							break;
						
						case ' ':
						case '\t':
							
							switch(state) {
								case RdrState.InHostComment:
								case RdrState.InNLComment:
									sb.Append( c );
									break;
								case RdrState.InHostIPAddress:
									state = RdrState.InHostWS1;
									
									(current as HostEntry).IPAddress = sb.ToString();
									sb.Length = 0;
									
									break;
								case RdrState.InNLWhitespace:
								case RdrState.InHostWS1:
								case RdrState.InHostWS2:
									break;
								case RdrState.InHostName:
									state = RdrState.InHostWS2;
									
									(current as HostEntry).HostName = sb.ToString();
									sb.Length = 0;
									
									break;
							}
							
							break;
						
						default:
							
							switch(state) {
								case RdrState.InNLWhitespace:
									current = new HostEntry();
									state = RdrState.InHostIPAddress;
									sb.Append( c );
									break;
								case RdrState.InHostComment:
								case RdrState.InHostIPAddress:
								case RdrState.InHostName:
								case RdrState.InNLComment:
									sb.Append( c );
									break;
								case RdrState.InHostWS1:
									state = RdrState.InHostName;
									sb.Append( c );
									break;
								case RdrState.InHostWS2:
									throw new Exception("Unexpected character");
							}
							
							break;
					}
					
				}//while
				
				if( sb.Length > 0 ) {
					
					switch(state) {
						case RdrState.InHostComment:
							(current as HostEntry).Comment = sb.ToString();
							sb.Length = 0;
							break;
						case RdrState.InNLComment:
							
							(current as Comment).Text = sb.ToString();
							sb.Length = 0;
							
							break;
						case RdrState.InHostIPAddress:
						case RdrState.InHostWS1:
							throw new Exception("Unexpected EOF");
						case RdrState.InHostWS2:
							break;
						case RdrState.InHostName:
							
							(current as HostEntry).HostName = sb.ToString();
							sb.Length = 0;
							
							break;
					}
					
					if( current != null ) Elements.Add( current );
					current = null;
					
				}
				
			}
			
		}
		
		public void Save(String fileName) {
			
			using(StreamWriter wtr = new StreamWriter(fileName, false)) {
				
				foreach(Element e in Elements) {
					
					wtr.WriteLine( e.ToString() );
				}
				
			}
		}
		
		private enum RdrState {
			InNLWhitespace,
			InNLComment,
			InHostIPAddress,
			InHostWS1,
			InHostWS2,
			InHostName,
			InHostComment
		}
		
	}
}
