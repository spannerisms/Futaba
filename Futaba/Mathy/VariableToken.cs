namespace Futaba;

internal abstract class VariableToken : ExpressionItem, IReturnableToken, IToken, IExpressionReturn {
	public TokenType Type => TokenType.Variable;
	public TokenRule Rule => TokenRule.Primary;

	public bool ForcesUnary => false;

	public bool IsInfix => false;

	public abstract ExpressionState ReturnState { get; }
	public abstract decimal Value { get; }
	public abstract bool Resolved { get; }
	public bool NeedsRequest => false;

	public bool TryToResolve() => Resolved;

	public ExpressionItem CreateExpressionItem() => this;
}

internal sealed class ValueVariableToken(Variable thevar) : VariableToken {
	public override ExpressionState State => ExpressionState.Resolved;

	public override ExpressionState ReturnState => ExpressionState.Resolved;

	public override decimal Value => _value;

	public override bool Resolved => true;

	private readonly decimal _value = thevar.Value;

	public override (decimal, ExpressionState) TryEvaluate() {
		return (_value, ExpressionState.Resolved);
	}
}

internal sealed class StringVariableToken : VariableToken, IStringToken {
	public string Contents { get; }
	public override ExpressionState State => ExpressionState.Resolved;

	public override ExpressionState ReturnState => ExpressionState.Resolved;

	public override decimal Value => _value;

	public override bool Resolved => true;

	private readonly decimal _value;

	public StringVariableToken(Variable thevar, SnesEncoder encoder) {
		string str = thevar.Contents;
		Contents = str;

		if (str.Length > 0) {
			_value = encoder.Encode(str[0]);
		}
	}

	public override (decimal, ExpressionState) TryEvaluate() {
		return (_value, ExpressionState.Resolved);
	}
}

internal sealed class NullVariableToken(string name) : VariableToken {
	public string Name => name;

	public override ExpressionState State => ExpressionState.VariableNotFound;

	public override ExpressionState ReturnState => ExpressionState.VariableNotFound;

	public override decimal Value => MathHelpers.NaN;

	public override bool Resolved => false;

	public override (decimal, ExpressionState) TryEvaluate() {
		return (MathHelpers.NaN, ExpressionState.VariableNotFound);
	}
}
