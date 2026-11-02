using System.Collections.Frozen;

namespace Futaba;

internal delegate ExpressionItem? FunctionCallGet(Assembler assembler, List<ExpressionItem> args);

internal static class FunctionCall {
	internal static readonly FrozenDictionary<string, FunctionCallGet> BuiltInFunctions = new Dictionary<string, FunctionCallGet>() {
		[Sin.Name] = Sin.Create,
		[Cos.Name] = Cos.Create,
		[Tan.Name] = Tan.Create,
		[Asin.Name] = Asin.Create,
		[Acos.Name] = Acos.Create,
		[Atan.Name] = Atan.Create,

		[Log10.Name] = Log10.Create,
		[Log2.Name] = Log2.Create,
		[LogB.Name] = LogB.Create,

		[Pow.Name] = Pow.Create,
		[Root2.Name] = Root2.Create,
		[Root.Name] = Root.Create,

		[Max.Name] = Max.Create,
		[Min.Name] = Min.Create,

		[Clamp.Name] = Clamp.Create,
		[Round.Name] = Round.Create,
		[Select.Name] = Select.Create,

		[VramAddress.Name] = VramAddress.Create,
		[ObSel.Name] = ObSel.Create,
		[BgNba.Name] = BgNba.Create,
		[BgSc.Name] = BgSc.Create,
		[Rtn.Name] = Rtn.Create,
		[ArgOf.Name] = ArgOf.Create,
		[Rebank.Name] = Rebank.Create,

		[Floor.Name] = Floor.Create,
		[Ceil.Name] = Ceil.Create,
		[IntCast.Name] = IntCast.Create,
		[Abs.Name] = Abs.Create,

		[Bcd.Name] = Bcd.Create,
		[BitN.Name] = BitN.Create,
		[HashString.Name] = HashString.Create,
		[GetStringLength.Name] = GetStringLength.Create,
		[CompareStrings.Name] = CompareStrings.Create,

		[Rand.Name] = Rand.Create,

		["col"] = GetColor,
		["color"] = GetColor,
		[GetFileFunc.Name] = ReadFile
	}.ToFrozenDictionary();

	//////////////////////////////////////////////////////////////////////////////////////////////////////////

	private static GetFileFunc? ReadFile(Assembler assembler, List<ExpressionItem> args) {
		switch (args.Count) {
			case 1: return new GetFileFunc(assembler, args[0], null, null);
			case 2: return new GetFileFunc(assembler, args[0], args[1], null);
			case 3: return new GetFileFunc(assembler, args[0], args[1], args[2]);
		}

		assembler.Error($"Wrong number of arguments to function [{GetFileFunc.Name}]: got {args.Count}, expected 1, 2 or 3.");

		return null;
	}

	private sealed class GetFileFunc(Assembler assembler, ExpressionItem itemName, ExpressionItem? itemOffset, ExpressionItem? itemSize)
			: ExpressionItem {
		public const string Name = "read";
		private const int MaxBytes = 8;

		public override ExpressionItem Clone() => new GetFileFunc(assembler, itemName, itemSize, itemOffset);

		private ExpressionState _state = ExpressionState.Unresolved;
		public override ExpressionState State => _state;

		public override (decimal, ExpressionState) TryEvaluate() {
			int size;

			ExpressionState state;

			if (itemSize is null) {
				size = 1;
				state = ExpressionState.Resolved;
			} else {
				(var dc, state) = itemSize.TryEvaluate();

				if (!state.IsResolved) {
					return (MathHelpers.NaN, _state = state);
				}

				size = dc.AsInt();

				if (size is < 1 or > MaxBytes) {
					return (MathHelpers.NaN, _state = ExpressionState.InvalidArgument);
				}
			}

			int offset;

			if (itemOffset is null) {
				offset = 0;

			} else {
				var (dc, ds) = itemOffset.TryEvaluate();

				if (!ds.IsResolved) {
					return (MathHelpers.NaN, _state = ds);
				}

				offset = dc.AsInt();

				if (offset < 0) {
					return (MathHelpers.NaN, _state = ExpressionState.InvalidArgument);
				}

				// don't overwrite state if itemSize was ExpressionState.ResolvedButForceRequest
				if (state is ExpressionState.Resolved) {
					state = ds;
				}
			}

			if (itemName is not IStringToken st) {
				return (MathHelpers.NaN, _state = ExpressionState.InvalidArgumentToStringFunction);
			}

			Span<byte> rdb = stackalloc byte[MaxBytes];
			rdb.Clear();

			// using try because there are so many potential errors,
			// and this function shouldn't be called that frequently,
			// so the overhead of catch is fine
			if (assembler.TryGetBinary(st.Contents, out var rdFile)) {
				if ((uint) size <= MaxBytes && rdFile.TryGetSpan(offset, size, out var copySpan)) {
					copySpan.CopyTo(rdb);

					ulong ret = BinaryPrimitives.ReadUInt64LittleEndian(rdb);
					return (ret, _state = state);
				}
			}

			return (MathHelpers.NaN, _state = ExpressionState.InvalidArgument);
		}
	}

