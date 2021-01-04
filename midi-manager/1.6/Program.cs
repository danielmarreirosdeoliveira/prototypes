using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.ComponentModel;

namespace midiManager
{


class Program
{static void Main(string[] args)
{
	Midi myMidi = new Midi();
	myMidi.start();
}}


class Mapping
{
	public class Set
	{
		public int inputByte1;
		public int inputByte2;
		public int outputByte1;
		public int outputByte2;
	}
	public List<Set> sets;

	public Mapping()
	{
		sets = new List<Set>();
	}
	
	public void feed(
		int iByte1,
		int iByte2,
		int oByte1,
		int oByte2)
	{
		Set newset = new Set();

		newset.inputByte1=iByte1;
		newset.inputByte2=iByte2;
		newset.outputByte1=oByte1;
		newset.outputByte2=oByte2;
		
		sets.Add(newset);
	}

	public int getByte1(
			int vByte1,
			int vByte2)
	{
		for (int i=0;i<sets.Count;i++)
		{
			if ((vByte1==sets[i].inputByte1)
				&&(vByte2==sets[i].inputByte2))
					return sets[i].outputByte1;
		}
		return -1;
	}
	public int getByte2(
			int vByte1,
			int vByte2)
	{
		for (int i=0;i<sets.Count;i++)
		{
			if ((vByte1==sets[i].inputByte1)
				&&(vByte2==sets[i].inputByte2))
					return sets[i].outputByte2;
		}
		return -1;
	}

}


class  Midi
{



private     MIDIOXLib.MoxScriptClass mox = new MIDIOXLib.MoxScriptClass();
private     MidiCentral                        midiCentral;
private     InstrCentral                       instrCentral;
private     HuiCentral                         huiCentral;

private     Device host;
private     Device huiHost;


public int   midiBankwechsel      = 192;  

int hostInputID     = 0;
int hostOutputID    = 0;
int huiHostOutputID = 0;
Mapping mapping = new Mapping();

public void start()
{
	mox.MidiInput   += new MIDIOXLib._IMoxScriptEvents_MidiInputEventHandler(this.midiIn);
	host             = new Device(mox,0);
	huiHost          = new Device(mox,0);
	
	midiCentral      = new MidiCentral(mox,host);
	
	readConfigFile(".\\config\\init.txt");
	
	// nachdem die erforderlichen outputs bereitstehen (devices):
	host.deviceID    = hostOutputID;
	huiHost.deviceID = huiHostOutputID;
	huiCentral       = new HuiCentral(mox,huiHost);


	//main----------------
	mox.FireMidiInput=1;
	mox.DivertMidiInput=1;
	Console.ReadLine();
	mox.FireMidiInput=0;
	//main----------------
}


private void midiIn(int ts, int port, int byte1, int byte2, int byte3)
{

	// code mappings start ....................
	if (port!=hostInputID)
	{
		if (instrCentral.processNotes(byte1,byte2,byte3)==1) return;
		
		int b1=mapping.getByte1(byte1,byte2);
		int b2=mapping.getByte2(byte1,byte2);

		if ((b1!=-1)&&(b2!=-1))
		{
			byte1 = b1;
			byte2 = b2;
		}

		if ((byte1%16)<9) 
		{
			huiCentral.fire(byte1,byte2,byte3);
			return;
		}

		instrCentral.processCommands(byte1,byte2,byte3);
		
		if (byte1>175)
	   		midiCentral.processMidi(port,byte1,byte2,byte3);
		if (byte1==191)
		{
			int newMixer = midiCentral.processChannel(port,byte1,byte2,byte3);
			if (newMixer!=-1)
			{
				huiCentral.changeMixer(newMixer);
			}
		}
	}
	else
		midiCentral.processHost(byte1,byte2,byte3);	
}


private void readConfigFile(string dateiName)
{
	int devicesCount = 0;
	int controllerCount = 0;
	int instrumentCount = 0;
	int rowsCount = 0;
	int mappingCount = 0;
	StreamReader reader = null;
	try 
	{
		Console.Write("reading file: ");
		Console.WriteLine(dateiName);
		Console.WriteLine("..");

		reader = File.OpenText(dateiName);
		int zeile = 0;
		string line;
		line = reader.ReadLine();
		while (line!=null)
		{
  			string[] stringList = line.Split(':');
			zeile++;
			line = reader.ReadLine();
			if (stringList.Length<2) continue;
			if (stringList[0]=="#")  continue;

			if (stringList[0]=="rows")
			{
				instrCentral = new InstrCentral(mox,host,Convert.ToInt16(stringList[1]));
				rowsCount++;
			}

			if (stringList[0]=="shift")
			{
				if (stringList[0].Length<3)
				throw(
				new ParameterException(zeile));

				int shiftButtonChannel = Convert.ToInt16( stringList[1] ) + 143;
				int shiftButtonNote    = Convert.ToInt16( stringList[2] );

				if ((shiftButtonChannel-143<1)
					||(shiftButtonChannel-143>16)
					||(shiftButtonNote<0)
					||(shiftButtonNote>127))
				throw(
				new ParameterException(
					zeile,
					"format must be \"shift:(1..16):(1..16)\""));

				instrCentral.setShift(shiftButtonChannel,shiftButtonNote);
			}

			if (stringList[0]=="device")
			{
				if (stringList.Length<4)
				throw(
				new ParameterException(zeile));
			
				int i = Convert.ToInt16( stringList[1] );
				int o = Convert.ToInt16( stringList[2] );
				int ho= Convert.ToInt16( stringList[3] );

				if ((i<1)
					||(i>16)
					||(o<1)
					||(o>16)
					||(ho<1)
					||(ho>16))
				throw(
				new ParameterException(
					zeile,
					"format must be \"device:(1..16):(1..16):(1..16)\""));
				
				hostInputID=i - 1 ;
				hostOutputID=o  ;
				huiHostOutputID=ho;
				devicesCount++;
			}
			
			
			if (stringList[0]=="controller")
			{
				string fully = ".\\config\\";
				fully += stringList[1];
				midiCentral.addController(fully);
				controllerCount++;
			}
		
			if (stringList[0]=="map")
			{
				if (stringList.Length<5)
				throw(
				new ParameterException(zeile));

				int i1 = Convert.ToInt16( stringList[1] );
				int i2 = Convert.ToInt16( stringList[2] );
				int o1 = Convert.ToInt16( stringList[3] );
				int o2 = Convert.ToInt16( stringList[4] );
				
				if ((i1<0)
					||(i1>255)
					||(i2<0)
					||(i2>127)
					||(o1<0)
					||(o1>255)
					||(o2<0)
					||(o2>127))
				throw(
				new ParameterException(
					zeile,
					"format must be \"map:(0..255):(0..127):(0..255):(0..127)\""));

				mapping.feed(i1,i2,o1,o2);	
				mappingCount++;
			}
			
		
			if (stringList[0]=="instrument")
			{
				if (rowsCount==0)
				throw(
				new ParameterException(
					zeile,
					"place a rows-tag before any instrument tag"));

				
				
				if (stringList.Length<3)
				throw(
				new ParameterException(zeile));
				
				int i = Convert.ToInt16( stringList[1] );

				if ((i<1)
					||(i>2))
				throw(
				new ParameterException(
					zeile,
					"format must be \"instrument:(1..2):fileName\""));

				string fully = ".\\config\\";
				fully += stringList[2]; 
				instrumentCount++;
				instrCentral.addInstrument(
						i-1,
						fully);
			}
		}
		reader.Close();
	}
	catch(FileNotFoundException f)
	{
		Console.WriteLine("file not found");
	}
	catch(FormatException f)
	{
	}
	catch(ParameterException p)
	{
	}
	catch(Exception e)
	{
	}
	finally
	{
		Console.WriteLine("..");
		Console.WriteLine("    parsing finished ---->");
		if (devicesCount==0)
			Console.WriteLine("    no device could be found");
		Console.Write("    instruments found: ");
		Console.WriteLine(instrumentCount);
		Console.Write("    controllers found: ");
		Console.WriteLine(controllerCount);
		Console.Write("    mappings found: ");
		Console.WriteLine(mappingCount);
		
	}
}



}
}
