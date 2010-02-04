using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

namespace W3b.MsnpServer {
	
	public class CommandParser {
		
/* <command> := <cmd> [<trId>] [<param> ...]<nl>
 * <command> := <cmd> [<trId>] [<param> ...] <length><nl><binary>
 * 
 * <cmd>     := 3-character string, can be a 3-digit base10 number
 * <trId>    := integer, from 0 to 2^31-1
 * <param>   := arbitrary strings that do not contain whitespace
 * <nl>      := \r\n
 * <length>  := integer, from 0 to 2^31-1
 * <binary>  := arbitary binary string of length <length>, may contain \r\n and other substrings */
		
		public static CommandParser Parse(String everything) {
			
			using(StringReader rdr = new StringReader(everything)) {
				
				CommandParser cmd = new CommandParser();
				cmd.Raw = everything;
				cmd.Compile( rdr );
				return cmd;
			}
		}
		
		private void Compile(TextReader input) {
			
			Char[] commandName = new Char[3];
			if( input.Read( commandName, 0, 3 ) != 3 ) throw new SyntaxException("Command too short");
			
			Verb = new String( commandName );
			
			/////////////////////////////////////////
			
			List<String> parameters = new List<String>();
			bool hasPayload = false;
			
			StringBuilder sb = new StringBuilder();
			int nc; char c;
			while( (nc = input.Read()) != -1) {
				c = (char)nc;
				
				switch(c) {
					case '\r':
					case ' ':
						
						if( sb.Length > 0 ) parameters.Add( sb.ToString() );
						sb.Length = 0;
						break;
					
					case '\n':
						
						if( input.Peek() != -1 ) hasPayload = true;
						goto leaveLoop;
						
					default:
						sb.Append( c );
						break;
				}
				
			}
leaveLoop:
			
			Parameters = parameters.ToArray();
			
			/////////////////////////////////////////
			
			if( hasPayload ) {
				
				Payload = input.ReadToEnd();
			}
			
		}
		
#if NEVER
		public static Msnp2Command Parse(Byte[] message, int length) {
			
			String ascii = Encoding.ASCII.GetString( message, 0, length );
			
			String[] components = ascii.Split(' ');
			
			// TODO: Build a better parser
			// here's a simple grammar
/*
 * <command> := <cmd> [<trId>] [<param> ...]<nl>
 * <command> := <cmd> [<trId>] [<param> ...] <length><nl><binary>
 * 
 * <cmd>     := 3-character string, can be a 3-digit base10 number
 * <trId>    := integer, from 0 to 2^31-1
 * <param>   := arbitrary strings that do not contain whitespace
 * <nl>      := \r\n
 * <length>  := integer, from 0 to 2^31-1
 * <binary>  := arbitary binary string of length <length>, may contain \r\n and other substrings
 * 
 * */
			
			Msnp2Command ret = new Msnp2Command();
			
			//////////////////////////////////
			// 1: The command
			
			if( components[0].Length != 3 ) throw new Msnp2ProtocolException("Unexpected command: " + components[0] );
			
			int err;
			
			// if it's in the dictionary then set it
			if( _dictReverse.ContainsKey( components[0] ) ) {
				
				ret.Command = _dictReverse[ components[0] ];
				
			} else if( Int32.TryParse( components[0], out err ) ) {
				
				ret.Command = Msnp2Cmd.Err;
				ret.Error   = (Msnp2Error)err;
				
			} else throw new Msnp2ProtocolException("Unrecognised command: " + components[0] );
			
			//////////////////////////////////
			// 2: The Transaction ID
			
			int trim = 1;
			
			if( components.Length > 1 ) {
				
				int trId;
				if( Int32.TryParse( components[1], out trId ) ) {
					
					ret.TrId = trId;
					trim++;
				}
			}
			
			//////////////////////////////////
			// 3: The Rest of the Message
			
			if( components.Length > 2 ) {
				
				ret.Params = new String[ components.Length - 2 ]; // miss off [0] and [1]
				Array.Copy( components, 2, ret.Params, 0, components.Length - 2 );
			}
			
			//////////////////////////////////
			// 4: Identify Payload
			
			// TODO: There's a problem: what if the payload is larger than the receiving socket's buffer size?
			
			int payloadIdx = -1;
			int payloadLen = -1;
			
			for(int i=0;i<components.Length;i++) {
				
				// if one of the components /contains/ an \r\n and doesn't end with it AND isn't the last item
				// AND the bit of the component before \r\n is a number then there's a payload
				// and so components will need to be rebuilt
				
				String c = components[i];
				
				if( i == components.Length - 1 && c.EndsWith("\r\n") ) {
					
					ret.Params[ ret.Params.Length - 1 ] = c.Substring(0, c.Length - 2); // trim the \r\n
					
				} else
				if( i != components.Length - 1 && c.Contains("\r\n") && !c.EndsWith("\r\n") ) {
					
					String lengthPart = c.Substring(0, c.IndexOf('\r'));
					if( Int32.TryParse( lengthPart, out payloadLen ) ) {
						
						payloadIdx = i;
						break;
					}
					
				}
			}
			
			if( payloadIdx > -1 ) {
				
				// rebuild components
				String nonPayloadComponents = ascii.Substring( 0, ascii.Length - payloadLen );
				ret.Params  = nonPayloadComponents.Split(' ');
				ret.Payload = ascii.Substring( ascii.Length - nonPayloadComponents.Length );
			}
			
			return ret;
			
		}
#endif
		
		public String   Raw        { get; private set; }
		public String   Verb       { get; private set; }
		public String[] Parameters { get; private set; }
		public String   Payload    { get; private set; }
		
		public override String ToString() {
			
			StringBuilder sb = new StringBuilder();
			sb.Append( Verb );
			sb.Append(" ");
			sb.Append( Parameters.ToCsv() );
			
			if( Payload != null ) {
				sb.Append(";Payload = '");
				sb.Append( Payload );
				sb.Append('\'');
			}
			
			return sb.ToString();
			
		}
		
	}
	
}