	private static ExpressionItem? GetColor(Assembler assembler, List<ExpressionItem> args) {
		int argCount = args.Count;

		if (argCount is 1) {
			return new UnaryColor(args[0]);

		} else if (argCount is 3) {
			return new TernaryColor(args[0], args[1], args[2]);

		} else {
			assembler.Error($"Wrong number of arguments to function [color]: got {argCount}, expected 1 or 3.");
			return null;
		}
	}
	private sealed class TernaryColor(ExpressionItem itema, ExpressionItem itemb, ExpressionItem itemc) : TernaryExpression(itema, itemb, itemc) {
		public override ExpressionItem Clone() => new TernaryColor(ItemA.Clone(), ItemB.Clone(), ItemC.Clone());

		protected override decimal Equate(decimal a, decimal b, decimal c) {
			int r = a.AsByte() >> 3;
			int g = (b.AsByte() & 0xF8) << 2;
			int bl = c.AsByte() << 7;

			return r | g | bl;
		}
	}

	private sealed class UnaryColor(ExpressionItem item) : UnaryExpression(item) {
		public override ExpressionItem Clone() => new UnaryColor(Item.Clone());
		protected override decimal Equate(decimal a) {
			int rgb = a.AsInt();

			int r = rgb >> 19;
			int g = (rgb >> 6) & 0x03E0;
			int b = (rgb << 7) & 0x7C00;

			return r | g | b;
		}
	}

	private sealed class Rand(ExpressionItem left, ExpressionItem right, XoRandom random) : BinaryExpression(left, right), IFunctionFactory<Rand> {
		public const string Name = "rand";
		public static string FunctionName => Name;

		public static Rand? Create(Assembler assembler, List<ExpressionItem> args) {
			if (IFunctionFactory<Sin>.TestForBadArity(args, assembler)) {
				return new Rand(args[0], args[1], assembler._random);
			} else {
				return null;
			}
		}

		private readonly XoRandom _random = random;
		public override ExpressionItem Clone() => new Rand(Left.Clone(), Right.Clone(), _random);

		protected override decimal Equate(decimal a, decimal b) {
			if (a > b) {
				(a, b) = (b, a);
			}

			return _random.NextInt64(a.AsLong(), b.AsLong());
		}
	}

	public sealed class Sin(ExpressionItem item) : UnaryExpression(item), IFunctionFactory<Sin> {
		public const string Name = "sin";
		public static string FunctionName => Name;
		public static Sin? Create(Assembler assembler, List<ExpressionItem> args) {
			if (IFunctionFactory<Sin>.TestForBadArity(args, assembler)) {
				return new Sin(args[0]);
			} else {
				return null;
			}
		}

		public override ExpressionItem Clone() => new Sin(Item.Clone());

		protected override decimal Equate(decimal a) {
			return (decimal) Math.Sin((double) a);
		}
	}

	public sealed class Cos(ExpressionItem item) : UnaryExpression(item), IFunctionFactory<Cos> {
		public const string Name = "cos";
		public static string FunctionName => Name;

