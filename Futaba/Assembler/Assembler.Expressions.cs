namespace Futaba;

// TODO can probably use a class that implements array pools instead of List<> to reduce allocations
unsafe partial class Assembler {
	private IExpressionReturn ParseOperand(OperandString operand) {
		return ParseOperand(operand.Start, operand.End, SymbolContext.Default);
	}

	private IExpressionReturn ParseOperandSliced(OperandString operand, int sliceStart) {
		return ParseOperand(operand.Start + sliceStart, operand.End, SymbolContext.Default);
	}

	private IExpressionReturn ParseOperandSliced(OperandString operand, int sliceStart, int sliceEnd) {
		return ParseOperand(operand.Start + sliceStart, operand.End - sliceEnd, SymbolContext.Default);
	}

	private IExpressionReturn ParseOperand(OperandString operand, SymbolContext context) {
		return ParseOperand(operand.Start, operand.End, context);
	}

	// TODO consider a WriteOperand that directly writes bytes to bypass making tons of wrapper objects for numbers when not needed
	// GC is kinda really good though... using a singleton didn't have a performance benefit whatsoever
	// jpdasm: 676000 allocs for number
	// usdasm: 300000
	// push: 6000
	private IExpressionReturn ParseOperand(char* opreading, char* opend, SymbolContext context) {
		FastRead.TrimWhiteSpace(ref opreading, ref opend);

		if (opreading == opend) {
			Error("Empty expression");
			return MathHelpers.InvalidExpression;
		}

		IToken? last = null;

		char c = *opreading;

		// heuristic checks to avoid full equation parsing
		// we expect most values to be either bare symbols or bare raw numbers
		// so we'll reduce allocations and avoid parsing tokens and navigating trees
		// by looking for these things first and returning them in special containers
		switch (c) {
			// this is the only place bare relative labels are allowed
			case '+':
				int plusCount = FastRead.CountCharacter(ref opreading, opend, '+');

				// this is a label
				if (opreading == opend) {
					if (TryGetNextPlusLabel(plusCount, out var plusLabel)) {
						return plusLabel.CreateExpressionItem(context);
					} else {
						return InvalidExpression.MissingSymbol;
					}
				}

				// leading plus signs are special in that they do nothing,
				// so we can skip all of them
				// no special casing for +number, because that's dumb
				// it's not important to optimize the way -number is
				break;

			case '-':
				char* minusPeek = opreading + 1;

				if (minusPeek < opend) {
					// might be a number
					if (minusPeek->IsADecimalDigit) {
						opreading = minusPeek;
						decimal minval = FastRead.ReadDecimal(ref opreading, opend);
						minval = -minval;

						FastRead.SkipInlineWhitespace(ref opreading, opend);

						if (opreading == opend) {
							return new NumberToken(minval);
						}

						last = new NumberToken(minval);
						break;
					}
				}

				int minusCount = FastRead.CountCharacter(ref opreading, opend, '-');

				// this is a label
				if (opreading == opend) {
					if (TryGetPreviusMinusLabel(minusCount, out var minLabel)) {
						return minLabel.CreateExpressionItem(context);
					} else {
						return InvalidExpression.MissingSymbol;
					}
				}

				// otherwise it's a bunch of minus signs. what are you doing?

				if ((minusCount & 1) is 1) {
					// odd number of - means negate
					// even means do nothing
					last = SymbolicToken.Negate;
				}

				break;

			// symbol or function
			case 'A' or 'B' or 'C' or 'D' or 'E' or 'F' or 'G' or 'H' or 'I' or 'J' or 'K' or 'L' or 'M' or
					'N' or 'O' or 'P' or 'Q' or 'R' or 'S' or 'T' or 'U' or 'V' or 'W' or 'X' or 'Y' or 'Z' or
					'a' or 'b' or 'c' or 'd' or 'e' or 'f' or 'g' or 'h' or 'i' or 'j' or 'k' or 'l' or 'm' or
					'n' or 'o' or 'p' or 'q' or 'r' or 's' or 't' or 'u' or 'v' or 'w' or 'x' or 'y' or 'z' or
					'_':

				char* symbolNameStart = opreading;

				int llen = FastRead.ReadAlphanumericHelper(opreading, opend);
				opreading += llen;

				// check why the loop broke
				if (*opreading is '(') { // function
					CharSpan funcName = CharSpanHelpers.CreateSpan(symbolNameStart, opreading);
					opreading++;
					last = new FunctionToken(new string(funcName));
				} else {
					int sublen = FastRead.ReadSublabelBetter(opreading, opend);
					opreading += sublen;
					CharSpan symbolName = CharSpanHelpers.CreateSpan(symbolNameStart, opreading);

					ExpressionSymbol addSym = MakeSymbolToken(symbolName, context, ref opreading, opend);

					if (opreading == opend) {
						return addSym;
					}

					last = addSym;
				}

				break;

			// DOT
			case '.':
				if (TryExpandSublabelName(ref opreading, opend, out var sublabelexpanded)) {
					ExpressionSymbol addLab = MakeSymbolToken(sublabelexpanded, context, ref opreading, opend);

					if (opreading == opend) {
						return addLab;
					}

					last = addLab;
					break;
				}

				return InvalidExpression.MissingSymbol;

			// DIGITS
			case '0' or '1' or '2' or '3' or '4' or '5' or '6' or '7' or '8' or '9':
				var decval = FastRead.ReadDecimal(ref opreading, opend);

				FastRead.SkipInlineWhitespace(ref opreading, opend);

				if (opreading == opend) {
					return new NumberToken(decval);
				}

				last = new NumberToken(decval);
				break;

			// DOLLAR SIGN
			case '$':
				if (!FastRead.TryReadHex(ref opreading, opend, out int hexval)) {
					return InvalidExpression.SyntaxError;
				}

				if (opreading == opend) {
					return new NumberToken(hexval);
				}

				last = new NumberToken(hexval);
				break;

			// PERCENTAGE SIGN
			case '%':
				if (!FastRead.TryReadBinary(ref opreading, opend, out int binval)) {
					return InvalidExpression.SyntaxError;
				}

				if (opreading == opend) {
					return new NumberToken(binval);
				}

				last = new NumberToken(binval);
				break;

			// SINGLE QUOTE
			case '\'':
				if (TryMakeCharacterToken(ref opreading, opend, out var ctok)) {
					if (opreading == opend) {
						return ctok;
					}

					last = ctok;
					break;
				}

				return InvalidExpression.SyntaxError;

			// VARIABLES
			case '!':
				if (TryMakeVariableToken(ref opreading, opend, out var vartok)) {
					if (opreading == opend) {
						return vartok;
					} else {
						last = vartok;
					}
				} else {
					return InvalidExpression.MissingVariable;
				}

				break;
		}

		// if we failed the heuristic tests, continue on

		List<IToken> tokens = new((int) (opend - opreading));

		if (last is not null) {
			tokens.Add(last);
		} else {
			last = SymbolicToken.Start;
		}

		if (ContinueExpressionScan(tokens, last, opreading, opend, context, false)) {
			return new ExpressionTree(CreateExpressionTree(tokens));
		} else {
			return InvalidExpression.SyntaxError;
		}
	}

