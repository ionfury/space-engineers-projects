/* 
 /// Torpedo Launch System /// 
///      DEV BUILD 		    ///
_____________________________________________________ 
Setup:
*/

/*
_______________
-- Constants --
_______________
*/
	const bool DEBUG = true;
	const string FORWARD_THRUST_NAME = "[forward-thrust-torpedo]"; 						//name tag of forward thrust on the missile 
	const string MANEUVERING_THRUSTERS_NAME = "[maneuvering-thrust-torpedo]"; //name tag of manevering thrusters on the missile 
	const string MERGE_NAME = "[merge-missile-main]"; 												//name tag of missile merge           
	const string ART_MASS_NAME = "[artificial-mass]"; 												//name tag of missile mass 
	const string BATTERY_NAME = "[battery]"; 																	//name tag of missile battery  
	const string SHOOTER_REFERENCE_NAME = "[shooter]"; 												//name tag of shooter's remote 
	const string MISSILE_REFERENCE_NAME = "[missile-remote]"; 				  			//name tag of missile's remote 
	const string GYRO_NAME = "[gyro-torpedo]"; 																//name of missile gyro     
	const double GUIDANCE_DELAY = 2; 																					//time (in seconds) that the missile will delay guidance activation after launch
	const double MAX_ROTATION_DEGREES = 360; 																	//in degrees per second (360 max for small ships, 180 max for large ships)  
	const double MAX_DISTANCE = 10000; 																				//maximum guidance distance in meters; don't set passed view distance 
	const int TICK_LIMIT = 1; 																								//program runs every x frames  

/* 
_____________
-- Program --
_____________
*/  
	IMyCubeGrid _cubeGrid;
	List<IMyTerminalBlock> forwardThrusters 		= new List<IMyTerminalBlock>();  
	List<IMyTerminalBlock> maneuveringThrusters = new List<IMyTerminalBlock>();           
	List<IMyTerminalBlock> artMasses 						= new List<IMyTerminalBlock>();          
	List<IMyTerminalBlock> mergeBlocks 					= new List<IMyTerminalBlock>();                
	List<IMyTerminalBlock> batteries				 	  = new List<IMyTerminalBlock>(); 
	List<IMyTerminalBlock> remotes 							= new List<IMyTerminalBlock>();   
	List<IMyTerminalBlock> shooterReferenceList = new List<IMyTerminalBlock>();   
	List<IMyTerminalBlock> missileReferenceList = new List<IMyTerminalBlock>();   
	List<IMyTerminalBlock> gyroList 						= new List<IMyTerminalBlock>();
	List<IMyTerminalBlock> screenList						= new List<IMyTerminalBlock>();
	List<IMyTerminalBlock> timerBlocks					= new List<IMyTerminalBlock>();
	 
	Vector3D targetVectorNorm = new Vector3D(0, 0, 0); 
	Vector3D originPos = new Vector3D(0, 0, 0); 
	IMyRemoteControl shooterReference;   
	IMyRemoteControl missileReference;
	IMyTextPanel statusMonitor;
	IMyTimerBlock timerBlock;
	bool init = false; 
	bool hasFired = false; 
	bool shouldKill = false; 
	double delta_origin;  
	int current_tick = 0;   
	int duration = 0;       
	int timeElapsed = 0;    
  
/// PURPOSE: Navigate missile along vector specified by shooter 
/// INPUT  : string expecting one of the following:
///						"kill" : end guidance
///						int		 : begin guidance for missile matching nameing conventions 
/// OUTPUT : none, main method duh
void Main(string arg)   
{    
	EchoDebug("Tick: " + current_tick.ToString()); 
	
	
	if(arg == "kill" && hasFired == true) 
	{ 
		shouldKill = true; 
		//MAX_DISTANCE = double.PositiveInfinity; 
	} 

	if (!init) // Setup Vars
	{ 
		Init();
	}         
	else // Engage guidance
	{
		MissileSystems(); 
		 
	  if (duration < Math.Ceiling(GUIDANCE_DELAY * 60)) 
		{  
			duration++;  
			return;  
		}  
		else  
		{  
			if((current_tick % TICK_LIMIT) == 0) 
			{ 
				EchoDebug("Guidance Active");   
				GuideMissile();   
				current_tick = 0;   
			} 
		}
		current_tick++;      
	} 
	  
	EchoDebug("Has run?: " + init); 
}

