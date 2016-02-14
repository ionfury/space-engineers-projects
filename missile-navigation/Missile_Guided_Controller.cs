 
    //------------------------------------------------------  
 
    int TickCount; 
    int Clock = 5; 
    string TorpedoName = "Torpedo1"; 
    float GyroMult = 3f; 
    float ThrustMult = 1f; 
    float GyroConstrain = 3f; 
    float Roll = 0.5f; 
    float LockMaxDistance = 15000f; 
    float LockBase = 0.01f; 
    const float TorpVelocity = 100; 
    const float FThrust = 3000000; 
    const float HookDist = 2000f; 
 
    MyTorpedo thisTorpedo; 
    public static class FireModes 
    { 
        public const int Unguided = 0; 
        public const int LaserGuided = 1; 
        public const int LockOnChase = 2; 
        public const int LockOnHook = 3; 
        public const int LockOnIntercept = 4; 
        public const int LockOnCounterMissile = 5; 
    } 
    public static class LockingStates 
    { 
        public const int StartLock = 0; 
        public const int Locked = 1; 
        public const int SearchTargetByLaser = 2; 
        public const int SearchTargetByVelocity = 3; 
        public const int SearchTargetInConus = 4; 
    } 
    public static class Commands 
    { 
        public const int StartTrack = 0; 
        public const int ContinueTrack = 1; 
        public const int StopTrack = 2; 
        public const int Launch = 3; 
        public const int GyroLock = 4; 
        public const int GoOn = 5; 
        public const int Relock = 6; 
        public const int Terminate = 7; 
    } 
 
    void Main(string argument) 
    { 
        if (thisTorpedo == null) 
            thisTorpedo = new MyTorpedo(TorpedoName, this); 
        var Timer = GridTerminalSystem.GetBlockWithName(thisTorpedo.MyName + "TimerClock") as IMyTimerBlock; 
        TickCount++; 
 
        if (argument != "") 
        { 
            TickCount = 0; 
            switch (argument) 
            { 
                case "Forward": 
                    { 
                        thisTorpedo.FireMode = FireModes.Unguided; 
                        thisTorpedo.Command = Commands.Launch; 
                        break; 
                    } 
                case "Vector": 
                    { 
                        thisTorpedo.FireMode = FireModes.LaserGuided; 
                        thisTorpedo.Command = Commands.Launch; 
                        break; 
                    } 
                case "LockOnStart": 
                    { 
                        thisTorpedo.FireMode = FireModes.LockOnChase; 
                        thisTorpedo.Command = Commands.StartTrack; 
                        break; 
                    } 
                case "LockOnStop": 
                    { 
                        thisTorpedo.FireMode = FireModes.LockOnChase; 
                        thisTorpedo.Command = Commands.StopTrack; 
                        break; 
                    } 
                case "LockOnHook": 
                    { 
                        thisTorpedo.Hook = true; 
                        thisTorpedo.FireMode = FireModes.LockOnChase; 
                        thisTorpedo.Command = Commands.Launch; 
                        break; 
                    } 
                case "LockOnLaunch": 
                    { 
                        thisTorpedo.Hook = false; 
                        thisTorpedo.FireMode = FireModes.LockOnChase; 
                        thisTorpedo.Command = Commands.Launch; 
                        break; 
                    } 
                default: 
                    break; 
            } 
        } 
 
        if ((TickCount % Clock) == 0) 
        { 
            thisTorpedo.Update(); 
        } 
        if ((thisTorpedo.Command != Commands.Terminate)&&(thisTorpedo.Command != Commands.StopTrack)) 
        { 
            Timer.GetActionWithName("TriggerNow").Apply(Timer); 
        } 
        else 
        { 
            Timer.GetActionWithName("Stop").Apply(Timer); 
        } 
    } 
 
    public class MyTorpedo 
    { 
        public MyTorpedo(string TorpedoName, Program MyProg) 
        { 
            MyName = TorpedoName; 
            ParentProgram = MyProg; 
            navBlock = new MyNavigation(this); 
            thrustBlock = new MyThrusters(this); 
            gyroBlock = new MyGyros(this); 
            GyroMult = ParentProgram.GyroMult; 
            GyroConstrain = ParentProgram.GyroConstrain; 
           ThrustMult = ParentProgram.ThrustMult; 
            Roll = ParentProgram.Roll; 
            LockMaxDistance = ParentProgram.LockMaxDistance; 
            LockBase = ParentProgram.LockBase; 
            TP1 = ParentProgram.GridTerminalSystem.GetBlockWithName("TP1") as IMyTextPanel; 
            TP2 = ParentProgram.GridTerminalSystem.GetBlockWithName("TP2") as IMyTextPanel; 
            TP1.WritePublicTitle(TorpedoName); 
        } 
 
        private MyNavigation navBlock; 
        private MyThrusters thrustBlock; 
        private MyGyros gyroBlock; 
        private IMyTextPanel TP1; 
        private IMyTextPanel TP2; 
        private string CurrentStatus; 
        public bool Hook { get; set; } 
        public int FireMode { get; set; } 
        public int LockStatus { get; set; } 
        public int Command { get; set; } 
        public string MyName { get; set; } 
        public float GyroMult { get; set; } 
        public float GyroConstrain { get; set; } 
        public float ThrustMult { get; set; } 
        public float Roll { get; set; } 
        private float DodgeAngle { get; set; } 
        private float LockMaxDistance { get; set; } 
        private float LockBase { get; set; } 
        private string LastTorpedo { get; set; } 
 
        internal static Program ParentProgram; 
 
        public bool Relock() 
        { 
            if (!navBlock.LockStable) 
            { 
                navBlock.UpdateTransmitter(); 
                CurrentStatus = "Lock is LOST!"; 
                navBlock.UpdateLock(LockMaxDistance, LockBase, LockingStates.SearchTargetByLaser); 
                return false; 
            } 
            else 
                return true; 
        } 
 
        public void Update() 
        { 
            ParentProgram.Echo(Hook.ToString()); 
            if ((Command >= Commands.Launch) && (ParentProgram.TickCount > 12000)) 
            { 
                Command = Commands.Terminate; 
                CurrentStatus = "Terminated"; 
            } 
            if ((FireMode == FireModes.LockOnChase) && (Command == Commands.StartTrack)) 
            { 
                navBlock.UpdateLock(LockMaxDistance, LockBase, LockingStates.StartLock); 
                Command = Commands.ContinueTrack; 
                CurrentStatus = "Locking"; 
                UpdatePanelInfo(); 
            } 
 
            if ((FireMode == FireModes.LockOnChase) && (Command == Commands.ContinueTrack)) 
            { 
                navBlock.UpdateTransmitter(); 
                navBlock.UpdateLock(LockMaxDistance, LockBase, LockingStates.Locked); 
                navBlock.AnalyzeTargetVelocity(); 
                navBlock.FindInterceptVector(); 
                Command = Commands.ContinueTrack; 
                if (navBlock.LockStable) 
                    CurrentStatus = "Target Locked"; 
                UpdatePanelInfo(); 
            } 
            if ((FireMode == FireModes.LaserGuided) || (Command == Commands.Launch)) 
                navBlock.UpdateTransmitter(); 
 
            if (Command == Commands.Launch) 
            { 
                TurnGroup(MyName + "Merge", "Off"); 
                IMyTerminalBlock Beacon = ParentProgram.GridTerminalSystem.GetBlockWithName(MyName) as IMyTerminalBlock; 
                if (Beacon != null) 
                    Beacon.GetActionWithName("OnOff_On").Apply(Beacon); 
                gyroBlock.Turn("On"); 
                gyroBlock.SetOverride(false); 
                thrustBlock.Turn("On"); 
                TurnGroup(MyName + "Sensor", "On"); 
                IMyWarhead WarH = ParentProgram.GridTerminalSystem.GetBlockWithName(MyName + "Warhead") as IMyWarhead; 
                if (WarH != null) 
                    WarH.ApplyAction("StartCountdown"); 
                ParentProgram.TickCount = 0; 
                thrustBlock.SetOverride("F", FThrust); 
                if (FireMode == FireModes.LockOnChase) 
                { 
                    navBlock.OriginalSignature = navBlock.TargetSignature; 
                    CurrentStatus = "Launch"; 
                    UpdatePanelInfo(); 
                } 
                Command = Commands.GyroLock; 
            } 
            if (Command == Commands.GyroLock) 
            { 
                if (FireMode == FireModes.LockOnChase) 
                { 
                    if ((ParentProgram.TickCount >= 30) && (navBlock.Check6(1, 1))) 
                    { 
                        navBlock.UpdateLock(LockMaxDistance, LockBase, LockingStates.Locked); 
                        if (navBlock.LockStable) 
                        { 
                            Command = Commands.GoOn; 
                            CurrentStatus = "Re-Locking"; 
                        } 
                        else if (ParentProgram.TickCount >= 120) 
                        { 
                            Relock(); 
                        } 
                        UpdatePanelInfo(); 
                    } 
                } 
                else if (ParentProgram.TickCount >= 120) 
                    Command = Commands.GoOn; 
            } 
            if (Command == Commands.GoOn) 
            { 
                Vector3D GyroOver = new Vector3D(); 
                if ((FireMode == FireModes.LaserGuided) || (FireMode == FireModes.Unguided)) 
                { 
                    GyroOver = navBlock.GetAnglesLaser(new Vector3D(0, 0, 250)); 
                } 
                else if (FireMode == FireModes.LockOnChase) 
                { 
 
                    navBlock.UpdateLock(LockMaxDistance, LockBase, LockingStates.Locked); 
                    if (Relock()) 
                    { 
                        navBlock.AnalyzeTargetVelocity(); 
                        navBlock.FindInterceptVector(); 
                        GyroOver = navBlock.GetWorldAngles(navBlock.TargetGPS); 
                        CurrentStatus = "Chasing"; 
                        //ParentProgram.Echo(GyroOver.ToString()); 
                        if (!navBlock.LockStable) 
                            Command = Commands.GyroLock; 
                        if ((ParentProgram.TickCount >= 300) && (navBlock.LockStable) && (navBlock.TargetVelStable)) 
                            if (Hook) 
                                FireMode = FireModes.LockOnHook; 
                            else 
                                FireMode = FireModes.LockOnIntercept; 
                    } 
                    UpdatePanelInfo(); 
                } 
                else if (FireMode == FireModes.LockOnHook) 
                { 
                    navBlock.UpdateLock(LockMaxDistance, LockBase, LockingStates.Locked); 
                    if (Relock()) 
                    { 
                        navBlock.AnalyzeTargetVelocity(); 
                        navBlock.FindInterceptVector(TorpVelocity); 
                        CurrentStatus = "Hook"; 
                        GyroOver = navBlock.GetWorldAngles(Vector3D.Normalize(navBlock.InterceptVector - navBlock.MyPos)*(navBlock.TargetVector.Length()-HookDist)/navBlock.TargetVector.Length() + Vector3D.Normalize(navBlock.TargetVelVector)*0.4f + navBlock.MyPos); 
                        if (((navBlock.TargetTang / navBlock.TargetOrth > -1) && (navBlock.TargetTang / navBlock.TargetOrth < 0)) || (navBlock.TargetVector.Length() < HookDist) || (navBlock.TargetVelVector.Length() < 5)) 
                            FireMode = FireModes.LockOnIntercept; 
                    } 
                    UpdatePanelInfo(); 
                } 
                else if (FireMode == FireModes.LockOnIntercept) 
                { 
                    navBlock.UpdateLock(LockMaxDistance, LockBase, LockingStates.Locked); 
                    if (Relock()) 
                    { 
                        if (navBlock.TargetVector.Length() > navBlock.OriginalSignature * 1.5) 
                        { 
                            navBlock.AnalyzeTargetVelocity(); 
                            navBlock.FindInterceptVector(TorpVelocity); 
                            CurrentStatus = "Intercepting"; 
                        } 
                        else 
                            CurrentStatus = "FIXED"; 
                        GyroOver = navBlock.GetWorldAngles(navBlock.InterceptVector); 
                    } 
                    UpdatePanelInfo(); 
 
                } 
                ParentProgram.Echo(GyroOver.ToString()); 
                GyroOver.SetDim(0, Math.Round(GyroOver.GetDim(0), 4) * GyroMult); 
                GyroOver.SetDim(1, Math.Round(GyroOver.GetDim(1), 4) * GyroMult); 
 
                for (int x = 0; x <= 2; x++) 
                { 
                    if (GyroOver.GetDim(x) > GyroConstrain) 
                        GyroOver.SetDim(x, GyroConstrain); 
                    else if (GyroOver.GetDim(x) < -GyroConstrain) 
                        GyroOver.SetDim(x, -GyroConstrain); 
                } 
                if (ParentProgram.TickCount > 240) 
                    if (Math.Abs(Roll)>0.01f) 
                        GyroOver.SetDim(2, -Roll); 
                gyroBlock.SetOverride(true, GyroOver, 1); 
                //ParentProgram.Echo(GyroOver.ToString()); 
            } 
        } 
        public void UpdatePanelInfo() 
        { 
            if ((TP1 != null) && (TP2 != null)) 
            { 
                LastTorpedo = TP1.GetPublicTitle(); 
                if (LastTorpedo == MyName) 
                { 
                    string Output = ""; 
                    Output += " Lock Stable: " + navBlock.LockStable.ToString().ToUpperInvariant() + "\n"; 
                    Output += " Ready To Go: " + navBlock.TargetVelStable.ToString().ToUpperInvariant() + "\n"; 
                    Output += " Target data: \n"; 
                    Output += " X: " + Math.Round(navBlock.TargetGPS.GetDim(0)) + "\n Y: " + Math.Round(navBlock.TargetGPS.GetDim(1)) + "\n Z: " + Math.Round(navBlock.TargetGPS.GetDim(2)) + "\n"; 
                    Output += " Signature: " + Math.Round(navBlock.TargetSignature) + "\n"; 
                    Output += " Distance: " + Math.Round(navBlock.TargetVector.Length()) + "\n"; 
                    Output += " Velocity: " + Math.Round(navBlock.TargetVelVector.Length(), 3) + "\n"; 
                    TextOutput(1, Output); 
                    Output = ""; 
                    Output += " Can Intercept: " + navBlock.CanIntercept.ToString().ToUpperInvariant() + "\n"; 
                    Output += " Status: " + CurrentStatus + "\n"; 
                    Output += " Tang Vel: " + Math.Round(navBlock.TargetTang) + "\n"; 
                    Output += " Orth Vel: " + Math.Round(navBlock.TargetOrth) + "\n"; 
                    Output += " Intercept Time: " + Math.Round(navBlock.InterceptTime) + "\n"; 
                    Output += " Laser Pointer: \n"; 
                    Output += " X: " + Math.Round(navBlock.GuidingVector.GetDim(0)) + "\n Y: " + Math.Round(navBlock.GuidingVector.GetDim(1)) + "\n Z: " + Math.Round(navBlock.GuidingVector.GetDim(2)) + "\n"; 
                    TextOutput(2, Output); 
                } 
            } 
        } 
        public void TextOutput(int Screen, string Output = "") 
        { 
            IMyTextPanel ScrObj; 
            if (Screen == 1) 
                ScrObj = TP1; 
            else 
                ScrObj = TP2; 
            if (ScrObj != null) 
            { 
                ScrObj.ShowTextureOnScreen(); 
                if (Output != "") 
                { 
                    ScrObj.WritePublicText(Output); 
                } 
                ScrObj.ShowPublicTextOnScreen(); 
                ScrObj.GetActionWithName("OnOff_On").Apply(ScrObj); 
            } 
        } 
        private static void TurnGroup(string t, string OnOff) 
        { 
            var GrItems = GetBlocksFromGroup(t); 
            for (int i = 0; i < GrItems.Count; i++) 
            { 
                var GrItem = GrItems[i] as IMyTerminalBlock; 
                GrItem.GetActionWithName("OnOff_" + OnOff).Apply(GrItem); 
            } 
        } 
        private static List<IMyTerminalBlock> GetBlocksFromGroup(string group) 
        { 
            var blocks = new List<IMyTerminalBlock>(); 
            ParentProgram.GridTerminalSystem.SearchBlocksOfName(group, blocks); 
            if (blocks != null) 
            { return blocks; } 
            throw new Exception("GetBlocksFromGroup: Group \"" + group + "\" not found"); 
        } 
 
        private class MyThrusters 
        { 
            public MyThrusters(MyTorpedo myTorp) 
            { 
                myTorpedo = myTorp; 
            } 
            private MyTorpedo myTorpedo; 
            private static string Prefix = "Thr"; 
            public void SetOverride(string axis, float OverrideValue) 
            { 
                var Thrusts = new List<IMyTerminalBlock>(); 
                ParentProgram.GridTerminalSystem.SearchBlocksOfName(myTorpedo.MyName + Prefix + axis, Thrusts); 
                for (int i = 0; i < Thrusts.Count; i++) 
                { 
                    IMyThrust Thrust = Thrusts[i] as IMyThrust; 
                    if (Thrust != null) 
                        Thrust.SetValue("Override", OverrideValue / Thrusts.Count); 
                } 
            } 
            public void Turn(string OnOff) 
            { 
                TurnGroup(ParentProgram.thisTorpedo.MyName + "Thr", OnOff); 
            } 
        } 
 
        private class MyGyros 
        { 
            private MyTorpedo myTorpedo; 
            private static string Prefix = "Gyro"; 
 
            public MyGyros(MyTorpedo myTorp) 
            { 
                myTorpedo = myTorp; 
            } 
 
            public void Turn(string OnOff) 
            { 
                TurnGroup(ParentProgram.thisTorpedo.MyName + "Gyro", OnOff); 
            } 
            public void SetOverride(bool OverrideOnOff = true, string axis = "", float OverrideValue = 0, float Power = 1) 
            { 
                var Gyros = new List<IMyTerminalBlock>(); 
                ParentProgram.GridTerminalSystem.SearchBlocksOfName(myTorpedo.MyName + Prefix, Gyros); 
                for (int i = 0; i < Gyros.Count; i++) 
                { 
                    IMyGyro Gyro = Gyros[i] as IMyGyro; 
                    if (Gyro != null) 
                    { 
                        if ((!Gyro.GyroOverride) && OverrideOnOff) 
                            Gyro.ApplyAction("Override"); 
                        Gyro.SetValue("Power", Power); 
                        if (axis != "") 
                            Gyro.SetValue(axis, OverrideValue); 
                    } 
                } 
            } 
            public void SetOverride(bool OverrideOnOff, Vector3 settings, float Power = 1) 
            { 
                var Gyros = new List<IMyTerminalBlock>(); 
                ParentProgram.GridTerminalSystem.SearchBlocksOfName(myTorpedo.MyName + Prefix, Gyros); 
                for (int i = 0; i < Gyros.Count; i++) 
                { 
                    IMyGyro Gyro = Gyros[i] as IMyGyro; 
                    if (Gyro != null) 
                    { 
                        if ((!Gyro.GyroOverride) && OverrideOnOff) 
                            Gyro.ApplyAction("Override"); 
                        Gyro.SetValue("Power", Power); 
                        Gyro.SetValue("Yaw", settings.GetDim(0)); 
                        Gyro.SetValue("Pitch", settings.GetDim(1)); 
                        Gyro.SetValue("Roll", settings.GetDim(2)); 
                    } 
                } 
            } 
 
        } 
 
        private class MyNavigation 
        { 
            private MyTorpedo myTorpedo; 
            private IMyRemoteControl RemCon; 
            private IMyTerminalBlock Transmitter; 
            public MatrixD InvMatrix { get; private set; } 
            public Vector3D GuidingVector { get; private set; } 
            public Vector3D TargetGPS { get; private set; } 
            public Vector3D TargetVector { get; private set; } 
            public Vector3D MyPos { get; private set; } 
            public Vector3D MyPrevPos { get; private set; } 
            public double TargetSignature { get; private set; } 
            public double TargetPrevSignature { get; private set; } 
            public double OriginalSignature { get; set; } 
            public Vector3D TargetVelVector { get; private set; } 
            public Vector3D PrevTargetVelVector { get; private set; } 
            private Vector3D PrevPos; 
            private List<Vector3D> VelVectors; 
            public Vector3D InterceptVector; 
            private int iVect; 
            public bool LockStable { get; private set; } 
            public bool TargetVelStable { get; private set; } 
            public bool CanIntercept { get; private set; } 
            public double TargetOrth { get; private set; } 
            public double TargetTang { get; private set; } 
            public double InterceptTime { get; private set; } 
 
            private int PrevTick; 
            private int TickPassed; 
            public MyNavigation(MyTorpedo myTorp) 
            { 
                VelVectors = new List<Vector3D>(); 
                List<IMyTerminalBlock> blocks = new List<IMyTerminalBlock>(); 
                myTorpedo = myTorp; 
                RemCon = ParentProgram.GridTerminalSystem.GetBlockWithName(myTorpedo.MyName + "RemCon") as IMyRemoteControl; 
                ParentProgram.GridTerminalSystem.SearchBlocksOfName("<Transmitter>", blocks); 
                if (blocks[0] != null) 
                    Transmitter = blocks[0]; 
            } 
 
            public double GetVal(string Key) 
            { 
                string val = "0"; 
                string pattern = @"(" + Key + "):([^:^;]+);"; 
                System.Text.RegularExpressions.Match match = System.Text.RegularExpressions.Regex.Match(Transmitter.CustomName, pattern); 
                if (match.Success) 
                { 
                    val = match.Groups[2].Value; 
                } 
                return Convert.ToDouble(val); 
            } 
            public string GetValStr(string Key) 
            { 
                string val = "0"; 
                string pattern = @"(" + Key + "):([^:^;]+);"; 
                System.Text.RegularExpressions.Match match = System.Text.RegularExpressions.Regex.Match(Transmitter.CustomName, pattern); 
                if (match.Success) 
                { 
                    val = match.Groups[2].Value; 
                } 
                return val; 
            } 
 
            public void UpdateTransmitter() 
            { 
                InvMatrix = new MatrixD(GetVal("M11"), GetVal("M12"), GetVal("M13"), GetVal("M14"), 
                                        GetVal("M21"), GetVal("M22"), GetVal("M23"), GetVal("M24"), 
                                        GetVal("M31"), GetVal("M32"), GetVal("M33"), GetVal("M34"), 
                                        GetVal("M41"), GetVal("M42"), GetVal("M43"), GetVal("M44")); 
                GuidingVector = new Vector3D(GetVal("X"), GetVal("Y"), GetVal("Z")); 
            } 
 
            public Vector3D GetAnglesLaser(Vector3D Target, bool Zfix=false) 
            { 
                MyPrevPos = MyPos; 
                MyPos = RemCon.GetPosition(); 
                Vector3D V3Dcenter = RemCon.GetPosition(); 
                Vector3D V3Dfow = RemCon.WorldMatrix.Forward + V3Dcenter; 
                Vector3D V3Dup = RemCon.WorldMatrix.Up + V3Dcenter; 
                Vector3D V3Dleft = RemCon.WorldMatrix.Left + V3Dcenter; 
 
                V3Dcenter = Vector3D.Transform(V3Dcenter, InvMatrix); 
                Target += new Vector3D(0, 0, V3Dcenter.GetDim(2)); 
                V3Dfow = (Vector3D.Transform(V3Dfow, InvMatrix)) - V3Dcenter; 
                V3Dup = (Vector3D.Transform(V3Dup, InvMatrix)) - V3Dcenter; 
                V3Dleft = (Vector3D.Transform(V3Dleft, InvMatrix)) - V3Dcenter; 
 
                Vector3D TargetNorm = Vector3D.Normalize(Target - V3Dcenter); 
                double TargetYaw = Math.Acos(Vector3D.Dot(V3Dleft, TargetNorm)) * 180 / Math.PI - 90; 
                double TargetPitch = Math.Acos(Vector3D.Dot(V3Dup, TargetNorm)) * 180 / Math.PI - 90; 
                double TargetRoll; 
                if (Zfix) 
                { 
                    TargetRoll = Math.Acos(V3Dup.GetDim(0) / Math.Sin(Math.Acos(V3Dfow.GetDim(0)))) * 180 / Math.PI - 90; 
                    if (!(Math.Abs(TargetRoll) > 0)) { TargetRoll = 0; } 
                    if (V3Dleft.GetDim(0) > 0) 
                       TargetRoll = -TargetRoll; 
                } 
                else { 
                    Vector3D V3DampNorm = Vector3D.Normalize(Vector3D.Reject((MyPos - MyPrevPos), RemCon.WorldMatrix.Forward)); 
                    TargetRoll = Math.Acos(Vector3D.Dot(RemCon.WorldMatrix.Up, -V3DampNorm)); 
                    if ((RemCon.WorldMatrix.Left - V3DampNorm).Length() > Math.Sqrt(2)) 
                        TargetRoll = -TargetRoll; 
                } 
 
                return new Vector3D(TargetYaw, -TargetPitch, TargetRoll); 
            } 
 
            public Vector3D GetWorldAngles(Vector3D Target) 
            { 
                Vector3D V3Dcenter = RemCon.GetPosition(); 
                Vector3D V3Dfow = RemCon.WorldMatrix.Forward; 
                Vector3D V3Dup = RemCon.WorldMatrix.Up; 
                Vector3D V3Dleft = RemCon.WorldMatrix.Left; 
 
                Vector3D TargetNorm = Vector3D.Normalize(Target - V3Dcenter); 
                double TargetYaw = Math.Acos(Vector3D.Dot(V3Dleft, TargetNorm)) * 180 / Math.PI - 90; 
                double TargetPitch = Math.Acos(Vector3D.Dot(V3Dup, TargetNorm)) * 180 / Math.PI - 90; 
 
                Vector3D V3DampNorm = Vector3D.Normalize(Vector3D.Reject((MyPos - MyPrevPos), RemCon.WorldMatrix.Forward)); 
                double TargetRoll = Math.Acos(Vector3D.Dot(RemCon.WorldMatrix.Up, -V3DampNorm)); 
                if ((RemCon.WorldMatrix.Left - V3DampNorm).Length() > Math.Sqrt(2)) 
                    TargetRoll = -TargetRoll; 
 
                return new Vector3D(TargetYaw, -TargetPitch, TargetRoll); 
            } 
 
            public void AnalyzeTargetVelocity() 
            { 
                if (iVect > 11) 
                    iVect = 0; 
                if (VelVectors.Count < 12) 
                { 
                    VelVectors.Add(TargetGPS - PrevPos); 
                    TargetVelVector += (TargetGPS - PrevPos); 
                    PrevTargetVelVector = TargetVelVector; 
                    TargetVelStable = false; 
                } 
                else 
                { 
                    TargetVelVector -= VelVectors[iVect]; 
                    VelVectors[iVect] = TargetGPS - PrevPos; 
                    TargetVelVector += VelVectors[iVect]; 
                    if (((TargetVelVector - PrevTargetVelVector).Length() < 3) && (LockStable)) 
                        TargetVelStable = true; 
                    else 
                        TargetVelStable = false; 
                    PrevTargetVelVector = TargetVelVector; 
                } 
                iVect++; 
            } 
 
 
 
            public void UpdateLock(float Distance, float Base, int LockStatus) 
            { 
                Vector3D O1, O2, F1, F2; 
                //     ParentProgram.Echo(ParentProgram.TickCount.ToString()); 
                //     ParentProgram.Echo((MyPos-MyPrevPos).ToString()); 
                //     ParentProgram.Echo((MyPos - MyPrevPos).Length().ToString()); 
                MyPrevPos = MyPos; 
                MyPos = RemCon.GetPosition(); 
                ParentProgram.Echo(MyPos.ToString()); 
                if (ParentProgram.TickCount < PrevTick) 
                { 
                    PrevTick = 0; 
                } 
                else 
                { 
                    TickPassed = ParentProgram.TickCount - PrevTick; 
                    PrevTick = ParentProgram.TickCount; 
                } 
                Vector3D Target; 
                if (LockStatus == LockingStates.StartLock) 
                { 
                    Target = RemCon.GetPosition() + RemCon.WorldMatrix.Forward; 
                } 
                else if (LockStatus == LockingStates.Locked) 
                { 
                    if (TargetVelStable) 
                        Target = TargetGPS + (TargetVelVector * (TickPassed / 60)); 
                    else 
                        Target = TargetGPS; 
                    Distance = (float)((Target - MyPos).Length() + 100); 
                } 
                else 
                { 
                    Target = GuidingVector; 
                } 
 
                O1 = Target; 
                F1 = RemCon.GetFreeDestination(O1, Distance, Base); 
                if (O1 != F1) 
                { 
                    O2 = Vector3D.Reflect((MyPos - F1), Vector3D.Normalize(O1 - MyPos)) + MyPos; 
                    F2 = RemCon.GetFreeDestination(O2, Distance, Base); 
                    int counter = 0; 
                    while ((GetAngle(F1 - MyPos, F2 - MyPos) < GetAngle(O1 - MyPos, F2 - MyPos)) && (counter < 4)) 
                    { 
                        O2 = Vector3D.Reflect((MyPos - F2), Vector3D.Normalize(O2 - MyPos)) + MyPos; 
                        F2 = RemCon.GetFreeDestination(O2, Distance, Base); 
                        counter++; 
                    } 
                    counter = 0; 
                    if (O2 == F2) 
                    { 
                        while ((O2 == F2) && (counter < 4)) 
                        { 
                            O2 = (Vector3D.Normalize(O1 - MyPos) + Vector3D.Normalize(O2 - MyPos)) / 2 + MyPos; 
                            F2 = RemCon.GetFreeDestination(O2, Distance, Base); 
                            counter++; 
                        } 
                    } 
                    TargetPrevSignature = TargetSignature; 
                    TargetSignature = (F2 - F1).Length(); 
                    if ((TargetSignature > 30) && (TargetSignature < 1000) && ((Math.Abs(TargetSignature - TargetPrevSignature) / TargetSignature) < 0.05)) 
                        LockStable = true; 
                    else 
                        LockStable = false; 
                    PrevPos = TargetGPS; 
                    TargetGPS = (F2 + F1) / 2; 
                    TargetVector = TargetGPS - MyPos; 
                } 
            } 
            public bool Check6(float Distance, float Base) 
            { 
                Vector3D Vec6 = RemCon.GetPosition() + RemCon.WorldMatrix.Forward; 
                return (Vec6 == RemCon.GetFreeDestination(Vec6, Distance, Base)); 
            } 
 
            double GetAngle(Vector3D Vector1, Vector3D Vector2) 
            { 
                return Math.Acos(Vector3D.Dot(Vector3D.Normalize(Vector1), Vector3D.Normalize(Vector2))); 
            } 
            public void FindInterceptVector(float shotSpeed = TorpVelocity) 
            { 
                Vector3D shotOrigin = RemCon.GetPosition(); 
                Vector3D dirToTarget = Vector3D.Normalize(TargetGPS - shotOrigin); 
                Vector3D targetVelOrth = Vector3D.Dot(TargetVelVector, dirToTarget) * dirToTarget; 
                Vector3D targetVelTang = TargetVelVector - targetVelOrth; 
                Vector3D shotVelTang = targetVelTang; 
                double shotVelSpeed = shotVelTang.Length(); 
                double shotSpeedOrth = 0; 
                if (shotVelSpeed > shotSpeed) 
                { 
                    InterceptVector = Vector3D.Normalize(TargetVelVector) * shotSpeed; 
                    CanIntercept = false; 
                    InterceptTime = 0; 
                } 
                else 
                { 
                    shotSpeedOrth = 
                    Math.Sqrt(shotSpeed * shotSpeed - shotVelSpeed * shotVelSpeed); 
                    Vector3 shotVelOrth = dirToTarget * shotSpeedOrth; 
                    InterceptVector = (shotVelOrth + shotVelTang) * 300 + shotOrigin; 
                    CanIntercept = true; 
                } 
                TargetTang = targetVelTang.Length(); 
                TargetOrth = targetVelOrth.Length(); 
                if ((dirToTarget + targetVelOrth).Length() < (dirToTarget - targetVelOrth).Length()) 
                    TargetOrth = -1 * TargetOrth; 
                InterceptTime = TargetVector.Length() / (shotSpeedOrth - TargetOrth); 
 
            } 
 
 
        } 
    } 
 
    //------------------------------------------------------  