	private bool ContinueExpressionScan(List<IToken> tokens, IToken last, char* exprReading, char* exprEnd, SymbolContext context, bool parsingFunction) {
		// we're using "last = " because we do, in fact, want to check the last token sometimes
		// but we don't want to check it from the list, because that's slower
		while (exprReading < exprEnd) {
			switch (*exprReading) {
				// WHITE SPACE
				case ' ':
				case '\t':
				case '\r':
					FastRead.SkipInlineWhitespace(ref exprReading, exprEnd);
					continue;

				// PARENS
				case '(':
					exprReading++;

					// peek ahead for potential relative label
					FastRead.SkipInlineWhitespace(ref exprReading, exprEnd);

					if (exprReading < exprEnd && *exprReading is '+' or '-') {
						char rep = *exprReading;

						int repcount = FastRead.CountCharacter(ref exprReading, exprEnd, rep);

						FastRead.SkipInlineWhitespace(ref exprReading, exprEnd);

						// it's a relative label
						if (*exprReading is ')') {
							Symbol? reladd;

							if (rep is '+') {
								TryGetNextPlusLabel(repcount, out reladd);
							} else {
								TryGetPreviusMinusLabel(repcount, out reladd);
							}

							last = (reladd ?? BadSymbolPlaceHolder.Instance).CreateExpressionItem(context);
							exprReading++;
							tokens.Add(last);
						} else {
							// otherwise it's some thing and we can just crunch down the number of successive symbols
							tokens.Add(SymbolicToken.LeftParen);

							if (rep is '-' && (repcount & 1) is 1) {
								last = SymbolicToken.Negate;
								tokens.Add(last);
							} else {
								// + doesn't need anything here, because it's a semantically meaningless operator
							}
						}
					} else {
						last = SymbolicToken.LeftParen;
						tokens.Add(last);
					}
					continue;

				case ')':
					last = SymbolicToken.RightParen;
					goto Advance1;

				// GREATER THAN
				case '>':
					switch (ExprPeek()) {
						case '>':
							if (last.ForcesUnary) {
								last = SymbolicToken.HighByte;
								goto Advance1;
							} else if (ExprPeek2() is '>') {
								last = SymbolicToken.ArithmeticRightShift;
								goto Advance3;
							} else {
								last = SymbolicToken.LogicalRightShift;
								goto Advance2;
							}

						case '&':
							last = SymbolicToken.High16;
							goto Advance2;

						case '=':
							last = SymbolicToken.GreaterThanEqual;
							goto Advance2;
					}

					if (last.ForcesUnary) {
						last = SymbolicToken.HighByte;
					} else {
						last = SymbolicToken.GreaterThan;
					}

					goto Advance1;

				// LESS THAN
				case '<':
					switch (ExprPeek()) {
						case '<':
							if (last.ForcesUnary) {
								last = SymbolicToken.LowByte;
								goto Advance1;
							} else {
								last = SymbolicToken.ArithmeticLeftShift;
								goto Advance2;
							}

						case '&':
							last = SymbolicToken.Low16;
							goto Advance2;

						case '=':
							last = SymbolicToken.LessThanEqual;
							goto Advance2;
					}

					if (last.ForcesUnary) {
						last = SymbolicToken.LowByte;
					} else {
						last = SymbolicToken.LessThan;
					}

					goto Advance1;

				// CARET
				case '^':
					// using single branch switch statements throughout this
					// just so the syntax is easier to expand without major refactors
					switch (ExprPeek()) {
						case '&':
							last = SymbolicToken.BankOnly;
							goto Advance2;
					}

					if (last.ForcesUnary) {
						last = SymbolicToken.BankByte;
					} else {
						last = SymbolicToken.BitwiseEor;
					}

					goto Advance1;

				// TILDE
				case '~':
					last = SymbolicToken.Not;
					goto Advance1;

				// PLUS SIGN
				case '+':
					if (last.ForcesUnary) {
						// don't add a token for +x when it's unary, since that does nothing
						exprReading++;
						continue;
					} else {
						last = SymbolicToken.Addition;
					}

					goto Advance1;

				// HYPHEN
				case '-':
					if (last.ForcesUnary) {
						last = SymbolicToken.Negate;
					} else {
						last = SymbolicToken.Subtraction;
					}

					goto Advance1;

				// SPLAT
				case '*':
					last = SymbolicToken.Multiplication;
					goto Advance1;

				// SOLIDUS
				case '/':
					switch (ExprPeek()) {
						case '/':
							last = SymbolicToken.IntegerDivision;
							goto Advance2;
					}

					last = SymbolicToken.Division;
					goto Advance1;

				// VERTICAL BAR
				case '|':
					switch (ExprPeek()) {
						case '|':
							last = SymbolicToken.LogicalOr;
							goto Advance2;
					}

					last = SymbolicToken.BitwiseOr;
					goto Advance1;

				// AMPERSAND
				case '&':
					switch (ExprPeek()) {
						case '&':
							last = SymbolicToken.LogicalAnd;
							goto Advance2;
					}

					last = SymbolicToken.BitwiseAnd;
					goto Advance1;

				case ',':
					last = SymbolicToken.Comma;
					goto Advance1;

				// symbol or function
				case 'A' or 'B' or 'C' or 'D' or 'E' or 'F' or 'G' or 'H' or 'I' or 'J' or 'K' or 'L' or 'M' or
						'N' or 'O' or 'P' or 'Q' or 'R' or 'S' or 'T' or 'U' or 'V' or 'W' or 'X' or 'Y' or 'Z' or
						'a' or 'b' or 'c' or 'd' or 'e' or 'f' or 'g' or 'h' or 'i' or 'j' or 'k' or 'l' or 'm' or
						'n' or 'o' or 'p' or 'q' or 'r' or 's' or 't' or 'u' or 'v' or 'w' or 'x' or 'y' or 'z' or
						'_':

					char* symbolNameStart = exprReading;

					int llen = FastRead.ReadAlphanumericHelper(exprReading, exprEnd);
					exprReading += llen;

					// check why the loop broke
					if (*exprReading is '(') { // function
						CharSpan funcName = CharSpanHelpers.CreateSpan(symbolNameStart, exprReading);
						last = new FunctionToken(new string(funcName));
						goto Advance1;

					} else {
						int sublen = FastRead.ReadSublabelBetter(exprReading, exprEnd);
						exprReading += sublen;

						CharSpan symbolName = CharSpanHelpers.CreateSpan(symbolNameStart, exprReading);

						last = MakeSymbolToken(symbolName, context, ref exprReading, exprEnd);
						tokens.Add(last);
					}

					continue;

				// DIGITS
				case '0' or '1' or '2' or '3' or '4' or '5' or '6' or '7' or '8' or '9':
					var decval = FastRead.ReadDecimal(ref exprReading, exprEnd);
					last = new NumberToken(decval);
					tokens.Add(last);
					continue;

				// DOLLAR SIGN
				case '$':
					if (FastRead.TryReadHex(ref exprReading, exprEnd, out int hexval)) {
						last = new NumberToken(hexval);
						tokens.Add(last);
						continue;
					}

					Error(MsgInfo.InvalidHex);
					goto Invalid;

				// PERCENTAGE SIGN
				case '%':
					if (last.ForcesUnary) {
						if (FastRead.TryReadBinary(ref exprReading, exprEnd, out int binval)) {
							last = new NumberToken(binval);
							tokens.Add(last);
							continue;
						}

						Error(MsgInfo.InvalidBin);
						goto Invalid;
					}

					last = SymbolicToken.Modulo;
					goto Advance1;

				// QUESTION MARK
				case '?':
					if (ExprPeek() is '?') {
						last = SymbolicToken.NullCoalesce;
						goto Advance2;
					}

					last = SymbolicToken.NullCoalescePostfix;
					goto Advance1;

				// DOT
				case '.':
					if (TryExpandSublabelName(ref exprReading, exprEnd, out var sublabelexpanded)) {
						last = MakeSymbolToken(sublabelexpanded, context, ref exprReading, exprEnd);
						tokens.Add(last);
						continue;
					}

					goto Invalid;

				// EQUALS SIGN
				case '=':
					switch (ExprPeek()) {
						case '=':
							last = SymbolicToken.Equality;
							goto Advance2;
					}

					goto InvalidCharacter;

				// EXCLAMATION MARK
				case '!':
					// internal variables handled in TryMakeVariableToken
					switch (ExprPeek()) {
						case '=':
							last = SymbolicToken.Inequality;
							goto Advance2;
					}

					// TODO capture variables as variables when in functions and give them a unique clone method for capturing value?
					if (TryMakeVariableToken(ref exprReading, exprEnd, out var vartok)) {
						last = vartok;
						tokens.Add(last);
						continue;
					}

					// Error handled in TryMakeVariableToken
					goto Invalid;

				// LEFT BRACE
				case '{':
					if (!parsingFunction) {
						Error("Invalid syntax: function argument outside of function");
						goto Invalid;
					}

					exprReading++;

					FastRead.TryReadIdentifier(ref exprReading, exprEnd, out CharSpan paramName);

					if (!paramName.IsEmpty && exprReading < exprEnd && *exprReading is '}') {
						// look for existing copies of this parameter
						// they need to all be the exact same object
						var tokenSpan = CollectionsMarshal.AsSpan(tokens);

						foreach (var o in tokenSpan) {
							if (o is NamedParameter na && na.Name.SequenceEqual(paramName)) {
								last = na;
								goto Advance1;
							}
						}

						last = new NamedParameter(new string(paramName));
						goto Advance1;
					}

					Error("Invalid parameter dereference");
					goto Invalid;

				// RIGHT BRACKET
				case ']':
					switch (ExprPeek()) {
						case '[':
							last = SymbolicToken.LEMerge;
							goto Advance2;
					}

					goto InvalidCharacter;

				// SINGLE QUOTE
				case '\'':
					if (TryMakeCharacterToken(ref exprReading, exprEnd, out var ctok)) {
						last = ctok;
						tokens.Add(last);
						continue;
					}

					goto Invalid;

				// DOUBLE QUOTE
				case '"':
					if (TryReadAndExplodeStringContents(ref exprReading, exprEnd, out string? str)) {
						last = new StringToken(str, _currentEncoder);
						tokens.Add(last);
						continue;
					} else {
						Error("Invalid string");
						goto Invalid;
					}

				// invalid characters
				case '#':
				case '@':
				case ':':
				case '\\':
				case '`':
				case '}':
				case '[':
					goto InvalidCharacter;

				// this should never happen, but if it does, treat is as a comment
				case ';':
					goto Finished;

				// ditto
				case NewLine:
					goto Finished;
			}

			InvalidCharacter:
			UnexpectedCharacter(exprReading);

			Invalid:
			return false;

			Advance1:
			exprReading++;
			tokens.Add(last);
			continue;

			Advance2:
			exprReading += 2;
			tokens.Add(last);
			continue;

			Advance3:
			exprReading += 3;
			tokens.Add(last);
			continue;
		}

		Finished:
		tokens.Add(SymbolicToken.End);

		return true;

		char ExprPeek() {
			if (exprReading == exprEnd) return BadChar;
			return exprReading[1];
		}

		char ExprPeek2() {
			char* peeker = exprReading + 2;
			if (peeker >= exprEnd) return BadChar;
			return *peeker;
		}
	}

