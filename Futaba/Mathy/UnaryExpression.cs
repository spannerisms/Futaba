namespace Futaba;

internal abstract class UnaryExpression(ExpressionItem item) : ExpressionItem {
	public ExpressionItem Item => item;

	protected abstract decimal Equate(decimal a);

	public override ExpressionState State => _state;

	protected ExpressionState _state = ExpressionState.Unresolved;

	public override (decimal, ExpressionState) TryEvaluate() {
		var (value, state) = Item.TryEvaluate();

		_state = state;

		if (state.IsResolved) {
			return (Equate(value), state);
		} else {
			return (MathHelpers.NaN, state);
		}
	}

	internal abstract class SafeUnary(ExpressionItem item) : UnaryExpression(item) {
		protected abstract bool Validate(decimal a);

		public override (decimal, ExpressionState) TryEvaluate() {
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

	public sealed class Not(ExpressionItem item) : UnaryExpression(item) {
		public override ExpressionItem Clone() => new Not(Item.Clone());
		protected override decimal Equate(decimal a) {
			return ~a.AsLong();
		}
	}

	public sealed class Negate(ExpressionItem item) : UnaryExpression(item) {
		public override ExpressionItem Clone() => new Negate(Item.Clone());
		protected override decimal Equate(decimal a) {
			return -a;
		}
	}

	public sealed class LowByte(ExpressionItem item) : UnaryExpression(item) {
		public override ExpressionItem Clone() => new LowByte(Item.Clone());
		protected override decimal Equate(decimal a) {
			return a.AsLong() & 0xFFL;
		}
	}

	public sealed class HighByte(ExpressionItem item) : UnaryExpression(item) {
		public override ExpressionItem Clone() => new HighByte(Item.Clone());
		protected override decimal Equate(decimal a) {
			return (a.AsLong() >> 8) & 0xFFL;
		}
	}

	public sealed class BankByte(ExpressionItem item) : UnaryExpression(item) {
		public override ExpressionItem Clone() => new BankByte(Item.Clone());
		protected override decimal Equate(decimal a) {
			return (a.AsLong() >> 16) & 0xFFL;
		}
	}

	public sealed class Low16(ExpressionItem item) : UnaryExpression(item) {
		public override ExpressionItem Clone() => new Low16(Item.Clone());
		protected override decimal Equate(decimal a) {
			return a.AsLong() & SnesHelpers.AbsoluteMask;
		}
	}

	public sealed class High16(ExpressionItem item) : UnaryExpression(item) {
		public override ExpressionItem Clone() => new High16(Item.Clone());
		protected override decimal Equate(decimal a) {
			return (a.AsLong() >> 8) & 0xFFFFL;
		}
	}

	public sealed class BankOnly(ExpressionItem item) : UnaryExpression(item) {
		public override ExpressionItem Clone() => new BankOnly(Item.Clone());
		protected override decimal Equate(decimal a) {
			return a.AsLong() & SnesHelpers.BankMask;
		}
	}

	public sealed class NullCoalesce(ExpressionItem item) : UnaryExpression(item) {
		public override ExpressionItem Clone() => new NullCoalesce(Item.Clone());

		public override (decimal, ExpressionState) TryEvaluate() {
			var (value, state) = Item.TryEvaluate();

			if (state.WarrantsRequest) {
				return (value, _state = ExpressionState.ResolvedButForceRequest);

			} else if (!state.IsResolved) {
				value = MathHelpers.NaN;
			}

			return (value, _state = state);
		}

		protected override decimal Equate(decimal a) {
			return a;
		}
	}
}
