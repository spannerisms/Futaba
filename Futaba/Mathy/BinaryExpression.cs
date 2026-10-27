namespace Futaba;

// TODO consider moving these subclasses into the symbolic tokens
internal abstract class BinaryExpression(ExpressionItem left, ExpressionItem right) : ExpressionItem {
	public ExpressionItem Left => left;
	public ExpressionItem Right => right;
	protected abstract decimal Equate(decimal a, decimal b);

	protected ExpressionState _state = ExpressionState.Unresolved;
	public override ExpressionState State => _state;

	public override (decimal, ExpressionState) TryEvaluate() {
		var (lvalue, state) = Left.TryEvaluate();

		if (state.IsResolved) {
			(var rvalue, state) = Right.TryEvaluate();

			if (state.IsResolved) {
				return (Equate(lvalue, rvalue), _state = state);
			}
		}

		return (MathHelpers.NaN, _state = state);
	}

	internal abstract class SafeBinary(ExpressionItem left, ExpressionItem right) : BinaryExpression(left, right) {
		protected abstract bool Validate(decimal a, decimal b);

		public override (decimal, ExpressionState) TryEvaluate() {
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

	public sealed class Addition(ExpressionItem left, ExpressionItem right) : BinaryExpression(left, right) {
		public override ExpressionItem Clone() => new Addition(Left.Clone(), Right.Clone());
		protected override decimal Equate(decimal a, decimal b) {
			return a + b;
		}
	}

	public sealed class Subtraction(ExpressionItem left, ExpressionItem right) : BinaryExpression(left, right) {
		public override ExpressionItem Clone() => new Subtraction(Left.Clone(), Right.Clone());
		protected override decimal Equate(decimal a, decimal b) {
			return a - b;
		}
	}

	public sealed class Multiplication(ExpressionItem left, ExpressionItem right) : BinaryExpression(left, right) {
		public override ExpressionItem Clone() => new Multiplication(Left.Clone(), Right.Clone());
		protected override decimal Equate(decimal a, decimal b) {
			return a * b;
		}
	}

	public abstract class BinaryDivision(ExpressionItem left, ExpressionItem right) : BinaryExpression(left, right) {
		public override (decimal, ExpressionState) TryEvaluate() {
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

	public sealed class Division(ExpressionItem left, ExpressionItem right) : BinaryDivision(left, right) {
		public override ExpressionItem Clone() => new Division(Left.Clone(), Right.Clone());
		protected override decimal Equate(decimal a, decimal b) {
			return a / b;
		}
	}

	public sealed class IntegerDivision(ExpressionItem left, ExpressionItem right) : BinaryDivision(left, right) {
		public override ExpressionItem Clone() => new IntegerDivision(Left.Clone(), Right.Clone());
		protected override decimal Equate(decimal a, decimal b) {
			return (a / b).AsLong();
		}
	}

	public sealed class Modulo(ExpressionItem left, ExpressionItem right) : BinaryDivision(left, right) {
		public override ExpressionItem Clone() => new Modulo(Left.Clone(), Right.Clone());
		protected override decimal Equate(decimal a, decimal b) {
			return a % b;
		}
	}

	public sealed class ArithmeticLeftShift(ExpressionItem left, ExpressionItem right) : BinaryExpression(left, right) {
		public override ExpressionItem Clone() => new ArithmeticLeftShift(Left.Clone(), Right.Clone());
		protected override decimal Equate(decimal a, decimal b) {
			return a.AsLong() << b.AsInt();
		}
	}

	public sealed class LogicalRightShift(ExpressionItem left, ExpressionItem right) : BinaryExpression(left, right) {
		public override ExpressionItem Clone() => new LogicalRightShift(Left.Clone(), Right.Clone());
		protected override decimal Equate(decimal a, decimal b) {
			return a.AsLong() >> b.AsInt();
		}
	}

	public sealed class ArithmeticRightShift(ExpressionItem left, ExpressionItem right) : BinaryExpression(left, right) {
		public override ExpressionItem Clone() => new ArithmeticRightShift(Left.Clone(), Right.Clone());
		protected override decimal Equate(decimal a, decimal b) {
			return a.AsLong() >>> b.AsInt();
		}
	}

	public sealed class BitwiseAnd(ExpressionItem left, ExpressionItem right) : BinaryExpression(left, right) {
		public override ExpressionItem Clone() => new BitwiseAnd(Left.Clone(), Right.Clone());
		protected override decimal Equate(decimal a, decimal b) {
			return a.AsLong() & b.AsLong();
		}
	}

	public sealed class BitwiseOr(ExpressionItem left, ExpressionItem right) : BinaryExpression(left, right) {
		public override ExpressionItem Clone() => new BitwiseOr(Left.Clone(), Right.Clone());
		protected override decimal Equate(decimal a, decimal b) {
			return a.AsLong() | b.AsLong();
		}
	}

	public sealed class BitwiseEor(ExpressionItem left, ExpressionItem right) : BinaryExpression(left, right) {
		public override ExpressionItem Clone() => new BitwiseEor(Left.Clone(), Right.Clone());
		protected override decimal Equate(decimal a, decimal b) {
			return a.AsLong() ^ b.AsLong();
		}
	}

	public sealed class LittleEndianMerge(ExpressionItem left, ExpressionItem right) : BinaryExpression(left, right) {
		public override ExpressionItem Clone() => new LittleEndianMerge(Left.Clone(), Right.Clone());
		protected override decimal Equate(decimal a, decimal b) {
			return (a.AsLong() & 0xFFL) | ((b.AsLong() & 0xFFL) << 8);
		}
	}

	public sealed class LogicalAnd(ExpressionItem left, ExpressionItem right) : BinaryExpression(left, right) {
		public override ExpressionItem Clone() => new LogicalAnd(Left.Clone(), Right.Clone());
		protected override decimal Equate(decimal a, decimal b) {
			return MathHelpers.GetTruth(a.IsTrue() && b.IsTrue());
		}
	}

	public sealed class LogicalOr(ExpressionItem left, ExpressionItem right) : BinaryExpression(left, right) {
		public override ExpressionItem Clone() => new LogicalOr(Left.Clone(), Right.Clone());
		protected override decimal Equate(decimal a, decimal b) {
			return MathHelpers.GetTruth(a.IsTrue() || b.IsTrue());
		}
	}

	public sealed class Equality(ExpressionItem left, ExpressionItem right) : BinaryExpression(left, right) {
		public override ExpressionItem Clone() => new Equality(Left.Clone(), Right.Clone());
		protected override decimal Equate(decimal a, decimal b) {
			return MathHelpers.GetTruth(a == b);
		}
	}

	public sealed class Inequality(ExpressionItem left, ExpressionItem right) : BinaryExpression(left, right) {
		public override ExpressionItem Clone() => new Inequality(Left.Clone(), Right.Clone());
		protected override decimal Equate(decimal a, decimal b) {
			return MathHelpers.GetTruth(a != b);
		}
	}

	public sealed class GreaterThan(ExpressionItem left, ExpressionItem right) : BinaryExpression(left, right) {
		public override ExpressionItem Clone() => new GreaterThan(Left.Clone(), Right.Clone());
		protected override decimal Equate(decimal a, decimal b) {
			return MathHelpers.GetTruth(a > b);
		}
	}

	public sealed class LessThan(ExpressionItem left, ExpressionItem right) : BinaryExpression(left, right) {
		public override ExpressionItem Clone() => new LessThan(Left.Clone(), Right.Clone());
		protected override decimal Equate(decimal a, decimal b) {
			return MathHelpers.GetTruth(a < b);
		}
	}

	public sealed class GreaterThanEqual(ExpressionItem left, ExpressionItem right) : BinaryExpression(left, right) {
		public override ExpressionItem Clone() => new GreaterThanEqual(Left.Clone(), Right.Clone());
		protected override decimal Equate(decimal a, decimal b) {
			return MathHelpers.GetTruth(a >= b);
		}
	}

	public sealed class LessThanEqual(ExpressionItem left, ExpressionItem right) : BinaryExpression(left, right) {
		public override ExpressionItem Clone() => new LessThanEqual(Left.Clone(), Right.Clone());
		protected override decimal Equate(decimal a, decimal b) {
			return MathHelpers.GetTruth(a <= b);
		}
	}

	public sealed class NullCoalesce(ExpressionItem left, ExpressionItem right) : BinaryExpression(left, right) {
		public override ExpressionItem Clone() => new NullCoalesce(Left.Clone(), Right.Clone());

		public override (decimal, ExpressionState) TryEvaluate() {
			var (value, state) = Left.TryEvaluate();

			if (state.IsResolved) {
				return (value, _state = state);
			} else {
				(value, state) = Right.TryEvaluate();

				if (state.IsResolved) {
					return (value, _state = ExpressionState.ResolvedButForceRequest);
				}
			}

			return (MathHelpers.NaN, _state = state);
		}

		protected override decimal Equate(decimal a, decimal b) {
			return a;
		}
	}
}
