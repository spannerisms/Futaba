namespace Futaba;

internal unsafe abstract class SourceObject : IDisposable {
	protected const int BufferPadding = 32;

	public abstract FileInfo? FileInfo { get; }
	public abstract string ObjectName { get; }
	protected char* AllocBlock { get; set; }
	public char* SourceStart { get; protected set; }
	public char* SourceEnd { get; protected set; }

	internal abstract bool IsMacro { get; }
	protected bool _disposed = false;

	~SourceObject() {
		Dispose(false);
	}

	protected void ApplyPadding() {
		// add some padding to obviate bounds checking
		new Span<char>(SourceEnd, BufferPadding ).Fill(BadChar);
		new Span<char>(AllocBlock, BufferPadding).Fill(BadChar);
	}

	protected static char* Allocate(int length) {
		return NativeMemory.AllocNice<char>(length + BufferPadding * sizeof(char));
	}

	/// <inheritdoc cref="IDisposable.Dispose()"/>
	public virtual void Dispose() {
		Dispose(true);
		GC.SuppressFinalize(this);
	}

	private void Dispose(bool disposing) {
		if (_disposed) return;

		if (disposing) {
			DisposeResources();
		}

		_disposed = true;

		NativeMemory.AlignedFree(AllocBlock);
	}

	protected abstract void DisposeResources();
}