		public static Cos? Create(Assembler assembler, List<ExpressionItem> args) {
			if (IFunctionFactory<Cos>.TestForBadArity(args, assembler)) {
				return new Cos(args[0]);
			} else {
				return null;
			}
		}

		public override ExpressionItem Clone() => new Cos(Item.Clone());

		protected override decimal Equate(decimal a) {
			return (decimal) Math.Cos((double) a);
		}
	}

	public sealed class Tan(ExpressionItem item) : UnaryExpression(item), IFunctionFactory<Tan> {
		public const string Name = "tan";
		public static string FunctionName => Name;

		public static Tan? Create(Assembler assembler, List<ExpressionItem> args) {
			if (IFunctionFactory<Tan>.TestForBadArity(args, assembler)) {
				return new Tan(args[0]);
			} else {
				return null;
			}
		}

		public override ExpressionItem Clone() => new Tan(Item.Clone());

		protected override decimal Equate(decimal a) {
			return MathHelpers.ClampCastDoubleAsDecimal(Math.Tan((double) a));
		}
	}


	public sealed class Asin(ExpressionItem item) : UnaryExpression(item), IFunctionFactory<Asin> {
		public const string Name = "asin";
		public static string FunctionName => Name;

		public static Asin? Create(Assembler assembler, List<ExpressionItem> args) {
			if (IFunctionFactory<Asin>.TestForBadArity(args, assembler)) {
				return new Asin(args[0]);
			} else {
				return null;
			}
		}

		public override ExpressionItem Clone() => new Asin(Item.Clone());

		protected override decimal Equate(decimal a) {
			return (decimal) Math.Asin((double) a);
		}
	}

	public sealed class Acos(ExpressionItem item) : UnaryExpression(item), IFunctionFactory<Acos> {
		public const string Name = "acos";
		public static string FunctionName => Name;

		public static Acos? Create(Assembler assembler, List<ExpressionItem> args) {
			if (IFunctionFactory<Acos>.TestForBadArity(args, assembler)) {
				return new Acos(args[0]);
			} else {
				return null;
			}
		}

		public override ExpressionItem Clone() => new Acos(Item.Clone());

		protected override decimal Equate(decimal a) {
			return (decimal) Math.Acos((double) a);
		}
	}

	public sealed class Atan(ExpressionItem item) : UnaryExpression(item), IFunctionFactory<Atan> {
		public const string Name = "atan";
		public static string FunctionName => Name;
		public static Atan? Create(Assembler assembler, List<ExpressionItem> args) {
			if (IFunctionFactory<Atan>.TestForBadArity(args, assembler)) {
				return new Atan(args[0]);
			} else {
				return null;
			}
		}

		public override ExpressionItem Clone() => new Atan(Item.Clone());

		protected override decimal Equate(decimal a) {
			return (decimal) Math.Atan((double) a);
		}
	}

	private sealed class ItemExists(ExpressionItem item) : ExpressionItem, IFunctionFactory<ItemExists> {
		public const string Name = "exists";
		public static string FunctionName => Name;
		public static int Arity => 1;
		private ExpressionState _state;
		public override ExpressionState State => _state;

		public static ItemExists? Create(Assembler assembler, List<ExpressionItem> args) {
			if (IFunctionFactory<ItemExists>.TestForBadArity(args, assembler)) {
				return new ItemExists(args[0]);
			} else {
				return null;
			}
		}

		public override ExpressionItem Clone() => new ItemExists(item.Clone());

		public override (decimal, ExpressionState) TryEvaluate() {
			decimal value;

			var (_, state) = item.TryEvaluate();

			switch (state) {
				case ExpressionState.Resolved:
					value = 1;
					break;

				case ExpressionState.ResolvedButForceRequest:
					value = 0;
					break;

				case ExpressionState.MissingSymbol:
					value = 0;
					state = ExpressionState.ResolvedButForceRequest;
					break;

				case ExpressionState.VariableNotFound:
					value = 0;
					state = ExpressionState.Resolved;
					break;

				default:
					value = 0;
					break;
			}

			return (value, _state = state);
		}
	}

