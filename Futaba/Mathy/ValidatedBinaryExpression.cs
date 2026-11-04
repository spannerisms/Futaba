namespace Futaba;

internal abstract class ValidatedBinaryExpression(ExpressionItem left, ExpressionItem right) : ExpressionItem {
	public ExpressionItem Left => left;
	public ExpressionItem Right => right;
	public static int Arity => 2;

	public sealed override ExpressionState State => _state;
	protected ExpressionState _state = ExpressionState.Unresolved;

	protected abstract decimal Equate(decimal a, decimal b);
	protected abstract bool Validate(decimal a, decimal b);

	public sealed override (decimal, ExpressionState) TryEvaluate() {
		var (rvalue, state) = Right.TryEvaluate();

		if (state.IsResolved) {
			(var lvalue, state) = Left.TryEvaluate();

			if (state.IsResolved) {
				if (Validate(lvalue, rvalue)) {
					return (Equate(lvalue, rvalue), _state = state);
				} else {
					return (MathHelpers.NaN, _state);
				}
			}
		}

		return (MathHelpers.NaN, _state = state);
	}
}
