const string AIRLOCKTAG = "Airlock:##NAME##";
const string AIRLOCKNAME = "##NAME##";
const string INTERIORAIRLOCKTAG = "dontusethis";
const string EXTERIORAIRLOCKTAG = "Exterior";

const double AIRLOCKDELAY = 4.0;

Color LIGHT_PRESSURIZED = Color.AntiqueWhite;
Color LIGHT_DEPRESSURIZED = Color.Firebrick;
Color LIGHT_CHANGING = Color.Goldenrod;
Color LIGHT_LOCKDOWN = Color.Firebrick;

const bool DEBUGMODE = true;

Airlock airlock;
bool init = false;

void Main(string arg)
{
	switch (arg)
	{
		case "Restart":
		  break;
		default:
		  break;
	}
	DateTime timestamp = DateTime.Now;
	DebugPrint(timestamp.ToString(), Echo);
	
	if(!init)
	{
		airlock = new Airlock("primary", this);
		init = true;
	}
	airlock.HandleState();
}

public class Airlock
{
  private string _name;
	private List<IMyDoor> _interiorDoors;
	private List<IMyDoor> _exteriorDoors;
	private List<IMyAirVent> _vents;
	private List<IMyOxygenTank> _tanks;
	private List<IMyLightingBlock> _lights;
	private Program SpaceEngineers;
	private DateTime _lastAction;
	
	private int _state;
	
	public Airlock(string name, Program program)
	{
		SpaceEngineers = program;
		DebugPrint("MyAirlock('" + name + "')", SpaceEngineers.Echo);
		_name = name;
		_state = -1;
		_lastAction = DateTime.Now;
		
		_interiorDoors = new List<IMyDoor>();
		_exteriorDoors = new List<IMyDoor>();
		_vents = new List<IMyAirVent>();
		_tanks = new List<IMyOxygenTank>();
		_lights = new List<IMyLightingBlock>();
		
		FindBlocks();
	}
	
	/// Find all blocks for this airlock and collect them
	public void FindBlocks()
	{
		DebugPrint("FindBlocks()", SpaceEngineers.Echo);
		var blocks = new List<IMyTerminalBlock>();
		var airlockName = AIRLOCKTAG.Replace(AIRLOCKNAME, _name);
		
		_interiorDoors.Clear();
		_exteriorDoors.Clear();
		_vents.Clear();
		_tanks.Clear();
		_lights.Clear();
	
		//program chokes when we do anything with types :(
		SpaceEngineers.GridTerminalSystem.SearchBlocksOfName(airlockName, blocks, SpaceEngineers.isIMyInteriorDoor);
		foreach(var block in blocks)
		  _interiorDoors.Add(block as IMyDoor);

		SpaceEngineers.GridTerminalSystem.SearchBlocksOfName(airlockName, blocks, SpaceEngineers.isIMyExteriorDoor);
		foreach(var block in blocks)
		  _exteriorDoors.Add(block as IMyDoor);
			
		SpaceEngineers.GridTerminalSystem.SearchBlocksOfName(airlockName, blocks, SpaceEngineers.isIMyAirVent);
		foreach(var block in blocks)
		  _vents.Add(block as IMyAirVent);
			
		SpaceEngineers.GridTerminalSystem.SearchBlocksOfName(airlockName, blocks, SpaceEngineers.isIMyOxygenTank);
		foreach(var block in blocks)
		  _tanks.Add(block as IMyOxygenTank);
			
		SpaceEngineers.GridTerminalSystem.SearchBlocksOfName(airlockName, blocks, SpaceEngineers.isIMyLightingBlock);
		foreach(var block in blocks)
		  _lights.Add(block as IMyLightingBlock);
		
		DebugPrint("  InteriorDoors:" + _interiorDoors.Count.ToString() +
						 "\n  ExteriorDoors:" + _exteriorDoors.Count.ToString() +
					   "\n  AirVents:     " + _vents.Count.ToString() +
						 "\n  OxygenTanks:  " + _tanks.Count.ToString() +
						 "\n  Lights:       " + _lights.Count.ToString(),
						      SpaceEngineers.Echo);
	}

	public void HandleState()
	{
		DebugPrint("HandleState() - " + _lastAction.ToString(), SpaceEngineers.Echo);
		
		switch(_state)
		{
			case AirlockState.Open:
				Open();
				break;
			case AirlockState.Pressurized:
			  Pressurized();
				break;
			case AirlockState.Draining:
			  Draining();
				break;
			case AirlockState.Depressurized:
			  Depressurized();
				break;
			case AirlockState.Filling:
			  Filling();
				break;
			case AirlockState.Lockdown:
				Lockdown();
				break;
			default:
			  _state = AirlockState.Depressurized;
			  break;
		}
	}
	
	// If the airlock is open, freeze all functions and wait DELAY, then drain.
	private void Open()
	{
		DebugPrint("Open()", SpaceEngineers.Echo);
		bool nextState = true;
		foreach(var door in _exteriorDoors)
		{
			if(door.OpenRatio == 0.0)
			{
				door.ApplyAction(IMyTerminalBlockActions.Off);
			}
		}
				
	  if(nextState && DateTime.Now > _lastAction.AddSeconds(AIRLOCKDELAY))
		{
			_lastAction = DateTime.Now;
			_state = AirlockState.Pressurized;
		}
	}
	
	
	private void Pressurized()
	{
		bool nextState = true;
		
		foreach(var door in _exteriorDoors)
		{
			if(door.OpenRatio > 0.0)
			{
				nextState = false;
				door.ApplyAction(IMyDoorActions.Close);
				door.ApplyAction(IMyTerminalBlockActions.On);
			}
			else
			{
				door.ApplyAction(IMyDoorActions.Close);
				door.ApplyAction(IMyTerminalBlockActions.Off);
			}
		}
		
		if(nextState)
		{
			_lastAction = DateTime.Now;
			_state = AirlockState.Draining;
		}
	}
	
