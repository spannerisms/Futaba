namespace Futaba;

internal class SymbolicToken(TokenType type, TokenRule rule, bool forceUnary, bool infix) : IToken {
	public TokenType Type => type;
	public TokenRule Rule => rule;
	public bool ForcesUnary => forceUnary;
	public bool IsInfix => infix;

	public virtual ExpressionItem CreateExpressionItem() => MathHelpers.BadExpressionNode;
	public virtual ExpressionItem CreateExpressionItem(ExpressionItem item) => MathHelpers.BadExpressionNode;
	public virtual ExpressionItem CreateExpressionItem(ExpressionItem left, ExpressionItem right) => MathHelpers.BadExpressionNode;

	//
	// What follows here is a bunch of singletons
	//

	public static readonly SymbolicToken Start      = new(type: TokenType.Start, rule: TokenRule.Expression, forceUnary: true, infix: true);
	public static readonly SymbolicToken End        = new(type: TokenType.End, rule: TokenRule.Expression, forceUnary: true, infix: true);
	public static readonly SymbolicToken LeftParen  = new(type: TokenType.LeftParen, rule: TokenRule.Primary, forceUnary: true, infix: false);
	public static readonly SymbolicToken RightParen = new(type: TokenType.RightParen, rule: TokenRule.Primary, forceUnary: false, infix: false);
	public static readonly SymbolicToken Comma      = new(type: TokenType.Comma, rule: TokenRule.Primary, forceUnary: true, infix: false);

	//
	// Actual operators
	//

	public static readonly SymbolicToken Addition = new SingletonAddition();
	private sealed class SingletonAddition()
		: SymbolicToken(type: TokenType.Addition, rule: TokenRule.Additive, forceUnary: true, infix: true) {

		public override ExpressionItem CreateExpressionItem(ExpressionItem left, ExpressionItem right) {
			return new ExprAddition(left, right);
		}

		private sealed class ExprAddition(ExpressionItem left, ExpressionItem right) : BinaryExpression(left, right) {
			public override ExpressionItem Clone() => new ExprAddition(Left.Clone(), Right.Clone());
			protected override decimal Equate(decimal a, decimal b) {
				return a + b;
			}
		}
	}

	public static readonly SymbolicToken Subtraction = new SingletonSubtraction();
	private sealed class SingletonSubtraction()
		: SymbolicToken(type: TokenType.Subtraction, rule: TokenRule.Additive, forceUnary: true, infix: true) {

		public override ExpressionItem CreateExpressionItem(ExpressionItem left, ExpressionItem right) {
			return new ExprSubtraction(left, right);
		}

		private sealed class ExprSubtraction(ExpressionItem left, ExpressionItem right) : BinaryExpression(left, right) {
			public override ExpressionItem Clone() => new ExprSubtraction(Left.Clone(), Right.Clone());
			protected override decimal Equate(decimal a, decimal b) {
				return a - b;
			}
		}
	}

	public static readonly SymbolicToken Multiplication = new SingletonMultiplication();
	private sealed class SingletonMultiplication()
		: SymbolicToken(type: TokenType.Multiplication, rule: TokenRule.Multiplicative, forceUnary: true, infix: true) {

		public override ExpressionItem CreateExpressionItem(ExpressionItem left, ExpressionItem right) {
			return new ExprMultiplication(left, right);
		}

		private sealed class ExprMultiplication(ExpressionItem left, ExpressionItem right) : BinaryExpression(left, right) {
			public override ExpressionItem Clone() => new ExprMultiplication(Left.Clone(), Right.Clone());
			protected override decimal Equate(decimal a, decimal b) {
				return a * b;
			}
		}
	}

	public static readonly SymbolicToken Division = new SingletonDivision();
	private sealed class SingletonDivision()
		: SymbolicToken(type: TokenType.Division, rule: TokenRule.Multiplicative, forceUnary: true, infix: true) {

		public override ExpressionItem CreateExpressionItem(ExpressionItem left, ExpressionItem right) {
			return new ExprDivision(left, right);
		}

		private sealed class ExprDivision(ExpressionItem left, ExpressionItem right) : ValidatedDivisorExpression(left, right) {
			public override ExpressionItem Clone() => new ExprDivision(Left.Clone(), Right.Clone());
			protected override decimal Equate(decimal a, decimal b) {
				return a / b;
			}
		}
	}

