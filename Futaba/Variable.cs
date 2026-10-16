namespace Futaba;

internal enum VariableType {
	String,
	Value,
}

/// <summary>
/// Represents a dynamically typed variable.
/// </summary>
public sealed class Variable : IFormattable {
	/// <summary>
	/// Gets the underyling .NET type this instance currently represents.
	/// </summary>
	public Type ItemType => VarType switch {
		VariableType.String => typeof(string),
		VariableType.Value => typeof(decimal),
		_ => typeof(object),
	};

	/// <summary>
	/// Gets the underlying value this instance currently holds.
	/// </summary>
	public object Item => VarType switch {
		VariableType.String => _contents,
		VariableType.Value => _value,
		_ => string.Empty,
	};

	/// <summary>
	/// Gets this instance's name.
	/// </summary>
	public string Name { get; }

	internal VariableType VarType {
		get;

		set {
			if (field == value) {
				return;
			}

			switch (field) {
				case VariableType.String:
					_contents = string.Empty;
					break;
			}

			field = value;
		}

	}

	private string _contents = string.Empty;
	internal string Contents {
		get {
			return VarType switch {
				VariableType.String => _contents,
				VariableType.Value => $"{_value}",
				_ => string.Empty,
			};
		}

		set {
			_contents = value;
			VarType = VariableType.String;
		}
	}

	private decimal _value = 0;

	internal decimal Value {
		get {
			if (VarType is VariableType.Value) {
				return _value;
			}

			return MathHelpers.NaN;
		}

		set {
			_value = value;
			VarType = VariableType.Value;
		}
	}

	private Variable(string name) {
		Name = name;
	}

	/// <summary>
	/// Creates a new string variable instance with the given contents.
	/// </summary>
	internal Variable(string name, string contents) : this(name) {
		Contents = contents;
	}

	/// <summary>
	/// Creates a new value variable instance with the given value.
	/// </summary>
	internal Variable(string name, decimal value) : this(name) {
		Value = value;
	}

	internal void Append(CharSpan s) {
		Contents = $"{Contents}{s}";
	}

	internal Variable Clone() {
		return VarType switch {
			VariableType.String => new(Name, Contents),
			VariableType.Value => new(Name, Value),
			_ => new(Name, 0)
		};
	}

	/// <inheritdoc/>
	public override string ToString() {
		return Contents;
	}

	/// <inheritdoc/>
	public string ToString(string? format, IFormatProvider? formatProvider) {
		return VarType switch {
			VariableType.String => Contents,
			VariableType.Value => CharSpanHelpers.ExtendedStringFormat(Value, format),
			_ => $"!{Name}",
		};
	}

	/// <inheritdoc cref="TryCreate(string, decimal, out Variable?, out string?)"/>
	public static bool TryCreate(string name, int value, [NotNullWhen(true)] out Variable? variable, [NotNullWhen(false)] out string? error) {
		return TryCreate(name, value, out variable, out error);
	}

	/// <summary>
	/// Attempts creation of a variable with a valid name and value.
	/// </summary>
	/// <param name="name">The name of the variable to create.</param>
	/// <param name="value">The value to assign the variable.</param>
	/// <param name="variable">If this method returns successfully, this will contain the created variable.</param>
	/// <param name="error">If this method does not return successfull, this will contain an error message describing the failure.</param>
	/// <returns><see langword="true"/> if the variable was successfully created; otherwise, <see langword="false"/>.</returns>
	public static bool TryCreate(string name, decimal value, [NotNullWhen(true)] out Variable? variable, [NotNullWhen(false)] out string? error) {
		if (ValidateName(name, out error)) {
			variable = new(name, value);
			return true;
		} else {
			variable = null;
			return false;
		}
	}

	/// <inheritdoc cref="TryCreate(string, decimal, out Variable?, out string?)"/>
	public static bool TryCreate(string name, string? contents, [NotNullWhen(true)] out Variable? variable, [NotNullWhen(false)] out string? error) {
		if (ValidateName(name, out error)) {
			variable = new(name, contents ?? string.Empty);
			return true;
		} else {
			variable = null;
			return false;
		}
	}

	/// <inheritdoc cref="TryCreate(string, decimal, out Variable?, out string?)"/>
	public static bool TryCreate(string name, CharSpan contents, [NotNullWhen(true)] out Variable? variable, [NotNullWhen(false)] out string? error) {
		if (ValidateName(name, out error)) {
			variable = new(name, new string(contents));
			return true;
		} else {
			variable = null;
			return false;
		}
	}

	private static bool ValidateName(string name, [NotNullWhen(false)] out string? error) {
		if (string.IsNullOrEmpty(name)) {
			error = "Empty variable name.";
			return false;
		}

		CharSpan n = name;

		foreach (char c in n) {
			if (!c.IsIdentifierCharacter) {
				error = "Invalid variable name.";
				return false;
			}
		}

		error = null;
		return true;
	}
}
