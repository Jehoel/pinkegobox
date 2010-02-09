using System;
using System.Collections.Generic;

using Verb = W3b.MsnpServer.Protocol.Msnp2NotificationVerbs;

namespace W3b.MsnpServer.Protocol {
	
	public static class Msnp2NotificationVerbs {
		public const String None = "";
		public const String Err = null;
		
		//////////////////////////////////
		// Dispatch Commands
		public const String Cvr = "CVR";
		public const String Cvq = "CVQ";
		
		public const String Inf = "INF";
		public const String Ver = "VER";
		public const String Usr = "USR";
		public const String Xfr = "XFR";
		
		public const String Out = "OUT";
		
		//////////////////////////////////
		// Notification Commands
		public const String Blp = "BLP";
		public const String Gtc = "GTC";
		
		public const String Add = "ADD";
		public const String Rem = "REM";
		
		public const String Lst = "LST";
		public const String Chg = "CHG";
		public const String Rea = "REA";
		public const String Rng = "RNG";
		public const String Syn = "SYN";
		public const String Url = "URL";
		
		public const String Nln = "NLN";
		public const String Iln = "ILN";
		public const String Fln = "FLN";
		
		public const String Snd = "SND"; // send invite to known user
		public const String Fnd = "FND"; // find user
		public const String Sdc = "SDC"; // send invite to unknown user (a user found via FND)
	}
	
	public class Msnp2NotificationProtocol : NotificationProtocol {
		
		public Msnp2NotificationProtocol(NotificationServer server) : base("MSNP2", 2, server) {
		}
		
		protected Msnp2NotificationProtocol(String name, int pref, NotificationServer server) : base(name, pref, server) {
		}
		
		public override void HandleCommand(NotificationConnection c, Command cmd) {
			
			switch(cmd.Verb) {
				// Dispatch Tasks
				case Verb.Cvr:
					HandleCvr(c, cmd);
					break;
				case Verb.Cvq:
					HandleCvr(c, cmd);
					break;
				case Verb.Inf:
					HandleInf(c, cmd);
					break;
				case Verb.Usr:
					HandleUsr(c, cmd);
					break;
					
				// Notification
				// Initial
				case Verb.Syn:
					HandleSyn(c, cmd);
					break;
				case Verb.Lst:
					HandleLst(c, cmd);
					break;
				
				// List Management
				case Verb.Add:
					HandleAdd(c, cmd);
					break;
				case Verb.Rem:
					HandleRem(c, cmd);
					break;
				case Verb.Rea:
					HandleRea(c, cmd);
					break;
				
				case Verb.Chg:
					HandleChg(c, cmd);
					break;
				case Verb.Blp:
					HandleBlp(c, cmd);
					break;
				case Verb.Gtc:
					HandleBlp(c, cmd);
					break;
				
				// Info
				case Verb.Url:
					HandleUrl(c, cmd);
					break;
				case Verb.Out:
					HandleOut(c, cmd);
					break;
				
				case Verb.Xfr:
					HandleXfr(c, cmd);
					break;
				
// TODO
//				case Verb.Fnd:
//				case Verb.Sdc:
//				case Verb.Snd:
//					break;
				
				default:
					HandleUnrecognised(c, cmd);
					break;
			}
			
		}
		
#region Handle
		
	#region Switchboard Overlap
		/////////////////////////////////////////////////////////
		// Switchboard overlap
		
		protected virtual void HandleCvr(NotificationConnection c, Command cmd) {
			
			Msnp2Common.HandleCvr(Server, c, cmd);
		}
		
		protected virtual void HandleCvq(NotificationConnection c, Command cmd) {
			
			Msnp2Common.HandleCvq(Server, c, cmd);
		}
		
		protected virtual void HandleInf(NotificationConnection c, Command cmd) {
			
			Msnp2Common.HandleInf(Server, c, cmd);
		}
		
