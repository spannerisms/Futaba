namespace Futaba;

internal sealed unsafe class MacroCall {
	private readonly string _name;
	private readonly SourceLine _declarationSite;

	internal int Arity { get; }

	internal bool IsFunction { get; init; }

	private readonly List<string> _arguments;

	private readonly string _body;

	private readonly string _identifier;
	private int _invokeCount = 0;

	// using reserved unicode characters to allow fast replacement
	// (they need replacing anyways to preempt false positives)
	private const char SpecialMarker = '\uF201';
	private const string SpecialID = "\uF200";

	internal MacroCall(string name, List<string> argumentNames, StringBuilder body, int macronum, SourceLine decSite) {
		_name = name;

		Arity = argumentNames.Count;

		_arguments = argumentNames;

		for (int i = 0; i < Arity; i++) {
			body.Replace($"{{{_arguments[i]}}}", $"{(char) (SpecialMarker + i)}");
		}

		body.Replace("{?}", SpecialID);

		_body = body.ToString();

		_identifier = $"M{macronum:X5}";
		_declarationSite = decSite;

	}

	internal bool TryExplode(List<OperandString> args, string uniqueId, [NotNullWhen(true)] out string? invoker, [MaybeNullWhen(true)] out string? errors) {
		_invokeCount++;

		int arglen = args.Count;

		if (arglen != Arity) {
			invoker = null;
			errors = arglen < Arity
				? "Not enough arguments in macro call"
				: "Too many arguments in macro call";
			return false;
		}

		StringBuilder ret = new(_body);

		// replace these temporarily to prevent recursive replacements

		for (int i = 0; i < Arity; i++) {
			CharSpan repv = (string) args[i];

			if (repv is ['"', .. var contents, '"']) {
				repv = contents;
			}
			
			ret.Replace($"{(char) (SpecialMarker + i)}", repv);
		}

		ret.Replace(SpecialID, uniqueId);

		invoker = ret.ToString();

		errors = null;
		return true;
	}

	private string GetUniqueCallName() => $"{_identifier}{_invokeCount:X5}";

	internal bool TryGetInvoker(SourceLine callSite, List<OperandString> args, [MaybeNullWhen(false)] out SourceObject invoker, out string? errors) {
		string uniqueId = GetUniqueCallName();

		if (!TryExplode(args, uniqueId, out var theBody, out errors)) {
			invoker = null;
			return false;
		}

		invoker = new MacroInvocation(this, theBody, uniqueId, callSite);

		errors = null;

		return true;
	}

	private sealed class MacroInvocation : SourceObject {
		internal override bool IsMacro => true;
		private readonly string _identifier;

		public override FileInfo? FileInfo => _callSite.Source.FileInfo;
		private readonly SourceLine _callSite;
		private readonly MacroCall _macroType;

		internal MacroInvocation(MacroCall macroClass, string body, string name, SourceLine callSite) {
			int bufferLen = body.Length;

			AllocBlock = Allocate(bufferLen);

			SourceStart = AllocBlock + BufferPadding;

			body.AsSpan().CopyTo(new(SourceStart, body.Length));

			SourceEnd = SourceStart + body.Length;

			ApplyPadding();
			_identifier = name;

			_callSite = callSite;
			_macroType = macroClass;
		}

		public override string ObjectName => _identifier;

		public override string ToString() {
			return $"{_callSite.Source}: [Macro \"{_macroType._name}\" (@{_macroType._declarationSite})] ";
		}

		protected override void DisposeResources() { }
	}




}
