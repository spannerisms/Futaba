namespace Futaba;

internal interface IExpressionReturn : IFormattable {
	public ExpressionState ReturnState { get; }

	public decimal Value { get; }

	public int ValueInt32 {
		[DebuggerStepThrough]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => Value.AsInt();
	}

	public bool Resolved { get; }

	public bool NeedsRequest { get; }

	public bool TryToResolve();

	string IFormattable.ToString(string? format, IFormatProvider? formatProvider) {
		if (Resolved) {
			return CharSpanHelpers.ExtendedStringFormat(Value, format);
		} else {
			return "[???]";
		}
	}
}

internal sealed class InvalidExpression : IExpressionReturn {
	public ExpressionState ReturnState { get; }

	private InvalidExpression(ExpressionState reason) {
		ReturnState = reason;
	}

	public decimal Value => 0M;

	public int ValueInt32 => 0;

	public bool Resolved => false;

	public bool NeedsRequest => false;

	public bool TryToResolve() => false;

	public static readonly InvalidExpression SyntaxError = new(ExpressionState.SyntaxError);
	public static readonly InvalidExpression MissingSymbol = new(ExpressionState.MissingSymbol);
	public static readonly InvalidExpression MissingVariable = new(ExpressionState.VariableNotFound);
	public static readonly InvalidExpression MissingIdentifier = new(ExpressionState.MissingIdentifier);

}
