namespace Futaba;

internal sealed class StringToken : ExpressionItem, IStringToken, IToken {
	public string Contents { get; }

	private readonly decimal _value;

	public StringToken(string str, SnesEncoder encoder) {
		Contents = str;

		if (str.Length > 0) {
			_value = encoder.Encode(str[0]);
		} else {
			_value = 0M;
		}
	}

	public TokenType Type => TokenType.String;

	public TokenRule Rule => TokenRule.Primary;

	public bool ForcesUnary => false;
	public bool IsInfix => false;
	public override ExpressionState State => ExpressionState.Resolved;

	public ExpressionItem CreateExpressionItem() => this;

	public override (decimal, ExpressionState) TryEvaluate() {
		return (_value, ExpressionState.Resolved);
	}
}