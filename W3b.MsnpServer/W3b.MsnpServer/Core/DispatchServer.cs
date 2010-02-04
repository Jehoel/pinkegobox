using System;
using System.Collections.Generic;
using System.Text;
using System.Net.Sockets;
using System.Net;

using Verb = W3b.MsnpServer.Protocol.Msnp2DispatchVerbs;
using W3b.MsnpServer.Protocol;

namespace W3b.MsnpServer {
	
	public class DispatchConnection : ClientConnection {
		
		public DispatchConnection(int bufferLength, Socket socket) : base(bufferLength, socket) {
		}
		
		public DispatchProtocol Protocol    { get; set; }
		public AuthDetails      AuthDetails { get; set; }
		
	}
	
	public abstract class AuthDetails {
	}
	
	public sealed class DispatchServer : MsnpServer<DispatchConnection> {
		
#region Singleton
		private static DispatchServer _instance;
		public static DispatchServer Instance {
			get { return _instance ?? (_instance = new DispatchServer()); }
		}
#endregion
		
		private List<DispatchProtocol> _protocols = new List<DispatchProtocol>();
		
		private DispatchServer() : base("DS", ConsoleColor.Blue, 1863) {
			
			_protocols = new List<DispatchProtocol>() {
				new Cvr0DispatchProtocol(this),
				new Msnp2DispatchProtocol(this)
			};
			_protocols.Sort( (px, py) => py.Pref.CompareTo( px.Pref ) ); // descending sort order
		}
		
		protected override ClientConnection CreateClientConnection(int bufferLength, Socket socket) {
			
			return new DispatchConnection(bufferLength, socket);
		}
		
		protected override void OnDataReceived(DispatchConnection c) {
			
			Command cmd = GetCommand( c );
			if( cmd == null ) return;
			
			if(cmd.Verb == Verb.Ver) {
				// negociate protocol version before dc.Protocol can be set
				NegotiateVersion( c, cmd );
				return;
			}
			
			c.Protocol.HandleCommand( c, cmd );
		}
		
		private void NegotiateVersion(DispatchConnection c, Command cmd) {
			
			foreach(DispatchProtocol protocol in _protocols) { // this is sorted by preference
				
				if( cmd.Params.Contains( protocol.Name ) ) {
					
					Command response = new Command(Verb.Ver, cmd.TrId, protocol.Name);
					Send( c, response );
					
					c.Protocol = protocol;
					
					break;
				}
			}
			
		}
		
		public override void CloseConnection(DispatchConnection conn) {
			
			if( IsStarted ) {
				
				SendOut( conn, OutReason.None );
				
			} else { // if the server is stopped, which means this is being sent because the server is being shut down
				
				SendOut( conn, OutReason.Ssd );
			}
			
		}
		
		private void SendOut(DispatchConnection c, OutReason reason) {
			
			// TODO: This can be moved to the protocol classes somehow...
			// yeah, I need to figure out a way to get protocols to support async commands
			
			Command outCmd;
			if( reason != OutReason.None ) {
				
				outCmd = new Command(Verb.Out, -1, reason == OutReason.Oth ? "OTH" : "SSD");
				
			} else {
				
				outCmd = new Command(Verb.Out, -1);
			}
			
			Send( c, outCmd );
		}
		
	}
}
