using System;
using System.Collections.Generic;
using System.Net.Sockets;

using W3b.MsnpServer.Protocol;

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
		
		public SwitchboardInvitation(SwitchboardSession session, User user) {
			
			Session = session;
			Id   = _counter++;
			User = user;
			Key  = AuthenticationService.CreateChallengeString();
		}
		
		public SwitchboardSession Session { get; private set; }
		
		public Int32  Id       { get; private set; }
		public String Protocol { get; set; }
		public User   User     { get; private set; }
		public String Key      { get; private set; }
		
		public bool   Rsvp     { get; private set; }
		
		public void SetRsvp() {
			if( Protocol == null ) throw new InvalidOperationException("Cannot mark an invitation as accepted if the protocol name has not been set.");
			Rsvp = true;
		}
		
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
		
		public SwitchboardInvitation CreateInvitation(User invitee) {
			
			SwitchboardInvitation invite = new SwitchboardInvitation( this, invitee );
			Invitations.Add( invite );
			
			return invite;
		}
		
		public Int32                       Id          { get; private set; }
		public List<SwitchboardInvitation> Invitations { get; private set; }
		public List<SwitchboardConnection> Connections { get; private set; }
		public DateTime                    Created     { get; private set; }
		public User                        Creator     { get; private set; }
		public SwitchboardServer           Server      { get; private set; }
		
	}
	
	public sealed class SwitchboardServer : MsnpServer<SwitchboardConnection> {
		
		private static SwitchboardServer _instance;
		public static SwitchboardServer Instance {
			get { return _instance ?? (_instance = new SwitchboardServer()); }
		}
		
		
		private List<SwitchboardSession>  _sessions  = new List<SwitchboardSession>();
		private BaseSwitchboardProtocol _base;
		
		private SwitchboardServer() : base("SB", ConsoleColor.Red, 1865) {
			
			_base = new BaseSwitchboardProtocol(this);
		}
		
		protected override ClientConnection CreateClientConnection(int bufferLength, Socket socket) {
			
			return new SwitchboardConnection( bufferLength, socket );
		}
		
		protected override void OnDataReceived(SwitchboardConnection c) {
			
			Command cmd = GetCommand(c);
			if( cmd == null ) return;
			
			// only way to determine protocol level is by handling USR and ANS verbs
			// rather than do what DS and NS do by handling VER themselves, use BaseSwitchboardProtocol which sets the connection's protocol instance
			
			if( c.Protocol == null ) {
				
				_base.HandleCommand( c, cmd );
				
				if( c.Protocol == null ) throw new ProtocolException("Could not authenticate SwitchboardSession and match protocol version");
				
			} else {
				
				c.Protocol.HandleCommand( c, cmd );
			}
			
		}
		
		public SwitchboardSession CreateSession(User creator) {
			
			SwitchboardSession session = new SwitchboardSession( this, creator);
			_sessions.Add( session );
			return session;
		}
		
		public SwitchboardInvitation GetInvitationByUserAndKey(User user, String key) {
			
			foreach(SwitchboardSession session in _sessions) {
				
				foreach(SwitchboardInvitation invite in session.Invitations) {
					
					if( invite.User == user && invite.Key.Equals(key, StringComparison.OrdinalIgnoreCase)  ) return invite;
				}
			}
			return null;
		}
		
		public SwitchboardInvitation GetInvitationByUserKeyAndId(User user, String key, int sessionId) {
			
			// meh
			SwitchboardInvitation invite = GetInvitationByUserAndKey( user, key );
			return invite.Session.Id == sessionId ? invite : null;
		}
		
		public bool BroadcastCommand(SwitchboardSession session, Command cmd, SwitchboardConnection ignore) {
			
			if( cmd.TrId != -1 && cmd.TrId != 0 ) throw new ProtocolException("Only Asynchronous server commands can be sent.");
			
			foreach(SwitchboardConnection conn in session.Connections) {
				
				if( conn != ignore )
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
