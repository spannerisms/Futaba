namespace Futaba;

internal sealed class FunctionToken(string name) : IToken {
	public string Name => name;

	public TokenType Type => TokenType.FunctionCall;

	public TokenRule Rule => TokenRule.FunctionCall;

	public bool ForcesUnary => true;
	public bool IsInfix => false;

	public ExpressionItem CreateExpressionItem() => MathHelpers.BadExpressionNode;

}
