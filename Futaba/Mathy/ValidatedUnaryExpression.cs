namespace Futaba;

internal abstract class ValidatedUnaryExpression(ExpressionItem item) : ExpressionItem {
	public ExpressionItem Item => item;
	public static int Arity => 1;

	public sealed override ExpressionState State => _state;
	protected ExpressionState _state = ExpressionState.Unresolved;

	protected abstract decimal Equate(decimal a);
	protected abstract bool Validate(decimal a);

	public sealed override (decimal, ExpressionState) TryEvaluate() {
		var (value, state) = Item.TryEvaluate();

		if (state.IsResolved) {
			if (Validate(value)) {
				return (Equate(value), _state = state);

			} else {
				return (MathHelpers.NaN, _state);

			}
		} else {
			return (MathHelpers.NaN, _state = state);
		}
	}
}
