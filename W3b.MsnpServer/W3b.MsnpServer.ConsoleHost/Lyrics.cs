using System;
using System.Collections.Generic;

namespace W3b.MsnpServer.ConsoleHost {
	
	public static class Lyrics {
		
		private static readonly String[][] _lyrics = {
			new String[] {
				"It's gonna be ok",
				"Can’t afford another day",
				"At 50 bytes per second"
			},
			new String[] {
				"I’ve never seen your face",
				"I’ve never heard your voice"
			},
			new String[] {
				"But I think I like it",
				"When you instant message me",
				"With a promise",
				"I can feel it",
				"I can tell you're gonna be",
				"Just like me"
			},
			new String[] {
				"My eyes are gonna strain",
				"My heart is feeling pain",
				"At 50 beats per second"
			},
			new String[] {
				"I’ve never seen your eyes",
				"I’ve never heard your lies",
				"But I think I like it"
			},
			new String[] {
				"But I think I like it", // line repeated from above for poetic reasons
				"When you instant message me",
				"With a promise",
			},
			new String[] {
				"I can feel it",
				"I can tell you're gonna be",
				"Just like me",
				"Be",
				"Just like me",
			},
			new String[] {
				"You turn",
				"You turn ",
				"You turn on me"
			}
		};
		
		public static String[] GetLyrics() {
			List<String> allVerses = new List<String>();
			foreach(String[] verse in _lyrics) allVerses.AddRange( verse );
			return allVerses.ToArray();
		}
		
		public static String GetRandomVerse() {
			
			Random rng = new Random();
			int i = rng.Next(0, _lyrics.Length);
			
			String ret = String.Empty;
			for(int li=0;li<_lyrics[i].Length;li++) {
				if( li >  0 ) ret += "\r\n\t";
				ret += _lyrics[i][li];
				if( li == 0 ) ret += "...";
			}
			
			return ret;
		}
		
	}
}