	// Wait till the airlock is empty of oxygen, then depressurized
	private void Draining()
	{
		DebugPrint("Draining()", SpaceEngineers.Echo);
		var nextState = true;
		
		foreach(var door in _exteriorDoors)
		{
			door.ApplyAction(IMyDoorActions.Close);
			door.ApplyAction(IMyTerminalBlockActions.Off);
		}
		
		foreach(var vent in _vents)
		{
			vent.ApplyAction(IMyTerminalBlockActions.On);
			vent.ApplyAction(IMyAirVentActions.Drain);
		}
		
		if(_vents[0].GetOxygenLevel() == 0.0 ||  _tanks[0].GetOxygenLevel() == 1.0)
		
		if(nextState)
		{
			_lastAction = DateTime.Now;
			_state = AirlockState.Depressurized;
		}
	}
	
	// Wait here until something happens
	private void Depressurized()
	{
		DebugPrint("Depressurized", SpaceEngineers.Echo);
		bool nextState = false;
				
		foreach(var door in _exteriorDoors)
		{
			door.ApplyAction(IMyTerminalBlockActions.On);
			if(door.OpenRatio > 0.0)
			{
				nextState = true;
			}
		}
		
		foreach(var vent in _vents)
		{
			vent.ApplyAction(IMyTerminalBlockActions.Off);
		}
		
		if(nextState)
		{
			_lastAction = DateTime.Now;
			_state = AirlockState.Open;
		}
	}
	
	
	private void Filling()
	{}

	private void Lockdown()
	{
		foreach(var door in _exteriorDoors)
		{
			if(door.OpenRatio > 0.0)
			  door.ApplyAction(IMyDoorActions.Close);
			else
				door.ApplyAction(IMyTerminalBlockActions.Off);
		}
		foreach(var door in _interiorDoors)
		{
			if(door.OpenRatio > 0.0)
			  door.ApplyAction(IMyDoorActions.Close);
			else
				door.ApplyAction(IMyTerminalBlockActions.Off);
		}
		foreach(var light in _lights)
		{
			light.SetValue(IMyLightingBlockActions.Color, SpaceEngineers.LIGHT_LOCKDOWN);
			light.SetValue(IMyLightingBlockActions.BlinkInterval, 0.0F);
			light.SetValue(IMyLightingBlockActions.BlinkLength, 50.0F);
		}
	}
}

/// System.Type generic stuff isn't allowed so we need a method for every type :(
/// Determines if a block is an interior door.
bool isIMyInteriorDoor(IMyTerminalBlock block)
{
	var found = false;
	var x = block as IMyDoor;
	if(x != null)
	{
		if(block.CustomName.Contains(INTERIORAIRLOCKTAG))
		{
			found = true;
		}
	}
	return found && IsMyGrid(block);
}
/// Determines if a block is NOT an interior door.
bool isIMyExteriorDoor(IMyTerminalBlock block)
{
	var found = false;
	var x = block as IMyDoor;
	if(x != null)
	{
		if(!block.CustomName.Contains(INTERIORAIRLOCKTAG))
		{
			found = true;
		}
	}
	return found && IsMyGrid(block);
}
/// Determines if a block is an air vent.
bool isIMyAirVent(IMyTerminalBlock block)
{
	var x = block as IMyAirVent;
	return x != null && IsMyGrid(block);
}
/// Determines if block is an oxygen tank.
bool isIMyOxygenTank(IMyTerminalBlock block)
{
	var x = block as IMyOxygenTank;
	return x != null && IsMyGrid(block);
}
/// Determines if block is a light.
bool isIMyLightingBlock(IMyTerminalBlock block)
{
	var x = block as IMyLightingBlock;
	return x != null && IsMyGrid(block);
}

/// Determines if block is on the same grid as the programming block
public bool IsMyGrid(IMyTerminalBlock block)
{
	return (block.CubeGrid == Me.CubeGrid);
}


/// Fuck why don't enums work
static class AirlockState 
{
	public const int Open = 0;
	public const int Pressurized = 1;
	public const int Draining = 2;
	public const int Depressurized = 3;
	public const int Filling = 4;
	public const int OpenExterior = 5;
	public const int Lockdown = 100;
}
/// Actions for all blocks
static class IMyTerminalBlockActions 
{ 
  public const string On = "OnOff_On"; 
  public const string Off = "OnOff_Off"; 
} 
/// Actions for IMyDoor
static class IMyDoorActions 
{ 
  public const string Open = "Open_On"; 
  public const string Close = "Open_Off"; 
} 
/// Actions for IMyAirVent
static class IMyAirVentActions 
{ 
  public const string Fill = "Depressurize_Off"; 
  public const string Drain = "Depressurize_On"; 
} 
static class IMyLightingBlockActions
{
	public const string Color = "Color";
	public const string BlinkInterval = "Blink Interval";
	public const string BlinkLength = "Blink Length";
}

/// Write some message to writer when debug mode is on
static void DebugPrint(string message, Action<string> writer)
{
	if(DEBUGMODE)
	{
		writer(message);
	}
}
