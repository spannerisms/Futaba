namespace Futaba;

internal interface IFunctionFactory<TSelf> where TSelf : IFunctionFactory<TSelf> {
	public static abstract TSelf? Create(Assembler assembler, List<ExpressionItem> args);

	public static abstract string FunctionName { get; }
	public static abstract int Arity { get; }

	public static bool TestForBadArity(List<ExpressionItem> args, Assembler assembler) {
		int argCount = args.Count;

		if (argCount == TSelf.Arity) {
			return true;

		} else {
			assembler.Error($"Wrong number of arguments to function [{TSelf.FunctionName}]: got {argCount}; expected {TSelf.Arity}.");
			return false;
		}
	}
}
