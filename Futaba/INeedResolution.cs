namespace Futaba;

// TODO can maybe be made into an abstract class instead
internal interface INeedResolution {
	public IExpressionReturn Item { get; }

	public int Offset { get; }
	public int Size { get; }

	public uint Time { get; }

	public bool TryToResolve();

	public int Value { get; }

	// for warnings when things can't be resolved
	public SourceLine SourceLine { get; }
}
