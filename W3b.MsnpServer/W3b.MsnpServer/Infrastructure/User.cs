using System;
using System.Collections.Generic;
using System.Data.SQLite;

namespace W3b.MsnpServer {
	
	public class User {
		
#region User Instance
		
		private User(String userHandle, int serial) {
			
			UserHandle   = userHandle;
			
			Properties   = new UserProperties(this, serial);
			Profile      = new UserProfile();
			Status       = Status.Fln; // initially FLN (Offline)
		}
		
		public String UserHandle   { get; private set; }
		public String FriendlyName { get; set; }
		public String Password     { get; set; }
		
		public Status Status       { get; set; }
		
		public UserProperties Properties { get; private set; }
		public UserProfile    Profile    { get; private set; }
		
		public NotificationServer NotificationServer { get; set; }
		
#endregion
		
#region User Database
		
		/// <summary>In-memory cache of created users to prevent duplicate in-memory instances.</summary>
		private static Dictionary<String,User> _createdUsers = new Dictionary<String,User>();
		
		public static IEnumerable<User> AllUsers {
			get; private set;
		}
		
		public static User GetUser(String userHandle) {
			
			return _users.Get( userHandle.ToUpperInvariant() );
		}
		
		// to load the DB into memory...
		// 1. Create all the User instances, but don't populate their .Properties
		// 2. Then load all the Property lists and use the instantiated User instances to fill them
		
		private static Dictionary<String,User> _users;
		
		public static void LoadDatabase(String path) {
			
			using(SQLiteConnection conn = new SQLiteConnection("Data Source=" + path + ";")) {
				conn.Open();
				
				_users = LoadUsers( conn );
				
				AllUsers = _users.Values;
				
				LoadUserProperties( conn );
			}
			
		}
		
		public static void SaveDatabase(String path) {
			
			// TODO: Consider some form of database versioning?
			// so when the program quits, asks you to save changes to the DB or not?
			
			using(SQLiteConnection conn = new SQLiteConnection("Data Source=" + path + ";")) {
				conn.Open();
				
				// makes me wonder if it might just be easier to clear the database and commit everything from scratch, as introducing change tracking is just too painful
				// and longer "loading"/"saving" messages just impress the end-users :D
				
				String[] tables = new String[] {
					"AllowLists", "ReverseLists", "ForwardLists", "Users"
				};
				
				foreach(String table in tables) {
					SQLiteCommand cmd = new SQLiteCommand("DELETE FROM " + table, conn);
					cmd.ExecuteNonQuery();
				}
				
				//////////////////////////////////////////
				
				// and recommit from the in-memory representation
				// good thing the database doesn't use autoincr IDs
				
				foreach(User user in User.AllUsers) {
					
					SQLiteCommand cmd = new SQLiteCommand(
@"INSERT INTO Users (
	userHandle, friendlyName, password, propSerial, propBlp, propGtc, nameFirst, nameLast, locationCity, locationState, locationCountry
) VALUES (
	?,          ?,            ?,        ?,          ?,       ?,       ?,         ?,        ?,            ?,             ?)", conn);
					cmd.Parameters.AddWithValue(null, user.UserHandle);
					cmd.Parameters.AddWithValue(null, user.FriendlyName);
					cmd.Parameters.AddWithValue(null, user.Password);
					cmd.Parameters.AddWithValue(null, user.Properties.Serial);
					cmd.Parameters.AddWithValue(null, user.Properties.BlpSetting == AllowSetting.Allow ? "1" : "0" );
					cmd.Parameters.AddWithValue(null, user.Properties.GtcSetting == AllowSetting.Allow ? "1" : "0" );
					cmd.Parameters.AddWithValue(null, user.Profile.FirstName);
					cmd.Parameters.AddWithValue(null, user.Profile.LastName);
					cmd.Parameters.AddWithValue(null, user.Profile.City);
					cmd.Parameters.AddWithValue(null, user.Profile.State);
					cmd.Parameters.AddWithValue(null, user.Profile.Country);
					
					cmd.ExecuteNonQuery();
					
					
					
				}
				
				// TODO: User's lists
				
				
			}
			
		}
		