		protected virtual void HandleUsr(NotificationConnection c, Command cmd) {
			
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
		
		private void HandleUsrI(NotificationConnection c, Command cmd) {
			
			String userHandle = cmd.Params[2];
			String challenge  = AuthenticationService.CreateChallengeString();
			
			Command response = new Command(Verb.Usr, cmd.TrId, "MD5", "S", challenge );
			Server.Send(c, response );
			
			c.AuthDetails = new Md5AuthDetails( userHandle, challenge );
		}
		
		private void HandleUsrS(NotificationConnection c, Command cmd) {
			
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
					
					///////////////////////////////////////
					// The client will now send SYN
					
					// create a session, which is implicit; just bind the user to the connection
					user.Status = Status.Hdn; // no-longer NLN
					user.NotificationServer = Server;
					
					c.User = user;
					
				} else {
					
					Command responseFail = new Command(Error.AuthenticationFailed, cmd.TrId);
					Server.Send( c, responseFail );
				}
				
			}
			
		}
	#endregion
		
	#region Notification
		/////////////////////////////////////////////////////////
		// Notification
		
		protected virtual void HandleSyn(NotificationConnection c, Command cmd) {
			
			int clientSerial = Int32.Parse( cmd.Params[0] );
			int serverSerial = c.User.Properties.Serial;
			
			if( clientSerial > c.User.Properties.Serial ) {
				
				// this should never happen as the client's cache is always older than the server
				// but in a debugging situation or if the client's cache is corrupted then just lie to the client
				
				c.User.Properties.Serial = serverSerial = clientSerial + 1;
				// ha! take that MSN Messenger 1.0!
			}
			
			Command response = new Command(Verb.Syn, cmd.TrId, serverSerial.ToString() );
			Server.Send( c, response );
			
			///////////////////////
			
			int trId = cmd.TrId;
			
			if( clientSerial < c.User.Properties.Serial ) {
				// send out the lists to the client using the same TrId
				
				// "After the SYN reply from the server, the user property updates will be sent from the server in this sequence:
				// "GTC, BLP, LST FL, LST AL, LST BL, LST RL."
				
				// GTC
				SendGtc(c, trId, c.User.Properties.GtcSetting, serverSerial);
				
				// BLP
				SendBlp(c, trId, c.User.Properties.BlpSetting, serverSerial);
				
				// LST FL
				SendLst(c, trId, "FL", c.User.Properties.ForwardList.Values, serverSerial );
				SendLst(c, trId, "AL", c.User.Properties.VirtualAllowList  , serverSerial );
				SendLst(c, trId, "BL", c.User.Properties.VirtualBlockList  , serverSerial );
				SendLst(c, trId, "RL", c.User.Properties.ReverseList.Values, serverSerial );
				
			}
			
		}
		
		protected virtual void HandleChg(NotificationConnection c, Command cmd) {
			
			Status newStatus = Enumerations.GetStatus( cmd.Params[0] );
			
			Command response = new Command(Verb.Chg, cmd.TrId, cmd.Params[0] );
			Server.Send( c, response );
			
			Server.UserStatusChanged( c.User, newStatus );
			
			// .SentInitialChg is a protocol dialect-dependent property, it shouldn't be a member of NotificationConnection
			// there should be a ProtocolState member instead, which contains this stuff
			
			if( !c.SentInitialChg ) {
				
				// after the initial CHG from the client, give it the ILNs of its Forward List
				
				foreach(User forwardUser in c.User.Properties.VirtualAllowedForwardList) {
					
					if( forwardUser.Status != Status.Fln && forwardUser.Status != Status.Hdn ) {
						
						SendIln( c, cmd.TrId, forwardUser );
					}
					
				}
				
				c.SentInitialChg = true;
			}
			
		}
		
		protected virtual void HandleOut(NotificationConnection c, Command cmd) {
			
			// for now, this server implementation is entirely reactionary
			// I'll work on async and other stuff later
			
			// TODO inform the user's ReverseList
			// or should that be done from within CloseConnection, as a connection can be closed in more ways than OUT
			
			// close the connection, which sends the OUT message
			Server.CloseConnection( c );
		}
		
