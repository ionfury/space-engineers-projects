/* 
 /// Torpedo Firing System /// 
///      DEV BUILD 		    ///
_______________________________
Setup:
_wireGuidedString: Name associated with wire guided missile blocks
_autoGuidedString: Name associated with auto guided missile blocks
*/  
 
/* 
_______________
-- Variables --
_______________
*/ 

string _wireGuidedString = "[wire-guided-missile]";
string _autoGuidedString = "[auto-guided-missile]";
 
 /* 
_____________
-- Program --
_____________
*/   
 
List<IMyTerminalBlock> _missilePrograms = new List<IMyTerminalBlock>();
 
/// PURPOSE:
/// INPUT  : string expecting one of the following:
///							"fire-auto"  : Fires all missiles with the tag matching _autoGuidedString
///							"fire-guided": Fires all missiles with the tag matching _wireGuidedString
///							"kill"			 : Kills all active missile guidance.
/// OUTPUT : Main method duh
void Main(string arg)  
{   
	if (arg.Contains("fire-auto"))
	{
		Echo("Fire Auto targeting Missiles");
		FireMissile(_autoGuidedString);
		GrabPrograms(_autoGuidedString);
	}
	else if (arg.Contains("fire-guided"))
	{
		Echo("Fire Guided Missiles");
		FireMissile(_wireGuidedString);
		GrabPrograms(_wireGuidedString);
	}
	else if (arg == "kill")
	{
		KillGuidance();
	}
	else
	{
		Echo("Default");
	}
}  

/// PURPOSE: Trigger the action on any timer block matching name of missileName.
/// INPUT  : A string matching timer blocks which to trigger.
/// OUTPUT : None
void FireMissile(string missileName) 
{ 
	List<IMyTerminalBlock> fireTimers = new List<IMyTerminalBlock>();
	GridTerminalSystem.SearchBlocksOfName(missileName,fireTimers);  
	
	if(fireTimers.Count != 0)     
	{        
		for(int j = 0; j < fireTimers.Count; j++)    
		{  
			if(fireTimers[j] is IMyTimerBlock)
			{
				var fire_timer = fireTimers[j] as IMyTimerBlock;   
				fire_timer.ApplyAction("TriggerNow");          
				Echo(fire_timer.CustomName + " Fired!");
			}
		}    
	}  
	Echo("All Missiles Fired");
} 

/// PURPOSE: Store missile program blocks
/// INPUT  : Program Name
/// OUTPUT : None
void GrabPrograms(string programName) 
{ 
	List<IMyTerminalBlock> blocks = new List<IMyTerminalBlock>(); 
	GridTerminalSystem.GetBlocksOfType<IMyTerminalBlock>(blocks); 
	for(int i=0 ; i< blocks.Count ; i++) 
	{ 
		if(blocks[i] is IMyProgrammableBlock) 
		{ 
			if(_missilePrograms.Contains(blocks[i])) 
			{ 
				Echo("Ignored program " + blocks[i].CustomName); 
				continue; 
			} 
			else if(blocks[i].CustomName.Contains(programName)) 
			{ 
				_missilePrograms.Add(blocks[i] as IMyProgrammableBlock); 
			}
		} 
	} 
	Echo("Missile Programs: " + _missilePrograms.Count);
} 

/// PURPOSE: Kill guidance on all active missiles
/// INPUT  : None
/// OUTPUT : None
void KillGuidance() 
{ 
	string argument = "kill"; 
	List<TerminalActionParameter> arguments = new List<TerminalActionParameter>(); 
	arguments.Add(TerminalActionParameter.Get(argument)); 
	 
	if (_missilePrograms.Count == 0) 
	{ 
		Echo("No missiles to kill"); 
	}
	else
	{ 
		for(int i = 0 ; i < _missilePrograms.Count ; i++) 
		{ 
			var thisProgram = _missilePrograms[i] as IMyProgrammableBlock; 
			thisProgram.ApplyAction("Run", arguments); 
			Echo(thisProgram.CustomName + " Killed!"); 
		}
		_missilePrograms.Clear();
		Echo("All Missiles Killed"); 
	} 
}