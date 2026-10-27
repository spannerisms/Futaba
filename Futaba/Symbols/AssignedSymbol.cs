namespace Futaba.Symbols;

/// <summary>
/// Represents a symbol that has been explicitly assigned to an expression.
/// </summary>
public sealed class AssignedSymbol : Symbol {

	private readonly int _value;

	/// <inheritdoc/>
	public override int Address => _value;

	/// <inheritdoc/>
	public override int DefaultValue => _value;

	/// <inheritdoc/>
	public override int RomAddress => _value;

	/// <inheritdoc/>
	public override int BinaryOffset => _value;

	internal AssignedSymbol(string name, int value) : base(name) {
		_value = value;
	}

	internal override bool TryGetProperty(SymbolContext prop, out int val) {
		switch (prop) {
			case SymbolContext.Default:
			case SymbolContext.Provenance:
			case SymbolContext.RomAddress:
			case SymbolContext.BinaryOffset:
				val = _value;
				return true;
		}

		val = 0;
		return false;
	}

	/// <summary>
	/// Attempts to create a symbol with the given name and the assigned value. Fails if an invalid name is supplied.
	/// </summary>
	/// <param name="name"></param>
	/// <param name="value"></param>
	/// <param name="symbol"></param>
	/// <param name="error"></param>
	/// <returns><see langword="true"/> if the symbol was created; otherwise <see langword="false"/></returns>
	public static bool TryCreate(string name, int value, [NotNullWhen(true)] out AssignedSymbol? symbol, [NotNullWhen(false)] out string? error) {
		if (string.IsNullOrEmpty(name)) {
			error = "Empty symbol name.";
		} else if (FastRead.IsIdentifier(name)) {
			error = null;
			symbol = new(name, value);
			return true;
		} else {
			error = "Invalid symbol name.";
		}

		symbol = null;
		return false;
	}

	/// <inheritdoc cref="TryCreate(string, decimal, out AssignedSymbol?, out string?)"/>
	public static bool TryCreate(string name, decimal value, [NotNullWhen(true)] out AssignedSymbol? symbol, [NotNullWhen(false)] out string? error) {
		return TryCreate(name, value.AsInt(), out symbol, out error);
	}
}
