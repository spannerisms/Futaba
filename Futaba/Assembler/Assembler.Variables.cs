namespace Futaba;

unsafe partial class Assembler {
	private void DeclareVariable() {
		const string NoCompound = "Cannot perform compound assignment on a variable that does not yet exist.";
		_reading++;

		if (*_reading is InternalVariableToken) {
			Error("Internal variables are read-only");
			AbortCommand();
			return;
		}

		var vnameRead = FastRead.ReadAlphaNumeric(ref _reading, _fileEnd);

		if (vnameRead.Length < 1) {
			Error("Variable missing name.");
			AbortCommand();
			return;
		}

		string varName = vnameRead;

		FastRead.SkipInlineWhitespace(ref _reading, _fileEnd);

		char assignmentType = *_reading;

		ref var theVar = ref _variables.GetRefOrDefault(varName);

		switch (assignmentType) {
			case '=':
				_reading++;
				break;

			case '+':
			case '/':
			case '-':
			case '*':
			case '?':
				if (_reading[1] is '=') {
					_reading += 2;
					break;
				}

				goto default;

			default:
				UnexpectedCharacter();
				AbortCommand();
				return;
		}

		FastRead.SkipInlineWhitespace(ref _reading, _fileEnd);

		if (!ReadRestOfCommand(out OperandString theRest)) {
			Error("Missing operand for assignment");
			return;
		}

		if (theVar is not null && theVar.VarType is VariableType.String && assignmentType is '+') {
			theVar.Append(theRest);
		} else if (theRest is ['"', .. OperandString operand, '"']) {
			string opcon = ExplodeStringContents(operand);

			switch (assignmentType) {
				case '=':
					if (theVar is null) {
						theVar = new(varName, opcon);
					} else {
						theVar.Contents = opcon;
					}
					break;

				case '+' when theVar is null:
					Error(NoCompound);
					break;

				case '+':
					theVar.Append(opcon);
					break;

				case '?':
					theVar ??= new(varName, opcon);
					break;

				default:
					Error("Invalid assignment type");
					break;
			}
		} else {
			IExpressionReturn oper = ParseOperand(theRest);

			if (oper.Resolved) {
				switch (assignmentType) {
					case '=':
						if (theVar is null) {
							theVar = new(varName, oper.Value);
						} else {
							theVar.Value = oper.Value;
						}
						break;

					case '?':
						theVar ??= new(varName, oper.Value);
						break;

					case '+' or '-' or '*' or '/' when theVar is null:
						Error(NoCompound);
						break;

					case '+':
						theVar.Value += oper.Value;
						break;

					case '-':
						theVar.Value -= oper.Value;
						break;

					case '*':
						theVar.Value *= oper.Value;
						break;

					case '/':
						theVar.Value /= oper.Value;
						break;

					default:
						Error("Invalid assignment type.");
						break;
				}
			} else {
				// create if it doesn't exist to help reduce errors
				theVar ??= new(varName, 0M);
				Error_BadResolve(oper, "Unable to resolve value before assignment.");
			}
		}
	}

	private bool TryReadVariableName(ref char* start, char* end, out CharSpan name) {
		if (!FastRead.TryReadIdentifier(ref start, end, out name)) {
			Error("Missing variable name");
			return false;
		} else {
			return true;
		}
	}

	private bool TryGetVariable(CharSpan name, [MaybeNullWhen(false)] out Variable variable) {
		return _variables.TryGetValue(name, out variable);
	}

	/// <summary>
	/// Clears the initial variables dictionary.
	/// </summary>
	/// <inheritdoc cref="ThrowIfAssembling" path="//remarks|//exception"/>
	public void ClearInitialVariables() {
		ThrowIfAssembling();
		_initialVariables.Clear();
	}

	private bool TestAddingVariable(string name) {
		ThrowIfAssembling();

		return !string.IsNullOrWhiteSpace(name) && !name.ContainsAnyExcept(AsciiHelpers.IdentifierSearch);
	}

	/// <summary>
	/// Tries to add <paramref name="variable"/> to the assembler's lookup table.
	/// </summary>
	/// <param name="variable">The variable to add.</param>
	/// <remarks>
	/// Invalid variable names will be rejected.
	/// New variables will override older variables with the same name.
	/// <para><inheritdoc cref="ThrowIfAssembling" path="//remarks"/></para>
	/// </remarks>
	/// <returns><see langword="true"/> if the variable was successfully added.</returns>
	/// <inheritdoc cref="ThrowIfAssembling" path="//exception"/>
	public bool TryAddVariable(Variable variable) {
		if (TestAddingVariable(variable.Name)) {
			_initialVariables[variable.Name] = variable;
			return true;
		} else {
			return false;
		}
	}