/// PURPOSE: Initialize all shooter and guidance references for missile systems
/// INPUT  : None
/// OUTPUT : None
void Init()  
{  
	_cubeGrid = Me.CubeGrid;
	
	GridTerminalSystem.GetBlocksOfType<IMyGyro>(gyroList);
	GridTerminalSystem.GetBlocksOfType<IMyRemoteControl>(remotes);
	GridTerminalSystem.GetBlocksOfType<IMyTextPanel>(screenList);
	GridTerminalSystem.GetBlocksOfType<IMyTimerBlock>(timerBlocks);
	
// Establish Gyro References
  PruneGrid(ref gyroList);
	PruneGrid(ref screenList);
	PruneGrid(ref timerBlocks);
	
	foreach (IMyRemoteControl remote in remotes)
	{
		if(remote.CustomName.Contains(SHOOTER_REFERENCE_NAME) && remote.CubeGrid != _cubeGrid)   
		{     
			shooterReferenceList.Add(remote as IMyRemoteControl);   
			EchoDebug("Found Shooter");   
		}   

		if(remote.CubeGrid == _cubeGrid)   
		{   
			missileReferenceList.Add(remote as IMyRemoteControl);   
			EchoDebug("Found Missile");   
		}  
	}
	
	if(screenList.Count > 0)
	{
		statusMonitor = screenList[0] as IMyTextPanel;
		statusMonitor.ShowPublicTextOnScreen();
		statusMonitor.ApplyAction("OnOff_On");
	}

//Check if we do not have an shooter remote  
	if(shooterReferenceList.Count == 0)   
	{   
		EchoDebug("No shooter reference block found");   
		init = false; 
	}  
//Check if we do not have a missile remote  
	else if(missileReferenceList.Count == 0)   
	{   
		EchoDebug("No missile reference block found");   
		init = false;
	}   
//Check if we do not have a gyro
	else if(gyroList.Count == 0)  
	{  
		EchoDebug("No control gyro found");  
		init = false; 
	}
	else if(timerBlocks.Count == 0)
	{
		EchoDebug("No Timer Block Found");
		init = false; 
	}
//Ready to run!
	else  
	{     
		shooterReference = shooterReferenceList[0] as IMyRemoteControl;   
		missileReference = missileReferenceList[0] as IMyRemoteControl;   
		init = true;
				
		if(DEBUG)
		{
			EchoDebug("GyroCount: " + gyroList.Count);
			EchoDebug("Shooter: " + shooterReference.CustomName);	
			EchoDebug("Missile: " + missileReference.CustomName);
			EchoDebug("Ready to run"); 
		}
	}  
}  
 
