using System;
using System.Collections.Generic;
using System.Net.Sockets;
using W3b.MsnpServer.Protocol;

using Verb = W3b.MsnpServer.Protocol.Msnp2NotificationVerbs;

namespace W3b.MsnpServer {
	
	public class NotificationConnection : ClientConnection {
		
		public NotificationConnection(int bufferLength, Socket socket) : base(bufferLength, socket) {
		}
		
		public AuthDetails          AuthDetails    { get; set; }
		public NotificationProtocol Protocol       { get; set; }
		public User                 User           { get; set; }
		public Boolean              SentInitialChg { get; set; }
		
	}
	
	public sealed class NotificationServer : MsnpServer<NotificationConnection> {
		
#region Singleton
		private static NotificationServer _instance;
		public static NotificationServer Instance {
			get { return _instance ?? (_instance = new NotificationServer()); }
		}
#endregion
		
		private List<NotificationProtocol> _protocols = new List<NotificationProtocol>();
		
		private NotificationServer() : base("NS", ConsoleColor.Green, 1864) {
			
			_protocols = new List<NotificationProtocol>() {
				new Msnp2NotificationProtocol(this),
				new Msnp3NotificationProtocol(this),
				new Msnp4NotificationProtocol(this)
			};
			_protocols.Sort( (px, py) => py.Pref.CompareTo( px.Pref ) ); // descending sort order
		}
		
		protected override ClientConnection CreateClientConnection(int bufferLength, Socket socket) {
			
			return new NotificationConnection(bufferLength, socket);
		}
		
		protected override void OnDataReceived(NotificationConnection c) {
			
			Command cmd = GetCommand( c );
			if( cmd == null ) return;
			
			if(cmd.Verb == Verb.Ver) {
				// negociate protocol version before dc.Protocol can be set
				NegotiateVersion( c, cmd );
				return;
			}
			
			c.Protocol.HandleCommand( c, cmd );
		}
		
		private void NegotiateVersion(NotificationConnection c, Command cmd) {
			
			foreach(NotificationProtocol protocol in _protocols) { // this is sorted by preference
				
				if( cmd.Params.Contains( protocol.Name ) ) {
					
					Command response = new Command(Verb.Ver, cmd.TrId, protocol.Name);
					Send( c, response );
					
					c.Protocol = protocol;
					
					break;
				}
				
			}
			
		}
		
		public override void CloseConnection(NotificationConnection c) {
			
			if( c.User != null ) {
				
				c.User.Status = Status.Fln;
				c.User.NotificationServer = null;
			}
			
			if( IsStarted ) {
				
				c.Protocol.ASOut( c, OutReason.None );
				
			} else { // if the server is stopped, which means this is being sent because the server is being shut down
				
				c.Protocol.ASOut( c, OutReason.Ssd );
			}
			
			// TODO: Notify that user's contacts the user has gone offline
			// assuming the user hasn't already changed their status
			
		}
		
		private NotificationConnection GetConnectionForUser(User user) {
			
			ClientConnection[] connections = FindConections( c => ((NotificationConnection)c).User == user );
			
			return connections.Length == 0 ? null : (NotificationConnection)connections[0];
		}
		
#region Operations
		
		public void UserStatusChanged(User user, Status newStatus) {
			
			user.Status = newStatus;
			
			// Notify the client's (reverse list JOIN allow list)
			// oh, and these users need to be online too
			
			foreach(User reverseAllowedUser in user.Properties.VirtualAllowedReverseList) {
				
				NotificationConnection c = GetConnectionForUser( reverseAllowedUser );
				if( c != null && reverseAllowedUser.Status != Status.Fln ) { // the user is online, and a sanity check they aren't FLN
					
					if( user.Status.AppearOnline() ) {
						
						c.Protocol.ASNotifyNln( c, user );
						
					} else {
						
						c.Protocol.ASNotifyFln( c, user );
						
					}//if
					
				}//if
			}//foreach
			
		}
		