	public sealed class Ceil(ExpressionItem item) : UnaryExpression(item), IFunctionFactory<Ceil> {
		public const string Name = "ceil";
		public static string FunctionName => Name;

		public static Ceil? Create(Assembler assembler, List<ExpressionItem> args) {
			if (IFunctionFactory<Ceil>.TestForBadArity(args, assembler)) {
				return new Ceil(args[0]);
			} else {
				return null;
			}
		}

		public override ExpressionItem Clone() => new Ceil(Item.Clone());

		protected override decimal Equate(decimal a) {
			return decimal.Ceiling(a);
		}
	}

	public sealed class Floor(ExpressionItem item) : UnaryExpression(item), IFunctionFactory<Floor> {
		public const string Name = "floor";
		public static string FunctionName => Name;

		public static Floor? Create(Assembler assembler, List<ExpressionItem> args) {
			if (IFunctionFactory<Floor>.TestForBadArity(args, assembler)) {
				return new Floor(args[0]);
			} else {
				return null;
			}
		}

		public override ExpressionItem Clone() => new Floor(Item.Clone());

		protected override decimal Equate(decimal a) {
			return decimal.Floor(a);
		}
	}

	public sealed class IntCast(ExpressionItem item) : UnaryExpression(item), IFunctionFactory<IntCast> {
		public const string Name = "int";
		public static string FunctionName => Name;

		public static IntCast? Create(Assembler assembler, List<ExpressionItem> args) {
			if (IFunctionFactory<IntCast>.TestForBadArity(args, assembler)) {
				return new IntCast(args[0]);
			} else {
				return null;
			}
		}

		public override ExpressionItem Clone() => new IntCast(Item.Clone());

		protected override decimal Equate(decimal a) {
			return decimal.Truncate(a);
		}
	}

	public sealed class VramAddress(ExpressionItem item) : UnaryExpression(item), IFunctionFactory<VramAddress> {
		public const string Name = "vram";
		public static string FunctionName => Name;

		public static VramAddress? Create(Assembler assembler, List<ExpressionItem> args) {
			if (IFunctionFactory<VramAddress>.TestForBadArity(args, assembler)) {
				return new VramAddress(args[0]);
			} else {
				return null;
			}
		}

		public override ExpressionItem Clone() => new VramAddress(Item.Clone());

		protected override decimal Equate(decimal a) {
			ushort ret = (ushort) a.AsInt();
			ret >>= 1;
			return ret;
		}
	}

	public sealed class Rtn(ExpressionItem item) : UnaryExpression(item), IFunctionFactory<Rtn> {
		public const string Name = "rtn";
		public static string FunctionName => Name;

		public static Rtn? Create(Assembler assembler, List<ExpressionItem> args) {
			if (IFunctionFactory<Rtn>.TestForBadArity(args, assembler)) {
				return new Rtn(args[0]);
			} else {
				return null;
			}
		}

		public override ExpressionItem Clone() => new Rtn(Item.Clone());

		protected override decimal Equate(decimal a) {
			return a.AsLong() - 1;
		}
	}

	public sealed class ArgOf(ExpressionItem item) : UnaryExpression(item), IFunctionFactory<ArgOf> {
		public const string Name = "argof";
		public static string FunctionName => Name;

		public static ArgOf? Create(Assembler assembler, List<ExpressionItem> args) {
			if (IFunctionFactory<ArgOf>.TestForBadArity(args, assembler)) {
				return new ArgOf(args[0]);
			} else {
				return null;
			}
		}

		public override ExpressionItem Clone() => new ArgOf(Item.Clone());

		protected override decimal Equate(decimal a) {
			return a.AsLong() + 1;
		}
	}

	public sealed class Abs(ExpressionItem item) : UnaryExpression(item), IFunctionFactory<Abs> {
		public const string Name = "abs";
		public static string FunctionName => Name;

		public static Abs? Create(Assembler assembler, List<ExpressionItem> args) {
			if (IFunctionFactory<Abs>.TestForBadArity(args, assembler)) {
				return new Abs(args[0]);
			} else {
				return null;
			}
		}

