namespace Futaba;

internal abstract class UnaryExpression(ExpressionItem item) : ExpressionItem {
	public ExpressionItem Item => item;
	public static int Arity => 1;

	public sealed override ExpressionState State => _state;
	private ExpressionState _state = ExpressionState.Unresolved;

	protected abstract decimal Equate(decimal a);

	public sealed override (decimal, ExpressionState) TryEvaluate() {
		var (value, state) = Item.TryEvaluate();

		_state = state;

		if (state.IsResolved) {
			return (Equate(value), state);
		} else {
			return (MathHelpers.NaN, state);
		}
	}
}