		public void UserRenamed(User user, String newName) {
			
			user.FriendlyName = newName;
			
			// notify the user's contacts
			
			if( !user.Status.AppearOnline() ) return;
			
			Command asyncRea = new Command(Verb.Rea, 0, "0", user.UserHandle, newName);
			
			foreach(User notifyThisUser in user.Properties.VirtualAllowedReverseList) {
				
				NotificationConnection usersC = GetConnectionForUser( notifyThisUser );
				if( usersC != null && notifyThisUser.Status != Status.Fln ) {
					
					usersC.Protocol.ASNotifyRea( usersC, user );
				}
				
			}
			
		}
		
		public void UserFLRenamed(User owner, User target, String newName) {
			
			owner.Properties.ForwardList[ target ].CustomName = newName;
		}
		
		/// <summary>Adds the <paramref name="target"/> user to <paramref name="listOwner"/>'s <paramref name="list"/> list, then sends out appropriate notifications if necessary.</summary>
		public AddListResult AddToList(User listOwner, String listName, User target, String customName) {
			
			lock( listOwner.Properties )
			lock( target.Properties ) {
			
			switch(listName) {
				case "FL":
					
					// Add to the Forward List
					// Add to the Reverse List of the target
					// if the target is online send an async ADD notification to that user
					
					// NOTE: adding to the Allow List is explicitly done by the client, there is no implicit AL add
					
					listOwner.Properties.ForwardList.Add( target, customName );
					listOwner.Properties.Serial++;
					
					UserListEntry rlEntry = 
					target.Properties.ReverseList.Add( listOwner, listOwner.FriendlyName );
					target.Properties.Serial++;
					
					NotificationConnection targetC1 = GetConnectionForUser( target );
					if( targetC1 != null && target.Status != Status.Fln ) {
						
						targetC1.Protocol.ASNotifyAddRL( targetC1, rlEntry );
					}
					
					if( target.Status.AppearOnline() && target.Properties.GetResultantAS( listOwner ) == AllowSetting.Allow ) {
						// if listOwner is allowed to see target
						
						NotificationConnection listOwnerC = GetConnectionForUser( listOwner );
						listOwnerC.Protocol.ASNotifyIln( listOwnerC, target );
					}
					
					// TODO: If the added user is online, send ILN
					
					return AddListResult.Success;
				
				case "AL":
					
					// Check user isn't already on the BL, you can't be on both
					// Then add to the AL
					// If the target is online and has this user in their forward list, send ILN to the target
					
					UserPermissionListEntry alEntry;
					if( listOwner.Properties.PermissList.TryGetValue( target, out alEntry ) ) {
						
						return alEntry.Allowed == AllowSetting.Allow ? AddListResult.Success : AddListResult.MutexBL;
						
					} else {
						
						alEntry = new UserPermissionListEntry( target, customName, AllowSetting.Allow );
						
						listOwner.Properties.PermissList.Add( target, alEntry );
						listOwner.Properties.Serial++;
						
						if( listOwner.Status.AppearOnline() && target.Properties.ForwardList.ContainsKey( listOwner ) ) {
							
							NotificationConnection targetC2 = GetConnectionForUser( target );
							if( targetC2 != null && target.Status != Status.Fln ) {
								
								targetC2.Protocol.ASNotifyNln( targetC2, listOwner );
							}
						}
						
						return AddListResult.Success;
					}
					
				case "BL":
					
					// Same as AL, but with some tweaks for the different list
					
					UserPermissionListEntry blEntry;
					if( listOwner.Properties.PermissList.TryGetValue( target, out blEntry ) ) {
						
						return blEntry.Allowed == AllowSetting.Block ? AddListResult.Success : AddListResult.MutexAL;
						
					} else {
						
						blEntry = new UserPermissionListEntry( target, customName, AllowSetting.Block );
						
						listOwner.Properties.PermissList.Add( target, blEntry );
						listOwner.Properties.Serial++;
						
						if( target.Properties.ForwardList.ContainsKey( listOwner ) ) {
							
							NotificationConnection c2 = GetConnectionForUser( target );
							if( listOwner.Status.AppearOnline() && c2 != null && target.Status != Status.Fln ) {
								
								c2.Protocol.ASNotifyFln( c2, listOwner );
							}
						}
						
						return AddListResult.Success;
					}
				
				case "RL":
					return AddListResult.CannotAddtoRL;
				
				default:
					return AddListResult.UnknownList;
				
			}//switch
			
			}//lock
		}//AddToList
		
