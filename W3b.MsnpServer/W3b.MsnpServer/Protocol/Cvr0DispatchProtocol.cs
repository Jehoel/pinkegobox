using System;
using System.Collections.Generic;
using System.Text;

using Verb = W3b.MsnpServer.Protocol.Cvr0DispatchVerbs;

namespace W3b.MsnpServer.Protocol {
	
	public static class Cvr0DispatchVerbs {
		public const String None = "";
		public const String Err = null;
		
		public const String Cvr = "CVR";
		public const String Cvq = "CVQ";
	}
	
	public class Cvr0DispatchProtocol : DispatchProtocol {
		
		public Cvr0DispatchProtocol(DispatchServer server) : base("CVR0", 0, server) {
		}
		
		protected Cvr0DispatchProtocol(String name, int pref, DispatchServer server) : base(name, pref, server) {
		}
		
		public override void HandleCommand(DispatchConnection c, Command cmd) {
			
			switch(cmd.Verb) {
				case Verb.Cvq:
					HandleCvq(c, cmd);
					break;
				case Verb.Cvr:
					HandleCvr(c, cmd);
					break;
				default:
					
					
					
					break;
			}
			
		}
		
		protected virtual void HandleCvq(DispatchConnection c, Command cmd) {
			
			Msnp2Common.HandleCvq( Server, c, cmd );
		}
		
		protected virtual void HandleCvr(DispatchConnection c, Command cmd) {
			
			Msnp2Common.HandleCvr( Server, c, cmd );
		}
		
	}
}
