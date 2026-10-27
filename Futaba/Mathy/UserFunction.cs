namespace Futaba;

internal sealed class UserFunction(string name, ExpressionItem tree, NamedParameter[] args) {
	public string Name => name;
	public int Arity { get; } = args.Length;

	private readonly ExpressionItem _tree = tree;
	private readonly NamedParameter[] _args = args;

	public ExpressionItem? GetNewInstance(Assembler assembler, List<ExpressionItem> arguments) {
		if (arguments.Count != Arity) {
			assembler.Error($"Wrong number of arguments to {Name} call");
			return null;
		}

		for (int i = 0; i < Arity; i++) {
			_args[i].CurrentValue = arguments[i];
		}

		return _tree.Clone();
	}
}
