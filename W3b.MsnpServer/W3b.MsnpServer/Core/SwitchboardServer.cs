using System;
using System.Collections.Generic;
using System.Text;
using System.Net.Sockets;

using W3b.MsnpServer.Protocol;
using Verb = W3b.MsnpServer.Protocol.Msnp2SwitchboardVerbs;

namespace W3b.MsnpServer {
	
	public class SwitchboardConnection : ClientConnection {
		
		public SwitchboardConnection(int bufferLength, Socket socket) : base(bufferLength, socket) {
		}
		
		public SwitchboardProtocol Protocol { get; set; }
		public User                User     { get; set; }
		public SwitchboardSession  Session  { get; set; }
		
	}
	
	public class SwitchboardInvitation {
		
		private static Int32 _counter = 1;
		
		public SwitchboardInvitation() {
			
			Id  = _counter++;
			Key = AuthenticationService.CreateChallengeString();
		}
		
		public Int32  Id       { get; private set; }
		public String Protocol { get; private set; }
		public User   User     { get; private set; }
		public String Key      { get; private set; }
	}
	
	public class SwitchboardSession {
		
		private static Int32 _counter = 1;
		
		public SwitchboardSession(SwitchboardServer server, User creator) {
			Connections = new List<SwitchboardConnection>();
			Invitations = new List<SwitchboardInvitation>();
			Created     = DateTime.Now;
			
			Creator     = creator;
			Server      = server;
			Id          = _counter++;
		}
		
		public Int32                       Id          { get; private set; }
		public List<SwitchboardInvitation> Invitations { get; private set; }
		public List<SwitchboardConnection> Connections { get; private set; }
		public DateTime                    Created     { get; private set; }
		public String                      Key         { get; private set; }
		public User                        Creator     { get; private set; }
		public SwitchboardServer           Server      { get; private set; }
		
	}
	
	public sealed class SwitchboardServer : MsnpServer<SwitchboardConnection> {
		
		private static SwitchboardServer _instance;
		public static SwitchboardServer Instance {
			get { return _instance ?? (_instance = new SwitchboardServer()); }
		}
		
		
		private List<SwitchboardSession> _sessions = new List<SwitchboardSession>();
		
		private SwitchboardServer() : base("SB", ConsoleColor.Red, 1865) {
		}
		
		protected override ClientConnection CreateClientConnection(int bufferLength, Socket socket) {
			
			return new SwitchboardConnection( bufferLength, socket );
		}
		
		protected override void OnDataReceived(SwitchboardConnection c) {
			
			Command cmd = GetCommand(c);
			if( cmd == null ) return;
			
			
			
		}
		
		public SwitchboardSession CreateSession(User creator) {
			
			SwitchboardSession session = new SwitchboardSession( this, creator);
			_sessions.Add( session );
			return session;
		}
		
		public SwitchboardSession GetSessionByIdAndKey(Int32 id, String key) {
			
			foreach(SwitchboardSession session in _sessions) {
				
				if( session.Id == id && session.Key == key ) return session;
			}
			
			return null;
		}
		
		/// <summary>Returns a session created by the specified user (i.e. an unbound session). To get a bounded session, query SwitchboardConnection's Session property.</summary>
		public SwitchboardSession GetSessionByCreator(User creator, String key) {
			
			foreach(SwitchboardSession session in _sessions) {
				
				if( session.Creator == creator && session.Key == key ) return session;
			}
			
			return null;
		}
		
		public bool BroadcastCommand(SwitchboardSession session, Command cmd) {
			
			if( cmd.TrId != -1 && cmd.TrId != 0 ) throw new ProtocolException("Only Asynchronous server commands can be sent.");
			
			foreach(SwitchboardConnection conn in session.Connections) {
				
				Send( conn, cmd );
			}
			
			return true;
		}
		
		public void EndSession(SwitchboardSession session) {
			
			if( session.Connections.Count > 0 ) {
				// TODO: Something?
			}
			
			_sessions.Remove( session );
		}
		
		public override void CloseConnection(SwitchboardConnection c) {
			// TODO: remove user from all sessions they're a member of if they have'nt left cleanly
			// apparently there are also "system messages" that are sent to the users explaining that someone left improperly
			
			// SB servers don't echo OUT commands, btw; so for now this is a NOOP
		}
	}
}