	private bool TryMakeCharacterToken(ref char* lread, char* lend, [NotNullWhen(true)] out NumberToken? charOut) {
		char quotedChar;
		char* cread = lread;

		if (++cread < lend) {
			quotedChar = *cread;

			if (quotedChar is EscapeChar) {
				if (++cread < lend) {
					quotedChar = GetEscapedCharacter(*cread);
				} else {
					goto BadCharToken;
				}
			}

			if (++cread < lend && *cread is CharDelim) {
				charOut = new NumberToken(_currentEncoder.Encode(quotedChar));
				lread = cread + 1;
				return true;
			}
		}

		BadCharToken:
		Error("Unclosed character");
		charOut = null;
		lread = cread;
		return false;
	}

	private bool TryMakeVariableToken(ref char* lread, char* lend, [NotNullWhen(true)] out IReturnableToken? getVar) {
		lread++;

		if (lread < lend) {
			if (*lread is InternalVariableToken) {
				lread++;

				if (TryReadVariableName(ref lread, lend, out CharSpan intdefname)) {
					getVar = new NumberToken(GetInternalVariable(intdefname));

					return true;
				}
			} else {
				if (TryReadVariableName(ref lread, lend, out CharSpan varName)) {
					if (TryGetVariable(varName, out var theVar)) {
						getVar = theVar.VarType switch {
							VariableType.Value => new ValueVariableToken(theVar),
							VariableType.String => new StringVariableToken(theVar, _currentEncoder),
							_ => new NullVariableToken(theVar.Name)
						};
					} else {
						getVar = new NullVariableToken(new string(varName));
					}

					return true;
				}
			}
		}

		getVar = null;
		return false;
	}