		private static Dictionary<String,User> LoadUsers(SQLiteConnection c) {
			
			Dictionary<String,User> ret = new Dictionary<String,User>();
			
			//                   0           1             2         3           4        5        6          7         8             9              10
			String sql = "SELECT userHandle, friendlyName, password, propSerial, propBlp, propGtc, nameFirst, nameLast, locationCity, locationState, locationCountry FROM Users";
			
			SQLiteCommand cmd = new SQLiteCommand(sql, c);
			SQLiteDataReader rdr = cmd.ExecuteReader();
			
			while(rdr.Read()) {
				
				User user = new User( rdr.GetString(0), rdr.GetInt32(3) );
				
				user.FriendlyName      = rdr.GetString(1);
				user.Password          = rdr.GetString(2);
				
				user.Properties.BlpSetting = (AllowSetting)rdr.GetInt32(4);
				user.Properties.GtcSetting = (AllowSetting)rdr.GetInt32(5);
				
				user.Profile.FirstName = rdr.IsDBNull( 6) ? null : rdr.GetString( 6);
				user.Profile.LastName  = rdr.IsDBNull( 7) ? null : rdr.GetString( 7);
				user.Profile.City      = rdr.IsDBNull( 8) ? null : rdr.GetString( 8);
				user.Profile.State     = rdr.IsDBNull( 9) ? null : rdr.GetString( 9);
				user.Profile.Country   = rdr.IsDBNull(10) ? null : rdr.GetString(10);
				
				ret.Add( user.UserHandle.ToUpperInvariant(), user );
			}
			
			return ret;
		}
		
		private static void LoadUserProperties(SQLiteConnection c) {
			
			foreach(User user in AllUsers) {
				
				// get the user's ForwardList
				// get the user's ReverseList (since customNames are cached, dynamic regeneration isn't possible)
				// and the user's AllowList
				
				/////////////////////////////////////
				// ForwardList
				String sql = "SELECT owner, target, customName FROM ForwardLists WHERE owner = ?";
				SQLiteCommand cmd = new SQLiteCommand( sql, c );
				cmd.Parameters.AddWithValue( null, user.UserHandle );
				
				using(SQLiteDataReader rdr = cmd.ExecuteReader()) {
					
					while(rdr.Read()) {
						
						User   target = GetUser( rdr.GetString(1) );
						String cusNom = rdr.IsDBNull(2) ? null : rdr.GetString(2);
						
						user.Properties.ForwardList.Add( target, cusNom );
					}
				}
				
				/////////////////////////////////////
				// ReverseList
				sql = "SELECT owner, target, customName FROM ReverseLists WHERE owner = ?";
				cmd = new SQLiteCommand( sql, c );
				cmd.Parameters.AddWithValue( null, user.UserHandle );
				
				using(SQLiteDataReader rdr = cmd.ExecuteReader()) {
					
					while(rdr.Read()) {
						
						User   target = GetUser( rdr.GetString(1) );
						String cusNom = rdr.IsDBNull(2) ? null : rdr.GetString(2);
						
						user.Properties.ReverseList.Add( target, cusNom );
					}
				}
				
				
				/////////////////////////////////////
				// AllowList
				sql = "SELECT owner, target, customName, isAllowed FROM AllowLists WHERE owner = ?";
				cmd = new SQLiteCommand( sql, c );
				cmd.Parameters.AddWithValue( null, user.UserHandle );
				
				using(SQLiteDataReader rdr = cmd.ExecuteReader()) {
					
					while(rdr.Read()) {
						
						User   target  = GetUser( rdr.GetString(1) );
						String cusName = rdr.GetString(2);
						bool   allowed = rdr.GetBoolean(3);
						
						UserPermissionListEntry entry = new UserPermissionListEntry( target, cusName, allowed ? AllowSetting.Allow : AllowSetting.Block );
						user.Properties.PermissList.Add( target, entry );
					}
				}
				
			}//foreach
			
			// it might be an idea to do an integrity check on the ForwardList/ReverseLists to ensure they're all in-line
			
		}
		
		
		
#endregion
		
	}
	
	
	public class UserProperties {
		
		// in MSNP there are 4 abstract lists per user (FL, RL, AL, and BL)
		// in reality there exist only two concrete lists and one virtual list
		// so in here: FL and AL are concrete, RL is virtual, and BL doesn't exist
		
		// ForwardList FL = List of users that self wants to see and talk to
		// AllowList   AL = List of users that self wants to be able to see and talk to (or BL: deny them that privilege)
		// ReverseList RL = List of users that have self on their ForwardList
		
		// because membership in AL and BL is mutually exclusive I have made it one list
		
		// UPDATE:
		// Due to the 'caching' behaviour of the customName on ALL FOUR lists, I can't use a virtual reverse list for the actual reverse list
		// it has to be a real list again
		
