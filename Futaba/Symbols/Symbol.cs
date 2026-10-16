namespace Futaba.Symbols;

/// <summary>
/// Base class for encapsulating information about symbols created during assembly.
/// </summary>

[DebuggerDisplay("{ToString()}")]
public abstract class Symbol {
	/// <summary>
	/// Returns tnis symbol's name.
	/// </summary>
	public virtual string Name { get; }

	/// <summary>
	/// Returns the default value of this symbol used when no context is specified.
	/// </summary>
	public abstract int DefaultValue { get; }

	/// <summary>
	/// Returns the provenance of the symbol.
	/// </summary>
	public abstract int Address { get; }

	/// <summary>
	/// Returns the program counter of the symbol.
	/// </summary>
	public abstract int RomAddress { get; }

	/// <summary>
	/// Returns the binary offset of the symbol.
	/// </summary>
	public abstract int BinaryOffset { get; }

	/// <summary>
	/// Returns whether or not this symbol resolved to a value during assembly.
	/// </summary>
	internal virtual bool Resolved => true;

	private protected Symbol(string name) {
		Name = name;
	}

	internal abstract bool TryGetProperty(SymbolContext prop, out int val);

	/// <inheritdoc cref="object.ToString()"/>
	public override string ToString() {
		if (Resolved) {
			return $"{Name} = {DefaultValue}";
		} else {
			return $"{Name} = UNRESOLVED";
		}
	}

	internal ExpressionSymbol CreateExpressionItem(SymbolContext context) {
		return new ExpressionSymbol(this, context);
	}
}

[DebuggerDisplay("BAD SYMBOL")]
internal sealed class BadSymbolPlaceHolder : Symbol {
	public override int DefaultValue => 0;
	public override int Address => 0;
	public override int RomAddress => 0;
	public override int BinaryOffset => -1;
	private BadSymbolPlaceHolder() : base("BAD SYMBOL") { }
	internal override bool Resolved => false;

	internal static readonly BadSymbolPlaceHolder Instance = new();

	internal override bool TryGetProperty(SymbolContext prop, out int val) {
		val = 0;
		return false;
	}
}
