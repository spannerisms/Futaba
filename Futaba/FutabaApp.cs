global using System.Buffers;
global using System.Buffers.Binary;
global using System.Diagnostics;
global using System.Diagnostics.CodeAnalysis;
global using System.Runtime.CompilerServices;
global using System.Runtime.InteropServices;
global using System.Text;
global using Futaba.Snes;
global using Futaba.Symbols;
global using static Futaba.Directives;
// We use these a lot and I got tired of typing it out
global using CharSpan = System.ReadOnlySpan<char>;

[assembly: CLSCompliant(true)]

namespace Futaba;

internal static class FutabaApp {
	/// <summary>
	/// Futaba Assembler library version.
	/// </summary>
	public static readonly Version Version;

	internal static readonly decimal VersionDecimal;

	static FutabaApp() {
		Version = System.Reflection.Assembly.GetAssembly(typeof(FutabaApp))?.GetName().Version ?? new(0, 0);

		unsafe {
			ulong vm = (uint) Version.Major;
			vm *= 10_000;
			vm += (uint) Version.Minor;
			vm *= 10_000;
			vm += (uint) Version.Build;

			ulong* d = stackalloc ulong[2] { 0, vm };

			*(uint*) d = 0x0004_0000; // scale set to /10E4

			// read as a decimal
			VersionDecimal = *(decimal*) d;
		}
	}
}
