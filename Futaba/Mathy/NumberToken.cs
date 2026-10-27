namespace Futaba;

internal sealed class NumberToken(decimal val) : ExpressionItem, IReturnableToken, IToken, IExpressionReturn {
	public TokenType Type => TokenType.Number;

	public TokenRule Rule => TokenRule.Primary;

	public bool ForcesUnary => false;
	public bool IsInfix => false;

	public decimal Value => val;

	public override ExpressionState State => ExpressionState.Resolved;

	public ExpressionState ReturnState => ExpressionState.Resolved;

	public bool Resolved => true;

	public bool NeedsRequest => false;

	public ExpressionItem CreateExpressionItem() => this;

	public override (decimal, ExpressionState) TryEvaluate() {
		return (Value, ExpressionState.Resolved);
	}

	public bool TryToResolve() {
		return true;
	}

	public static readonly NumberToken Zero = new(0);
}