		public override ExpressionItem Clone() => new Abs(Item.Clone());

		protected override decimal Equate(decimal a) {
			return decimal.Abs(a);
		}
	}

	public sealed class Bcd(ExpressionItem item) : UnaryExpression(item), IFunctionFactory<Bcd> {
		public const string Name = "bcd";
		public static string FunctionName => Name;

		public static Bcd? Create(Assembler assembler, List<ExpressionItem> args) {
			if (IFunctionFactory<Bcd>.TestForBadArity(args, assembler)) {
				return new Bcd(args[0]);
			} else {
				return null;
			}
		}

		public override ExpressionItem Clone() => new Bcd(Item.Clone());

		protected override decimal Equate(decimal a) {
			ulong ret = 0;

			ulong quotient = (ulong) a.AsLong();

			int shifting = 0;

			while (quotient > 0) {
				(quotient, ulong remainder) = Math.DivRem(quotient, 10);

				ret += remainder << shifting;

				shifting += 4;
			}

			return ret;
		}
	}

	public sealed class BitN(ExpressionItem item) : UnaryExpression(item), IFunctionFactory<BitN> {
		public const string Name = "bitn";
		public static string FunctionName => Name;

		public static BitN? Create(Assembler assembler, List<ExpressionItem> args) {
			if (IFunctionFactory<BitN>.TestForBadArity(args, assembler)) {
				return new BitN(args[0]);
			} else {
				return null;
			}
		}

		public override ExpressionItem Clone() => new BitN(Item.Clone());

		protected override decimal Equate(decimal a) {
			return 1 << a.AsInt();
		}
	}

	private sealed class Pow(ExpressionItem left, ExpressionItem right) : BinaryExpression(left, right), IFunctionFactory<Pow> {
		public const string Name = "pow";
		public static string FunctionName => Name;
		public static Pow? Create(Assembler assembler, List<ExpressionItem> args) {
			if (IFunctionFactory<Pow>.TestForBadArity(args, assembler)) {
				return new Pow(args[0], args[1]);
			} else {
				return null;
			}
		}

		public override ExpressionItem Clone() => new Pow(Left.Clone(), Right.Clone());

		protected override decimal Equate(decimal a, decimal b) {
			return MathHelpers.ClampCastDoubleAsDecimal(Math.Pow((double) a, (double) b));
		}
	}

	private sealed class LogB(ExpressionItem left, ExpressionItem right) : BinaryExpression.SafeBinary(left, right), IFunctionFactory<LogB> {
		public const string Name = "logb";
		public static string FunctionName => Name;

		public static LogB? Create(Assembler assembler, List<ExpressionItem> args) {
			if (IFunctionFactory<LogB>.TestForBadArity(args, assembler)) {
				return new LogB(args[0], args[1]);
			} else {
				return null;
			}
		}

		public override ExpressionItem Clone() => new LogB(Left.Clone(), Right.Clone());
		protected override bool Validate(decimal a, decimal b) {
			if (b > 1M && a > 0M) {
				return true;
			} else {
				_state = ExpressionState.InvalidArgument;
				return false;
			}
		}

		// log_b(a)
		protected override decimal Equate(decimal a, decimal b) {
			return MathHelpers.ClampCastDoubleAsDecimal(Math.Log((double) a, (double) b));
		}
	}

	private sealed class Log2(ExpressionItem item) : UnaryExpression.SafeUnary(item), IFunctionFactory<Log2> {
		public const string Name = "log2";
		public static string FunctionName => Name;
		public static Log2? Create(Assembler assembler, List<ExpressionItem> args) {
			if (IFunctionFactory<Log2>.TestForBadArity(args, assembler)) {
				return new Log2(args[0]);
			} else {
				return null;
			}
		}

		public override ExpressionItem Clone() => new Log2(Item.Clone());
		protected override bool Validate(decimal a) {
			if (a > 0M) {
				return true;
			} else {
				_state = ExpressionState.InvalidArgument;
				return false;
			}
		}