		public RemoveListResult RemoveFromList(User listOwner, String listName, User target) {
			
			lock( listOwner.Properties )
			lock( target.Properties ) {
			
			switch(listName) {
				case "FL":
					
					if(!listOwner.Properties.ForwardList.Remove( target )) return RemoveListResult.UserNotInList;
					listOwner.Properties.Serial++;
					
					if( target.Properties.ReverseList.Remove( listOwner ) )
						target.Properties.Serial++;
					
					NotificationConnection targetC1 = GetConnectionForUser( target );
					if( targetC1 != null && target.Status != Status.Fln ) {
						
						// hold on, is the GTC/BLP setting involved at this point?
						targetC1.Protocol.ASNotifyRemRL( targetC1, listOwner.UserHandle );
					}
					
					return RemoveListResult.Success;
					
				case "AL":
					
					UserPermissionListEntry entryAl;
					if( !listOwner.Properties.PermissList.TryGetValue( target, out entryAl ) ) return RemoveListResult.UserNotInList;
					if( entryAl.Allowed == AllowSetting.Block ) return RemoveListResult.UserNotInList;
					
					listOwner.Properties.PermissList.Remove( target );
					listOwner.Properties.Serial++;
					
					if( listOwner.Status.AppearOnline() && target.Properties.ForwardList.ContainsKey( listOwner ) ) {
						
						NotificationConnection targetC2 = GetConnectionForUser( target );
						if( targetC2 != null && target.Status != Status.Fln ) {
							
							// BLP/GTC is involved here too; a user might still be allowed to see status, even if not on the AL
							targetC2.Protocol.ASNotifyFln( targetC2, listOwner );
						}
					}
					
					return RemoveListResult.Success;
					
				case "BL":
					
					UserPermissionListEntry entryBl;
					if( !listOwner.Properties.PermissList.TryGetValue( target, out entryBl ) ) return RemoveListResult.UserNotInList;
					if( entryBl.Allowed == AllowSetting.Allow ) return RemoveListResult.UserNotInList;
					
					listOwner.Properties.PermissList.Remove( target );
					listOwner.Properties.Serial++;
					
					if( listOwner.Status.AppearOnline() && target.Properties.ForwardList.ContainsKey( listOwner ) ) {
						
						NotificationConnection targetC2 = GetConnectionForUser( target );
						if( targetC2 != null && target.Status != Status.Fln ) {
							
							// BLP/GTC is involved here too; a user might still be allowed to see status, even if not on the AL
							targetC2.Protocol.ASNotifyNln( targetC2, listOwner );
						}
					}
					
					return RemoveListResult.Success;
					
				case "RL":
					return RemoveListResult.CannotRemoveFromRL;
				default:
					return RemoveListResult.UnknownList;
				
			}//switch
			}//lock
		}
		
		public void ASNotifyNln(User nlnUser, User recipient) {
			
			NotificationConnection connection = GetConnectionForUser( recipient );
			if( connection != null ) connection.Protocol.ASNotifyNln( connection,  nlnUser );
		}
		
		public void ASNotifyRng(User caller, User recipient, SwitchboardInvitation invite) {
			
			// should ASNotifyRng be using UserListEntry? Why can't it use .FriendlyName, why must it use .CustomName, which must be recalled from somewhere?
			UserListEntry callerEntry = new UserListEntry( caller, caller.FriendlyName );
			
			NotificationConnection connection = GetConnectionForUser( recipient );
			if( connection != null ) connection.Protocol.ASNotifyRng( callerEntry, connection, invite );
		}
		
		public void ASNotifyAddRL(User adder, User recipient) {
			
			// again, should this be using UserListEntry?
			UserListEntry adderEntry = new UserListEntry( adder, adder.FriendlyName );
			
			NotificationConnection connection = GetConnectionForUser( recipient );
			if( connection != null ) connection.Protocol.ASNotifyAddRL( connection, adderEntry );
		}
		
#endregion
		
	}
	
	public enum AddListResult {
		Success,
		MutexAL,
		MutexBL,
		CannotAddtoRL,
		UnknownList
	}
	
	public enum RemoveListResult {
		Success,
		CannotRemoveFromRL,
		UserNotInList,
		UnknownList
	}
	
}
