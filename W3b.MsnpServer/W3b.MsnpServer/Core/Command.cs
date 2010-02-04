using System;
using System.Text;

namespace W3b.MsnpServer {
	
	public class Command {
		
		private Command() {
			Verb    = null;
			Error   = Error.OK;
			TrId    = -1;
			Params  = new String[0];
		}
		
		public Command(String verb, int trId, params String[] parameters) {
			if( parameters == null ) throw new ArgumentNullException("parameters");
			Verb    = verb;
			TrId    = trId;
			Params  = parameters;
			foreach(String s in parameters) {
				if( s.Contains(" ") ) throw new ArgumentException("Parameters cannot contain spaces");
			}
		}
		
		public Command(Error error, int trId, params String[] parameters) : this(null, trId, parameters) {
			Error = error;
		}
		
		public static Command CreateWithPayload(String verb, int trId, String payload, params String[] parameters) {
			
			Command ret = new Command(verb, trId, parameters);
			ret.Payload = payload;
			return ret;
		}
		
		public static Command CreateFromBytes(Byte[] message, int length) {
			
			String ascii = Encoding.ASCII.GetString( message, 0, length );
			
			CommandParser p = CommandParser.Parse( ascii );
			
			Command ret = new Command();
			ret.Parsed = p;
			
			int errno;
			if( Int32.TryParse( p.Verb, out errno ) ) {
				
				ret.Verb  = null;
				ret.Error = (Error)errno;
				
			} else {
				
				ret.Verb  = p.Verb;
				ret.Error = Error.OK;
			}
			
			///////////////////////////////////
			
			if( p.Parameters.Length > 0 ) {
				
				int trid;
				if( Int32.TryParse( p.Parameters[0], out trid ) ) {
					ret.TrId = trid;
					if( p.Parameters.Length > 1 ) {
						ret.Params = new String[ p.Parameters.Length - 1 ];
						Array.Copy( p.Parameters, 1, ret.Params, 0, ret.Params.Length );
					}
				}
			}
			
			///////////////////////////////////
			
			if( p.Payload != null ) ret.Payload = p.Payload;
			
			return ret;
		}
		
		public override String ToString() {
			
			StringBuilder sb = new StringBuilder();
			if( Error == Error.OK ) sb.Append( Verb );
			else                    sb.Append( (int)Error );
			
			if( TrId  != -1 ) {
				sb.Append(" ");
				sb.Append( TrId );
			}
			
			for(int i=0;i<Params.Length;i++) {
				if( Params[i] == null ) continue;
				sb.Append(" ");
				sb.Append( Params[i] );
			}
			
			if( Payload == null ) {
				
				sb.Append("\r\n");
				
			} else {
				
				sb.Append(" ");
				sb.Append( Payload.Length );
				sb.Append("\r\n");
				sb.Append( Payload );
			}
			
			return sb.ToString();
		}
		
		public Byte[] ToByteArray() {
			
			return Encoding.ASCII.GetBytes( ToString() );
		}
		
		public String   Verb    { get; private set; }
		public Error    Error   { get; private set; }
		public String[] Params  { get; private set; }
		public Int32    TrId    { get; private set; }
		public String   Payload { get; private set; }
		
		public CommandParser Parsed { get; private set; }
		
	}
}