		protected virtual void HandleAdd(NotificationConnection c, Command cmd) {
			
			// >>> ADD TrID LIST UserHandle CustomUserName
			// <<< ADD TrID LIST ser# UserHandle CustomUserName
			
			String listName   = cmd.Params[0];
			String userHandle = cmd.Params[1];
			String customName = cmd.Params[2];
			
			User owner  = c.User;
			User target = User.GetUser( userHandle );
			if( target == null ) {
				
				Command errorDoesntExist = new Command(Error.InvalidUser, cmd.TrId);
				Server.Send( c, errorDoesntExist );
				return;
			}
			
			switch(listName) {
				case "FL":
				case "RL":
				case "AL":
				case "BL":
					
					Server.AddToList( owner, listName, target, customName );
					
					Command response = new Command(Verb.Add, cmd.TrId, listName, owner.Properties.Serial.ToStringInvariant(), userHandle, customName);
					Server.Send( c, response );
					
					break;
				default:
					
					Command errorSyntax = new Command(Error.SyntaxError, cmd.TrId);
					Server.Send( c, errorSyntax );
					return;
			}
		}
		
		protected virtual void HandleRem(NotificationConnection c, Command cmd) {
			
			// >>> REM TrID LIST UserHandle
			// <<< REM TrID LIST ser# UserHandle
			
			String listName   = cmd.Params[0];
			String userHandle = cmd.Params[1];
			
			User owner  = c.User;
			User target = User.GetUser( userHandle );
			if( target == null ) {
				
				Command errorDoesntExist = new Command(Error.InvalidUser, cmd.TrId);
				Server.Send( c, errorDoesntExist );
				return;
			}
			
			switch(listName) {
				case "FL":
				case "RL":
				case "AL":
				case "BL":
					
					Server.RemoveFromList( owner, listName, target );
					
					Command response = new Command(Verb.Rem, cmd.TrId, listName, owner.Properties.Serial.ToStringInvariant(), userHandle);
					Server.Send( c, response );
					
					break;
				default:
					
					Command errorSyntax = new Command(Error.SyntaxError, cmd.TrId);
					Server.Send( c, errorSyntax );
					return;
			}
			
		}
		
		protected virtual void HandleXfr(NotificationConnection c, Command cmd) {
			
			// >>> XFR TrID SB
			// <<< XFR TrID SB Address SP AuthChallengeInfo
			
			if( cmd.Params[0] != "SB" ) { // only XFRs to Switchboard servers are supported, besides what else is there to XFR to?
				
				Command errResponse = new Command(Error.SyntaxError, cmd.TrId);
				Server.Send( c, errResponse );
				return;
			}
			
			// provision the new session
			
			SwitchboardServer sb = SwitchboardServer.Instance;
			SwitchboardSession session = sb.CreateSession( c.User );
			
			SwitchboardInvitation invite = session.CreateInvitation( c.User );
			invite.Protocol = c.Protocol.Name;
			
			Command xfrResponse = new Command(Verb.Xfr, cmd.TrId, "SB", sb.GetEndPointForClient( c.Socket.LocalEndPoint ).ToString(), "CKI", invite.Key );
			Server.Send( c, xfrResponse );
			
		}
		
		protected virtual void HandleRea(NotificationConnection c, Command cmd) {
			
			// >>> REA trid email display-name
			// <<< REA trid ver-num email display-name
			
			// renames the customName in the ForwardList OR renames self
			// I don't know if I'm meant to send out any asynchronous updates to other users or anything
			
			// In xMSN, it sends an async version of REA (trId == 0) to everyone in the Allowed/Reverse list, so let's do that
			
			String userHandle = cmd.Params[0];
			String newName    = cmd.Params[1];
			
			if( newName.Length > 387 ) {
				
				Command error = new Command(Error.InvalidFriendlyName, cmd.TrId);
				Server.Send( c, error );
				
			} else {
				
				User target = User.GetUser( userHandle );
				if( target == c.User ) {
					
					Server.UserRenamed( target, newName );
					// Server.UserRenamed sends notifications to the contact's users
					
					// BTW, the 'ver-num' field is undocumented, so I guess return the userProperties serial?
					Command response = new Command(Verb.Rea, cmd.TrId, target.Properties.Serial.ToStringInvariant(), userHandle, newName );
					Server.Send( c, response );
					
				} else {
					
					// rename the specified object in the ForwardList. This obviously does not send any notifications out
					Server.UserFLRenamed( c.User, target, newName );
					
					// NOTE: can it send a REA to rename something in the RL, AL, or BL?
					
					Command response = new Command(Verb.Rea, cmd.TrId, c.User.Properties.Serial.ToStringInvariant(), userHandle, newName );
					Server.Send( c, response );
					
				}
				
			}
			
		}
		
