using System;
using System.Collections.Generic;
using System.Text;
using System.Net;

using C = System.Globalization.CultureInfo;

namespace System.Runtime.CompilerServices {
	
	[AttributeUsage(AttributeTargets.Method)]
	internal sealed class ExtensionAttribute : Attribute {
		
		public ExtensionAttribute() {
		}
		
	}
	
}

namespace W3b.MsnpServer {
	
	public static class Extensions {
		
#region Numbers
		
		public static String ToStringInvariant(this Byte n) {
			
			return n.ToString( C.InvariantCulture );
		}
		public static String ToStringInvariant(this SByte n) {
			
			return n.ToString( C.InvariantCulture );
		}
		public static String ToStringInvariant(this Int16 n) {
			
			return n.ToString( C.InvariantCulture );
		}
		public static String ToStringInvariant(this UInt16 n) {
			
			return n.ToString( C.InvariantCulture );
		}
		public static String ToStringInvariant(this Int32 n) {
			
			return n.ToString( C.InvariantCulture );
		}
		public static String ToStringInvariant(this UInt32 n) {
			
			return n.ToString( C.InvariantCulture );
		}
		public static String ToStringInvariant(this Int64 n) {
			
			return n.ToString( C.InvariantCulture );
		}
		public static String ToStringInvariant(this UInt64 n) {
			
			return n.ToString( C.InvariantCulture );
		}
		
		public static String ToStringInvariant(this Single n) {
			
			return n.ToString( C.InvariantCulture );
		}
		public static String ToStringInvariant(this Double n) {
			
			return n.ToString( C.InvariantCulture );
		}
		
#endregion
		
#region Strings
		
		public static String Left(this String str, Int32 length) {
			
			if(length < 0)          throw new ArgumentOutOfRangeException("length", length, "value cannot be less than zero");
			if(length > str.Length) throw new ArgumentOutOfRangeException("length", length, "value cannot be greater than the length of the string");
			
			return str.Substring(0, length);
			
		}
		
		/// <summary>Retreives a substring starting from the left to the right, missing <paramref name="fromRight" /> characters from the right. So "abcd".LeftFR(1) returns "abc"</summary>
		public static String LeftFR(this String str, Int32 fromRight) {
			
			if(fromRight < 0)          throw new ArgumentOutOfRangeException("fromRight", fromRight, "value cannot be less than zero");
			if(fromRight > str.Length) throw new ArgumentOutOfRangeException("fromRight", fromRight, "value cannot be greater than the length of the string");
			
			return str.Substring(0, str.Length - fromRight );
			
		}
		
		public static String Right(this String str, Int32 length) {
			
			if(length < 0)          throw new ArgumentOutOfRangeException("length", length, "value cannot be less than zero");
			if(length > str.Length) throw new ArgumentOutOfRangeException("length", length, "value cannot be greater than the length of the string");
			
			return str.Substring( str.Length - length );
			
		}
		
#endregion
		
#region Arrays
		
		public static String ToHexString(this Byte[] array) {
			
			StringBuilder sb = new StringBuilder( array.Length * 2 );
			for(int i=0;i<array.Length;i++) {
				
				sb.Append( array[i].ToString("X2", C.InvariantCulture) );
			}
			
			return sb.ToString();
			
		}
		
		public static Byte[] SubArray(this Byte[] array, Int32 startIndex, Int32 length) {
			
			if(startIndex >= array.Length         ) throw new ArgumentOutOfRangeException("startIndex");
			if(startIndex + length >= array.Length) throw new ArgumentOutOfRangeException("length");
			
			Byte[] retval = new Byte[ length ];
			
			for(int i=0;i<length;i++) retval[i] = array[startIndex + i];
			
			return retval;
			
		}
		
		public static Int32 IndexOf(this Array array, Object item) {
			
			return Array.IndexOf( array, item );
			
		}
		
		public static Boolean Contains(this Array array, Object item) {
			return array.IndexOf( item ) > -1;
		}
		
		public static void AddRange<T>(this IList<T> list, IEnumerable<T> items) {
			
			// not as fast as AddRange in List<T>, but it'll do
			
			foreach(T item in items) {
				list.Add( item );
			}
		}
		
		public static String ToCsv(this Object[] array) {
			
			return array.ToCsv(", ");
		}
		
		public static String ToCsv(this Object[] array, String seperator) {
			
			StringBuilder sb = new StringBuilder();
			for(int i=0;i<array.Length;i++) {
				
				sb.Append( array[ i ] );
				if( i < array.Length - 1 ) sb.Append( seperator );
			}
			
			return sb.ToString();
		}
		
#endregion
		
#region IP
		
		/// <summary>Returns a CSV of all listening addresses followed by the port number.</summary>
		public static String ToString2(this IPEndPoint p) {
			
			String addresses = '{' + p.GetListeningAddresses().ToCsv() + "}:";
			return addresses + p.Port.ToStringInvariant();
		}
		
/*		/// <summary>Returns the IP address and port number for the specified otherEnd endpoint to connect to.</summary>
		public static String ToString3(this IPEndPoint p, EndPoint otherEnd) {
			
			// HACK / TODO: Find out how to get the right IPAddress from GetListeningAddresses that match 'otherEnd'
			
			IPEndPoint otherEndIp = (IPEndPoint)otherEnd;
			
			IPAddress[] listeningOn = GetListeningAddresses(p);
			
			// if( otherEnd.Address.ToString() == "127.0.0.1" ) return "127.0.0.1:" + p.Port;
			
			//return p.ToString();
			
			
			
			return "127.0.0.1:" + p.Port;
		} */
		
		public static IPAddress[] GetListeningAddresses(this IPEndPoint p) {
			
			if( p.Address.Equals( IPAddress.Any ) || p.Address.Equals( IPAddress.IPv6Any ) ) {
				
				IPHostEntry hostEntry = Dns.GetHostEntry( Dns.GetHostName() );
				
				IPAddress[] ret = new IPAddress[ hostEntry.AddressList.Length + 1 ];
				ret[0] = IPAddress.Loopback;
				Array.Copy( hostEntry.AddressList, 0, ret, 1, hostEntry.AddressList.Length );
				
				return ret;
			}
			
			return new IPAddress[] { p.Address };
		}
		
#endregion
		
		public static TValue Get<TKey,TValue>(this Dictionary<TKey,TValue> d, TKey key) where TValue : class {
			
			TValue ret;
			if( d.TryGetValue( key, out ret ) ) return ret;
			return null;
		}
		
		public static T Find<T>(this IEnumerable<T> enumerable, Predicate<T> predicate) where T : class {
			
			foreach(T t in enumerable) if( predicate(t) ) return t;
			
			return null;
		}
		
		public static bool IsOnline(this Status status) {
			// TODO: problem: this also includes HDN state as being Offline, when it isn't
			// I'll need to revisit this method in future to ensure the right condition (include "appear offline") is met
			return ((int)status & 1) == 1;
		}
		
	}
	
}
