namespace Futaba;

internal class ExpressionTree : IExpressionReturn {
	private readonly ExpressionItem _root;

	public decimal Value { get; private set; }

	public ExpressionTree(ExpressionItem tree) {
		_root = tree;
		TryToResolve();
	}

	public bool Resolved { get; private set; }

	public bool NeedsRequest { get; private set; }

	public ExpressionState ReturnState => _root.State;

	public bool TryToResolve() {
		var (value, state) = _root.TryEvaluate();

		Value = value;

		NeedsRequest = state.WarrantsRequest;

		return Resolved = state.IsResolved;
	}
}
