namespace Futaba.Symbols;

/// <summary>
/// Represents a self-growing region of address space.
/// </summary>
public sealed class Dock : Symbol {
	private int _address;
	private int _romaddress;
	private int _binaryoffset;
	private readonly int _capacity;

	internal DockState State { get; private set; }

	/// <summary>
	/// Returns the binary offset of the beginning of this dock.
	/// </summary>
	public int BaseOffset { get; }

	/// <summary>
	/// Returns the address of the beginning of this dock.
	/// </summary>
	public int BaseAddress { get; }

	/// <summary>
	/// Returns the ROM address of the beginning of this dock.
	/// </summary>
	public int BaseRomAddress { get; }

	/// <summary>
	/// Returns the number of bytes the dock has advanced.
	/// </summary>
	public int Distance => BinaryOffset - BaseOffset;

	/// <inheritdoc/>
	public override int DefaultValue => Address;

	/// <summary>
	/// Returns the current address of the dock.
	/// </summary>
	public override int Address => _address;

	/// <summary>
	/// Returns the current ROM address of the dock.
	/// </summary>
	public override int RomAddress => _romaddress;

	/// <summary>
	/// Returns the current binary offset of the dock.
	/// </summary>
	public override int BinaryOffset => _binaryoffset;

	internal Dock(string name, int address, int romaddress, int binoffset, int capacity) : base(name) {
		BaseAddress = address;
		BaseRomAddress = romaddress;
		BaseOffset = binoffset;

		_address = address;
		_romaddress = romaddress;
		_binaryoffset = binoffset;

		_capacity = capacity;
		State = capacity < 1 ? DockState.Unprotected : DockState.Protected;
	}

	internal void Advance(int step, Func<int, int> mapper) {
		_binaryoffset += step;
		_address += step;
		_romaddress = mapper(_binaryoffset);

		if (State is DockState.Protected) {
			if ((_binaryoffset - BaseOffset) < 0) {
				State = DockState.Overflowed;
			}
		}
	}

	internal bool OffsetIsInRegion(int offset) {
		int test = offset - BaseOffset;

		return ((uint) test) < _capacity;
	}

	internal override bool TryGetProperty(SymbolContext prop, out int val) {
		switch (prop) {
			case SymbolContext.Default:
			case SymbolContext.Provenance:
				val = Address;
				return true;

			case SymbolContext.RomAddress:
				val = RomAddress;
				return true;

			case SymbolContext.BinaryOffset:
				val = BinaryOffset;
				return true;

			case SymbolContext.Capacity:
				val = _capacity;
				return true;

			case SymbolContext.Size:
				val = _binaryoffset - BaseOffset;
				return true;
		}

		val = 0;
		return false;
	}

}