void GuideMissile()    
{    
//---Get positions of our blocks with relation to world center  
	if(!shouldKill)
	{
		originPos = shooterReference.GetPosition();   
	}
	var missilePos = missileReference.GetPosition();   

//---Find current distance from shooter to missile  
	delta_origin = Vector3D.Distance(originPos, missilePos);  

//---Check if we are in range      
	if(delta_origin < MAX_DISTANCE) //change this later to be larger  
	{  
//---Get forward vector from our shooter vessel  
		if(!shouldKill) 
		{ 
			var shooterForward = shooterReference.Position + Base6Directions.GetIntVector(shooterReference.Orientation.TransformDirection(Base6Directions.Direction.Forward));    
			var targetVector = shooterReference.CubeGrid.GridIntegerToWorld(shooterForward);    
			targetVectorNorm = Vector3D.Normalize(targetVector - shooterReference.GetPosition());  
		} 
			
//---Find vector from shooter to missile  
		var missileVector = Vector3D.Subtract(missilePos, originPos);  

//---Calculate angle between shooter vector and missile vector  
		double dotProduct; Vector3D.Dot(ref targetVectorNorm, ref missileVector, out dotProduct);  
		double x = dotProduct / missileVector.Length();       
		double rawDevAngle = Math.Acos(x) * 180f / Math.PI; //angle between shooter vector and missile  

//---Calculate perpendicular distance from shooter vector  
		var projectionVector = dotProduct * targetVectorNorm;  
		double deviationDistance = Vector3D.Distance(projectionVector,missileVector);  
		EchoDebug("Angular Dev: " + rawDevAngle.ToString());  

//---Determine scaling factor 
		double scalingFactor;  
		if(rawDevAngle < 90)  
		{  
			if(deviationDistance > 200)  
			{  
				scalingFactor = delta_origin; //if we are too far from the beam, dont add any more distance till we are closer  
			}  
			else  
			{  
				scalingFactor = (delta_origin + 200); //travel approx. 100m from current position in direction of target vector  
			}  
		}  
		else  
		{  
			scalingFactor = 200; //if missile is behind the shooter, goes 200m directly infront of shooter for better accuracy  
		}  
		var destination = shooterReference.GetPosition() + scalingFactor * targetVectorNorm;   
		EchoDebug(destination.ToString()); //debug  

//---Find front left and top vectors of our missile 
		var missileGridX = missileReference.Position + Base6Directions.GetIntVector(missileReference.Orientation.TransformDirection(Base6Directions.Direction.Forward));  
		var missileWorldX = missileReference.CubeGrid.GridIntegerToWorld(missileGridX) - missilePos;  

		var missileGridY = missileReference.Position + Base6Directions.GetIntVector(missileReference.Orientation.TransformDirection(Base6Directions.Direction.Left));  
		var missileWorldY = missileReference.CubeGrid.GridIntegerToWorld(missileGridY) - missilePos;  

		var missileGridZ = missileReference.Position + Base6Directions.GetIntVector(missileReference.Orientation.TransformDirection(Base6Directions.Direction.Up));  
		var missileWorldZ = missileReference.CubeGrid.GridIntegerToWorld(missileGridZ) - missilePos;  

//---Find vector from missile to destination  
		var shipToTarget = Vector3D.Subtract(destination, missilePos);  

//---Project target vector onto our top left and up vectors  
		double dotX; Vector3D.Dot(ref shipToTarget, ref missileWorldX, out dotX);  
		double dotY; Vector3D.Dot(ref shipToTarget, ref missileWorldY, out dotY);  
		double dotZ; Vector3D.Dot(ref shipToTarget, ref missileWorldZ, out dotZ);  
		var projTargetX = dotX / (missileWorldX.Length() * missileWorldX.Length()) * missileWorldX;  
		var projTargetY = dotY / (missileWorldY.Length() * missileWorldY.Length()) * missileWorldY;  
		var projTargetZ = dotZ / (missileWorldZ.Length() * missileWorldZ.Length()) * missileWorldZ;  
		var projTargetXYplane = projTargetX + projTargetY; 

//---Get Yaw and Pitch Angles  
		double angleYaw = Math.Atan(projTargetY.Length() / projTargetX.Length());  
		double anglePitch = Math.Atan(projTargetZ.Length() / projTargetXYplane.Length());  

//---Check if x is positive or negative  
		double checkPositiveX; Vector3D.Dot(ref missileWorldX, ref projTargetX, out checkPositiveX); EchoDebug("check x:" + checkPositiveX.ToString());  
		if(checkPositiveX < 0)  
		{  
			angleYaw += Math.PI/2; 
		}  

//---Check if yaw angle is left or right  
		double checkYaw; Vector3D.Dot(ref missileWorldY, ref projTargetY, out checkYaw); EchoDebug("check yaw:" + checkYaw.ToString());  
		if(checkYaw > 0) //yaw is backwards
		{
			angleYaw = -angleYaw;  
		}
		EchoDebug("yaw angle:" + angleYaw.ToString());  

//---Check if pitch angle is up or down  
		double checkPitch; Vector3D.Dot(ref missileWorldZ, ref projTargetZ, out checkPitch); EchoDebug("check pitch:" + checkPitch.ToString());  
		if(checkPitch < 0)  
				anglePitch = -anglePitch;  
		EchoDebug("pitch angle:" + anglePitch.ToString());  

//---Angle controller  
		double max_rotation_radians = MAX_ROTATION_DEGREES * (Math.PI / 180); 
		double yawSpeed = max_rotation_radians * angleYaw / Math.Abs(angleYaw); 
		double pitchSpeed = max_rotation_radians * anglePitch / Math.Abs(anglePitch); 

		//Alt method 1: Proportional Control 
		//(small ship gyros too weak for this to be effective) 
		/* 
				double yawSpeed = angleYaw / Math.PI * max_rotation_radians;  
				double pitchSpeed = anglePitch / Math.PI * max_rotation_radians; 
		*/ 
		 
		//Alt method 2: Proportional Control with bounds 
		//(Small gyros still too weak :/) 
				/*if (angleYaw < Math.PI/4)  
				{  
						yawSpeed = angleYaw * max_rotation_radians / (Math.PI/4);  
				}  
				else  
				{  
						yawSpeed = max_rotation_radians;  
				}  
					
				if (anglePitch < Math.PI/4)  
				{  
						pitchSpeed = anglePitch * max_rotation_radians / (Math.PI/4);  
				}  
				else  
				{  
						pitchSpeed = max_rotation_radians;  
				}*/  

	//---Set appropriate gyro override 
		for(int i = 0; i < gyroList.Count; i++)  
		{  
			var thisGyro = gyroList[i] as IMyGyro;  
			thisGyro.SetValue<float>("Yaw", (float)yawSpeed);  
			thisGyro.SetValue<float>("Pitch", (float)pitchSpeed);  
			thisGyro.SetValue("Override", true);  
		}  
	}  
	else  
	{  
		EchoDebug("Out of range");  
		shouldKill = true; 
	}  
}  

