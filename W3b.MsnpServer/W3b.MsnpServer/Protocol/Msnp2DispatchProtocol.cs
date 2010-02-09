using System;

using Verb = W3b.MsnpServer.Protocol.Msnp2DispatchVerbs;

namespace W3b.MsnpServer.Protocol {
	
	public static class Msnp2DispatchVerbs {
		public const String None = "";
		public const String Err = null;
		
		public const String Cvr = "CVR";
		public const String Cvq = "CVQ";
		
		public const String Inf = "INF";
		public const String Ver = "VER";
		public const String Usr = "USR";
		public const String Xfr = "XFR";
		
		public const String Out = "OUT";
	}
	
	public class Md5AuthDetails : AuthDetails {
		
		public Md5AuthDetails(String userHandle, String challenge) {
			
			UserHandle = userHandle;
			Challenge  = challenge;
		}
		
		public String UserHandle { get; private set; }
		public String Challenge  { get; private set; }
		public String Response   { get; set; }
		
	}
	
	public class Msnp2DispatchProtocol : Cvr0DispatchProtocol {
		
		public Msnp2DispatchProtocol(DispatchServer server) : base("MSNP2", 2, server) {
		}
		
		protected Msnp2DispatchProtocol(String name, int pref, DispatchServer server) : base(name, pref, server) {
		}
		
		public override void HandleCommand(DispatchConnection c, Command cmd) {
			
			switch(cmd.Verb) {
				case Verb.Cvr:
					HandleCvr( c, cmd );
					break;
				case Verb.Cvq:
					HandleCvq( c, cmd );
					break;
				case Verb.Inf:
					HandleInf( c, cmd );
					break;
				case Verb.Usr:
					HandleUsr( c, cmd );
					break;
				default:
					HandleUnrecognised( c, cmd );
					break;
			}
			
		}
		
		protected virtual void HandleInf(DispatchConnection c, Command cmd) {
			
			Msnp2Common.HandleInf(Server, c, cmd);
		}
		
		protected virtual void HandleUsr(DispatchConnection c, Command cmd) {
			
			// there are two steps:
			// >>> USR trID SP I userHandle
			// <<< USR trID SP S challenge
			// >>> USR trID SP S response
			// <<< USR trID OK userHandle friendlyName
			
			// for now, only MD5 is supported
			if( cmd.Params[0] != "MD5" ) {
				
				Command response = new Command(Error.NotExpected, cmd.TrId);
				Server.Send(c, response );
				
			} else {
				
				if( cmd.Params[1] == "I" ) {
					
					HandleUsrI( c, cmd );
					
				} else if ( cmd.Params[1] == "S" ) {
					
					HandleUsrS( c, cmd );
				}
				
			}
			
		}
		
		private void HandleUsrI(DispatchConnection c, Command cmd) {
			
			String userHandle = cmd.Params[2];
			String challenge  = AuthenticationService.CreateChallengeString();
			
			Command response = new Command(Verb.Usr, cmd.TrId, "MD5", "S", challenge );
			Server.Send(c, response );
			
			c.AuthDetails = new Md5AuthDetails( userHandle, challenge );
		}
		
		private void HandleUsrS(DispatchConnection c, Command cmd) {
			
			Md5AuthDetails auth = c.AuthDetails as Md5AuthDetails;
			String   chResponse = cmd.Params[2];
			
			if( auth == null ) {
				// ordinarily that would be an Exception condition
				Command response = new Command(Error.AuthenticationFailed, cmd.TrId);
				Server.Send( c, response );
				
			} else {
				
				User user;
				if( AuthenticationService.AuthenticateMd5( auth.UserHandle, auth.Challenge, chResponse, out user) == AuthenticationResult.Success ) {
					
					Command responseOk = new Command(Verb.Usr, cmd.TrId, "OK", user.UserHandle, user.FriendlyName);
					Server.Send( c, responseOk );
					
					/////////////////////////////
					// Send XFR, otherwise client will use this Dispatch server as a notification server (it starts by sending SYN)
					// note that the official client deviates from the IETF draft by requiring an additional "0" parameter on the XFR command
					
					// curiously, it seems MSN Messenger 1.x caches the last Notification server and reconnects to it directly
					// in which case you don't send an XFR and instead begin the rest of an NS server's duties
					
					String sendTo = NotificationServer.Instance.GetEndPointForClient( c.Socket.LocalEndPoint ).ToString();
					
					Command responseXfr = new Command(Verb.Xfr, cmd.TrId, "NS", sendTo, "0");
					Server.Send( c, responseXfr );
				
				} else {
					
					Command response = new Command(Error.AuthenticationFailed, cmd.TrId);
					Server.Send( c, response );
				}
				
			}
			
		}
		
	}
}
