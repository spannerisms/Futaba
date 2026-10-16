namespace Futaba;

internal abstract class SymbolicToken : IToken {
	public abstract TokenType Type { get; }
	public abstract TokenRule Rule { get; }
	public abstract bool ForcesUnary { get; }
	public abstract bool IsInfix { get; }

	public virtual ExpressionItem CreateExpressionItem() {
		return MathHelpers.BadExpressionNode;
	}

	public virtual ExpressionItem CreateExpressionItem(ExpressionItem item) {
		return MathHelpers.BadExpressionNode;
	}

	public virtual ExpressionItem CreateExpressionItem(ExpressionItem left, ExpressionItem right) {
		return MathHelpers.BadExpressionNode;
	}

	/*
	 * What follows here is a bunch of singletos
	 * to help speedup the type and rule checks and the expression item creation
	 */
	// TODO make these properties of a single sealed class instead?
	public static readonly SymbolicToken Start = new SingletonStart();
	private sealed class SingletonStart : SymbolicToken {
		public override TokenType Type => TokenType.Start;
		public override TokenRule Rule => TokenRule.Expression;
		public override bool IsInfix => true;
		public override bool ForcesUnary => true;
	}

	public static readonly SymbolicToken End = new SingletonEnd();
	private sealed class SingletonEnd : SymbolicToken {
		public override TokenType Type => TokenType.End;
		public override TokenRule Rule => TokenRule.Expression;
		public override bool ForcesUnary => true;
		public override bool IsInfix => true;
	}

	public static readonly SymbolicToken LeftParen = new SingletonLeftParen();
	private sealed class SingletonLeftParen : SymbolicToken {
		public override TokenType Type => TokenType.LeftParen;
		public override TokenRule Rule => TokenRule.Primary;
		public override bool ForcesUnary => true;
		public override bool IsInfix => false;
	}

	public static readonly SymbolicToken RightParen = new SingletonRightParen();
	private sealed class SingletonRightParen : SymbolicToken {
		public override TokenType Type => TokenType.RightParen;
		public override TokenRule Rule => TokenRule.Primary;
		public override bool ForcesUnary => false;
		public override bool IsInfix => false;
	}

	public static readonly SymbolicToken Comma = new SingletonComma();
	private sealed class SingletonComma : SymbolicToken {
		public override TokenType Type => TokenType.Comma;
		public override TokenRule Rule => TokenRule.Primary;
		public override bool ForcesUnary => true;
		public override bool IsInfix => false;
	}

	public static readonly SymbolicToken Addition = new SingletonAddition();
	private sealed class SingletonAddition : SymbolicToken {
		public override TokenType Type => TokenType.Addition;
		public override TokenRule Rule => TokenRule.Additive;
		public override bool ForcesUnary => true;
		public override bool IsInfix => true;

		public override ExpressionItem CreateExpressionItem(ExpressionItem left, ExpressionItem right) {
			return new BinaryExpression.Addition(left, right);
		}
	}

	public static readonly SymbolicToken Subtraction = new SingletonSubtraction();
	private sealed class SingletonSubtraction : SymbolicToken {
		public override TokenType Type => TokenType.Subtraction;
		public override TokenRule Rule => TokenRule.Additive;
		public override bool ForcesUnary => true;
		public override bool IsInfix => true;

		public override ExpressionItem CreateExpressionItem(ExpressionItem left, ExpressionItem right) {
			return new BinaryExpression.Subtraction(left, right);
		}
	}

	public static readonly SymbolicToken Multiplication = new SingletonMultiplication();
	private sealed class SingletonMultiplication : SymbolicToken {
		public override TokenType Type => TokenType.Multiplication;
		public override TokenRule Rule => TokenRule.Multiplicative;
		public override bool ForcesUnary => true;
		public override bool IsInfix => true;

		public override ExpressionItem CreateExpressionItem(ExpressionItem left, ExpressionItem right) {
			return new BinaryExpression.Multiplication(left, right);
		}
	}

	public static readonly SymbolicToken Division = new SingletonDivision();
	private sealed class SingletonDivision : SymbolicToken {
		public override TokenType Type => TokenType.Division;
		public override TokenRule Rule => TokenRule.Multiplicative;
		public override bool ForcesUnary => true;
		public override bool IsInfix => true;

		public override ExpressionItem CreateExpressionItem(ExpressionItem left, ExpressionItem right) {
			return new BinaryExpression.Division(left, right);
		}
	}

