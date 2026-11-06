namespace Futaba;

internal abstract class StringExpression : ExpressionItem {
	public sealed override ExpressionState State => _state;
	protected ExpressionState _state = ExpressionState.Unresolved;

	protected string? ValidateString(ExpressionItem testItem) {
		if (testItem is not IStringToken str) {
			_state = ExpressionState.InvalidArgumentToStringFunction;
			return null;
		}

		return str.Contents;
	}
}