		public UserList       ForwardList { get; private set; }
		public UserList       ReverseList { get; private set; }
		public PermissionList PermissList { get; private set; }
		
		/// <summary>Determines how the client should behave when it discovers that a user is in its RL, but is not in its AL or BL. (Note that this occurs when a user has been added to another user's list, but has not been explicitly allowed or blocked)</summary>
		public AllowSetting GtcSetting { get; set; }
		/// <summary>Determines how the server should treat messages (MSG and RNG) from users. If the current setting is AL, messages from users who are not in BL will be delivered. If the current setting is BL, only messages from people who are in the AL will be delivered.</summary>
		public AllowSetting BlpSetting { get; set; }
		
		/// <summary>Similar to DNS Zone serial numbers in behaviour, this is incremented every time an update is made</summary>
		public Int32 Serial { get; set; }
		
		private User _owner;
		
		public UserProperties(User owner, int serial) {
			_owner      = owner;
			ForwardList = new UserList();
			ReverseList = new UserList();
			PermissList = new PermissionList();
			Serial      = serial;
		}
		
		public AllowSetting GetResultantAS(User user) {
			
			UserPermissionListEntry explicitEntry;
			
			if( PermissList.TryGetValue( user, out explicitEntry ) ) {
				
				return explicitEntry.Allowed;
			} else {
				
				return GtcSetting;
			}
		}
		
		/// <summary>Returns all Users who have added this user to their ForwardLists. This is not the explicit ReverseList. This is an O(n^2) operation.</summary>
		public IEnumerable<User> VirtualReverseList {
			get {
				
				foreach(User otherUser in User.AllUsers) {
					
					if( otherUser.Properties.ForwardList.ContainsKey( _owner ) ) yield return otherUser;
				}
			}
		}
		
		/// <summary>Returns all Users in this user's Forward List who have added this user to their allow list (or have an Allow GTC setting).</summary>
		public IEnumerable<User> VirtualAllowedForwardList {
			get {
				
				foreach(User forwardUser in ForwardList.Keys) {
					
					UserListEntry entry = ForwardList[forwardUser];
					if( entry.User.Properties.GetResultantAS( _owner ) == AllowSetting.Allow ) yield return forwardUser;
				}
				
			}
		}
		
		/// <summary>Returns all Users who have added this user to their ForwardLists AND are allowed to receive info from this user.</summary>
		public IEnumerable<User> VirtualAllowedReverseList {
			get {
				
				foreach(User otherUser in VirtualReverseList) {
					
					if( GetResultantAS( otherUser ) == AllowSetting.Allow ) yield return otherUser;
					
				}//foreach
			}//get
		}//prop
		
		public IEnumerable<UserListEntry> VirtualAllowList {
			get {
				
				foreach(UserPermissionListEntry entry in PermissList.Values) {
					
					if( entry.Allowed == AllowSetting.Allow ) yield return entry;
				}
				
			}
		}
		
		public IEnumerable<UserListEntry> VirtualBlockList {
			get {
				
				foreach(UserPermissionListEntry entry in PermissList.Values) {
					
					if( entry.Allowed == AllowSetting.Block ) yield return entry;
				}
				
			}
		}
		
	}
	
	public class UserList : Dictionary<User,UserListEntry> {
		
		public UserListEntry Add(User user, String customName) {
			UserListEntry e = new UserListEntry( user, customName );
			base.Add( user, e );
			return e;
		}
		
	}
	
	public class PermissionList : Dictionary<User,UserPermissionListEntry> {
		
		public void Add(User user, String customName, AllowSetting allowed) {
			base.Add( user, new UserPermissionListEntry( user, customName, allowed) );
		}
		
	}
	
	public class UserListEntry {
		public UserListEntry(User user, String customName) {
			User       = user;
			CustomName = customName;
		}
		public User   User       { get; set; }
		public String CustomName { get; set; }
	}
	
	public class UserPermissionListEntry : UserListEntry {
		public UserPermissionListEntry(User user, String customName, AllowSetting allowed) : base(user, customName) {
			Allowed    = allowed;
		}
		public AllowSetting Allowed { get; set; }
	}
	
	public class UserProfile {
		
		public String FirstName { get; set; }
		public String LastName  { get; set; }
		public String City      { get; set; }
		public String State     { get; set; }
		public String Country   { get; set; }
	}
	
	public enum AllowSetting {
		Allow = 1,
		Block = 0
	}
	
}
