namespace Futaba;

/// <summary>
/// Dictates the maximum warning level to emit for instructions without an operand size token.
/// </summary>
public enum MissingTokenSeverity {
	/// <summary>
	/// Tokens are not required on any instruction.
	/// No warnings or errors are emitted.
	/// </summary>
	Ignore = 0,

	/// <summary>
	/// Tokens are required on instructions where operand size may be ambiguous.
	/// Instructions where the remaining syntax can precisely identify
	/// the operand size do not require a token.
	/// </summary>
	Ambiguous,

	/// <summary>
	/// Tokens are required on instructions where operand size may be ambiguous.
	/// Extra warnings are emitted for the JSR, JMP, and JML instructions that use
	/// a 16-bit operand when the operand is potentially invalid from the call point.
	/// </summary>
	AbsolutePointers,

	/// <summary>
	/// All instructions or that share a mnemonic or address memory require a size token.
	/// <br/>
	/// Only the following mnemonics are excluded:
	/// BRK; COP; WDM; JSR; JSL; JMP; JML; REP; SEP; MVN;
	/// dbnz Y, rel; tcall; pcall
	/// </summary>
	AllInstructions
}
