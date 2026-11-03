namespace Futaba;

internal static class MsgInfo {
	internal const string UnexKeyword = "Unexpected keyword";
	internal const string BadIdentifier = "Missing or invalid identifier";
	internal const string CommandGarbage = "Extraneous content after expected end of command";

	internal const string BadAddressMarker = "Malformed address marker";

	internal const string InvalidAddressingMode = "Invalid addressing mode";

	internal const string MissingLabelName = "Missing label name";
	internal const string BadLabelName = "Missing or invalid label name";
	internal const string MissingSublabel = "Missing sublabel name";
	internal const string MissingSizeToken = "Missing size token.";
	internal const string MissingAllocator = "Missing allocator token ::";
	internal const string InvalidFileRange = "Invalid file range";

	internal const string InvalidHex = "Invalid hex literal";
	internal const string InvalidDec = "Invalid decimal literal";
	internal const string InvalidBin = "Invalid binary literal";

	internal const string MissingDirectiveArgument = "Missing arguments to directive";
	internal const string UnclosedIf = "Unclosed if statement block";
	internal const string UnclosedRepeat = "Unclosed repeat statement block";
	internal const string UnclosedString = "Unclosed string";

	internal const string PositiveArgument = "Argument value must be > 0.";
	internal const string ValueMustResolveNow = "Value must be resolvable immediately";

	internal const string InvalidFuncArg = "Invalid function argument";
	internal const string InvalidMacroName = "Missing or invalid macro name";

	internal const string NoChipChange = "Cannot change the coprocessor of this type of assemble.";

	internal static string MustBeResolvedNow(string directive) => $"argument to {directive} must be immediately resolvable";
	internal static string VariableNotFound(CharSpan name) => $"Variable not found: {name}";
}