		protected override decimal Equate(decimal a) {
			return MathHelpers.ClampCastDoubleAsDecimal(Math.Log2((double) a));
		}
	}

	private sealed class Log10(ExpressionItem item) : UnaryExpression.SafeUnary(item), IFunctionFactory<Log10> {
		public const string Name = "log";
		public static string FunctionName => Name;
		public static Log10? Create(Assembler assembler, List<ExpressionItem> args) {
			if (IFunctionFactory<Log10>.TestForBadArity(args, assembler)) {
				return new Log10(args[0]);
			} else {
				return null;
			}
		}

		public override ExpressionItem Clone() => new Log10(Item.Clone());
		protected override bool Validate(decimal a) {
			if (a > 0M) {
				return true;
			} else {
				_state = ExpressionState.InvalidArgument;
				return false;
			}
		}

		protected override decimal Equate(decimal a) {
			return MathHelpers.ClampCastDoubleAsDecimal(Math.Log10((double) a));
		}
	}

	private sealed class Root(ExpressionItem left, ExpressionItem right) : BinaryExpression.SafeBinary(left, right), IFunctionFactory<Root> {
		public const string Name = "root";
		public static string FunctionName => Name;
		public static Root? Create(Assembler assembler, List<ExpressionItem> args) {
			if (IFunctionFactory<Root>.TestForBadArity(args, assembler)) {
				return new Root(args[0], args[1]);
			} else {
				return null;
			}
		}

		public override ExpressionItem Clone() => new Root(Left.Clone(), Right.Clone());

		protected override bool Validate(decimal a, decimal b) {
			if ((double) b != 0.0D && a >= 0M) {
				return true;
			} else {
				_state = ExpressionState.InvalidArgument;
				return false;
			}
		}

		protected override decimal Equate(decimal a, decimal b) {
			return MathHelpers.ClampCastDoubleAsDecimal(Math.Pow((double) a, (double) (1 / b)));
		}
	}

	private sealed class Root2(ExpressionItem item) : UnaryExpression.SafeUnary(item), IFunctionFactory<Root2> {
		public const string Name = "sqrt";
		public static string FunctionName => Name;

		public static Root2? Create(Assembler assembler, List<ExpressionItem> args) {
			if (IFunctionFactory<Root2>.TestForBadArity(args, assembler)) {
				return new Root2(args[0]);
			} else {
				return null;
			}
		}

		public override ExpressionItem Clone() => new Root2(Item.Clone());

		protected override bool Validate(decimal a) {
			if (a >= 0M) {
				return true;
			} else {
				_state = ExpressionState.InvalidArgument;
				return false;
			}
		}

		protected override decimal Equate(decimal a) {
			return MathHelpers.ClampCastDoubleAsDecimal(Math.Sqrt((double) a));
		}
	}

	private sealed class Max(ExpressionItem left, ExpressionItem right) : BinaryExpression(left, right), IFunctionFactory<Max> {
		public const string Name = "max";
		public static string FunctionName => Name;

		public static Max? Create(Assembler assembler, List<ExpressionItem> args) {
			if (IFunctionFactory<Max>.TestForBadArity(args, assembler)) {
				return new Max(args[0], args[1]);
			} else {
				return null;
			}
		}

		public override ExpressionItem Clone() => new Max(Left.Clone(), Right.Clone());

		protected override decimal Equate(decimal a, decimal b) {
			return a >= b ? a : b;
		}
	}

	private sealed class Min(ExpressionItem left, ExpressionItem right) : BinaryExpression(left, right), IFunctionFactory<Min> {
		public const string Name = "min";
		public static string FunctionName => Name;

		public static Min? Create(Assembler assembler, List<ExpressionItem> args) {
			if (IFunctionFactory<Min>.TestForBadArity(args, assembler)) {
				return new Min(args[0], args[1]);
			} else {
				return null;
			}
		}

		public override ExpressionItem Clone() => new Min(Left.Clone(), Right.Clone());

		protected override decimal Equate(decimal a, decimal b) {
			return a <= b ? a : b;
		}
	}