	public static readonly SymbolicToken IntegerDivision = new SingletonIntegerDivision();
	private sealed class SingletonIntegerDivision : SymbolicToken {
		public override TokenType Type => TokenType.IntegerDivision;
		public override TokenRule Rule => TokenRule.Multiplicative;
		public override bool IsInfix => true;
		public override bool ForcesUnary => true;

		public override ExpressionItem CreateExpressionItem(ExpressionItem left, ExpressionItem right) {
			return new BinaryExpression.IntegerDivision(left, right);
		}
	}

	public static readonly SymbolicToken Modulo = new SingletonModulo();
	private sealed class SingletonModulo : SymbolicToken {
		public override TokenType Type => TokenType.Modulo;
		public override TokenRule Rule => TokenRule.Multiplicative;
		public override bool IsInfix => true;
		public override bool ForcesUnary => true;

		public override ExpressionItem CreateExpressionItem(ExpressionItem left, ExpressionItem right) {
			return new BinaryExpression.Modulo(left, right);
		}
	}

	public static readonly SymbolicToken ArithmeticLeftShift = new SingletonArithmeticLeftShift();
	private sealed class SingletonArithmeticLeftShift : SymbolicToken {
		public override TokenType Type => TokenType.ArithmeticLeftShift;
		public override TokenRule Rule => TokenRule.Shifts;
		public override bool IsInfix => true;
		public override bool ForcesUnary => true;

		public override ExpressionItem CreateExpressionItem(ExpressionItem left, ExpressionItem right) {
			return new BinaryExpression.ArithmeticLeftShift(left, right);
		}
	}

	public static readonly SymbolicToken LogicalRightShift = new SingletonLogicalRightShift();
	private sealed class SingletonLogicalRightShift : SymbolicToken {
		public override TokenType Type => TokenType.LogicalRightShift;
		public override TokenRule Rule => TokenRule.Shifts;
		public override bool IsInfix => true;
		public override bool ForcesUnary => true;

		public override ExpressionItem CreateExpressionItem(ExpressionItem left, ExpressionItem right) {
			return new BinaryExpression.LogicalRightShift(left, right);
		}
	}

	public static readonly SymbolicToken ArithmeticRightShift = new SingletonArithmeticRightShift();
	private sealed class SingletonArithmeticRightShift : SymbolicToken {
		public override TokenType Type => TokenType.ArithmeticRightShift;
		public override TokenRule Rule => TokenRule.Shifts;
		public override bool IsInfix => true;
		public override bool ForcesUnary => true;

		public override ExpressionItem CreateExpressionItem(ExpressionItem left, ExpressionItem right) {
			return new BinaryExpression.ArithmeticRightShift(left, right);
		}
	}

	public static readonly SymbolicToken BitwiseAnd = new SingletonBitwiseAnd();
	private sealed class SingletonBitwiseAnd : SymbolicToken {
		public override TokenType Type => TokenType.BitwiseAnd;
		public override TokenRule Rule => TokenRule.BitwiseAnd;
		public override bool IsInfix => true;
		public override bool ForcesUnary => true;

		public override ExpressionItem CreateExpressionItem(ExpressionItem left, ExpressionItem right) {
			return new BinaryExpression.BitwiseAnd(left, right);
		}
	}

	public static readonly SymbolicToken BitwiseOr = new SingletonBitwiseOr();
	private sealed class SingletonBitwiseOr : SymbolicToken {
		public override TokenType Type => TokenType.BitwiseOr;
		public override TokenRule Rule => TokenRule.BitwiseOr;
		public override bool IsInfix => true;
		public override bool ForcesUnary => true;

		public override ExpressionItem CreateExpressionItem(ExpressionItem left, ExpressionItem right) {
			return new BinaryExpression.BitwiseOr(left, right);
		}
	}

	public static readonly SymbolicToken BitwiseEor = new SingletonBitwiseEor();
	private sealed class SingletonBitwiseEor : SymbolicToken {
		public override TokenType Type => TokenType.BitwiseEor;
		public override TokenRule Rule => TokenRule.BitwiseEor;
		public override bool IsInfix => true;
		public override bool ForcesUnary => true;

		public override ExpressionItem CreateExpressionItem(ExpressionItem left, ExpressionItem right) {
			return new BinaryExpression.BitwiseEor(left, right);
		}
	}

	public static readonly SymbolicToken LEMerge = new SingletonLEMerge();
	private sealed class SingletonLEMerge : SymbolicToken {
		public override TokenType Type => TokenType.LEMerge;
		public override TokenRule Rule => TokenRule.BitwiseOr;
		public override bool IsInfix => true;
		public override bool ForcesUnary => true;

