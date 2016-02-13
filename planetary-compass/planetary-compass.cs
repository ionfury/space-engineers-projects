const string remoteName = "[Heading]";
const string compassDisplayName = "[CompassDisplay]";
const double rad2deg = 180 / Math.PI; //constant to convert radians to degrees  
const string compassFormat = "-350--355--="  
    + "N=--005--010--015--020--025--030--035--040-=N.E=-050--055--060--065--070--075--080--085--=E=--095--100"  
    + "--105--110--115--120--125--130-=S.E=-140--145--150--155--160--165--170--175--=S=--185--190--195--200"  
    + "--205--210--215--220-=S.W=-230--235--240--245--250--255--260--265--=W=--275--280--285--290--295--300"  
    + "--305--310-=N.W=-320--325--330--335--340--345--350--355--="  
    + "N=--005--010-";  

List<IMyTerminalBlock> remotes = new List<IMyTerminalBlock>();
List<IMyTerminalBlock> screens = new List<IMyTerminalBlock>();

IMyRemoteControl remote;
Vector3D absoluteNorth = new Vector3D(0, 0, 1); // z is north

/// System.Type generic stuff isn't allowed
/// Determines if a block is of type IMyRemoteControl
bool isIMyRemoteControl(IMyTerminalBlock block)
{
	var remote = block as IMyRemoteControl;
	return remote != null;
}

bool isIMyTextPanel(IMyTerminalBlock block)
{
	var panel = block as IMyTextPanel;
	return panel != null;
}

Vector3D VectorProjection(Vector3D a, Vector3D b)
{
	Vector3D projection = a.Dot(b) / b.Length() / b.Length() * b;
	return projection;
}

void Main()
{
  if(Init())
	{
		var bearing = Bearing();
		WriteBearing(bearing);
    Echo(string.Format("{0:000}", Math.Round(bearing)));
	}
	else
	{
		Echo("Initalization failure.");
	}
}

/// Initialize variables if need be
bool Init()
{
	bool init = true;
	
	if(remotes.Count == 0 || screens.Count == 0 || remote == null)
	{
		GridTerminalSystem.SearchBlocksOfName(remoteName, remotes, isIMyRemoteControl);
		GridTerminalSystem.SearchBlocksOfName(compassDisplayName, screens, isIMyTextPanel);
		
		if(remotes.Count == 0)
		{
			Echo("No IMyRemoteControl block found with tag '" + remoteName + "' found.");
			init = false;
		}
		else if (screens.Count == 0)
		{
			Echo("No IMyTextPanel block found with tag '" + compassDisplayName + "' found.");
			init = false;
		}
		
		remote = remotes[0] as IMyRemoteControl;
		
		foreach(var thisScreen in screens)
		{
			var screen = thisScreen as IMyTextPanel;
			if(screen != null)
			{
				screen.ShowPublicTextOnScreen();
				screen.SetValue("FontSize", 1.55f);
				screen.SetValue("FontColor", new Color(0,200,0));
			}
		}
	}
	return init;	
}

/// calculate our bering (0-360 degrees)
double Bearing()
{
	double bearing = 0;
	
	// get orientation vectors for craft
	MatrixD orientation = remote.WorldMatrix;
	Vector3D forward = orientation.Forward;
	
	// get gravity vector
	Vector3D gravity = remote.GetNaturalGravity();
	
	double gravityMagnitude = gravity.Length();
	if(double.IsNaN(gravityMagnitude) || gravityMagnitude == 0)
	{
		Echo("Not in a gravity well!");
		bearing = -1;
		return bearing;
	}
	
	// find relative vectors
	Vector3D relativeEast  = gravity.Cross(absoluteNorth);
	Vector3D relativeNorth = relativeEast.Cross(gravity);
	
	Vector3D forwardNorth = VectorProjection(forward, relativeNorth);
	Vector3D forwardEast = VectorProjection(forward, relativeEast);
	Vector3D forwardPlane = forwardEast + forwardNorth;
	
	bearing = Math.Acos(forwardPlane.Dot(relativeNorth) / forwardPlane.Length() / relativeNorth.Length()) * rad2deg;
	
	double dotForwardOnEast = forward.Dot(relativeEast);
	
	if(dotForwardOnEast < 0)
	{
		bearing = 360 - bearing;
	}
	
	return bearing;
}

/// take a 360 degree bering and convert it into something we can print
void WriteBearing(double bearing)
{
	var cardinalDirection = "";
	//get cardinal direction  
	if(bearing < 22.5 || bearing >= 337.5)  
	{  
			cardinalDirection = "N";  
	}  
	else if(bearing < 67.5)  
	{  
			cardinalDirection = "NE";  
	}  
	else if(bearing < 112.5)  
	{  
			cardinalDirection = "E";  
	}  
	else if(bearing < 157.5)  
	{  
			cardinalDirection = "SE";  
	}  
	else if(bearing < 202.5)  
	{  
			cardinalDirection = "S";  
	}  
	else if(bearing < 247.5)  
	{  
			cardinalDirection = "SW";  
	}  
	else if(bearing < 292.5)  
	{  
			cardinalDirection = "W";  
	}  
	else if(bearing < 337.5)  
	{  
			cardinalDirection = "NW";  
	}  
	
	var message = "Bearing: " + string.Format("{0:000}", Math.Round(bearing))
			+ " " + cardinalDirection  
			+ "\n[" + compassFormat.Substring((int)Math.Floor(bearing), 25)  
			+ "]\n" + "------------------^------------------";  
	
	
	foreach(var thisScreen in screens)
	{
		var screen = thisScreen as IMyTextPanel;
		if(screen != null)
		{
			screen.WritePublicText(message);
		}
	}
}