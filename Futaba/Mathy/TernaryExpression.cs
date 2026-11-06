namespace Futaba;

internal abstract class TernaryExpression(ExpressionItem itema, ExpressionItem itemb, ExpressionItem itemc) : ExpressionItem {
	public ExpressionItem ItemA => itema;
	public ExpressionItem ItemB => itemb;
	public ExpressionItem ItemC => itemc;
	public static int Arity => 3;

	public sealed override ExpressionState State => _state;
	protected ExpressionState _state = ExpressionState.Unresolved;

	protected abstract decimal Equate(decimal a, decimal b, decimal c);

	public sealed override (decimal, ExpressionState) TryEvaluate() {
		var (avalue, state) = ItemA.TryEvaluate();

		if (state.IsResolved) {
			(var bvalue, state) = ItemB.TryEvaluate();

			if (state.IsResolved) {
				(var cvalue, state) = ItemC.TryEvaluate();

				if (state.IsResolved) {
					return (Equate(avalue, bvalue, cvalue), _state = state);
				}
			}
		}

		_state = state;
		return (MathHelpers.NaN, state);
	}

}
