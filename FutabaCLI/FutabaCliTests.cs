using System.Text;

namespace FutabaCLI;

#if UNITTESTS
#pragma warning disable IDE0079
#pragma warning disable CS1591 // debug only class, not part of the public API
internal static class FutabaCliTests {
	public static void RunAll() {
		// Regression test for a bug where ParseManifest() compared a decoded char
		// count against the file's raw byte length. A UTF-8 BOM (3 bytes, 0
		// decoded chars) is enough to make those diverge, wrongly rejecting an
		// otherwise well-formed manifest with "Problem parsing manifest file."
		// The no-BOM case is a baseline: it must pass both before and after the
		// fix, proving the manifest content itself -- not the fix -- is what's
		// valid here.
		PostResult("ManifestContainer.ParseManifest: no BOM (baseline)", TestManifest(withBom: false));
		PostResult("ManifestContainer.ParseManifest: UTF-8 BOM", TestManifest(withBom: true));
	}

	static bool TestManifest(bool withBom) {
		string tempDir = Path.Combine(Path.GetTempPath(), $"futaba-tests-{Guid.NewGuid()}");
		Directory.CreateDirectory(tempDir);

		try {
			string manifestBody = "[FUTABA:assemble]\n\nassembly:\n\tentry = test.asm\n";
			string path = Path.Combine(tempDir, "test.futaba");
			File.WriteAllText(path, manifestBody, new UTF8Encoding(encoderShouldEmitUTF8Identifier: withBom));

			using ManifestContainer manifest = new(new FileInfo(path));
			manifest.ParseManifest();

			return manifest.ManifestGood;
		} finally {
			Directory.Delete(tempDir, recursive: true);
		}
	}

	static void PostResult(string test, bool passed) {
		Console.ForegroundColor = passed ? ConsoleColor.DarkGreen : ConsoleColor.Red;
		Console.Write(passed ? "PASS  " : "FAIL  ");
		Console.ResetColor();
		Console.WriteLine(test);
	}
}
#endif