		public override ExpressionItem CreateExpressionItem(ExpressionItem left, ExpressionItem right) {
			return new BinaryExpression.LittleEndianMerge(left, right);
		}
	}

	public static readonly SymbolicToken LogicalAnd = new SingletonLogicalAnd();
	private sealed class SingletonLogicalAnd : SymbolicToken {
		public override TokenType Type => TokenType.LogicalAnd;
		public override TokenRule Rule => TokenRule.LogicalAnd;
		public override bool IsInfix => true;
		public override bool ForcesUnary => true;

		public override ExpressionItem CreateExpressionItem(ExpressionItem left, ExpressionItem right) {
			return new BinaryExpression.LogicalAnd(left, right);
		}
	}

	public static readonly SymbolicToken LogicalOr = new SingletonLogicalOr();
	private sealed class SingletonLogicalOr : SymbolicToken {
		public override TokenType Type => TokenType.LogicalOr;
		public override TokenRule Rule => TokenRule.LogicalOr;
		public override bool IsInfix => true;
		public override bool ForcesUnary => true;

		public override ExpressionItem CreateExpressionItem(ExpressionItem left, ExpressionItem right) {
			return new BinaryExpression.LogicalOr(left, right);
		}
	}

	public static readonly SymbolicToken Not = new SingletonNot();
	private sealed class SingletonNot : SymbolicToken {
		public override TokenType Type => TokenType.Not;
		public override TokenRule Rule => TokenRule.Unary;
		public override bool ForcesUnary => true;
		public override bool IsInfix => false;

		public override ExpressionItem CreateExpressionItem(ExpressionItem item) {
			return new UnaryExpression.Not(item);
		}
	}

	public static readonly SymbolicToken Negate = new SingletonNegate();
	private sealed class SingletonNegate : SymbolicToken {
		public override TokenType Type => TokenType.Negate;
		public override TokenRule Rule => TokenRule.Unary;
		public override bool ForcesUnary => true;
		public override bool IsInfix => false;

		public override ExpressionItem CreateExpressionItem(ExpressionItem item) {
			return new UnaryExpression.Negate(item);
		}
	}

	public static readonly SymbolicToken LowByte = new SingletonLowByte();
	private sealed class SingletonLowByte : SymbolicToken {
		public override TokenType Type => TokenType.LowByte;
		public override TokenRule Rule => TokenRule.Unary;
		public override bool ForcesUnary => true;
		public override bool IsInfix => false;

		public override ExpressionItem CreateExpressionItem(ExpressionItem item) {
			return new UnaryExpression.LowByte(item);
		}
	}

	public static readonly SymbolicToken HighByte = new SingletonHighByte();
	private sealed class SingletonHighByte : SymbolicToken {
		public override TokenType Type => TokenType.HighByte;
		public override TokenRule Rule => TokenRule.Unary;
		public override bool ForcesUnary => true;
		public override bool IsInfix => false;

		public override ExpressionItem CreateExpressionItem(ExpressionItem item) {
			return new UnaryExpression.HighByte(item);
		}
	}

	public static readonly SymbolicToken BankByte = new SingletonBankByte();
	private sealed class SingletonBankByte : SymbolicToken {
		public override TokenType Type => TokenType.BankByte;
		public override TokenRule Rule => TokenRule.Unary;
		public override bool ForcesUnary => true;
		public override bool IsInfix => false;

		public override ExpressionItem CreateExpressionItem(ExpressionItem item) {
			return new UnaryExpression.BankByte(item);
		}
	}

	public static readonly SymbolicToken Low16 = new SingletonLow16();
	private sealed class SingletonLow16 : SymbolicToken {
		public override TokenType Type => TokenType.Low16;
		public override TokenRule Rule => TokenRule.Unary;
		public override bool ForcesUnary => true;
		public override bool IsInfix => false;

		public override ExpressionItem CreateExpressionItem(ExpressionItem item) {
			return new UnaryExpression.Low16(item);
		}
	}

	public static readonly SymbolicToken High16 = new SingletonHigh16();
	private sealed class SingletonHigh16 : SymbolicToken {
		public override TokenType Type => TokenType.High16;
		public override TokenRule Rule => TokenRule.Unary;
		public override bool ForcesUnary => true;
		public override bool IsInfix => false;

		public override ExpressionItem CreateExpressionItem(ExpressionItem item) {
			return new UnaryExpression.High16(item);
		}
	}

