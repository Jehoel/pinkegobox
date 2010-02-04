using System;

namespace W3b.MsnpServer.Protocol {
	
	public abstract class Protocol<TConnection> where TConnection : ClientConnection {
		
		protected Protocol(String name, int preference) {
			Name = name;
			Pref = preference;
		}
		
		public abstract void HandleCommand(TConnection c, Command cmd);
		
		public String Name { get; private set; }
		public Int32  Pref { get; private set; }
		
	}
	
	public abstract class DispatchProtocol : Protocol<DispatchConnection> {
		
		protected DispatchProtocol(String name, int pref, DispatchServer server) : base(name, pref) {
			Server = server;
		}
		
		protected DispatchServer Server {
			get; private set;
		}
		
		protected virtual void HandleUnrecognised(DispatchConnection c, Command cmd) {
			
			Server.LogCS(c, "Unrecognised DS Command", false);
			
			Command response = new Command(Error.SyntaxError, cmd.TrId);
			Server.Send( c, response );
		}
		
	}
	
	public abstract class NotificationProtocol : Protocol<NotificationConnection> {
		
		protected NotificationProtocol(String name, int pref, NotificationServer server) : base(name, pref) {
			Server = server;
		}
		
		protected NotificationServer Server {
			get; private set;
		}
		
		protected virtual void HandleUnrecognised(NotificationConnection c, Command cmd) {
			
			Server.LogCS(c, "Unrecognised NS Command", false);
			
			Command response = new Command(Error.SyntaxError, cmd.TrId);
			Server.Send( c, response );
		}
		
		///////////////////////////
		// Async Methods
		
		public abstract void ASOut(NotificationConnection c, OutReason reason);
		
		public abstract void ASNotifyNln(NotificationConnection c, User updatedUser);
		public abstract void ASNotifyIln(NotificationConnection recipient, User ilnUser);
		public abstract void ASNotifyFln(NotificationConnection recipient, User flnUser);
		
		public abstract void ASNotifyRng(UserListEntry caller, NotificationConnection recipient, SwitchboardSession session);
		
		public abstract void ASNotifyAddRL(NotificationConnection recipient, UserListEntry newRLEntry);
		public abstract void ASNotifyRemRL(NotificationConnection recipient, UserListEntry removedRLEntry);
		
		public abstract void ASNotifyRea(NotificationConnection recipient, User updatedUser);
		
	}
	
	public abstract class SwitchboardProtocol : Protocol<SwitchboardConnection> {
		
		protected SwitchboardProtocol(String name, int pref, SwitchboardServer server) : base(name, pref) {
			Server = server;
		}
		
		protected SwitchboardServer Server {
			get; private set;
		}
		
		protected virtual void HandleUnrecognised(SwitchboardConnection c, Command cmd) {
			
			Server.LogCS(c, "Unrecognised SB Command", false);
			
			Command response = new Command(Error.SyntaxError, cmd.TrId);
			Server.Send( c, response );
		}
		
	}
	
}