	/// <summary>
	/// Tries to create new variable with the specified value
	/// and add it to the assembler's lookup table.
	/// </summary>
	/// <param name="name">The name of the variable to add.</param>
	/// <param name="value">The value of the variable.</param>
	/// <inheritdoc cref="TryAddVariable(Variable)" path="//returns|//remarks|//exception"/>
	public bool TryAddVariable(string name, string value) {
		if (TestAddingVariable(name)) {
			_initialVariables[name] = new(name, value);
			return true;
		} else {
			return false;
		}
	}

	/// <inheritdoc cref="TryAddVariable(string, string)"/>
	public bool TryAddVariable(string name, int value) {
		if (TestAddingVariable(name)) {
			_initialVariables[name] = new(name, value);
			return true;
		} else {
			return false;
		}
	}

	/// <inheritdoc cref="TryAddVariable(string, string)"/>
	public bool TryAddVariable(string name, decimal value) {
		if (TestAddingVariable(name)) {
			_initialVariables[name] = new(name, value);
			return true;
		} else {
			return false;
		}
	}

	/// <summary>
	/// Adds a collection of variables to the assembler's lookup table.
	/// </summary>
	/// <param name="variables">The collection of variables to add.</param>
	/// <remarks>
	/// New variables will override older variables with the same name.
	/// <para><inheritdoc cref="ThrowIfAssembling" path="//remarks"/></para>
	/// </remarks>
	/// <inheritdoc cref="ThrowIfAssembling" path="//exception"/>
	public void AddVariables(Variable[] variables) {
		ThrowIfAssembling();

		foreach (Variable toAdd in variables) {
			_initialVariables[toAdd.Name] = toAdd;
		}
	}

	/// <inheritdoc cref="AddVariables(Variable[])"/>
	public void AddVariables(IEnumerable<Variable> variables) {
		ThrowIfAssembling();

		foreach (Variable toAdd in variables) {
			_initialVariables[toAdd.Name] = toAdd;
		}
	}

	/// <returns><c>0</c> if invalid</returns>
	internal decimal GetInternalVariable(CharSpan name) {
		if (name.Length < 10) {
			Span<char> tspan = stackalloc char[name.Length];
			name.FastLower(tspan);

			switch (tspan) {
				case "pc": /*            */ return _pc;
				case "site": /*          */ return Provenance;
				case "here": /*          */ return Provenance;
				case "offset": /*        */ return _offset;
				case "rand": /*          */ return _random.NextUInt64();
				case "pi": /*            */ return 3.14159265358979323846264M;
				case "e": /*             */ return 2.71828182845904523536028M;
				case "rad": /*           */ return 0.01745329251994329576923M;
				case "name": /*          */ return 0x03FF;
				case "line": /*          */ return _sourceLine;
				case "version": /*       */ return FutabaApp.VersionDecimal;
			}
		}

		Error("Invalid internal variable name");

		return MathHelpers.NaN;
	}

	private string ExplodeStringContents(char* strStart, char* strEnd) {
		return ExplodeStringContents(CharSpanHelpers.CreateSpan(strStart, strEnd));
	}

	private char GetEscapedCharacter(char c) {
		switch (c) {
			case 'n':
				return '\n';

			case 't':
				return '\t';

			case '"':
			case '{':
			case '}':
			case '!':
			case CharDelim:
			case EscapeChar:
				return c;

			default:
				Warning($"Unrecognized escape sequence: \\{c}");
				return c;
		}
	}

	private string ExplodeStringContents(CharSpan str) {
		StringBuilder ret = new();

		char c;

		int sublen = str.Length;

		for (int i = 0; i < sublen; i++) {
			c = str[i];

			if (c is EscapeChar) {
				if (++i < sublen) {
					ret.Append(GetEscapedCharacter(str[i]));
				}
			} else if (c is '!') {
				if (++i == sublen) {
					ret.Append('!');
				} else {
					char dd = str[i];
					if (dd.IsIdentifierCharacter) {
						CharSpan varname = ReadIdentifier(str);

						if (TryGetVariable(varname, out var def)) {
							switch (def.VarType) {
								case VariableType.String:
									ret.Append(def.Contents);
									break;

								case VariableType.Value:
									ret.Append(def.Contents);
									break;
							}
						} else {
							Error($"Variable !{varname} not found.");
						}
						// check for internal variables
					} else if (dd is ':') {
						if (++i == sublen) {
							ret.Append("!:");
						} else {
							dd = str[i];
							if (dd.IsIdentifierCharacter) {
								CharSpan varname = ReadIdentifier(str);
								ret.Append(GetInternalVariable(varname));
							} else {
								ret.Append("!:");
								ret.Append(dd);
							}
						}
					} else {
						ret.Append('!');
						ret.Append(dd);
					}
				}
			} else {
				ret.Append(c);
			}

			///////////////////////////////////////////////////

			CharSpan ReadIdentifier(CharSpan subsub) {
				int nomstart = i;
				int adv = nomstart;

				// first char is definitely an 
				while (++adv < sublen && subsub[adv].IsIdentifierCharacter) ;

				i = adv;
				return subsub[nomstart..adv];
			}
		}

		return ret.ToString();
	}
}
