using System;
using System.Collections.Generic;
using System.Text;

using System.Security.Cryptography;
using System.Collections.ObjectModel;

namespace W3b.MsnpServer {
	
	public static class AuthenticationService {
		
		public static AuthenticationResult AuthenticateMd5(String userHandle, String challenge, String challengeResponse, out User user) {
			
			user = User.GetUser( userHandle );
			if( user == null ) return AuthenticationResult.NoSuchUser;
			
			Byte[] expectedResponseClear = Encoding.ASCII.GetBytes( challenge + user.Password );
			Byte[] expectedResponseHash  = MD5.Create().ComputeHash( expectedResponseClear );
			
			String expectedResponseStr   = ByteArrayToHexString( expectedResponseHash );
			
			if( String.Equals( expectedResponseStr, challengeResponse, StringComparison.OrdinalIgnoreCase ) ) {
				
				return AuthenticationResult.Success;
			}
			
			else return AuthenticationResult.BadPassword;
		}
		
		public static String CreateChallengeString() {
			
			RandomNumberGenerator rng = RandomNumberGenerator.Create();
			
			Byte[] challengeBytes = new Byte[ 8 ]; // the length of the challenge string is arbitrary, I'm choosing 8 to be nice
			
			rng.GetNonZeroBytes( challengeBytes );
			
			return ByteArrayToHexString( challengeBytes );
		}
		
		private static string ByteArrayToHexString(byte[] bytes) {
			
			StringBuilder ret      = new StringBuilder();
			const String  hexChars = "0123456789ABCDEF";
			
			foreach(byte b in bytes) {
				
				ret.Append( hexChars[ (int)(b >> 4 ) ] );
				ret.Append( hexChars[ (int)(b & 0xF) ] );
			}
			
			return ret.ToString();
		}
		
	}
	
	public enum AuthenticationResult {
		Success,
		NoSuchUser,
		BadPassword
	}

	
}
