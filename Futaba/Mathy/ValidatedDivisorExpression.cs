namespace Futaba;

internal abstract class ValidatedDivisorExpression(ExpressionItem left, ExpressionItem right) : ExpressionItem {
	public ExpressionItem Left => left;
	public ExpressionItem Right => right;
	public static int Arity => 2;

	public sealed override ExpressionState State => _state;
	private ExpressionState _state = ExpressionState.Unresolved;

	protected abstract decimal Equate(decimal a, decimal b);

	public sealed override (decimal, ExpressionState) TryEvaluate() {
		var (rvalue, state) = Right.TryEvaluate();

		if (state.IsResolved) {
			if (rvalue.IsFalse()) {
				state = ExpressionState.DivideByZero;
			} else {
				(var lvalue, state) = Left.TryEvaluate();

				if (state.IsResolved) {
					return (Equate(lvalue, rvalue), _state = state);
				}
			}
		}

		return (MathHelpers.NaN, _state = state);
	}
}
