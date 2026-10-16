using System.Collections.Frozen;

namespace Futaba;

internal static class Directives {
	internal const int LongestDirectiveLength = 15;

	internal const char SPLAT = '*';
	internal const char BadChar = '\xBEBE';

	internal const char
		SublabelDelimiter = '.',
		SymbolContextAccess = ':',
		InternalVariableToken = ':',
		CommandSeparator = ':',

		ImmediateMode = '#',
		ARegister = 'A',
		XRegister = 'X',
		YRegister = 'Y',
		StackRelative = 'S',

		IndirectOpen = '(',
		IndirectClose = ')',
		IndirectLongOpen = '[',
		IndirectLongClose = ']',

		RepeatOp = '#',
		BranchLiteral = '#',
		BROP = SPLAT,
		JNOP = '&',
		BTYS = '<',

		IndexToken65816 = ',',
		IndexTokenSPC70 = '+',

		CharDelim = '\'',
		CommentChar = ';',
		NewLine = '\n',
		EscapeChar = '\\';

	internal const string
		RAW                           = "raw",

		ORG                           = "org",
		REBANK                        = "rebank",
		SITE                          = "site",
		WARNPC                        = "warnpc",
		WARNSITE                      = "warnsite",
		SAFEORG                       = "safeorg",
		SKIP                          = "skip",
		SKIPTO                        = "skipto",
		MMC                           = "mmc",

		BANKGUARD                     = "bankguard",

		DOCK                          = "dock",
		UNDOCK                        = "undock",
		PILING                        = "piling",
		PILE                          = "pile",
		PLOP                          = "plop",
		POOL                          = "pool",
		ENDPOOL                       = "endpool",

		FILL                          = "fill",
		BYTE                          = "byte",
		WORD                          = "word",
		LONG                          = "long",
		DOUBLE                        = "double",
		RANDOM                        = "random",
		SIZE                          = "size",
		COUNT                         = "count",
		UNTIL                         = "until",
		ALIGN                         = "align",
		ARRANGE                       = "arrange",

		INCBIN                        = "incbin",
		INCSRC                        = "incsrc",
		ENCODER                       = "encoder",

		MACRO                         = "macro",
		ENDMACRO                      = "endmacro",
		FUNCTION                      = "function",

		ALLOCATE                      = "allocate",
		ENDALLOCATE                   = "endallocate",

		LANG                          = "lang",
		ARCH                          = "arch",

		PUSHPC                        = "pushpc",
		PULLPC                        = "pullpc",

		PUSHSITE                      = "pushsite",
		PULLSITE                      = "pullsite",

		IF                            = "if",
		ENDIF                         = "endif",
		ELSE                          = "else",

		BREAKPOINTS                   = "breakpoints",

		PRINT                         = "print",
		PRINTF                        = "printf",
		WARN                          = "warn",
		ERROR                         = "error";

	internal const string LongBar = "----------------------------------------------------------------------------------------------------";

	static Directives() {
		Debug.Assert(LongBar.Length is 100);
	}

	internal static readonly FrozenSet<string> ReservedKeywords = [
		// 65816 registers
		"A", "X", "Y", "a", "x", "y",

		// SPC700 registers
		"SP", "Sp", "sp", "sP", "C", "c",

		// Super FX registers
		"R0", "R1", "R2", "R3", "R4", "R5", "R6", "R7", "R8", "R9", "R10", "R11", "R12", "R13", "R14", "R15",
		"r0", "r1", "r2", "r3", "r4", "r5", "r6", "r7", "r8", "r9", "r10", "r11", "r12", "r13", "r14", "r15",
	];
}