	public static readonly SymbolicToken BankOnly = new SingletonBankOnly();
	private sealed class SingletonBankOnly : SymbolicToken {
		public override TokenType Type => TokenType.BankOnly;
		public override TokenRule Rule => TokenRule.Unary;
		public override bool ForcesUnary => true;
		public override bool IsInfix => false;

		public override ExpressionItem CreateExpressionItem(ExpressionItem item) {
			return new UnaryExpression.BankOnly(item);
		}
	}

	public static readonly SymbolicToken Equality = new SingletonEquality();
	private sealed class SingletonEquality : SymbolicToken {
		public override TokenType Type => TokenType.Equality;
		public override TokenRule Rule => TokenRule.Equality;
		public override bool IsInfix => true;
		public override bool ForcesUnary => true;

		public override ExpressionItem CreateExpressionItem(ExpressionItem left, ExpressionItem right) {
			return new BinaryExpression.Equality(left, right);
		}
	}

	public static readonly SymbolicToken Inequality = new SingletonInequality();
	private sealed class SingletonInequality : SymbolicToken {
		public override TokenType Type => TokenType.Inequality;
		public override TokenRule Rule => TokenRule.Equality;
		public override bool IsInfix => true;
		public override bool ForcesUnary => true;

		public override ExpressionItem CreateExpressionItem(ExpressionItem left, ExpressionItem right) {
			return new BinaryExpression.Inequality(left, right);
		}
	}

	public static readonly SymbolicToken GreaterThan = new SingletonGreaterThan();
	private sealed class SingletonGreaterThan : SymbolicToken {
		public override TokenType Type => TokenType.GreaterThan;
		public override TokenRule Rule => TokenRule.Comparison;
		public override bool IsInfix => true;
		public override bool ForcesUnary => true;

		public override ExpressionItem CreateExpressionItem(ExpressionItem left, ExpressionItem right) {
			return new BinaryExpression.GreaterThan(left, right);
		}
	}

	public static readonly SymbolicToken LessThan = new SingletonLessThan();
	private sealed class SingletonLessThan : SymbolicToken {
		public override TokenType Type => TokenType.LessThan;
		public override TokenRule Rule => TokenRule.Comparison;
		public override bool IsInfix => true;
		public override bool ForcesUnary => true;

		public override ExpressionItem CreateExpressionItem(ExpressionItem left, ExpressionItem right) {
			return new BinaryExpression.LessThan(left, right);
		}
	}

	public static readonly SymbolicToken GreaterThanEqual = new SingletonGreaterThanEqual();
	private sealed class SingletonGreaterThanEqual : SymbolicToken {
		public override TokenType Type => TokenType.GreaterThanEqual;
		public override TokenRule Rule => TokenRule.Comparison;
		public override bool IsInfix => true;
		public override bool ForcesUnary => true;

		public override ExpressionItem CreateExpressionItem(ExpressionItem left, ExpressionItem right) {
			return new BinaryExpression.GreaterThanEqual(left, right);
		}
	}

	public static readonly SymbolicToken LessThanEqual = new SingletonLessThanEqual();
	private sealed class SingletonLessThanEqual : SymbolicToken {
		public override TokenType Type => TokenType.LessThanEqual;
		public override TokenRule Rule => TokenRule.Comparison;
		public override bool IsInfix => true;
		public override bool ForcesUnary => true;

		public override ExpressionItem CreateExpressionItem(ExpressionItem left, ExpressionItem right) {
			return new BinaryExpression.LessThanEqual(left, right);
		}
	}

	public static readonly SymbolicToken NullCoalesce = new SingletonNullCoalesce();
	private sealed class SingletonNullCoalesce : SymbolicToken {
		public override TokenType Type => TokenType.NullCoalesce;
		public override TokenRule Rule => TokenRule.NullCoalesce;
		public override bool IsInfix => true;
		public override bool ForcesUnary => true;

		public override ExpressionItem CreateExpressionItem(ExpressionItem left, ExpressionItem right) {
			return new BinaryExpression.NullCoalesce(left, right);
		}
	}

	public static readonly SymbolicToken NullCoalescePostfix = new SingletonNullCoalescePostfix();
	private sealed class SingletonNullCoalescePostfix : SymbolicToken {
		public override TokenType Type => TokenType.NullCoalesceShort;
		public override TokenRule Rule => TokenRule.Postfix;
		public override bool IsInfix => false;
		public override bool ForcesUnary => false;

		public override ExpressionItem CreateExpressionItem(ExpressionItem item) {
			return new UnaryExpression.NullCoalesce(item);
		}
	}
}