	public static readonly SymbolicToken IntegerDivision = new SingletonIntegerDivision();
	private sealed class SingletonIntegerDivision()
		: SymbolicToken(type: TokenType.IntegerDivision, rule: TokenRule.Multiplicative, forceUnary: true, infix: true) {

		public override ExpressionItem CreateExpressionItem(ExpressionItem left, ExpressionItem right) {
			return new ExprIntegerDivision(left, right);
		}

		private sealed class ExprIntegerDivision(ExpressionItem left, ExpressionItem right) : ValidatedDivisorExpression(left, right) {
			public override ExpressionItem Clone() => new ExprIntegerDivision(Left.Clone(), Right.Clone());
			protected override decimal Equate(decimal a, decimal b) {
				return (a / b).AsLong();
			}
		}
	}

	public static readonly SymbolicToken Modulo = new SingletonModulo();
	private sealed class SingletonModulo()
		: SymbolicToken(type: TokenType.Modulo, rule: TokenRule.Multiplicative, forceUnary: true, infix: true) {

		public override ExpressionItem CreateExpressionItem(ExpressionItem left, ExpressionItem right) {
			return new ExprModulo(left, right);
		}

		private sealed class ExprModulo(ExpressionItem left, ExpressionItem right) : ValidatedDivisorExpression(left, right) {
			public override ExpressionItem Clone() => new ExprModulo(Left.Clone(), Right.Clone());
			protected override decimal Equate(decimal a, decimal b) {
				return a % b;
			}
		}
	}

	public static readonly SymbolicToken ArithmeticLeftShift = new SingletonArithmeticLeftShift();
	private sealed class SingletonArithmeticLeftShift()
		: SymbolicToken(type: TokenType.ArithmeticLeftShift, rule: TokenRule.Shifts, forceUnary: true, infix: true) {

		public override ExpressionItem CreateExpressionItem(ExpressionItem left, ExpressionItem right) {
			return new ExprArithmeticLeftShift(left, right);
		}

		private sealed class ExprArithmeticLeftShift(ExpressionItem left, ExpressionItem right) : BinaryExpression(left, right) {
			public override ExpressionItem Clone() => new ExprArithmeticLeftShift(Left.Clone(), Right.Clone());
			protected override decimal Equate(decimal a, decimal b) {
				return a.AsLong() << b.AsInt();
			}
		}
	}

	public static readonly SymbolicToken LogicalRightShift = new SingletonLogicalRightShift();
	private sealed class SingletonLogicalRightShift()
		: SymbolicToken(type: TokenType.LogicalRightShift, rule: TokenRule.Shifts, forceUnary: true, infix: true) {

		public override ExpressionItem CreateExpressionItem(ExpressionItem left, ExpressionItem right) {
			return new ExprLogicalRightShift(left, right);
		}

		private sealed class ExprLogicalRightShift(ExpressionItem left, ExpressionItem right) : BinaryExpression(left, right) {
			public override ExpressionItem Clone() => new ExprLogicalRightShift(Left.Clone(), Right.Clone());
			protected override decimal Equate(decimal a, decimal b) {
				return a.AsLong() >> b.AsInt();
			}
		}
	}

	public static readonly SymbolicToken ArithmeticRightShift = new SingletonArithmeticRightShift();
	private sealed class SingletonArithmeticRightShift()
		: SymbolicToken(type: TokenType.ArithmeticRightShift, rule: TokenRule.Shifts, forceUnary: true, infix: true) {

		public override ExpressionItem CreateExpressionItem(ExpressionItem left, ExpressionItem right) {
			return new ExprArithmeticRightShift(left, right);
		}

		private sealed class ExprArithmeticRightShift(ExpressionItem left, ExpressionItem right) : BinaryExpression(left, right) {
			public override ExpressionItem Clone() => new ExprArithmeticRightShift(Left.Clone(), Right.Clone());
			protected override decimal Equate(decimal a, decimal b) {
				return a.AsLong() >>> b.AsInt();
			}
		}
	}

