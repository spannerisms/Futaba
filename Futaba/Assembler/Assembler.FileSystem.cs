namespace Futaba;

partial class Assembler {
	/// <summary>
	/// Occurs when a change is discovered in any source file.
	/// </summary>
	public event FileSystemEventHandler? SourceChange {
		add => _filesChanged += value;
		remove => _filesChanged -= value;
	}

	[return: NotNullIfNotNull(nameof(directoryName))]
	private FileWatcher? GetWatcher(string? directoryName) {
		if (directoryName is null) {
			return null;
		}

		ref var watcher = ref CollectionsMarshal.GetValueRefOrAddDefault(_watchedFiles, directoryName, out _);

		if (watcher is null) {
			watcher = new(directoryName);
			watcher.SourceChange += RaiseSourceChangeEvent;
		} else {
			watcher.Enabled = true;
		}

		return watcher;
	}

	private void RaiseSourceChangeEvent(object sender, FileSystemEventArgs e) {
		if (_disposed || _processedEvent) {
			return;
		}

		_processedEvent = true;
		_filesChanged?.Invoke(this, e);
	}

	private bool TryGetWatchedFile<T>(Dictionary<string, T> dictionary, string path, [NotNullWhen(true)] out T? file)
		where T : class, IWatchedFile, IWatchedFileFactory<T> {

		if (TryGetFileName(path, out var fullPath)) {
			ref T? fileRef = ref CollectionsMarshal.GetValueRefOrAddDefault(dictionary, fullPath, out _);

			if (fileRef is not null) {
				fileRef.Refresh();

			} else {
				fileRef = T.Create(fullPath);
			}

			if (fileRef.Error is not null) {
				SourceLine eline = _currentSourceObject == SourceFile.Empty
					? default
					: CurrentSourceLine;

				Error("Problem loading file", fileRef.Error, eline);

				// we still want to watch this path for changes
				if (Incremental) {
					GetWatcher(Path.GetDirectoryName(fullPath))?.WatchFile(path);
				}

				file = null;
				return false;
			}

			if (Incremental) {
				GetWatcher(fileRef.DirectoryName)?.WatchFile(fileRef);
			}

			fileRef.WasUsed = true;

			file = fileRef;
			return true;

		} else {
			file = null;
			return false;
		}
	}

	internal bool TryGetSource(string path, [NotNullWhen(true)] out SourceFile? file) {
		return TryGetWatchedFile(_loadedFiles, path, out file);
	}

	internal bool TryGetBinary(string path, [NotNullWhen(true)] out SourceBinary? file) {
		return TryGetWatchedFile(_loadedBinaries, path, out file);
	}

	internal bool TryGetFileName(string path, [NotNullWhen(true)] out string? filePath) {
		if (Path.IsPathFullyQualified(path)) {
			filePath = path;
			return true;

		} else if (_currentSourceObject.FileInfo?.Directory is DirectoryInfo dir) {
			// fully qualify it
			filePath = Path.Join(dir.FullName, path);
			return true;

		} else {
			Error("Unable to find current directory.");
			filePath = null;
			return false;
		}
	}

}
