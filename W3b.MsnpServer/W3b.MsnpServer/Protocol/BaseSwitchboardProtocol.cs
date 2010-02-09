using System;
using System.Collections.Generic;
using System.Text;

using Verb = W3b.MsnpServer.Protocol.Msnp2SwitchboardVerbs;

namespace W3b.MsnpServer.Protocol {
	
	public class BaseSwitchboardProtocol : SwitchboardProtocol {
		
		private List<SwitchboardProtocol> _protocols = new List<SwitchboardProtocol>();
		
		public BaseSwitchboardProtocol(SwitchboardServer server) : base("BASE", 0, server) {
			
			_protocols = new List<SwitchboardProtocol>() {
				new Msnp2SwitchboardProtocol(server)
			};
			_protocols.Sort( (px, py) => py.Pref.CompareTo( px.Pref ) ); // descending sort order
		}
		
		protected BaseSwitchboardProtocol(String name, int pref, SwitchboardServer server) : base(name, pref, server) {
		}
		
		public override void HandleCommand(SwitchboardConnection c, Command cmd) {
			
			switch(cmd.Verb) {
				case Verb.Usr:
					HandleUsr(c, cmd);
					break;
				case Verb.Ans:
					HandleAns(c, cmd);
					break;
				default:
					HandleUnrecognised(c, cmd);
					break;
			}
			
		}
		
		protected virtual void HandleUsr(SwitchboardConnection c, Command cmd) {
			
			// >>> USR TrID UserHandle AuthResponseInfo
			// <<< USR TrID OK UserHandle FriendlyName
			
			// authenticate and ensure there's a matching session invite
			
			String userHandle = cmd.Params[0];
			String keyToken   = cmd.Params[1];
			
			User user = User.GetUser( userHandle );
			
			SwitchboardInvitation invite = Server.GetInvitationByUserAndKey( user, keyToken );
			if( invite == null ) {
				
				Command err = new Command(Error.AuthenticationFailed, cmd.TrId);
				Server.Send( c, err );
				
			} else {
				
				invite.SetRsvp();
				
				if( c.Protocol == null )
					c.Protocol = _protocols.Find( p => p.CompatibleWithProtocol( invite.Protocol ) ); //( p => p.Name == invite.Protocol );
				
				SwitchboardSession session = invite.Session;
				c.User    = user;
				c.Session = session;
				session.Connections.Add( c );
				
				User thisUser = User.GetUser( userHandle );
				
				Command response = new Command(Verb.Usr, cmd.TrId, "OK", userHandle, thisUser.FriendlyName);
				Server.Send( c, response );
				
			}
		}
		
		protected virtual void HandleAns(SwitchboardConnection c, Command cmd) {
			
			// >>> ANS TrID LocalUserHandle AuthResponseInfo SessionID
			// <<< IRO TrID Participant# TotalParticipants UserHandle FriendlyName
			// <<< ANS TrID OK
			
			// add this user to that session, assuming it authenticates
			
			String sessionKey = cmd.Params[1];
			Int32 sessionId   = Int32.Parse( cmd.Params[2] );
			
			User user = User.GetUser( cmd.Params[0] );
			
			if( user == null ) {
				Command responseErr = new Command(Error.AuthenticationFailed, cmd.TrId);
				Server.Send( c, responseErr );
				return;
			}
			
			SwitchboardInvitation invite = Server.GetInvitationByUserKeyAndId( user, sessionKey, sessionId );
			
			if( invite == null ) {
				Command responseErr = new Command(Error.AuthenticationFailed, cmd.TrId);
				Server.Send( c, responseErr );
				return;
			}
			
			invite.SetRsvp();
			
			if( c.Protocol == null )
				c.Protocol = _protocols.Find( p => p.CompatibleWithProtocol( invite.Protocol ) ); //( p => p.Name == invite.Protocol );
			
			SwitchboardSession session = invite.Session;
			
			int cnt = session.Connections.Count;
			for(int i=0;i<session.Connections.Count;i++) {
				
				SwitchboardConnection sc = session.Connections[i];
				
				if( sc != c ) {
					
					Command iro = new Command(Verb.Iro, cmd.TrId, (i+1).ToStringInvariant(), cnt.ToStringInvariant(), sc.User.UserHandle, sc.User.FriendlyName );
					Server.Send( c, iro );
				}
			}
			
			c.User    = user;
			c.Session = session;
			
			session.Connections.Add( c );
			
			Command respOk = new Command(Verb.Ans, cmd.TrId, "OK");
			Server.Send( c, respOk );
			
			// When a new user joins a Switchboard session, the server sends the
			// following command to all participating clients, including the client
			// joining the session:
			
			// <<< JOI CalleeUserHandle CalleeUserFriendlyName
			
			// UPDATE: Actually, I think not; don't send it to the person joining
			
			Command joi = new Command(Verb.Joi, -1, user.UserHandle, user.FriendlyName);
			Server.BroadcastCommand( session, joi, c );
		}
		
	}
}