	public static readonly SymbolicToken BitwiseAnd = new SingletonBitwiseAnd();
	private sealed class SingletonBitwiseAnd()
		: SymbolicToken(type: TokenType.BitwiseAnd, rule: TokenRule.BitwiseAnd, forceUnary: true, infix: true) {

		public override ExpressionItem CreateExpressionItem(ExpressionItem left, ExpressionItem right) {
			return new ExprBitwiseAnd(left, right);
		}

		private sealed class ExprBitwiseAnd(ExpressionItem left, ExpressionItem right) : BinaryExpression(left, right) {
			public override ExpressionItem Clone() => new ExprBitwiseAnd(Left.Clone(), Right.Clone());
			protected override decimal Equate(decimal a, decimal b) {
				return a.AsLong() & b.AsLong();
			}
		}
	}

	public static readonly SymbolicToken BitwiseOr = new SingletonBitwiseOr();
	private sealed class SingletonBitwiseOr()
		: SymbolicToken(type: TokenType.BitwiseOr, rule: TokenRule.BitwiseOr, forceUnary: true, infix: true) {

		public override ExpressionItem CreateExpressionItem(ExpressionItem left, ExpressionItem right) {
			return new ExprBitwiseOr(left, right);
		}

		private sealed class ExprBitwiseOr(ExpressionItem left, ExpressionItem right) : BinaryExpression(left, right) {
			public override ExpressionItem Clone() => new ExprBitwiseOr(Left.Clone(), Right.Clone());
			protected override decimal Equate(decimal a, decimal b) {
				return a.AsLong() | b.AsLong();
			}
		}
	}

	public static readonly SymbolicToken BitwiseEor = new SingletonBitwiseEor();
	private sealed class SingletonBitwiseEor()
		: SymbolicToken(type: TokenType.BitwiseEor, rule: TokenRule.BitwiseEor, forceUnary: true, infix: true) {

		public override ExpressionItem CreateExpressionItem(ExpressionItem left, ExpressionItem right) {
			return new ExprBitwiseEor(left, right);
		}

		private sealed class ExprBitwiseEor(ExpressionItem left, ExpressionItem right) : BinaryExpression(left, right) {
			public override ExpressionItem Clone() => new ExprBitwiseEor(Left.Clone(), Right.Clone());
			protected override decimal Equate(decimal a, decimal b) {
				return a.AsLong() ^ b.AsLong();
			}
		}
	}

	public static readonly SymbolicToken LEMerge = new SingletonLEMerge();
	private sealed class SingletonLEMerge()
		: SymbolicToken(type: TokenType.LEMerge, rule: TokenRule.BitwiseOr, forceUnary: true, infix: true) {

		public override ExpressionItem CreateExpressionItem(ExpressionItem left, ExpressionItem right) {
			return new ExprLittleEndianMerge(left, right);
		}

		private sealed class ExprLittleEndianMerge(ExpressionItem left, ExpressionItem right) : BinaryExpression(left, right) {
			public override ExpressionItem Clone() => new ExprLittleEndianMerge(Left.Clone(), Right.Clone());
			protected override decimal Equate(decimal a, decimal b) {
				return (a.AsLong() & 0xFFL) | ((b.AsLong() & 0xFFL) << 8);
			}
		}
	}

	public static readonly SymbolicToken LogicalAnd = new SingletonLogicalAnd();
	private sealed class SingletonLogicalAnd()
		: SymbolicToken(type: TokenType.LogicalAnd, rule: TokenRule.LogicalAnd, forceUnary: true, infix: true) {

		public override ExpressionItem CreateExpressionItem(ExpressionItem left, ExpressionItem right) {
			return new ExprLogicalAnd(left, right);
		}

		private sealed class ExprLogicalAnd(ExpressionItem left, ExpressionItem right) : BinaryExpression(left, right) {
			public override ExpressionItem Clone() => new ExprLogicalAnd(Left.Clone(), Right.Clone());
			protected override decimal Equate(decimal a, decimal b) {
				return MathHelpers.GetTruth(a.IsTrue() && b.IsTrue());
			}
		}
	}

