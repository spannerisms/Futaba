namespace Futaba;

internal abstract class StringExpression : ExpressionItem {
	public override ExpressionState State => _state;

	protected ExpressionState _state = ExpressionState.Unresolved;
	protected string? ValidateString(ExpressionItem testItem) {
		if (testItem is not IStringToken str) {
			_state = ExpressionState.InvalidArgumentToStringFunction;
			return null;
		}

		return str.Contents;
	}

	public sealed class HashString(ExpressionItem item) : StringExpression {
		public override (decimal, ExpressionState) TryEvaluate() {
			var s = ValidateString(item);

			decimal value;

			if (s is not null) {
				_state = ExpressionState.Resolved;
				value = (ulong) CharSpanHelpers.HashString(s!);
			} else {
				value = MathHelpers.NaN;
			}

			return (value, _state);
		}
	}

	public sealed class GetStringLength(ExpressionItem item) : StringExpression {
		public override ExpressionItem Clone() => new GetStringLength(item.Clone());

		public override (decimal, ExpressionState) TryEvaluate() {
			var s = ValidateString(item);

			decimal value;

			if (s is not null) {
				_state = ExpressionState.Resolved;
				value = s.Length;
			} else {
				value = MathHelpers.NaN;
			}

			return (value, _state);
		}
	}

	public sealed class CompareStrings(ExpressionItem left, ExpressionItem right) : StringExpression {
		public override ExpressionItem Clone() => new CompareStrings(left.Clone(), right.Clone());

		public override (decimal, ExpressionState) TryEvaluate() {
			string? a = ValidateString(left);
			string? b = a is not null ? ValidateString(right) : null;

			decimal value;

			if (b is not null) {
				_state = ExpressionState.Resolved;

				value = MathHelpers.GetTruth(a!.Equals(b));
			} else {
				value = MathHelpers.NaN;
			}

			return (value, _state);
		}
	}
}