/// PURPOSE: Engage systems and prepare to launch
/// INPUT  : None
/// OUTPUT : None
void MissileSystems()               
{
	GridTerminalSystem.GetBlocksOfType<IMyVirtualMass>(artMasses);
	GridTerminalSystem.GetBlocksOfType<IMyShipMergeBlock>(mergeBlocks);
	GridTerminalSystem.GetBlocksOfType<IMyBatteryBlock>(batteries);
	
	PruneGrid(ref artMasses);
	PruneGrid(ref mergeBlocks);
	PruneGrid(ref batteries);
 
//---Check for the required launch components 
	if(mergeBlocks.Count == 0) 
	{ 
		EchoDebug("No merges found"); 
	}   
	else if(artMasses.Count == 0) 
	{ 
		EchoDebug("No detach mass found"); 
	} 
	else if(batteries.Count == 0) 
	{ 
		EchoDebug("No batteries found"); 
	} 

	if (timeElapsed == 0)          
	{
		//statusMonitor.WritePublicText("Missile Armed...");
		for(int i = 0 ; i < batteries.Count ; i++)    
		{    
			var thisBattery = batteries[i] as IMyBatteryBlock;  
			thisBattery.ApplyAction("OnOff_On");
			thisBattery.SetValue("Recharge", false);     
		}   
	}    
	else if(timeElapsed == 60)    
	{
	//	statusMonitor.WritePublicText("Detaching...");
		for(int i = 0 ; i < artMasses.Count ; i++)       
		{       
			var thisMass = artMasses[i] as IMyVirtualMass;       
			thisMass.ApplyAction("OnOff_On");        
		}  
		EchoDebug("Detach Mass On");    

		for(int i = 0 ; i < mergeBlocks.Count ; i++)    
		{    
			var thisMerge = mergeBlocks[i] as IMyShipMergeBlock;    
			thisMerge.ApplyAction("OnOff_Off");    
		} 
		EchoDebug("Detach Merge blocks");
	}    
	else if(timeElapsed == 180)    
	{
		//statusMonitor.WritePublicText("Engaging...");
		for(int i = 0 ; i < artMasses.Count ; i++)       
		{       
			var thisMass = artMasses[i] as IMyVirtualMass;       
			thisMass.ApplyAction("OnOff_Off");        
		}
		ThrusterOverride();          
		ManeuveringThrust();         
		EchoDebug("Detach Mass Off");          
		EchoDebug("Main Thruster Override On"); 
		hasFired = true; 
	}                   

	if (timeElapsed < 180)
	{
		timeElapsed++;
	}
	
	if(DEBUG)
	{
		EchoDebug("artMasses:" + artMasses.Count);
		EchoDebug("mergeBlocks:" + mergeBlocks.Count);
		EchoDebug("batteries:" + batteries.Count); 
	}
} 

void EchoDebug(string text)
{
	if(DEBUG)
	{
		Echo(text);
	}
}

/// PURPOSE: Prune a list of IMyTerminalBlocks of non-grid blocks
/// INPUT  : None
/// OUTPUT : None 
void PruneGrid(ref List<IMyTerminalBlock> list)
{
	for(int x = 0; x < list.Count; x++)
	{
		var block = list[x];
		if(block.CubeGrid.ToString() != _cubeGrid.ToString())
		{
			list.Remove(block);
			x--;
		}
	}
}

/// PURPOSE: Engage maneuvering thrusters
/// INPUT  : None
/// OUTPUT : None 
void ManeuveringThrust()         
{         
	if(maneuveringThrusters.Count == 0) 
	{
		EchoDebug("No manoeuvring thrust found"); 
	}
	
	GridTerminalSystem.GetBlocksOfType<IMyThrust>(maneuveringThrusters);
	PruneGrid(ref maneuveringThrusters);
	
	foreach(IMyThrust thrust in maneuveringThrusters)       
	{
		thrust.ApplyAction("OnOff_On");          
	}
}         

/// PURPOSE: Engage thruster override 
/// INPUT  : None
/// OUTPUT : None 
void ThrusterOverride()          
{          
	if(forwardThrusters.Count == 0)
	{
		EchoDebug("No main thrust found"); 
	}
	
	GridTerminalSystem.GetBlocksOfType<IMyThrust>(forwardThrusters);
	PruneGrid(ref forwardThrusters);        
	
	foreach(IMyThrust thrust in forwardThrusters)             
	{
		if(thrust.BlockDefinition.SubtypeId == "SmallBlockLargeThrust")
		{
		  thrust.ApplyAction("OnOff_On"); 
		  thrust.SetValue<float>("Override", float.MaxValue);
		}
	}           
}