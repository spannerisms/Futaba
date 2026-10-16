namespace Futaba;

internal enum BreakpointsControl {
	Kill = -1,
	Off = 0,
	On,
}

/// <summary>
/// Represents a breakpoint in assembly code.
/// </summary>
public sealed class Breakpoint {

	private readonly BreakpointTriggers _triggers;

	/// <summary>
	/// Returns whether this breakpoint was declared to break execution on its address being read.
	/// </summary>
	public bool BreakOnRead => _triggers.HasFlag(BreakpointTriggers.Read);

	/// <summary>
	/// Returns whether this breakpoint was declared to break execution on its address being written.
	/// </summary>
	public bool BreakOnWrite => _triggers.HasFlag(BreakpointTriggers.Write);

	/// <summary>
	/// Returns whether this breakpoint was declared to break execution on its address being executed.
	/// </summary>
	public bool BreakOnExecute => _triggers.HasFlag(BreakpointTriggers.Execute);

	/// <summary>
	/// Returns the address this breakpoint is declared at.
	/// </summary>
	public int Address { get; }

	/// <summary>
	/// Gets the comment assigned to this instance.
	/// </summary>
	public string Comment { get; } = string.Empty;

	/// <summary>
	/// Gets the length of the range of addresses this instance breaks on.
	/// </summary>
	public int Length { get; } = 1;

	internal Breakpoint(int address, BreakpointTriggers triggers) {
		Address = address;
		_triggers = triggers;
	}

}

[Flags]
internal enum BreakpointTriggers {
	None = 0,
	Read = 1,
	Write = 2,
	Execute = 4,

	RWX = Read | Write | Execute
}

internal enum BreakpointInserts {
	Nothing = 0,
	NOP,
	WDM,
	BRK,
	COP,
}
