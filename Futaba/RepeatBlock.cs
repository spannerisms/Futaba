namespace Futaba;

internal unsafe sealed class RepeatBlock : SourceObject {
	private readonly SourceLine _callSite;
	internal override bool IsMacro => false;
	public override string ObjectName { get; }
	public override string? DirectoryName => _callSite.Source.DirectoryName;

	private SourceObject _parent;

	protected override void DisposeResources() { }

	private int _loops;

	public RepeatBlock(SourceObject parent, char* start, char* end, int loops) {
		_parent = parent;
		SourceStart = start;
		SourceEnd = end;
		_loops = loops;
		ObjectName = GetUniqueName();
	}

	protected override void Dispose(bool _) {
		_disposed = true;
		_parent = null!;
	}

	public bool TickLoops() {
		return (--_loops) > 0;
	}

	private static int s_loopId = 0;
	private static string GetUniqueName() {
		return $"<REPEAT>_{s_loopId++:X4}";
	}

}