	private sealed class Round(ExpressionItem left, ExpressionItem right) : BinaryExpression(left, right), IFunctionFactory<Round> {
		public const string Name = "round";
		public static string FunctionName => Name;

		public static Round? Create(Assembler assembler, List<ExpressionItem> args) {
			if (IFunctionFactory<Round>.TestForBadArity(args, assembler)) {
				return new Round(args[0], args[1]);
			} else {
				return null;
			}
		}

		public override ExpressionItem Clone() => new Round(Left.Clone(), Right.Clone());

		protected unsafe override decimal Equate(decimal a, decimal b) {
			int prec = b.AsInt();

			// decimal.Round doesn't accept negative values
			// so we've gotta roll this ourselves
			if (prec < 0) {
				// create a factor of 10 to use for manipulation
				decimal scaling = 1_0000_0000_0000_0000_0000_0000_0000M; // largest power of 10

				// change the scaler's scale factor to the appropriate power of 10
				*(int*) &scaling = (28 + prec) << 16;

				// divide by the scale factor
				a /= scaling;

				// kill the fractional part
				a = decimal.Truncate(a);

				// return to original order of magnitude
				a *= scaling;

				return a;

			} else if (prec > 28) {
				return a;

			} else {
				return decimal.Round(a, prec);
			}
		}
	}

	private sealed class Rebank(ExpressionItem left, ExpressionItem right) : BinaryExpression(left, right), IFunctionFactory<Rebank> {
		public const string Name = "rebank";
		public static string FunctionName => Name;

		public static Rebank? Create(Assembler assembler, List<ExpressionItem> args) {
			if (IFunctionFactory<Rebank>.TestForBadArity(args, assembler)) {
				return new Rebank(args[0], args[1]);
			} else {
				return null;
			}
		}

		public override ExpressionItem Clone() => new Rebank(Left.Clone(), Right.Clone());

		protected override decimal Equate(decimal a, decimal b) {
			return (a.AsLong() & SnesHelpers.AbsoluteMask) | ((b.AsLong() & 0xFFL) << 16);
		}
	}

	private sealed class Clamp(ExpressionItem itema, ExpressionItem itemb, ExpressionItem itemc)
			: TernaryExpression.SafeTernary(itema, itemb, itemc), IFunctionFactory<Clamp> {
		public const string Name = "clamp";
		public static string FunctionName => Name;

		public static Clamp? Create(Assembler assembler, List<ExpressionItem> args) {
			if (IFunctionFactory<Clamp>.TestForBadArity(args, assembler)) {
				return new Clamp(args[0], args[1], args[2]);
			} else {
				return null;
			}
		}

		public override ExpressionItem Clone() => new Clamp(ItemA.Clone(), ItemB.Clone(), ItemC.Clone());

		protected override bool Validate(decimal a, decimal b, decimal c) {
			if (b <= c) {
				return true;

			} else {
				_state = ExpressionState.InvalidArgument;
				return false;
			}
		}

		protected override decimal Equate(decimal a, decimal b, decimal c) {
			// not using decimal.Clamp because we've already validated the inputs
			if (a < b) {
				return b;

			} else if (a > c) {
				return c;

			} else {
				return a;
			}
		}
	}

	private sealed class Select(ExpressionItem itema, ExpressionItem itemb, ExpressionItem itemc)
			: TernaryExpression(itema, itemb, itemc), IFunctionFactory<Select> {
		public const string Name = "select";
		public static string FunctionName => Name;

		public static Select? Create(Assembler assembler, List<ExpressionItem> args) {
			if (IFunctionFactory<Select>.TestForBadArity(args, assembler)) {
				return new Select(args[0], args[1], args[2]);
			} else {
				return null;
			}
		}

		public override ExpressionItem Clone() => new Select(ItemA.Clone(), ItemB.Clone(), ItemC.Clone());

		protected override decimal Equate(decimal a, decimal b, decimal c) {
			return a.IsTrue() ? b : c;
		}
	}