		protected virtual void HandleBlp(NotificationConnection c, Command cmd) {
			
			// >>> BLP TrID [AL | BL]
			// <<< BLP TrID Ser# [AL | BL]
			
			c.User.Properties.BlpSetting = cmd.Params[0] == "AL" ? AllowSetting.Allow : AllowSetting.Block;
			c.User.Properties.Serial++;
			
			Command response = new Command(Verb.Blp, cmd.TrId, c.User.Properties.Serial.ToStringInvariant());
			Server.Send( c, response );
		}
		
		protected virtual void HandleGtc(NotificationConnection c, Command cmd) {
			
			// >>> GTC TrID [A | N]
			// <<< GTC TrID Ser# [A | N]
			
			c.User.Properties.GtcSetting = cmd.Params[0] == "A" ? AllowSetting.Allow : AllowSetting.Block;
			c.User.Properties.Serial++;
			
			Command response = new Command(Verb.Gtc, cmd.TrId, c.User.Properties.Serial.ToStringInvariant());
			Server.Send( c, response );
		}
		
		protected virtual void HandleLst(NotificationConnection c, Command cmd) {
			
			// >>> LST TrID list
			// <<< LST TrID list Ser# Item# TtlItems UserHandle CustomUserName
			
			// list is FL/RL/AL/BL for Forward List, Reverse List, Allow List, and Block List, respectively.
			// The Item# parameter contains the index of the item described in this command message. (E.g. item 1 of N, 2 of N, etc.)
			// - The TtlItems parameter contains the total number of items in this list.
			// - UserHandle is the user handle for this list item.
			// - CustomUserName is the friendly name for this list item.
			
			// If the list is empty, the response will be:
			
			// <<< LST TrID list Ser# 0 0
			
			switch(cmd.Params[0]) {
				case "FL":
					
					SendLst( c, cmd.TrId, "FL", c.User.Properties.ForwardList.Values, c.User.Properties.Serial );
					
					break;
				case "AL":
					
					SendLst( c, cmd.TrId, "AL", c.User.Properties.VirtualAllowList, c.User.Properties.Serial );
					
					break;
				case "BL":
					
					SendLst( c, cmd.TrId, "BL", c.User.Properties.VirtualBlockList, c.User.Properties.Serial );
					
					break;
				case "RL":
					
					SendLst( c, cmd.TrId, "RL", c.User.Properties.ReverseList.Values, c.User.Properties.Serial );
					
					break;
				default:
					Command errorSyntax = new Command(Error.InvalidParameter, cmd.TrId); // or syntax error
					Server.Send( c, errorSyntax );
					return;
			}
		}
		
