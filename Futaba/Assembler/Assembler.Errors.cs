namespace Futaba;

// TODO change to an object that handles errors so it can be passed around easily?
// would enable better potential verbose mode support 

partial class Assembler {
	const string ErrorText = " ERROR: ";
	const string WarningText = " WARNING: ";

	internal void IncrementWarnings() {
		if (WarningsAreErrors) {
			IncrementErrors();
		} else {
			WarningCount++;
		}
	}

	internal void IncrementErrors() {
		if (++ErrorCount > MaximumErrors) {
			TooManyErrors();
		}

		[DoesNotReturn]
		static void TooManyErrors() {
			throw new InvalidOperationException($"Too many errors occured. Assembly has been aborted.");
		}
	}

	internal void Error(string message) {
		Error(message, null, CurrentSourceLine);
	}

	internal void Warning(string message) {
		Warning(message, null, CurrentSourceLine);
	}

	internal void Error(string message, string details) {
		Error(message, details, CurrentSourceLine);
	}

	internal void Warning(string message, string details) {
		Warning(message, details, CurrentSourceLine);
	}

	internal void Error(string message, SourceLine srcLine) {
		Error(message, null, srcLine);
	}

	internal void Warning(string message, SourceLine srcLine) {
		Warning(message, null, srcLine);
	}

	internal void Error(string message, string? details, SourceLine srcLine) {
		ErrorOutWrite(ErrorText, message, details, srcLine, ConsoleColor.Red);
		IncrementErrors();
	}

	internal void Warning(string message, string? details, SourceLine srcLine) {
		ErrorOutWrite(WarningText, message, details, srcLine, ConsoleColor.DarkYellow);
		IncrementWarnings();
	}

	private void ErrorOutWrite(string messageType, string message, string? details, SourceLine srcLine, ConsoleColor color) {
		Console.ForegroundColor = color;

		if (!srcLine.IsNull) {
			ErrorOut.Write(srcLine);
		}
		
		ErrorOut.Write(messageType);
		ErrorOut.Write(message);

		if (details is not null) {
			ErrorOut.Write(": ");
			ErrorOut.Write(details);
		}

		ErrorOut.Write(" | ");

		Console.ForegroundColor = ConsoleColor.DarkGray;

		ErrorOut.Write(srcLine.GetLineContents());

		Console.ResetColor();

		ErrorOut.WriteLine();

	}



	internal void Error_BadResolve(IExpressionReturn expr, string unresolvedmsg) {
		Error_BadResolve(expr, unresolvedmsg, CurrentSourceLine);
	}

	internal void Error_BadResolve(IExpressionReturn expr) {
		Error_BadResolve(expr, MsgInfo.ValueMustResolveNow, CurrentSourceLine);
	}

	// TODO need to have expressions propagate an error string for more precision
	// also maybe remove the base message or let it be null
	internal void Error_BadResolve(IExpressionReturn expr, string unresolvedmsg, SourceLine sourceLine) {
		switch (expr.ReturnState) {
			case ExpressionState.InvalidContext:
				Error(unresolvedmsg, "Invalid symbol context", sourceLine);
				break;

			case ExpressionState.Unresolved:
				Error(unresolvedmsg, sourceLine);
				break;

			case ExpressionState.DivideByZero:
				Error(unresolvedmsg, "Division by zero", sourceLine);
				break;

			case ExpressionState.MissingSymbol:
				Error(unresolvedmsg, "Symbol does not exist", sourceLine);
				break;

			case ExpressionState.SyntaxError:
				Error(unresolvedmsg, "Syntax error in expression", sourceLine);
				break;

			case ExpressionState.InvalidArgument:
				Error(unresolvedmsg, "Invalid function argument", sourceLine);
				break;

			case ExpressionState.InvalidArgumentToStringFunction:
				Error(unresolvedmsg, "Invalid function argument", sourceLine);
				break;

			case ExpressionState.VariableNotFound:
				Error(unresolvedmsg, "Variable does not exist", sourceLine);
				break;

			case ExpressionState.MissingIdentifier:
				Error(unresolvedmsg, "Missing identifier", sourceLine);
				break;

			default:
				Error(unresolvedmsg, sourceLine);
				break;
		}
	}

	private void MissingOperand() {
		Error("Missing operand to instruction");
	}


	private void NoTokens(SizeToken size) {
		if (size is not SizeToken.Unspecified) {
			Error("Size tokens are not valid on this instruction.");
		}
	}



	private void PreferDotB(SizeToken size) {
		if (size is not SizeToken.B) {
			if (size is not SizeToken.Unspecified) {
				InvalidSizeToken();
			} else {
				AmbiguousSizeTokens();
			}
		}
	}

	private void OnlyDotB(SizeToken size) {
		if (size is not (SizeToken.B or SizeToken.Unspecified)) {
			InvalidSizeToken();
		}
	}

	private void OnlyDotW(SizeToken size) {
		if (size is not (SizeToken.W or SizeToken.Unspecified)) {
			InvalidSizeToken();
		}
	}

	private void PreferDotW(SizeToken size) {
		if (size is not SizeToken.W) {
			if (size is not SizeToken.Unspecified) {
				InvalidSizeToken();
			} else {
				AmbiguousSizeTokens();
			}
		}
	}

	private void AmbiguousSizeTokens() {
		if (MissingTokenSeverity >= MissingTokenSeverity.Ambiguous) {
			Warning(MsgInfo.MissingSizeToken);
		}
	}

	private void AggroDotB(SizeToken size) {
		if (size is not SizeToken.B) {
			if (size is SizeToken.Unspecified) {
				AggressiveSizeTokens();
			} else {
				InvalidSizeToken();
			}
		}
	}

	private void AggroDotW(SizeToken size) {
		if (size is not SizeToken.W) {
			if (size is SizeToken.Unspecified) {
				AggressiveSizeTokens();
			} else {
				InvalidSizeToken();
			}
		}
	}

	private void AggressiveSizeTokens() {
		if (MissingTokenSeverity >= MissingTokenSeverity.AllInstructions) {
			Warning(MsgInfo.MissingSizeToken);
		}
	}

	private void InvalidSizeToken() {
		Error(MsgInfo.InvalidAddressingMode, "invalid size token.");
	}

	private void InvalidAddressingMode() {
		Error(MsgInfo.InvalidAddressingMode);
	}

	private void TokensOnImplied() {
		Error(MsgInfo.InvalidAddressingMode, details: "do not include size tokens on implied instructions.");
	}

	private unsafe void UnexpectedCharacter() {
		Error($"Unexpected character: '{*_reading}'");
	}

	private unsafe void UnexpectedCharacter(char* ptr) {
		Error($"Unexpected character: '{*ptr}'");
	}
}
