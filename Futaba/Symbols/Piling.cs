namespace Futaba.Symbols;

/// <summary>
/// Represents a self-advancing data table symbol.
/// </summary>
public sealed class Piling : Symbol {
	/// <inheritdoc/>
	public override int DefaultValue => Address;

	/// <inheritdoc/>
	public override int Address { get; }

	/// <inheritdoc/>
	public override int RomAddress { get; }

	/// <inheritdoc/>
	public override int BinaryOffset { get; }

	/// <summary>
	/// Returns the current binary offset of the piling.
	/// </summary>
	public int PileOffset { get; internal set; }

	/// <summary>
	/// Returns the maximum capacity of this piling.
	/// </summary>
	public int Capacity { get; }

	internal int MaxOffset => BinaryOffset + Capacity;

	internal Piling(string name, int address, int romaddress, int offset, int size) : base(name) {
		Address = address;
		RomAddress = romaddress;
		BinaryOffset = offset;
		PileOffset = offset;
		Capacity = size;
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
				val = Capacity;
				return true;

			case SymbolContext.Size:
				val = PileOffset - BinaryOffset;
				return true;
		}

		val = 0;
		return false;
	}
}