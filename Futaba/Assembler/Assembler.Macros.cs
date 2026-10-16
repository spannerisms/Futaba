namespace Futaba;

unsafe partial class Assembler {
	private bool TryReadMacroHeader(out CharSpan macroName, [NotNullWhen(true)] out List<string>? args) {
		if (!DemandSpaceWithError()) {
			args = null;
			macroName = [];
			return false;
		}

		if (!FastRead.TryReadIdentifier(ref _reading, _fileEnd, out macroName)) {
			Error(MsgInfo.InvalidMacroName);
			AbortCommand();
			args = null;
			return false;
		}

		FastRead.SkipInlineWhitespace(ref _reading, _fileEnd);

		if (*_reading is not '(') {
			Error("Missing opening parenthesis");
			AbortCommand();
			args = null;
			return false;
		}

		_reading++;
		args = [];

		FastRead.SkipInlineWhitespace(ref _reading, _fileEnd);

		if (*_reading is ')') {
			_reading++;
			return true;
		}

		while (true) {
			FastRead.SkipInlineWhitespace(ref _reading, _fileEnd);

			if (AtEndOfCommand()) {
				Error("Unclosed macro header");
				return false;
			}


			if (!FastRead.TryReadIdentifier(ref _reading, _fileEnd, out OperandString nextArg)) {
				Error("Missing or invalid macro parameter name");
				return false;
			}

			FastRead.SkipInlineWhitespace(ref _reading, _fileEnd);

			args.Add(nextArg);

			if (*_reading is ',') {
				_reading++;
				continue;
			}

			if (*_reading is ')') {
				_reading++;
				break;
			}

			UnexpectedCharacter();
			return false;
		}


		return true;
	}


	private void Directive_FUNCTION() {
		if (!TryReadMacroHeader(out var macroName, out var args)) {
			return;
		}

		if (AdvanceOverWhitespace()
			|| ((_reading + 2) >= _fileEnd)
			|| !AsciiHelpers.FastTest(_reading, '=', '>')) {
			Error("Missing => operator for function declaration");
			return;
		}

		_reading += 2;

		if (!RestOfCommand(out char* funcStart, out char* funcEnd)) {
			Error("Empty function declaration");
			return;
		}

		string funcName = new(macroName);

		if (FunctionCall.BuiltInFunctions.ContainsKey(funcName)) {
			Error("An internal function with this name already exists");
			AbortCommand();
			return;
		}

		ref var getFunc = ref _functions.GetRefOrDefault(funcName);


		if (getFunc is not null) {
			Error("A user function with this name already exists");
			AbortCommand();
			return;
		}

		List<IToken> tokens = new((int) (funcEnd - funcStart));

		bool goodTree = ContinueExpressionScan(tokens, SymbolicToken.Start, funcStart, funcEnd, SymbolContext.Default, true);

		ExpressionItem funcTree = CreateExpressionTree(tokens);

		NamedParameter[] funcArgsReal = new NamedParameter[args.Count];

		if (!goodTree || funcTree == MathHelpers.BadExpressionNode) {
			funcArgsReal.AsSpan().Fill(new(string.Empty));
			getFunc = new(funcName, NumberToken.Zero, funcArgsReal);
			return;
		}

		// find named parameters
		// TOOD maybe manually write this for performance?
		var funcArgs = (from vam in tokens
						let nam = vam as NamedParameter
						where nam is not null
						select nam).Distinct();

		foreach (var argN in funcArgs) {
			int argI = args.IndexOf(argN.Name);

			if ((uint) argI < funcArgsReal.Length) {
				funcArgsReal[argI] = argN;
			} else {
				Error("Parameter in function call not declared in header", $"'{argN.Name}'");
			}
		}

		// look for unused arguments and give them a default
		for (int i = 0; i < funcArgsReal.Length; i++) {
			ref var argN = ref funcArgsReal[i];

			if (argN is null) {
				string uname = args[i];
				argN = new(uname);
				Warning("Unused parameter in function call", $"'{uname}'");
			}
		}

		getFunc = new(funcName, funcTree, funcArgsReal);
	}

	private void Directive_MACRO() {
		if (TryReadMacroHeader(out var macroName, out var args)) {
			ref var getMacro = ref _macros.GetRefOrDefault(macroName);

			if (!AdvanceOverWhitespace()) {
				Error("Extraneous content after macro header");
				AbortCommand();
			}
			
			if (CaptureBlock(MACRO, ENDMACRO, MACRO) is StringBuilder macroBody) {
				if (getMacro is not null) {
					Error("A macro with this name already exists");
				} else {
					getMacro = new(new string(macroName), args, macroBody, _macros.Count, CurrentSourceLine);
				}
			}
		}
	}

	private void InvokeMacro() {
		_reading++;

		if (!FastRead.TryReadIdentifier(ref _reading, _fileEnd, out OperandString macroname)) {
			Error(MsgInfo.InvalidMacroName);
		} else if (_macros.TryGetValue(macroname, out var macrocalled)) {
			if (ReadOutCommaItemsWithParen(out var macroargs)) {
				if (macrocalled.TryGetInvoker(CurrentSourceLine, macroargs, out var macrovoke, out var errors)) {
					FinishCommand();
					TransferSourceControl(macrovoke);
					return;
				}

				if (errors is null) {
					return;
				}

				Error(errors);
			}
		} else {
			Error($"Cannot find macro: {macroname}");
		}

		AbortCommand();
	}
}
