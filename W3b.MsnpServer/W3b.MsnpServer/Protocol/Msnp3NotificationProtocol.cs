using System;
using System.Collections.Generic;
using System.Text;

namespace W3b.MsnpServer.Protocol {
	
	public class Msnp3NotificationProtocol : Msnp2NotificationProtocol {
		
		public Msnp3NotificationProtocol(NotificationServer server) : base("MSNP3", 3, server) {
		}
		
		protected Msnp3NotificationProtocol(String name, int pref, NotificationServer server) : base(name, pref, server) {
		}
		
		
		
	}
	
	public class Msnp4NotificationProtocol : Msnp3NotificationProtocol {
		
		public Msnp4NotificationProtocol(NotificationServer server) : base("MSNP4", 4, server) {
		}
		
		protected Msnp4NotificationProtocol(String name, int pref, NotificationServer server) : base(name, pref, server) {
		}
		
		
		
	}
	
}
