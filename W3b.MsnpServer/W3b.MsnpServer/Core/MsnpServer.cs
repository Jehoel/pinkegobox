using System;
using System.Text;
using System.Net.Sockets;

namespace W3b.MsnpServer {
	
	public abstract class MsnpServer<TConnection> : Server where TConnection : ClientConnection {
		
		protected MsnpServer(String name, ConsoleColor color, int port) : base(name, port) {
			ConsoleColor = color;
		}
		
		public ConsoleColor ConsoleColor { get; private set; }
		
		protected Command GetCommand(TConnection c) {
			
			Command cmd = Command.CreateFromBytes( c.Buffer, c.Length );
			
			LogCS( c, cmd );
			return cmd;
		}
		
		protected override void OnDataReceived(ClientConnection connection) {
			OnDataReceived( connection as TConnection );
		}
		
		protected abstract void OnDataReceived(TConnection c);
		
		protected override void CloseConnection(ClientConnection connection) {
			CloseConnection( connection as TConnection );
			
			base.CloseConnection( connection );
		}
		
		public abstract void CloseConnection(TConnection c);
		
		public void Send(TConnection c, Command cmd) {
			
			lock( c ) {
				
				try {
					
					LogSC( c, cmd );
					
					c.Socket.Send( cmd.ToByteArray() );
					
				} catch(SocketException sex) {
					
					LogSC(c, "Send SocketException: " + sex.Message, false );
					
				} catch(ObjectDisposedException dex) {
					
					LogSC(c, "Send ObjectDisposedException: " + dex.Message, false );
				}
			}
		}
		
#region Logging
		
		private static String RN = @"\r\n";
		private Object _consoleLock = new Object();
		
		private void Log(bool direction, ClientConnection c, String message, bool transform) {
			
			lock( _consoleLock ) {
				
				Console.Write("[{0:HH:mm:ss}] ", DateTime.Now );
				
				LogColor(ConsoleColor, c.Socket.RemoteEndPoint);
				
				Console.Write(" " + c.Id );
				
				LogColor(direction ? ConsoleColor.Blue : ConsoleColor.Red, direction ? " >>> " : " <<< ");
				
				LogColor(ConsoleColor, Name + " ");
				
				message = transform ? TransformWhitespace( message ) : message;
				
				LogMessageRN( message );
				
				Console.WriteLine();
				
			}
		}
		
		private static void LogColor(ConsoleColor color, Object obj) {
			
			Console.ForegroundColor = color;
			Console.Write( obj );
			Console.ResetColor();
		}
		
		private static void LogMessageRN(String message) {
			
			// read through the string, adding to an SB buffer (to avoid constant Console.Writes) until an RN is encountered
			
			using(System.IO.StringReader rdr = new System.IO.StringReader(message)) {
				
				StringBuilder sb = new StringBuilder();
				int nc; char c;
				while( (nc = rdr.Read()) != -1 ) {
					c = (char)nc;
					
					if( c == '\\' && rdr.Peek() == 'r' ) {
						
						c = (char)rdr.Read(); // c == r
						c = (char)rdr.Read(); // c == \
						if( c == '\\' && rdr.Peek() == 'n' ) {
							
							rdr.Read(); // read the 'n' char
							
							Console.Write( sb.ToString() );
							sb.Length = 0;
							Console.ForegroundColor = ConsoleColor.DarkGray;
							Console.Write( RN );
							Console.ResetColor();
							
						} else {
							
							sb.Append("\\r");
							sb.Append( c ); // c is anything, it could be \\ if peek'd wasn't \n
						}
						
					} else {
						
						sb.Append( c );
					}
					
				}
				
				if( sb.Length > 0 ) Console.Write( sb.ToString() );
				
			}
			
		}
		
		public void LogSC(TConnection c, String message, bool transform) {
			
			Log( false, c, message, transform );
		}
		
		public void LogSC(TConnection c, Command cmd) {
			
			LogSC( c, cmd.ToString(), true );
		}
		
		public void LogCS(TConnection c, String message, bool transform) {
			
			Log( true, c, message, transform );
		}
		
		public void LogCS(TConnection c, Command cmd) {
			
			LogCS( c, cmd.ToString(), true );
		}
		
		private static String TransformWhitespace(String s) {
			
			StringBuilder sb = new StringBuilder(s);
			sb.Replace("\r", "\\r");
			sb.Replace("\n", "\\n");
			sb.Replace("\t", "\\t");
			
			return sb.ToString();
		}
		
#endregion
		
	}
}
