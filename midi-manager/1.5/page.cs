using System;
using System.Collections;
using System.Text;

namespace midiManager
{
	class Page
	{
		public int mode;
		public int startCC;
		public int numCC;
		public int channel;
		public int trigger;

		public int[] values; 

		public int getStart(){ return startCC; }

			// kontruktor
		public Page(
				int m,
				int s,
				int n,
				int c,
				int t)
		{
			mode=m;
			startCC=s;
			numCC=n;
			channel=c;
			trigger=t;
			values = new int[numCC];
		}

		public bool sort(
				int ch,
				int cc,
				int val)
		{
			if ((cc>=startCC)
					&&(channel==ch)
					&&(cc<startCC+numCC))
			{
				values[cc-startCC]=val;
				return true;
			}
			return false;
		}
	}

	class Mixer
	{
		public int channel;
		// die channel-select-kn�pfe bekommen 
		// einen offset, damit man von einem mixer
		// der die kan�le 9-16 bedient, auch mit 
		// den midiCC 0-8 die entsprechend transformierten
		// koordinaten erh�lt, um den channel im host angezeigt
		// zu bekommen
		public int activationAddition;

		public int[] values;

		public Mixer(
				int ch,
				int aA)
		{
			channel = ch;
			activationAddition=aA;
			values = new int[128];
		}

		public bool sort(
				int ch,
				int cc,
				int val)
		{
			if (channel==ch)
			{	
				values[cc]=val;
				return true;
			}
			return false;
		}


	}
}