		protected virtual void HandleUrl(NotificationConnection c, Command cmd) {
			
			// >>> URL trId service [parameter1] [parameterN]
			// <<< URL trId dummy URL 0
			
			// dummy = unknown literal string "dummy", seems to work
			// URL   = the URL-encoded URL being requested
			// 0     = unknown magic number
			
			// UPDATE: Messenger 3.6 doesn't seem to like the format of my response
			// UPDATE2: I got an updated syntax definition from the xMSN source, see above
			
			// in future, when I subclass servers by protocol I'll want to sort this table out so it only has entries relevant to the current protocol
			
			Dictionary<String,String> urls = new Dictionary<string,string>() {
				{"INBOX"     , "http://www.hotmail.com" }, // Email Inbox       - MSNP2
				{"COMPOSE"   , "mailto:{1}" },             // Send email        - MSNP2
//				{"COMPOSE"   , "mailto:{1}" },             // Send email to {0} - MSNP2
				{"MOBILE"    , "http://{1}" },             // MSN Mobile        - MSNP4
				{"PROFILE"   , "http://{1}" },             // MSN Profile       - MSNP4
				{"N2PACCOUNT", "http://{1}" },             // Net2Phone Account - MSNP4
				{"PERSON"    , "http://{1}" },             // Member Services   - MSNP4
				{"FOLDERS"   , "http://{1}" },             // "MSN Home" page   - MSNP5-7?
				{"CHGMOB"    , "http://{1}" },             // Mobile Settings   - MSNP5-7?
				{"CHAT"      , "http://{1}" },             // MSN Chat          - MSNP5-7?
			};
			
			/*
			 *     *  INBOX - Hotmail inbox
    * FOLDERS - Believed to be the Hotmail's "MSN home" URL.
    * COMPOSE - Compose an email
    * COMPOSE example@passport.com - Compose an email for example@passport.com
    * PROFILE 0x1409 - Edit your MSN member directory profile
    * CHGMOB - Mobile settings (pager etc.)
    * PERSON 0x0409 - Member services, password, secret question, account info
    * CHAT 0x0409 - Chat rooms

			 */
			
			
			String url = urls.Get( cmd.Params[0] );
			if( url != null ) url = String.Format(url, cmd.Params );
			url = UtilityMethods.UrlEncode( url );
			
			Command response = new Command(Verb.Url, cmd.TrId, "dummy", url, "0"); // I assume this is how it expects it
			Server.Send( c, response );
			
		}
		
	#endregion
#endregion
		
	#region Send to Self
		
		// NOTE: Sending messages to OTHER clients requires going through the NotificationServer to ensure the right protocol for the connection is used
		// since not every client is using THIS protocol version
		
		// I wonder if this is how the original MSNP servers were architectured as they realised they needed to support multiple protocol versions
		
		protected virtual void SendGtc(NotificationConnection c, int trId, AllowSetting gtcSetting, int serverSerial) {
			
			Command response = new Command(Verb.Gtc, trId, serverSerial.ToString(), gtcSetting == AllowSetting.Allow ? "A" : "N");
			Server.Send( c, response );
		}
		
		protected virtual void SendBlp(NotificationConnection c, int trId, AllowSetting gtcSetting, int serverSerial) {
			
			Command response = new Command(Verb.Blp, trId, serverSerial.ToString(), gtcSetting == AllowSetting.Allow ? "AL" : "BL");
			Server.Send( c, response );
		}
		
		protected virtual void SendLst(NotificationConnection c, int trId, String name, IEnumerable<UserListEntry> userEnum, int serverSerial) {
			
			List<UserListEntry> list = userEnum as List<UserListEntry> ?? new List<UserListEntry>( userEnum );
			
			int cnt = list.Count;
			if( cnt == 0 ) {
				
				// if list is empty:
				// LST TrID LIST Ser# 0 0
				
				Command response = new Command(Verb.Lst, trId, name, serverSerial.ToString(), "0", "0" );
				Server.Send( c, response );
				
				return;
			}
			
			for(int i=0;i<cnt;i++) {
				UserListEntry entry = list[i];
				
				// LST <trid> <listName> <serial> <itemIdx> <cntItems> <userHandle> <userCustomName>
				
				Command response = new Command(Verb.Lst, trId, name, serverSerial.ToString(), (i+1).ToString(), cnt.ToString(), entry.User.UserHandle, entry.CustomName);
				Server.Send( c, response );
			}
			
		}
		
		protected virtual void SendIln(NotificationConnection c, int trId, User user) {
			
			// <<< ILN TrID Substate UserHandle FriendlyName
			
			Command iln = new Command(Verb.Iln, trId, user.Status.ToString().ToUpperInvariant(), user.UserHandle, user.FriendlyName);
			Server.Send( c, iln );
		}
		
