namespace Futaba;

internal sealed class NamedParameter(string name) : ExpressionItem, IToken {
	public string Name => name;

	public ExpressionItem CurrentValue { get; set; } = NumberToken.Zero;

	public override ExpressionState State => CurrentValue.State;
	public TokenType Type => TokenType.Number;
	public TokenRule Rule => TokenRule.Primary;

	public bool ForcesUnary => false;
	public bool IsInfix => false;

	public ExpressionItem CreateExpressionItem() => this;

	public override (decimal, ExpressionState) TryEvaluate() {
		return CurrentValue.TryEvaluate();
	}

	public override ExpressionItem Clone() => CurrentValue.Clone();

}
