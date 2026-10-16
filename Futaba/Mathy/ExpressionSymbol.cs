namespace Futaba;

internal sealed class ExpressionSymbol(Symbol item, SymbolContext context) : ExpressionItem, IReturnableToken, IToken, IExpressionReturn {
	public Symbol Item => item;

	public string Name => Item.Name;

	public SymbolContext Context => context;
	public bool ForcesUnary => false;
	public bool IsInfix => false;
	public bool Resolved => Item.Resolved;
	public bool NeedsRequest => !Item.Resolved;
	public override ExpressionState State => Item.Resolved ? ExpressionState.Resolved : ExpressionState.MissingSymbol;
	public ExpressionState ReturnState => State;


	// Have the getter do value resolution
	// because this object should be considered resolved as soon as the label exists
	private ExpressionState _resolved = ExpressionState.Unresolved;
	public decimal Value {
		get {
			if (_resolved is ExpressionState.Resolved) {
				return field;
			}

			if (Item.Resolved) {
				if (Item.TryGetProperty(Context, out int val)) {
					_resolved = ExpressionState.Resolved;
					return field = val;
				} else {
					_resolved = ExpressionState.InvalidContext;
				}
			} else {
				_resolved = ExpressionState.Unresolved;
			}

			return MathHelpers.NaN;
		}
	}


	public TokenType Type => TokenType.Symbol;

	public TokenRule Rule => TokenRule.Primary;

	public override (decimal, ExpressionState) TryEvaluate() {
		return (Value, _resolved);
	}

	public bool TryToResolve() {
		return Resolved;
	}

	public ExpressionItem CreateExpressionItem() => this;
}