	public static readonly SymbolicToken LogicalOr = new SingletonLogicalOr();
	private sealed class SingletonLogicalOr()
		: SymbolicToken(type: TokenType.LogicalOr, rule: TokenRule.LogicalOr, forceUnary: true, infix: true) {

		public override ExpressionItem CreateExpressionItem(ExpressionItem left, ExpressionItem right) {
			return new ExprLogicalOr(left, right);
		}

		private sealed class ExprLogicalOr(ExpressionItem left, ExpressionItem right) : BinaryExpression(left, right) {
			public override ExpressionItem Clone() => new ExprLogicalOr(Left.Clone(), Right.Clone());
			protected override decimal Equate(decimal a, decimal b) {
				return MathHelpers.GetTruth(a.IsTrue() || b.IsTrue());
			}
		}
	}

	public static readonly SymbolicToken Not = new SingletonNot();
	private sealed class SingletonNot()
		: SymbolicToken(type: TokenType.Not, rule: TokenRule.Unary, forceUnary: true, infix: false) {

		public override ExpressionItem CreateExpressionItem(ExpressionItem item) {
			return new ExprNot(item);
		}

		private sealed class ExprNot(ExpressionItem item) : UnaryExpression(item) {
			public override ExpressionItem Clone() => new ExprNot(Item.Clone());
			protected override decimal Equate(decimal a) {
				return ~a.AsLong();
			}
		}
	}

	public static readonly SymbolicToken Negate = new SingletonNegate();
	private sealed class SingletonNegate()
		: SymbolicToken(type: TokenType.Negate, rule: TokenRule.Unary, forceUnary: true, infix: false) {

		public override ExpressionItem CreateExpressionItem(ExpressionItem item) {
			return new ExprNegate(item);
		}

		private sealed class ExprNegate(ExpressionItem item) : UnaryExpression(item) {
			public override ExpressionItem Clone() => new ExprNegate(Item.Clone());
			protected override decimal Equate(decimal a) {
				return -a;
			}
		}
	}

	public static readonly SymbolicToken LowByte = new SingletonLowByte();
	private sealed class SingletonLowByte()
		: SymbolicToken(type: TokenType.LowByte, rule: TokenRule.Unary, forceUnary: true, infix: false) {

		public override ExpressionItem CreateExpressionItem(ExpressionItem item) {
			return new ExprLowByte(item);
		}

		private sealed class ExprLowByte(ExpressionItem item) : UnaryExpression(item) {
			public override ExpressionItem Clone() => new ExprLowByte(Item.Clone());
			protected override decimal Equate(decimal a) {
				return a.AsLong() & 0xFFL;
			}
		}
	}

	public static readonly SymbolicToken HighByte = new SingletonHighByte();
	private sealed class SingletonHighByte()
		: SymbolicToken(type: TokenType.HighByte, rule: TokenRule.Unary, forceUnary: true, infix: false) {

		public override ExpressionItem CreateExpressionItem(ExpressionItem item) {
			return new ExprHighByte(item);
		}

		private sealed class ExprHighByte(ExpressionItem item) : UnaryExpression(item) {
			public override ExpressionItem Clone() => new ExprHighByte(Item.Clone());
			protected override decimal Equate(decimal a) {
				return (a.AsLong() >> 8) & 0xFFL;
			}
		}
	}

	public static readonly SymbolicToken BankByte = new SingletonBankByte();
	private sealed class SingletonBankByte()
		: SymbolicToken(type: TokenType.BankByte, rule: TokenRule.Unary, forceUnary: true, infix: false) {

		public override ExpressionItem CreateExpressionItem(ExpressionItem item) {
			return new ExprBankByte(item);
		}

		private sealed class ExprBankByte(ExpressionItem item) : UnaryExpression(item) {
			public override ExpressionItem Clone() => new ExprBankByte(Item.Clone());
			protected override decimal Equate(decimal a) {
				return (a.AsLong() >> 16) & 0xFFL;
			}
		}
	}

