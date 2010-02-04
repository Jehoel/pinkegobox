using System;
using System.Net;
using System.Net.Sockets;
using System.Collections.Generic;

namespace W3b.MsnpServer {
	
	public abstract class Server {
		
		private Socket                              _serverSocket;
		private Dictionary<Socket,ClientConnection> _connections;
		
		protected Server(String name, int port) {
			
			EndPoint = new IPEndPoint(IPAddress.Any, port);
			
			_connections = new Dictionary<Socket,ClientConnection>();
			
			Name = name;
		}
		
		public IPEndPoint EndPoint {
			get; private set;
		}
		
		public bool IsStarted {
			get; private set;
		}
		
		public String Name {
			get; private set;
		}
		
		public void Start() {
			
			if( IsStarted ) throw new InvalidOperationException("Server is already listening");
			
			_serverSocket = new Socket( EndPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp );
			_serverSocket.Bind( EndPoint );
			
			_serverSocket.Listen(Int32.MaxValue);
			
			_serverSocket.BeginAccept( OnClientConnect, null );
			
			IsStarted = true;
		}
		
		public void Stop() {
			
			if( !IsStarted ) throw new InvalidOperationException("Cannot stop an already stopped server.");
			IsStarted = false;
			
			// apparently you can't have an existing ServerSocket and simply tell it to stop Listening, it's a terminal state
//			_serverSocket.Shutdown(SocketShutdown.Both);
			_serverSocket.Close();
			
			ClientConnection[] connections;
			lock( _connectionsLock ) {
			
				// copy to a new array because CloseConnection modifies the _connections list
				connections = new ClientConnection[ _connections.Values.Count ];
				_connections.Values.CopyTo( connections, 0 );
			}
			
			foreach(ClientConnection conn in connections)
				CloseConnection( conn );
			
		}
		
		protected abstract ClientConnection CreateClientConnection(int bufferLength, Socket socket);
		
		private void OnClientConnect(IAsyncResult result) {
			
			if( !IsStarted ) return; // _serverSocket is disposed and continuing will throw an error. Apparently this callback is called when the _serverSocket closes
			
			Socket clientSocket = _serverSocket.EndAccept( result );
			
			//////////////////////////////////////////
			// Set up receiving data on the socket
			
			ClientConnection conn = CreateClientConnection( 1024, clientSocket ); // HACK: What happens if the client sends more data than fits into the buffer?
			
			clientSocket.BeginReceive( conn.Buffer, 0, conn.Buffer.Length, SocketFlags.None, OnDataReceived, conn);
			
			lock( _connectionsLock )
				_connections.Add( clientSocket, conn );
			
			// resume accepting connections
			if( IsStarted ) _serverSocket.BeginAccept( OnClientConnect, null );
			
		}
		
		private void OnDataReceived(IAsyncResult result) {
			
			System.Threading.Thread.Sleep(100); // for ease of reading the console
			
			ClientConnection conn = (ClientConnection)result.AsyncState;
			conn.Length = conn.Socket.EndReceive( result );
			
			if( conn.Length > 0 ) {
				
				OnDataReceived( conn );
				
				
				if( conn.Socket.Connected ) {
					
					try {
						
						conn.Socket.BeginReceive( conn.Buffer, 0, conn.Buffer.Length, SocketFlags.None, OnDataReceived, conn);
						
					} catch(SocketException) {
						
						// TODO: Do something?
					}
				}
				
			} else {
				
				DisposeConnection( conn );
			}
			
		}
		
		protected abstract void OnDataReceived(ClientConnection connection);
		
		public IPEndPoint GetEndPointForClient(EndPoint clientCurrentLocalEP) {
			
			IPEndPoint localIP = (IPEndPoint)clientCurrentLocalEP;
			
			return new IPEndPoint( localIP.Address, EndPoint.Port );
		}
		
	#region Session/Connection Management
		
		private Object _connectionsLock = new Object();
		
		protected virtual void CloseConnection(ClientConnection connection) {
			
			DisposeConnection( connection );
		}
		
		private void DisposeConnection(ClientConnection connection) {
			
			lock( _connectionsLock ) {
				
				connection.Socket.Shutdown(SocketShutdown.Both);
				connection.Socket.Disconnect(false);
					
				_connections.Remove( connection.Socket );
				
			}
		}
		
		protected ClientConnection GetConnection(Socket associatedSocket) {
			
			lock( _connectionsLock ) {
			
				if( _connections.ContainsKey( associatedSocket ) ) return _connections[ associatedSocket ];
				return null;
			}
		}
		
		protected ClientConnection[] FindConections(Predicate<ClientConnection> predicate) {
			
			lock( _connectionsLock ) {
				
				List<ClientConnection> ret = new List<ClientConnection>();
				
				foreach(ClientConnection conn in _connections.Values) {
					
					if( predicate( conn ) ) ret.Add( conn );
				}
				
				return ret.ToArray();
			}
		}
		
	#endregion
		
	}
	
	public abstract class ClientConnection {
		
		protected ClientConnection(int bufferLength, Socket socket) {
			Buffer    = new Byte[ bufferLength ];
			Socket    = socket;
			Id        = _counter++;
		}
		
		private static long _counter = 0;
		
		public Byte[] Buffer { get; private set; }
		public Int32  Length { get; set; }
		public Socket Socket { get; private set; }
		public Int64  Id     { get; private set; }
		
	}
	
}
