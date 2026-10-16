namespace Futaba.Snes;

/// <summary>
/// Represents a target region for ROM sales.
/// </summary>
public sealed class Region {
	/// <summary>
	/// The name of the region represented by the instance.
	/// </summary>
	public string RegionName { get; init; }

	/// <summary>
	/// 1 letter code that identifies the region.
	/// </summary>
	public char Code { get; init; }

	/// <summary>
	/// Byte value written to ROM header.
	/// </summary>
	public byte Value { get; init; }

	/// <summary>
	/// Approximate frame rate of the region in hertz, or -1 if not determinate.
	/// </summary>
	public int FrameRate { get; init; }

	private Region(byte value, char code, string region, int frames) {
		RegionName = region;
		Code = code;
		Value = value;
		FrameRate = frames;
	}

	/// <summary>
	/// Japan
	/// </summary>
	public static readonly Region Japan          = new(0x00, 'J', "Japan", 60);

	/// <summary>
	/// North America (English)
	/// </summary>
	public static readonly Region NorthAmerica   = new(0x01, 'E', "North America (English)", 60);

	/// <summary>
	/// Europe (English)
	/// </summary>
	public static readonly Region Europe         = new(0x02, 'P', "Europe (English)", 50);

	/// <summary>
	/// Scandinavia
	/// </summary>
	public static readonly Region Scandinavia    = new(0x03, 'W', "Scandinavia", 50);

	/// <summary>
	/// Europe (French)
	/// </summary>
	public static readonly Region EuropeFrench   = new(0x06, 'F', "Europe (French)", 50);

	/// <summary>
	/// Dutch
	/// </summary>
	public static readonly Region Dutch          = new(0x07, 'H', "Dutch", 50);

	/// <summary>
	/// Spanish
	/// </summary>
	public static readonly Region Spanish        = new(0x08, 'S', "Spanish", 50);

	/// <summary>
	/// German
	/// </summary>
	public static readonly Region German         = new(0x09, 'D', "German", 50);
	// DICK
	/// <summary>
	/// Italian
	/// </summary>
	public static readonly Region Italian        = new(0x0A, 'I', "Italian", 50);

	/// <summary>
	/// Chinese
	/// </summary>
	public static readonly Region Chinese        = new(0x0B, 'C', "Chinese", 50);

	/// <summary>
	/// Korean
	/// </summary>
	public static readonly Region Korean         = new(0x0D, 'K', "Korean", 60);

	/// <summary>
	/// Common
	/// </summary>
	public static readonly Region Common         = new(0x0E, 'A', "Common", -1);

	/// <summary>
	/// Canada
	/// </summary>
	public static readonly Region Canada         = new(0x0F, 'N', "Canada", 60);

	/// <summary>
	/// Brazil
	/// </summary>
	public static readonly Region Brazil         = new(0x10, 'B', "Brazil", 60);

	/// <summary>
	/// Australia
	/// </summary>
	public static readonly Region Australia      = new(0x11, 'U', "Australia", 50);

	/// <summary>
	/// Other 1
	/// </summary>
	public static readonly Region Other1         = new(0x12, 'X', "Other Variation", -1);

	/// <summary>
	/// Other 2
	/// </summary>
	public static readonly Region Other2         = new(0x13, 'Y', "Other Variation", -1);

	/// <summary>
	/// Other 3
	/// </summary>
	public static readonly Region Other3         = new(0x14, 'Z', "Other Variation", -1);

	/// <summary>
	/// Test a given name or alias for being a defined region.
	/// </summary>
	/// <param name="name">The name or initial of the region</param>
	/// <param name="region">A <see cref="Region"/> object identified by the supplied token</param>
	/// <returns><see langword="true"/> if a valid region was found; otherwise <see langword="false"/></returns>
	public static bool TryGetRegion(string name, [NotNullWhen(true)] out Region? region) {
		name = name.ToLower();

		region = name switch {
			"j" or "japan" or "nippon" or "jp" => Japan,
			"e" or "north america" or "na" or "us" or "usa" or "canada" => NorthAmerica,
			"p" or "europe" or "eu" or "uk" or "united kingdom" or "britain" => Europe,
			"w" or "scandinavia" => Scandinavia,
			"f" or "french" or "france" => EuropeFrench,
			"d" or "holland" or "dutch" => Dutch,
			"s" or "spanish" => Spanish,
			"d" or "german" or "deutch" => German,
			"i" or "italian" => Italian,
			"c" or "zh" or "chinese" => Chinese,
			"k" or "korea" or "korean" => Korean,
			"a" or "common" => Common,
			"n" or "quebec" or "french canada" => Canada,
			"b" or "brazil" or "portuguese" => Brazil,
			"u" or "australia" => Australia,
			_ => null
		};

		return region != null;
	}
}
