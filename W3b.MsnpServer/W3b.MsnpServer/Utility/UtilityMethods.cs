using System;
using System.IO;
using System.Text;

namespace W3b.MsnpServer {
	
	public static class UtilityMethods {
		
#region URL Encoding
		
		public static String UrlEncode(String s) {
			
			Byte[] bytes = Encoding.UTF8.GetBytes( s );
			
			Byte[] encoded = UrlEncodeBytesToBytesInternal( bytes, 0, bytes.Length, false );
			
			return Encoding.ASCII.GetString( encoded );
			
		}
		
		public static String UrlDecode(String s) {
			
			return UrlDecodeStringFromStringInternal(s, Encoding.UTF8);
		}
		
		
		private static byte[] UrlEncodeBytesToBytesInternal(byte[] bytes, int offset, int count, bool alwaysCreateReturnValue) {
			
			// ripped from System.Web.dll\System.Web.HttpUtility
			
			int num = 0;
			int num2 = 0;
			for(int i = 0;i < count;i++) {
				char ch = (char)bytes[offset + i];
				if(ch == ' ') {
					num++;
				} else if(!IsSafe(ch)) {
					num2++;
				}
			}
			if((!alwaysCreateReturnValue && (num == 0)) && (num2 == 0)) {
				return bytes;
			}
			byte[] buffer = new byte[count + (num2 * 2)];
			int num4 = 0;
			for(int j = 0;j < count;j++) {
				byte num6 = bytes[offset + j];
				char ch2 = (char)num6;
				if(IsSafe(ch2)) {
					buffer[num4++] = num6;
				} else if(ch2 == ' ') {
					buffer[num4++] = 0x2b;
				} else {
					buffer[num4++] = 0x25;
					buffer[num4++] = (byte)IntToHex((num6 >> 4) & 15);
					buffer[num4++] = (byte)IntToHex(num6 & 15);
				}
			}
			return buffer;
		}
		
		private static string UrlDecodeStringFromStringInternal(string s, Encoding e) {
			
			int length = s.Length;
			UrlDecoder decoder = new UrlDecoder(length, e);
			for(int i = 0;i < length;i++) {
				char ch = s[i];
				if(ch == '+') {
					ch = ' ';
				} else if((ch == '%') && (i < (length - 2))) {
					if((s[i + 1] == 'u') && (i < (length - 5))) {
						int num3 = HexToInt(s[i + 2]);
						int num4 = HexToInt(s[i + 3]);
						int num5 = HexToInt(s[i + 4]);
						int num6 = HexToInt(s[i + 5]);
						if(((num3 < 0) || (num4 < 0)) || ((num5 < 0) || (num6 < 0))) {
							goto Label_0106;
						}
						ch = (char)((((num3 << 12) | (num4 << 8)) | (num5 << 4)) | num6);
						i += 5;
						decoder.AddChar(ch);
						continue;
					}
					int num7 = HexToInt(s[i + 1]);
					int num8 = HexToInt(s[i + 2]);
					if((num7 >= 0) && (num8 >= 0)) {
						byte b = (byte)((num7 << 4) | num8);
						i += 2;
						decoder.AddByte(b);
						continue;
					}
				}
Label_0106:
				if((ch & 0xff80) == 0) {
					decoder.AddByte((byte)ch);
				} else {
					decoder.AddChar(ch);
				}
			}
			return decoder.GetString();
		}
		
		
		private static bool IsSafe(char ch) {
			
			if((((ch >= 'a') && (ch <= 'z')) || ((ch >= 'A') && (ch <= 'Z'))) || ((ch >= '0') && (ch <= '9'))) {
				return true;
			}
			switch(ch) {
				case '\'':
				case '(':
				case ')':
				case '*':
				case '-':
				case '.':
				case '_':
				case '!':
					return true;
			}
			return false;
		}
		
		internal static char IntToHex(int n) {
			if(n <= 9) {
				return (char)(n + 0x30);
			}
			return (char)((n - 10) + 0x61);
		}
		
		private static int HexToInt(char h) {
			if((h >= '0') && (h <= '9')) {
				return (h - '0');
			}
			if((h >= 'a') && (h <= 'f')) {
				return ((h - 'a') + 10);
			}
			if((h >= 'A') && (h <= 'F')) {
				return ((h - 'A') + 10);
			}
			return -1;
		}
		
		private class UrlDecoder {
			
			private int _bufferSize;
			private byte[] _byteBuffer;
			private char[] _charBuffer;
			private Encoding _encoding;
			private int _numBytes;
			private int _numChars;
			
			
			internal UrlDecoder(int bufferSize, Encoding encoding) {
				this._bufferSize = bufferSize;
				this._encoding = encoding;
				this._charBuffer = new char[bufferSize];
			}
			
			internal void AddByte(byte b) {
				if(this._byteBuffer == null) {
					this._byteBuffer = new byte[this._bufferSize];
				}
				this._byteBuffer[this._numBytes++] = b;
			}
			
			internal void AddChar(char ch) {
				if(this._numBytes > 0) {
					this.FlushBytes();
				}
				this._charBuffer[this._numChars++] = ch;
			}
			
			private void FlushBytes() {
				if(this._numBytes > 0) {
					this._numChars += this._encoding.GetChars(this._byteBuffer, 0, this._numBytes, this._charBuffer, this._numChars);
					this._numBytes = 0;
				}
			}
			
			internal string GetString() {
				if(this._numBytes > 0) {
					this.FlushBytes();
				}
				if(this._numChars > 0) {
					return new string(this._charBuffer, 0, this._numChars);
				}
				return string.Empty;
			}
		}
		
#endregion
		
#region Find File
		
		public static String FindFile(DirectoryInfo start, String fileName, int limit) {
			
			String ret = SearchDown( start, fileName, 0, limit );
			if( ret != null ) return ret;
			
			if( start.Parent != null ) {
				ret = SearchUp( start.Parent, start, fileName, 0, limit );
				if( ret != null ) return ret;
			}
			
			return null;
		}
		
		private static String SearchUp(DirectoryInfo directory, DirectoryInfo from, String fileName, int deep, int limit) {
			
			if( Math.Abs( deep ) == limit ) return null;
			
//			Console.WriteLine("SearchUp: " + directory.FullName);
			
			foreach(DirectoryInfo child in directory.GetDirectories()) {
				
				if( child.FullName.Equals( from.FullName, StringComparison.OrdinalIgnoreCase ) ) continue;
				
				String ret = SearchDown( child, fileName, deep + 1, limit );
				if( ret != null ) return ret;
			}
			
			if( directory.Parent != null ) {
				String ret = SearchUp( directory.Parent, directory, fileName, deep - 1, limit );
				if( ret != null ) return ret;
			}
			
			return null;
		}
		
		private static String SearchDown(DirectoryInfo directory, String fileName, int deep, int limit) {
			
			if( Math.Abs( deep ) == limit ) return null;
			
//			Console.WriteLine("SearchDown: " + directory.FullName);
			
			foreach(FileInfo file in directory.GetFiles()) {
				
				if( file.Name.Equals("PinkEgoBox.sqlite", StringComparison.OrdinalIgnoreCase) ) return file.FullName;
			}
			
			foreach(DirectoryInfo child in directory.GetDirectories()) {
				
				String ret = SearchDown( child, fileName, deep + 1, limit );
				if( ret != null ) return ret;
			}
			
			return null;
		}
		
#endregion
		
	}
	
}
