namespace Futaba;

internal enum TokenRule {
	Expression = 0, // NullCoalesce

	NullCoalesce,   // equality ( ( "??" ) equality )*

	Equality,       // comparison ( ( "==" | "!=" ) comparison )*
	Comparison,     // logicalor ( ( ">" | "<" | ">=" | "<=" ) logicalor )*

	LogicalOr,      // logicaland ( "||" logicaland )*
	LogicalAnd,     // bitwiseor ( "&&" bitwiseor )*

	BitwiseOr,      // bitwiseeor ( ( "|" | "][" ) bitwiseeor )*
	BitwiseEor,     // bitwiseand ( "^" bitwiseand )*
	BitwiseAnd,     // shifts ( "&" shifts )*

	Shifts,         // additive ( ( "<<" | ">>" | ">>>" ) additive )*

	Additive,       // multiplicative ( ( "+" | "-" ) multiplicative )*
	Multiplicative, // unary ( ( "*" | "/" | "//" | "%" ) unary )*

	Unary,          // ( "-" | "+" | ( "<" | ">" | "^" | "<&" | ">&" | "^&" ) expression | primary
	Postfix,        // primary ( "?" )*

	FunctionCall,

	Primary,        // NUMBER | STRING | SYMBOL | "(" expression ")"
}
