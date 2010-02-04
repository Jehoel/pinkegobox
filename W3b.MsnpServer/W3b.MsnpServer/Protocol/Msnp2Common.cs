using System;
using System.Collections.Generic;
using System.Text;

using Verb = W3b.MsnpServer.Protocol.Msnp2DispatchVerbs;

namespace W3b.MsnpServer.Protocol {
	
	/// <summary>Because Dispatch and Notification have overlap, this class contains the functionality to avoid code duplication</summary>
	public static class Msnp2Common {
		
		public static void HandleCvq<TConnection>(MsnpServer<TConnection> server, TConnection c, Command cmd) where TConnection : ClientConnection {
			
			// Messenger 1.0:
			// >>> CVQ 6 0x0409 winnt 5.1 i386 MSMSGS 1.0.0863
			
			// >>> CVQ trid localeId osType osVer cpuArch clientName clientVer
			
			// I *assume* the expected response is the same as CVR's
			
			
		}
		
		public static void HandleCvr<TConnection>(MsnpServer<TConnection> server, TConnection c, Command cmd) where TConnection : ClientConnection {
			
			
			// Later versions:
			// >>> CVR trid localeId osType osVer cpuArch libraryName clientVer clientName passport
			// <<< CVR trid recoVer recoVer2 minVer dlUrl infoUrl
			
			// recoVer  = recommended version, possibly recommended client version?
			// recoVer2 = identical to recoVer, possibly recommended library version?
			// minVer   = minimum supported version for this protocol
			
			Dictionary<String,String[]> recommendedVersions = new Dictionary<String,String[]>() {
				{ "Third", new String[] { "1.0.0000", "1.0.0000", "http://pathToPinkEgoBoxWebsite/"} }, // Third-party clients only
				{ "MSNP2", new String[] { "1.0.0863", "2.0.0085", "http://pathToDownloadMsgr20/" } },
				{ "MSNP3", new String[] { "2.0.0085", "2.2.1053", "http://pathToDownloadMsgr22/" } },
				{ "MSNP4", new String[] { "2.1.1047", "3.6.0025", "http://pathToDownloadMsgr36/" } },
				{ "MSNP5", new String[] { "3.0.0286", "3.6.0025", "http://pathToDownloadMsgr36/" } },
				{ "MSNP6", new String[] { "2.0.0085", "3.6.0025", "http://pathToDownloadMsgr36/" } }
			};
			
			String protocolVersion;
			
			if( cmd.Params[4] == "MSMSGS" || cmd.Params[4] == "MSNMSGR" ) {
				// official clients
				protocolVersion = "MSNP2";
				// TODO: Store the VER protocol listing in the SwitchboardConnection class so it knows which MSNP version to use in the lookup table
			} else {
				// third-party client
				
				protocolVersion = "Third";
			}
			
			String minimumSuppVersion = recommendedVersions[ protocolVersion ][0];
			String recommendedVersion = recommendedVersions[ protocolVersion ][1];
			String downloadUrl        = recommendedVersions[ protocolVersion ][2];
			String infoUrl            = @"http://msnpiki.msnfanatic.com/index.php/Reference:ProtocolTable";
			
			Command response = new Command(cmd.Verb, cmd.TrId, recommendedVersion, recommendedVersion, minimumSuppVersion, downloadUrl, infoUrl );
			server.Send( c, response );
		}
		
		public static void HandleInf<TConnection>(MsnpServer<TConnection> server, TConnection c, Command cmd) where TConnection : ClientConnection {
			
			// for the purposes of compatibility, just return 'MD5', though a spec-compliant implementation would return all the supported auth packages
			
			Command response = new Command(Verb.Inf, cmd.TrId, "MD5");
			server.Send( c, response );
		}
		
		
		
		
		
	}
}
