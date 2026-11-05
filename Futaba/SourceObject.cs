namespace Futaba;

internal enum SourceObjectType {
	Empty = 0,
	SourceFile,

	TrackedType,
	Macro,
}

internal unsafe abstract class SourceObject : IDisposable {
	protected const int BufferPadding = 32;
	public abstract string ObjectName { get; }
	protected char* AllocBlock { get; set; }
	public char* SourceStart { get; protected set; }
	public char* SourceEnd { get; protected set; }
	public abstract string? DirectoryName { get; }
	internal abstract SourceObjectType SourceType { get; }
	protected bool _disposed = false;

	~SourceObject() {
		Dispose(false);
	}

	protected void ApplyPadding() {
		// add some padding to obviate bounds checking
		new Span<char>(SourceEnd, BufferPadding).Fill(BadChar);
		new Span<char>(AllocBlock, BufferPadding).Fill(BadChar);
	}

	protected static char* Allocate(int length) {
		return NativeMemory.AllocNice<char>(length + BufferPadding * 2);
	}

	/// <inheritdoc/>
	public virtual void Dispose() {
		Dispose(true);
		GC.SuppressFinalize(this);
	}

	protected virtual void Dispose(bool disposing) {
		if (_disposed) return;

		if (disposing) {
			DisposeResources();
		}

		_disposed = true;

		NativeMemory.AlignedFree(AllocBlock);
	}

	protected abstract void DisposeResources();
}
