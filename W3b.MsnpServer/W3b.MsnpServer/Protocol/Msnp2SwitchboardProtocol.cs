using System;

using Verb = W3b.MsnpServer.Protocol.Msnp2SwitchboardVerbs;

namespace W3b.MsnpServer.Protocol {
	
	public static class Msnp2SwitchboardVerbs {
		public const String Ack = "ACK";
		public const String Ans = "ANS";
		public const String Bye = "BYE";
		public const String Cal = "CAL";
		public const String Iro = "IRO";
		public const String Joi = "JOI";
		public const String Msg = "MSG";
		public const String Nak = "NAK";
		public const String Usr = "USR";
		public const String Out = "OUT";
	}
	
	public class Msnp2SwitchboardProtocol : SwitchboardProtocol {
		
		public Msnp2SwitchboardProtocol(SwitchboardServer server) : base("MSNP2", 2, server) {
		}
		
		public override void HandleCommand(SwitchboardConnection c, Command cmd) {
			
			switch(cmd.Verb) {
				case Verb.Usr:
					HandleUsr(c, cmd);
					break;
				case Verb.Ans:
					HandleAns(c, cmd);
					break;
				case Verb.Cal:
					HandleCal(c, cmd);
					break;
				case Verb.Out:
					HandleOut(c, cmd);
					break;
				case Verb.Msg:
					HandleMsg(c, cmd);
					break;
				default:
					HandleUnrecognised(c, cmd);
					break;
			}
			
		}
		
		protected virtual void HandleUsr(SwitchboardConnection c, Command cmd) {
			
			// >>> USR TrID UserHandle AuthResponseInfo
			// <<< USR TrID OK UserHandle FriendlyName
			
			// authenticate and ensure there's a matching session
			
			String userHandle = cmd.Params[0];
			String keyToken   = cmd.Params[1];
			
			User user = User.GetUser( userHandle );
			
			SwitchboardSession session = Server.GetSessionByCreator( user, keyToken );
			if( session == null ) {
				
				Command err = new Command(Error.AuthenticationFailed, cmd.TrId);
				Server.Send( c, err );
				
			} else {
				
				c.User    = user;
				c.Session = session;
				session.Connections.Add( c );
				
				User thisUser = User.GetUser( userHandle );
				
				Command response = new Command(Verb.Usr, cmd.TrId, "OK", userHandle, thisUser.FriendlyName);
				Server.Send( c, response );
				
			}
			
		}
		
		protected virtual void HandleCal(SwitchboardConnection c, Command cmd) {
			
			// >>> CAL TrID UserHandle
			// <<< CAL TrID Status SessionID
			
			String recipientHandle = cmd.Params[0];
			
			User recipient = User.GetUser( recipientHandle );
			if( recipient.NotificationServer == null ||
				(recipient.Status & Status.Nln) != Status.Nln ) {
				// the user is not online (or is hidden)
				
				Command responseErr = new Command(Error.SwitchboardFailed, cmd.TrId); // I guess?
				Server.Send( c, responseErr );
				
				return;
			}
			
			// if the invited user is allowed to contact the calling user (or vice-versa) then that user's notification server sends a RNG
			// otherwise, SB returns an error
			
			// TODO: Check permissions first
			
			User           caller  = c.User;
			SwitchboardSession session = c.Session;
			
			recipient.NotificationServer.ASNotifyRng( caller, recipient, session );
			
			Command responseOk = new Command(Verb.Cal, cmd.TrId, "RINGING", session.Id.ToStringInvariant());
			Server.Send( c, responseOk );
			
		}
		
		protected virtual void HandleAns(SwitchboardConnection c, Command cmd) {
			
			// >>> ANS TrID LocalUserHandle AuthResponseInfo SessionID
			// <<< IRO TrID Participant# TotalParticipants UserHandle FriendlyName
			// <<< ANS TrID OK
			
			// add this user to that session, assuming it authenticates
			
			String sessionKey = cmd.Params[1];
			Int32 sessionId   = Int32.Parse( cmd.Params[2] );
			
			User user = User.GetUser( cmd.Params[0] );
			
			SwitchboardSession session = Server.GetSessionByIdAndKey( sessionId, sessionKey );
			if( session == null || user == null ) {
				
				Command responseErr = new Command(Error.AuthenticationFailed, cmd.TrId);
				Server.Send( c, responseErr );
				
			} else {
				
				int cnt = session.Connections.Count;
				for(int i=0;i<session.Connections.Count;i++) {
					
					Command iro = new Command(Verb.Iro, cmd.TrId, (i+1).ToStringInvariant(), cnt.ToStringInvariant(), session.Connections[i].User.UserHandle, session.Connections[i].User.FriendlyName);
					Server.Send( c, iro );
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
				
				Server.BroadcastCommand( session, new Command(Verb.Joi, -1, user.UserHandle, user.FriendlyName) );
				
			}
			
		}
		
		protected virtual void HandleOut(SwitchboardConnection c, Command cmd) {
			
			// remove the user from its SB session
			// and send BYE to the users
			
			if( c.Session != null ) {
				
				c.Session.Connections.Remove( c ); // HACK: This isn't thread-safe
				
				if( c.Session.Connections.Count == 0 ) Server.EndSession( c.Session );
				else                                   Server.BroadcastCommand( c.Session, cmd ); // TODO: er...?
				
				c.Session = null;
			}
			
			// SB servers don't echo an OUT unlike NS or DS
			Server.CloseConnection( c );
		}
		
		protected virtual void HandleMsg(SwitchboardConnection c, Command cmd) {
			
			// >>> MSG TrID [U | N | A] Length\r\nMessage
			// <<< NAK TrID
			// <<< ACK TrID
			
			String ackMode = cmd.Params[0];
			String message = cmd.Payload;
			int    messLen = Int32.Parse( cmd.Params[1] );
			
			if( messLen != message.Length ) throw new ProtocolException();
			
			SwitchboardSession session = c.Session;
			
			// TODO: I'll need to convert MSGs or do some kind of special handling if there's any protocol-specific functionality
			// <<< MSG UserHandle FriendlyName Length\r\nMessage
			Command broadcastMsg = Command.CreateWithPayload(Verb.Msg, -1, message, c.User.UserHandle, c.User.FriendlyName);
			bool succeeded = Server.BroadcastCommand( session, broadcastMsg );
			
			// TODO: I'll need to separate out ACK code because it is NOT as simple as this
			
			switch(ackMode) {
				case "U":
					// Send nothing
					return;
				case "N":
					// Send only if it fails
					if( !succeeded ) {
						Server.Send( c, new Command(Verb.Nak, cmd.TrId) );
					}
					break;
				case "A":
					
					// the spec didn't say what the response was in A mode (as it isn't implemented in MSNP2) but I assume this is what the client expects
					// ACK if it succeeds, and NAK if it fails
					Command response = new Command( succeeded ? Verb.Ack : Verb.Nak, cmd.TrId );
					Server.Send( c, response );
					
					break;
				default:
					
					Command errResponse = new Command( Error.InvalidParameter, cmd.TrId );
					Server.Send( c, errResponse );
					break;
			}
			
		}
		
	}
}
