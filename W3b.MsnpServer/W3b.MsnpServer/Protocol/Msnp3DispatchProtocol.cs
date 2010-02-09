using System;
using System.Collections.Generic;
using System.Text;

namespace W3b.MsnpServer.Protocol {
	
	public class Msnp3DispatchProtocol : Msnp2DispatchProtocol {
		
		public Msnp3DispatchProtocol(DispatchServer server) : base("MSNP3", 3, server) {
		}
		
		protected Msnp3DispatchProtocol(String name, int pref, DispatchServer server) : base(name, pref, server) {
		}
		
		
		
	}
	
	public class Msnp4DispatchProtocol : Msnp3DispatchProtocol {
		
		public Msnp4DispatchProtocol(DispatchServer server) : base("MSNP4", 4, server) {
		}
		
		protected Msnp4DispatchProtocol(String name, int pref, DispatchServer server) : base(name, pref, server) {
		}
		
		
		
	}
}
