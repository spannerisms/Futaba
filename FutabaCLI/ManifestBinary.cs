using Microsoft.Win32.SafeHandles;

namespace FutabaCLI;

internal sealed class ManifestBinary(string path, SafeFileHandle? handle) : ManifestFile(path, handle) {
	public byte[]? Data { get; set; }

	public static ManifestBinary TryCreate(string path, out HandleRevalidation result) {
		SafeFileHandle? handle = TryOpenHandle(path, out string? error);

		ManifestBinary ret = new(path, handle) {
			Error = error
		};

		if (error is null && handle is not null) {
			result = ret.Refresh();
		} else {
			result = HandleRevalidation.Bad;
		}

		return ret;
	}

	public override HandleRevalidation Refresh() {
		if (!RevalidateHandle(out var newHandle)) {
			Data = null;
			newHandle?.Dispose();
			Handle?.Dispose();
			Handle = null;

		} else {
			bool reload;

			if (!ReferenceEquals(newHandle, Handle)) {
				reload = true;
				Handle?.Dispose();
				Handle = newHandle;

			} else {
				reload = LastLoaded != File.GetLastWriteTimeUtc(newHandle);
			}

			Change = default;

			if (reload) {
				LastLoaded = File.GetLastWriteTimeUtc(Handle);

				try {
					var fstream = new FileStream(Handle, FileAccess.Read);

					int length = (int) fstream.Length;

					if (Data is null || Data.Length != length) {
						Data = GC.AllocateUninitializedArray<byte>(length);
					}

					fstream.ReadExactly(Data);
					Error = null;
					return HandleRevalidation.Good;

				} catch (Exception e) {
					Error = e.Message;
				}
			} else {
				Error = null;
				return HandleRevalidation.Unchanged;
			}
		}

		Data = null;
		return HandleRevalidation.Bad;
	}

	protected override void Subdispose(bool disposing) {
		Data = null;
	}
}