		/// <summary>Sends an NLN to the specified connection, this NLN shows the current state of <param name="user"/>'s connection</summary>
		protected virtual void SendNln(NotificationConnection c, User user) {
			
			// now online:    <<< NLN Substate UserHandle FriendlyName
			// initial state: <<< ILN TrID Substate UserHandle FriendlyName
			// now offline:   <<< FLN UserHandle
			
			// I don't understand ILN just yet, so I'll just implement NLN and FLN
			// UPDATE: ILN is sent only when logging on or when adding someone to your FL (and you're allowed to see them)
			
			if( (user.Status & Status.Nln) == Status.Nln ) {
				
				Command nln = new Command(Verb.Nln, -1, user.Status.ToString().ToUpperInvariant(), user.UserHandle, user.FriendlyName);
				Server.Send( c, nln );
				
			} else {
				
				Command fln = new Command(Verb.Fln, -1, user.UserHandle);
				Server.Send( c, fln );
			}
			
		}
		
	#endregion
		
	#region Async
		
		public override void ASOut(NotificationConnection c, OutReason reason) {
			
			Command outCmd;
			if( reason != OutReason.None ) {
				
				outCmd = new Command(Verb.Out, -1, reason == OutReason.Oth ? "OTH" : "SSD");
				
			} else {
				
				outCmd = new Command(Verb.Out, -1);
			}
			
			Server.Send( c, outCmd );
			
		}
		
		public override void ASNotifyNln(NotificationConnection recipient, User updatedUser) {
			
			SendNln( recipient, updatedUser );
		}
		
		public override void ASNotifyIln(NotificationConnection recipient, User ilnUser) {
			
			SendIln( recipient, 0, ilnUser );
		}
		
		public override void ASNotifyFln(NotificationConnection recipient, User flnUser) {
			
			// this doesn't work if the FLN is meant to be sent in a case of blocking
			SendNln( recipient, flnUser );
		}
		
		public override void ASNotifyRng(UserListEntry caller, NotificationConnection recipient, SwitchboardInvitation invitation) {
			
			// <<< RNG <SessionID> <SwitchboardServerAddress> <SP> <AuthChallengeInfo> <CallingUserHandle> <CallingUserFriendlyName>
			
			invitation.Protocol = this.Name; // note this property's value is appropriate for the current protocol subclass
			
			SwitchboardSession session = invitation.Session;
			
			String sessionId = session.Id.ToStringInvariant();
			String sbAddr = session.Server.GetEndPointForClient( recipient.Socket.LocalEndPoint ).ToString();
			
			Command rng = new Command(Verb.Rng, -1, sessionId, sbAddr, "CKI", invitation.Key, caller.User.UserHandle, caller.CustomName);
			Server.Send( recipient, rng ); // I assume this won't be null for small-scale stuff
		}
		
		public override void ASNotifyAddRL(NotificationConnection recipient, UserListEntry newRLEntry) {
			
			// ADD TrID LIST ser# UserHandle CustomUserName
			// as this is async, TrID == 0
			
			String serial = recipient.User.Properties.Serial.ToStringInvariant();
			String userHandle = newRLEntry.User.UserHandle;
			String customName = newRLEntry.CustomName;
			
			Command add = new Command(Verb.Add, 0, "RL", serial, userHandle, customName);
			Server.Send( recipient, add );
		}
		
		public override void ASNotifyRemRL(NotificationConnection recipient, String removedRLEntryUserHandle) {
			
			// <<< REM 0 RL <serial> <userHandle>
			
			String serial = recipient.User.Properties.Serial.ToStringInvariant();
			String userHandle = removedRLEntryUserHandle;
			
			Command rem = new Command(Verb.Rem, 0, "RL", serial, userHandle);
			Server.Send( recipient, rem );
		}
		
		public override void ASNotifyRea(NotificationConnection recipient, User changedName) {
			
			// I wondered if it was an async REA command sent to the client
			// but I'll try an NLN
			
			//Command rea = new Command(Verb.Rea, 0, changedName.UserHandle, changedName.FriendlyName);;
			//Server.Send( recipient, rea );
			
			SendNln( recipient, changedName );
		}
		
	#endregion
		
	}
}
