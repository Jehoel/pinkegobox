using System;
using System.Collections.Generic;
using System.Text;

namespace W3b.MsnpServer.ConsoleHost.Utility {
	/// <summary>This is a stopgap until I can figure out how MSNP13Downgrader did its magic</summary>
	public class Hosts {
		
		// assuming a verrrry simple format for hosts files:
		// 
		// file    : { [ <comment> ] | [ host ] }*
		// comment : <hash> <anything> <nl>
		// hash    : "#"
		// host    : <ipAddress> <whitespace> <hostName> [ <comment> ] <nl>
		// nl      : "\r\n"
		
		private abstract class Element {
			
			public abstract bool Parse(StreamReader rdr);
			
		}
		
		private class HostsFile : Element {
			
			public override bool Parse(StreamReader rdr) {
				
				List<Element> e = new List<Element>();
				
				Comment   c = new Comment();
				HostEntry h = new HostEntry();
				
				while( rdr.BaseStream.Position < rdr.BaseStream.Length ) {
					
					if     ( c.Parse( rdr ) ) e.Add( c );
					else if( h.Parse( rdr ) ) e.Add( h );
				}
				
				return true;
			}
			
			public IEnumerable<Element> Elements {
				get; private set;
			}
			
		}
		
		private class Comment : Element {
			
			public override bool Parse(StreamReader rdr) {
				
				if( rdr.Read() == '#' ) {
					
					StringBuilder sb = new StringBuilder();
					int nc; char c;
					while( (nc = rdr.Read()) != -1 ) {
						c = (char)nc;
						
						if( c == '\r') {
							
							if( rdr.Read() != '\n' ) rdr.BaseStream.Seek(-1, SeekOrigin.Current);
							
							break;
							
						} else if( c == '\n' ) {
							
							break;
							
						} else {
							
							sb.Append( c );
						}
						
					}
					
					CommentText = sb.ToString();
					return true;
					
				} else {
					
					rdr.BaseStream.Seek( -1, SeekOrigin.Current );
					return false;
					
				}
				
			}
			
			public String CommentText {
				get; private set;
			}
			
		}
		
		private class HostEntry : Element {
			
			public override bool Parse(StreamReader rdr) {
				
				IPAddress = new IPAddress();
				IPAddress.Parse( rdr );
				
				
				
				HostName = new Hostname();
				HostName.Parse( rdr );
				
				Comment c = new Comment();
				if( c.Parse( rdr ) ) {
					Comment = c;
				}
				
			}
			
			public IPAddress IPAddress  { get; private set; }
			public String    Whitespace { get; private set; }
			public Hostname  HostName   { get; private set; }
			public Comment   Comment    { get; private set; }
			
		}
		
		private class IPAddress : Element {
			
			public override bool Parse(StreamReader rdr) {
				
				
				
			}
			
		}
		
		private class Hostname : Element {
		
		}
		
		private class NewLine : Element {
			
		}
		
		private class Whitespace : Element {
			
			public override bool Parse(StreamReader rdr) {
				
				while( (nc = rdr.Read()) != -1 ) {
				
			}
			
			public String String { get; private set; }
		}
		
	}
}
