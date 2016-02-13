List<IMyTerminalBlock> _blocks;
List<IMyTerminalBlock> _gridBlocks;
IMyProgrammableBlock _activeProgram;

void Main (string argument)
{
  if (_blocks != GridTerminalSystem.Blocks)
  {
    _blocks = GridTerminalSystem.Blocks;
    GridTerminalSystem.GetBlocksOfType<IMyTerminalBlock>(_gridBlocks, block => block.CubeGrid = Me.CubeGrid);
  }
}