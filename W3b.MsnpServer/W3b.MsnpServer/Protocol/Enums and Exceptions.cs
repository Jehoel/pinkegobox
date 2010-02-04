using System;
using System.Collections.Generic;
using S = System.Runtime.Serialization;

namespace W3b.MsnpServer {
	
	public enum Error {
		OK                    =   0,
		SyntaxError           = 200,
		InvalidParameter      = 201,
		InvalidUser           = 205,
		FqdnMissing           = 206,
		AlreadyLogin          = 207,
		InvalidUsername       = 208,
		InvalidFriendlyName   = 209,
		ListFull              = 210,
		AlreadyThere          = 215,
		NotOnList             = 216,
		AlreadyintheMode      = 218,
		AlreadyinOppositeList = 219,
		SwitchboardFailed     = 280,
		NotifyXfrFailed       = 281,
		RequiredFieldsMissing = 300,
		NotLoggedIn           = 302,
		InternalServer        = 500,
		DbServer              = 501,
		FileOperation         = 510,
		MemoryAlloc           = 520,
		ServerBusy            = 600,
		ServerUnavailable     = 601,
		PeerNsDown            = 602,
		DbConnect             = 603,
		ServerGoingDown       = 604,
		CreateConnection      = 707,
		BlockingWrite         = 711,
		SessionOverload       = 712,
		UserTooActive         = 713,
		TooManySessions       = 714,
		NotExpected           = 715,
		BadFriendFile         = 717,
		AuthenticationFailed  = 911,
		NotAllowedwhenOffline = 913,
		NotAcceptingNewUsers  = 920
	}
	
/*	public enum Verb {
		None,
		Err, // Error, not an actual command per-se
	// Dispatch (and implicitly, Notification)
		Inf,
		Ver,
		Cvr,
		Cvq,
		Xfr,
		Out, // All Servers
		Usr, // All Servers
	// Notification
		Add,
		Blp,
		Chg,
		Fln,
		Gtc,
		Iln,
		Lst,
		Nln,
		Rea,
		Rem,
		Rng,
		Syn, // Dispatch too, as Dispatch servers can serve as Notification servers in MSNP2
		Url,
	// Switchboard
		Ack,
		Ans,
		Bye,
		Cal,
		Iro,
		Joi,
		Msg,
		Nak
	} */
	
	public enum Status {
		// all 'nln'  states have 1 bit set
		// all 'away' states have 2 bit set
		// all 'busy' states have 4 bit set
		Fln = 00, // Offline - 00000
		Hdn = 02, // Hidden  - 00010
		
		Nln = 01, // Online  - 00001
		
		Bsy = 05, // Busy    - 00101
		Phn = 13, // Phone   - 01101
		
		Awy = 03, // Away    - 00011
		Idl = 11, // Idle    - 01011
		Brb = 19, // BRB     - 10011
		Lun = 27  // Lunch   - 11011
	}
	
	public enum OutReason {
		None,
		Oth,
		Ssd
	}
	
	/// <summary>Contains reverse lookup dictionaries for MSNP enumerations</summary>
	public static class Enumerations {
		
//		private static Dictionary<String,Verb>   _verbs = BuildDict<Verb>();
		private static Dictionary<String,Status> _state = BuildDict<Status>();
		private static Dictionary<String,Error>  _error = BuildDict<Error>();
		
		private static Dictionary<String,TValue> BuildDict<TValue>() {
			
			Dictionary<String,TValue> ret = new Dictionary<string,TValue>();
			
			String[] names  =        Enum.GetNames(typeof(TValue));
			int[]    values = (int[])Enum.GetValues(typeof(TValue));
			
			for(int i=0;i<names.Length;i++)
				ret.Add( names[i].ToUpperInvariant(), (TValue)(Object)values[i] ); // double-cast, why is this necessary in C#'s generic type support?
			
			return ret;
		}
		
//		public static Verb GetVerb(String representation) {
//			
//			if( _verbs.ContainsKey( representation ) ) return _verbs[ representation ];
//			return Verb.None;
//		}
		
		public static Status GetStatus(String representation) {
			
			return _state[ representation ];
		}
		
		public static Error GetError(String representation) {
			
			return _error[ representation ];
		}
		
	}
	
	[Serializable]
	public class ProtocolException : Exception {
		
		public ProtocolException() {
		}
		
		public ProtocolException(String message) : base(message) {
		}
		
		public ProtocolException(String humanMessage, String command) : base(humanMessage) {
			CommandSent = command;
		}
		
		public ProtocolException(string message, Exception inner) : base(message, inner) {
		}
		
		protected ProtocolException(S.SerializationInfo info, S.StreamingContext context) : base(info, context) {
		}
		
		public String CommandSent {
			get; private set;
		}
	}
	
	[Serializable]
	public class SyntaxException : ProtocolException {
		
		public SyntaxException() {
		}
		
		public SyntaxException(string message) : base(message) {
		}
		
		public SyntaxException(string message, Exception inner) : base(message, inner) {
		}
		
		protected SyntaxException(S.SerializationInfo info, S.StreamingContext context) : base(info, context) {
		}
	}
	
}