	public static readonly SymbolicToken Low16 = new SingletonLow16();
	private sealed class SingletonLow16()
		: SymbolicToken(type: TokenType.Low16, rule: TokenRule.Unary, forceUnary: true, infix: false) {

		public override ExpressionItem CreateExpressionItem(ExpressionItem item) {
			return new ExprLow16(item);
		}

		private sealed class ExprLow16(ExpressionItem item) : UnaryExpression(item) {
			public override ExpressionItem Clone() => new ExprLow16(Item.Clone());
			protected override decimal Equate(decimal a) {
				return a.AsLong() & SnesHelpers.AbsoluteMask;
			}
		}
	}

	public static readonly SymbolicToken High16 = new SingletonHigh16();
	private sealed class SingletonHigh16()
		: SymbolicToken(type: TokenType.High16, rule: TokenRule.Unary, forceUnary: true, infix: false) {

		public override ExpressionItem CreateExpressionItem(ExpressionItem item) {
			return new ExprHigh16(item);
		}

		private sealed class ExprHigh16(ExpressionItem item) : UnaryExpression(item) {
			public override ExpressionItem Clone() => new ExprHigh16(Item.Clone());
			protected override decimal Equate(decimal a) {
				return (a.AsLong() >> 8) & 0xFFFFL;
			}
		}
	}

	public static readonly SymbolicToken BankOnly = new SingletonBankOnly();
	private sealed class SingletonBankOnly()
		: SymbolicToken(type: TokenType.BankOnly, rule: TokenRule.Unary, forceUnary: true, infix: false) {

		public override ExpressionItem CreateExpressionItem(ExpressionItem item) {
			return new ExprBankOnly(item);
		}

		private sealed class ExprBankOnly(ExpressionItem item) : UnaryExpression(item) {
			public override ExpressionItem Clone() => new ExprBankOnly(Item.Clone());
			protected override decimal Equate(decimal a) {
				return a.AsLong() & SnesHelpers.BankMask;
			}
		}
	}

	public static readonly SymbolicToken Equality = new SingletonEquality();
	private sealed class SingletonEquality()
		: SymbolicToken(type: TokenType.Equality, rule: TokenRule.Equality, forceUnary: true, infix: true) {

		public override ExpressionItem CreateExpressionItem(ExpressionItem left, ExpressionItem right) {
			return new ExprEquality(left, right);
		}

		private sealed class ExprEquality(ExpressionItem left, ExpressionItem right) : BinaryExpression(left, right) {
			public override ExpressionItem Clone() => new ExprEquality(Left.Clone(), Right.Clone());
			protected override decimal Equate(decimal a, decimal b) {
				return MathHelpers.GetTruth(a == b);
			}
		}
	}

	public static readonly SymbolicToken Inequality = new SingletonInequality();
	private sealed class SingletonInequality()
		: SymbolicToken(type: TokenType.Inequality, rule: TokenRule.Equality, forceUnary: true, infix: true) {

		public override ExpressionItem CreateExpressionItem(ExpressionItem left, ExpressionItem right) {
			return new ExprInequality(left, right);
		}

		private sealed class ExprInequality(ExpressionItem left, ExpressionItem right) : BinaryExpression(left, right) {
			public override ExpressionItem Clone() => new ExprInequality(Left.Clone(), Right.Clone());
			protected override decimal Equate(decimal a, decimal b) {
				return MathHelpers.GetTruth(a != b);
			}
		}
	}

	public static readonly SymbolicToken GreaterThan = new SingletonGreaterThan();
	private sealed class SingletonGreaterThan()
		: SymbolicToken(type: TokenType.GreaterThan, rule: TokenRule.Comparison, forceUnary: true, infix: true) {

		public override ExpressionItem CreateExpressionItem(ExpressionItem left, ExpressionItem right) {
			return new ExprGreaterThan(left, right);
		}

		private sealed class ExprGreaterThan(ExpressionItem left, ExpressionItem right) : BinaryExpression(left, right) {
			public override ExpressionItem Clone() => new ExprGreaterThan(Left.Clone(), Right.Clone());
			protected override decimal Equate(decimal a, decimal b) {
				return MathHelpers.GetTruth(a > b);
			}
		}
	}

