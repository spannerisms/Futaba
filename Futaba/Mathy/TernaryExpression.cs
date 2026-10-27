namespace Futaba;

internal abstract class TernaryExpression(ExpressionItem itema, ExpressionItem itemb, ExpressionItem itemc) : ExpressionItem {

	public ExpressionItem ItemA => itema;
	public ExpressionItem ItemB => itemb;
	public ExpressionItem ItemC => itemc;
	protected abstract decimal Equate(decimal a, decimal b, decimal c);

	protected ExpressionState _state = ExpressionState.Unresolved;
	public override ExpressionState State => _state;
	public override (decimal, ExpressionState) TryEvaluate() {
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

	internal abstract class SafeTernary(ExpressionItem itema, ExpressionItem itemb, ExpressionItem itemc) : TernaryExpression(itema, itemb, itemc) {
		protected abstract bool Validate(decimal a, decimal b, decimal c);

		public override (decimal, ExpressionState) TryEvaluate() {
			var (avalue, state) = ItemA.TryEvaluate();

			if (state.IsResolved) {
				(var bvalue, state) = ItemB.TryEvaluate();

				if (state.IsResolved) {
					(var cvalue, state) = ItemC.TryEvaluate();

					if (state.IsResolved) {
						if (Validate(avalue, bvalue, cvalue)) {
							return (Equate(avalue, bvalue, cvalue), _state = state);
						} else {
							return (MathHelpers.NaN, _state);
						}
					}
				}
			}

			return (MathHelpers.NaN, _state = state);
		}
	}

}