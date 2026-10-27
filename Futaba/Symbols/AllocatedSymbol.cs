namespace Futaba.Symbols;

/// <summary>
/// Represents a symbol defined inside an allocation block.
/// </summary>
public sealed class AllocatedSymbol : Symbol {
	/// <inheritdoc/>
	public override int DefaultValue => Address;

	/// <inheritdoc/>
	public override int Address { get; }

	/// <inheritdoc/>
	public override int RomAddress => Address;

	/// <summary>
	/// Returns -1, as these symbols do not correspond to a location in the binary file.
	/// </summary>
	public override int BinaryOffset => -1;

	/// <summary>
	/// Gets the number of bytes allocated to this symbol.
	/// </summary>
	public int Size { get; }

	internal AllocatedSymbol(string name, int address, int size) : base(name) {
		Address = address;
		Size = size;
	}

	internal override bool TryGetProperty(SymbolContext prop, out int val) {
		switch (prop) {
			case SymbolContext.Default:
			case SymbolContext.Provenance:
			case SymbolContext.RomAddress:
				val = Address;
				return true;

			case SymbolContext.BinaryOffset:
				val = BinaryOffset;
				return true;

			case SymbolContext.Size:
				val = Size;
				return true;
		}

		val = 0;
		return false;
	}

}