	public static readonly SymbolicToken LessThan = new SingletonLessThan();
	private sealed class SingletonLessThan()
		: SymbolicToken(type: TokenType.LessThan, rule: TokenRule.Comparison, forceUnary: true, infix: true) {

		public override ExpressionItem CreateExpressionItem(ExpressionItem left, ExpressionItem right) {
			return new ExprLessThan(left, right);
		}

		private sealed class ExprLessThan(ExpressionItem left, ExpressionItem right) : BinaryExpression(left, right) {
			public override ExpressionItem Clone() => new ExprLessThan(Left.Clone(), Right.Clone());
			protected override decimal Equate(decimal a, decimal b) {
				return MathHelpers.GetTruth(a < b);
			}
		}
	}

	public static readonly SymbolicToken GreaterThanEqual = new SingletonGreaterThanEqual();
	private sealed class SingletonGreaterThanEqual()
		: SymbolicToken(type: TokenType.GreaterThanEqual, rule: TokenRule.Comparison, forceUnary: true, infix: true) {

		public override ExpressionItem CreateExpressionItem(ExpressionItem left, ExpressionItem right) {
			return new ExprGreaterThanEqual(left, right);
		}

		private sealed class ExprGreaterThanEqual(ExpressionItem left, ExpressionItem right) : BinaryExpression(left, right) {
			public override ExpressionItem Clone() => new ExprGreaterThanEqual(Left.Clone(), Right.Clone());
			protected override decimal Equate(decimal a, decimal b) {
				return MathHelpers.GetTruth(a >= b);
			}
		}
	}

	public static readonly SymbolicToken LessThanEqual = new SingletonLessThanEqual();
	private sealed class SingletonLessThanEqual()
		: SymbolicToken(type: TokenType.LessThanEqual, rule: TokenRule.Comparison, forceUnary: true, infix: true) {

		public override ExpressionItem CreateExpressionItem(ExpressionItem left, ExpressionItem right) {
			return new ExprLessThanEqual(left, right);
		}

		private sealed class ExprLessThanEqual(ExpressionItem left, ExpressionItem right) : BinaryExpression(left, right) {
			public override ExpressionItem Clone() => new ExprLessThanEqual(Left.Clone(), Right.Clone());
			protected override decimal Equate(decimal a, decimal b) {
				return MathHelpers.GetTruth(a <= b);
			}
		}
	}

	public static readonly SymbolicToken NullCoalesce = new SingletonNullCoalesce();
	private sealed class SingletonNullCoalesce()
		: SymbolicToken(type: TokenType.NullCoalesce, rule: TokenRule.NullCoalesce, forceUnary: true, infix: true) {

		public override ExpressionItem CreateExpressionItem(ExpressionItem left, ExpressionItem right) {
			return new ExprNullCoalesce(left, right);
		}

		private sealed class ExprNullCoalesce(ExpressionItem left, ExpressionItem right) : ExpressionItem {
			public ExpressionItem Left => left;
			public ExpressionItem Right => right;
			public static int Arity => 2;

			public override ExpressionState State => _state;
			private ExpressionState _state = ExpressionState.Unresolved;

			public override ExpressionItem Clone() => new ExprNullCoalesce(Left.Clone(), Right.Clone());

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
		}
	}

	public static readonly SymbolicToken PostfixNullCoalesce = new SingletonPostfixNullCoalesce();
	private sealed class SingletonPostfixNullCoalesce()
		: SymbolicToken(type: TokenType.NullCoalesceShort, rule: TokenRule.Postfix, forceUnary: false, infix: false) {

		public override ExpressionItem CreateExpressionItem(ExpressionItem item) {
			return new ExprNullCoalesce(item);
		}

		private sealed class ExprNullCoalesce(ExpressionItem item) : ExpressionItem {
			public ExpressionItem Item => item;
			public static int Arity => 1;

			public sealed override ExpressionState State => _state;
			private ExpressionState _state = ExpressionState.Unresolved;
			public override ExpressionItem Clone() => new ExprNullCoalesce(Item.Clone());

			public override (decimal, ExpressionState) TryEvaluate() {
				var (value, state) = Item.TryEvaluate();

				if (state.WarrantsRequest) {
					return (value, _state = ExpressionState.ResolvedButForceRequest);

				} else if (!state.IsResolved) {
					value = MathHelpers.NaN;
				}

				return (value, _state = state);
			}
		}
	}
}
