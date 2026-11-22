namespace Futaba;

internal enum TokenType {
	Start,
	End,

	LeftParen,
	RightParen,
	Comma,

	// all operators go last
	BinaryOperators, // for categorizing

	Addition,
	Subtraction,
	Multiplication,
	Division,
	IntegerDivision,
	Modulo,
	ArithmeticLeftShift,
	ArithmeticRightShift,
	LogicalRightShift,
	BitwiseAnd,
	BitwiseOr,
	BitwiseEor,
	LEMerge,
	LogicalAnd,
	LogicalOr,

	Equality,
	Inequality,
	GreaterThan,
	LessThan,
	GreaterThanEqual,
	LessThanEqual,

	NullCoalesce,

	UnaryOperators, // for categorizing

	Not,
	Negate,
	LowByte,
	HighByte,
	BankByte,
	Low16,
	High16,
	BankOnly,
	NullCoalesceShort,

	FunctionCall,

	ValueTypes, // for categorizing

	Number,
	String,
	Char,
	Symbol,
	Variable,
}
