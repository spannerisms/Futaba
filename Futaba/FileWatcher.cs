namespace Futaba;

// TODO need test cases for when someone (for some reason) uses both incbin and incsrc on the same file
internal sealed class FileWatcher : IDisposable {
	private readonly Dictionary<string, IWatchedFile?> _files = [];
	public string DirectoryPath { get; }
	private readonly FileSystemWatcher _watcher;
	private bool _disposed;
	public bool IsEmpty => _files.Count is 0;

	public bool Enabled {
		get => _watcher.EnableRaisingEvents;
		set => _watcher.EnableRaisingEvents = value;
	}

	public FileWatcher(string directory) {
		DirectoryPath = directory;

		_watcher = new(directory, "*.*") {
			IncludeSubdirectories = false,
			EnableRaisingEvents = true,
			NotifyFilter = NotifyFilters.FileName | NotifyFilters.LastWrite | NotifyFilters.CreationTime | NotifyFilters.Size,
		};

		_watcher.Changed += OnAnyChange;
		_watcher.Deleted += OnAnyChange;
		_watcher.Created += OnAnyChange;
		_watcher.Renamed += OnRename;
	}

	private FileSystemEventHandler? _filesChanged;

	public event FileSystemEventHandler? SourceChange {
		add => _filesChanged += value;
		remove => _filesChanged -= value;
	}

	private void Strobe(string path, FileSystemEventArgs e) {
		if (_files.TryGetValue(path, out var file)) {
			file?.Change = e.ChangeType;
			_filesChanged?.Invoke(this, e);
		}
	}

	private void OnRename(object sender, RenamedEventArgs e) {
		if (_disposed) {
			return;
		}

		Strobe(e.OldFullPath, e);
	}

	private void OnAnyChange(object sender, FileSystemEventArgs e) {
		if (_disposed) {
			return;
		}

		Strobe(e.FullPath, e);
	}

	public void WatchFile(IWatchedFile file) {
		if (_disposed) {
			return;
		}

		ref var add = ref CollectionsMarshal.GetValueRefOrAddDefault(_files, file.FileName, out _);
		add ??= file;
	}

	public void WatchFile(string name) {
		if (_disposed) {
			return;
		}

		ref var add = ref CollectionsMarshal.GetValueRefOrAddDefault(_files, name, out _);
	}

	private void Dispose(bool disposing) {
		if (!_disposed) {
			if (disposing) {
				_watcher.EnableRaisingEvents = false;
				_watcher.Dispose();
			}

			_files?.Clear();
			_filesChanged = null;

			_disposed = true;
		}
	}

	public void Clear() {
		_files.Clear();
	}

	public void Dispose() {
		Dispose(true);
		GC.SuppressFinalize(this);
	}
}