	private sealed class BgSc(ExpressionItem itema, ExpressionItem itemb, ExpressionItem itemc)
			: TernaryExpression(itema, itemb, itemc), IFunctionFactory<BgSc> {
		public const string Name = "bgsc";
		public static string FunctionName => Name;

		public static BgSc? Create(Assembler assembler, List<ExpressionItem> args) {
			if (IFunctionFactory<BgSc>.TestForBadArity(args, assembler)) {
				return new BgSc(args[0], args[1], args[2]);
			} else {
				return null;
			}
		}

		public override ExpressionItem Clone() => new BgSc(ItemA.Clone(), ItemB.Clone(), ItemC.Clone());
		protected override decimal Equate(decimal a, decimal b, decimal c) {
			int ai = (a.AsInt() >> 9) & 0xFC;
			int bi = b.IsTrue() ? 0b10 : 0;
			int ci = c.IsTrue() ? 0b01 : 0;

			return ai | bi | ci;
		}
	}

	private sealed class BgNba(ExpressionItem left, ExpressionItem right) : BinaryExpression(left, right), IFunctionFactory<BgNba> {
		public const string Name = "bgnba";
		public static string FunctionName => Name;

		public static BgNba? Create(Assembler assembler, List<ExpressionItem> args) {
			if (IFunctionFactory<BgNba>.TestForBadArity(args, assembler)) {
				return new BgNba(args[0], args[1]);
			} else {
				return null;
			}
		}

		public override ExpressionItem Clone() => new BgNba(Left.Clone(), Right.Clone());

		protected override decimal Equate(decimal a, decimal b) {
			int ai = (a.AsInt() >> 13) & 0x0F;
			int bi = (b.AsInt() >> 9) & 0xF0;

			return ai | bi;
		}
	}

	private sealed class ObSel(ExpressionItem address, ExpressionItem nsoffset, ExpressionItem sizes)
			: TernaryExpression(address, nsoffset, sizes), IFunctionFactory<ObSel> {
		public const string Name = "obsel";
		public static string FunctionName => Name;

		public static ObSel? Create(Assembler assembler, List<ExpressionItem> args) {
			if (IFunctionFactory<ObSel>.TestForBadArity(args, assembler)) {
				return new ObSel(args[0], args[1], args[2]);
			} else {
				return null;
			}
		}

		public override ExpressionItem Clone() => new ObSel(ItemA.Clone(), ItemB.Clone(), ItemC.Clone());
		protected override decimal Equate(decimal a, decimal b, decimal c) {
			int ai = (a.AsInt() >> 14) & 0x07;
			int bi = (b.AsInt() << 3) & 0x18;
			int ci = (b.AsInt() << 5) & 0xE0;

			return ai | bi | ci;
		}
	}

	public sealed class HashString(ExpressionItem item) : StringExpression, IFunctionFactory<HashString> {
		public const string Name = "hash";
		public static string FunctionName => Name;
		public static int Arity => 1;

		public static HashString? Create(Assembler assembler, List<ExpressionItem> args) {
			if (IFunctionFactory<HashString>.TestForBadArity(args, assembler)) {
				return new HashString(args[0]);
			} else {
				return null;
			}
		}

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

	public sealed class GetStringLength(ExpressionItem item) : StringExpression, IFunctionFactory<GetStringLength> {
		public const string Name = "len";
		public static string FunctionName => Name;
		public static int Arity => 1;

		public static GetStringLength? Create(Assembler assembler, List<ExpressionItem> args) {
			if (IFunctionFactory<GetStringLength>.TestForBadArity(args, assembler)) {
				return new GetStringLength(args[0]);
			} else {
				return null;
			}
		}

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

	public sealed class CompareStrings(ExpressionItem left, ExpressionItem right) : StringExpression, IFunctionFactory<CompareStrings> {
		public const string Name = "streq";
		public static string FunctionName => Name;
		public static int Arity => 2;

		public static CompareStrings? Create(Assembler assembler, List<ExpressionItem> args) {
			if (IFunctionFactory<CompareStrings>.TestForBadArity(args, assembler)) {
				return new CompareStrings(args[0], args[1]);
			} else {
				return null;
			}
		}

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