	private ExpressionSymbol MakeSymbolToken(CharSpan symbolName, SymbolContext context, ref char* lread, char* lend) {
		if (*lread is SymbolContextAccess) {
			context = ReadSymbolContext(ref lread, lend);
		}

		Symbol gottem = RequestSymbol(symbolName);

		return new ExpressionSymbol(gottem, context);
	}

	private ExpressionSymbol MakeSymbolToken(string symbolName, SymbolContext context, ref char* lread, char* lend) {
		if (*lread is SymbolContextAccess) {
			context = ReadSymbolContext(ref lread, lend);
		}

		Symbol gottem = RequestSymbol(symbolName);

		return new ExpressionSymbol(gottem, context);
	}

	private SymbolContext ReadSymbolContext(ref char* lread, char* lend) {
		lread++;

		CharSpan contextname = FastRead.ReadAlphaNumericSpan(ref lread, lend);

		SymbolContext context = SymbolContexts.GetContext(contextname);

		if (context is SymbolContext.Invalid) {
			Error($"Empty or invalid symbol context: {contextname}");
			return SymbolContext.Default;
		} else {
			return context;
		}
	}

	private ExpressionItem CreateExpressionTree(List<IToken> tokens) {
		int index = 0;
		int max = tokens.Count - 1;

		bool error = false;

		ExpressionItem tree = Expression();

		if (!error) {
			return tree;
		}

		return MathHelpers.BadExpressionNode;

		/////////////////////////////

		ExpressionItem ErrorOut() {
			index = max;
			error = true;
			return MathHelpers.BadExpressionNode;
		}

		ExpressionItem Primary() {
			if (index >= max) return ErrorOut();

			IToken op = tokens[index];

			if (op.Type == TokenType.LeftParen) {
				index++;

				ExpressionItem item = Expression();

				if (tokens[index].Type is not TokenType.RightParen) {
					Error("Missing right parenthesis");
					return ErrorOut();
				}

				index++;
				return item;
			}

			if (op.Rule is TokenRule.Primary) {
				index++;
				return op.CreateExpressionItem();
			}

			Error("Unexpected token");
			return ErrorOut();
		}

		ExpressionItem Call() {
			if (index >= max) return ErrorOut();

			IToken op;

			if ((op = tokens[index]).Rule is TokenRule.FunctionCall && op is FunctionToken funcop) {
				List<ExpressionItem> args = [];

				if (++index >= max) {
					Error("Invalid function header");
					return ErrorOut();
				}

				if (tokens[index].Type is not TokenType.RightParen) {
					while (index < max) {
						args.Add(Expression());

						if (index < max) {
							op = tokens[index];

							if (op.Type is TokenType.RightParen) {
								break;
							} else if (op.Type is TokenType.Comma) {
								index++;
								continue;
							}
						}

						Error("Invalid function header");
						return ErrorOut();
					}
				}

				index++;

				string funcName = funcop.Name;

				if (FunctionCall.BuiltInFunctions.TryGetValue(funcName, out var funcMaker)) {
					return funcMaker(this, args) ?? ErrorOut();
				} else if (_functions.TryGetValue(funcName, out var thefunc)) {
					return thefunc.GetNewInstance(this, args) ?? ErrorOut();
				} else {
					Error($"Function {funcName} not found");
					return ErrorOut();
				}
			}

			return Primary();
		}

		ExpressionItem Postfix() {
			ExpressionItem left = Call();

			if (index >= max) return left;

			IToken op;

			while ((op = tokens[index]).Rule is TokenRule.Postfix) {
				index++;
				left = op.CreateExpressionItem(left);
			}

			return left;
		}

		ExpressionItem Unary() {
			if (index >= max) return ErrorOut();

			IToken op;

			if ((op = tokens[index]).Rule is TokenRule.Unary) {
				index++;
				return op.CreateExpressionItem(Unary());
			}

			return Postfix();
		}

		ExpressionItem Multiplicative() {
			ExpressionItem left = Unary();

			if (index >= max) return left;

			IToken op;

			while ((op = tokens[index]).Rule is TokenRule.Multiplicative) {
				index++;
				left = op.CreateExpressionItem(left, Unary());
			}

			return left;
		}

		ExpressionItem Additive() {
			ExpressionItem left = Multiplicative();

			if (index >= max) return left;

			IToken op;

			while ((op = tokens[index]).Rule is TokenRule.Additive) {
				index++;
				left = op.CreateExpressionItem(left, Multiplicative());
			}

			return left;
		}

		ExpressionItem Shifts() {
			ExpressionItem left = Additive();

			if (index >= max) return left;

			IToken op;

			while ((op = tokens[index]).Rule is TokenRule.Shifts) {
				index++;
				left = op.CreateExpressionItem(left, Additive());
			}

			return left;
		}

		ExpressionItem BitwiseAnd() {
			ExpressionItem left = Shifts();

			if (index >= max) return left;

			IToken op;

			while ((op = tokens[index]).Rule is TokenRule.BitwiseAnd) {
				index++;
				left = op.CreateExpressionItem(left, Shifts());
			}

			return left;
		}

		ExpressionItem BitwiseEor() {
			ExpressionItem left = BitwiseAnd();

			if (index >= max) return left;

			IToken op;

			while ((op = tokens[index]).Rule is TokenRule.BitwiseEor) {
				index++;
				left = op.CreateExpressionItem(left, BitwiseAnd());
			}

			return left;
		}

		ExpressionItem BitwiseOr() {
			ExpressionItem left = BitwiseEor();

			if (index >= max) return left;

			IToken op;

			while ((op = tokens[index]).Rule is TokenRule.BitwiseOr) {
				index++;
				left = op.CreateExpressionItem(left, BitwiseEor());
			}

			return left;
		}

		ExpressionItem LogicalAnd() {
			ExpressionItem left = BitwiseOr();

			if (index >= max) return left;

			IToken op;

			while ((op = tokens[index]).Rule is TokenRule.LogicalAnd) {
				index++;
				left = op.CreateExpressionItem(left, BitwiseOr());
			}

			return left;
		}

		ExpressionItem LogicalOr() {
			ExpressionItem left = LogicalAnd();

			if (index >= max) return left;

			IToken op;

			while ((op = tokens[index]).Rule is TokenRule.LogicalOr) {
				index++;
				left = op.CreateExpressionItem(left, LogicalAnd());
			}

			return left;
		}

		ExpressionItem Comparison() {
			ExpressionItem left = LogicalOr();

			if (index >= max) return left;

			IToken op;

			while ((op = tokens[index]).Rule is TokenRule.Comparison) {
				index++;
				left = op.CreateExpressionItem(left, LogicalOr());
			}

			return left;
		}

		ExpressionItem Equality() {
			ExpressionItem left = Comparison();

			if (index >= max) return left;

			IToken op;

			while ((op = tokens[index]).Rule is TokenRule.Equality) {
				index++;
				left = op.CreateExpressionItem(left, Comparison());
			}

			return left;
		}

		ExpressionItem NullCoalesce() {
			ExpressionItem left = Equality();

			if (index >= max) return left;

			IToken op;

			while ((op = tokens[index]).Rule is TokenRule.NullCoalesce) {
				index++;
				left = op.CreateExpressionItem(left, Equality());
			}

			return left;
		}

		// Inline this because it will always just be a call to the lowest precedence operator
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		ExpressionItem Expression() {
			return NullCoalesce();
		}

	}
}
