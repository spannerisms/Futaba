namespace Futaba;

internal abstract class SnesEncoder {
	internal abstract string Name { get; }

	internal Func<char, uint> EncoderFunction { get; }

	protected SnesEncoder() {
		EncoderFunction = Encode;
	}

	internal abstract uint Encode(char c);

	internal static readonly SnesEncoder ASCII = new AsciiSnesEncoder();

	internal static readonly SnesEncoder Unicode = new UnicodeSnesEncoder();

	internal sealed class UserDefined(string name, Dictionary<char, uint> lookup) : SnesEncoder {
		internal override string Name => name;

		internal override uint Encode(char c) {
			return lookup.GetValueOrDefault(c, 0U);
		}
	}

	private sealed class AsciiSnesEncoder : SnesEncoder {
		internal override string Name => "ascii";

		internal AsciiSnesEncoder() { }
		internal override uint Encode(char c) {
			return c is > '\u001F' and < '\u007F' ? c : 0x20u;
		}
	}

	private sealed class UnicodeSnesEncoder : SnesEncoder {
		internal override string Name => "unicode";

		internal UnicodeSnesEncoder() { }

		internal override uint Encode(char c) {
			return c;
		}
	}

}
