namespace Futaba;

internal static unsafe class FastRead {
	// nowhere else to shove these
	extension(NativeMemory) {
		internal static T* AllocNice<T>(int ct) where T : unmanaged {
			return (T*) NativeMemory.AlignedAlloc((nuint) (sizeof(T) * ct), 64);
		}

		internal static T* ReAllocNice<T>(T* pv, int ct) where T : unmanaged {
			return (T*) NativeMemory.AlignedRealloc(pv, (nuint) (sizeof(T) * ct), 64);
		}
	}

	extension(TextWriter txt) {
		public void Reset() {
			if (txt is StreamWriter s) {
				s.BaseStream.Position = 0;
				s.BaseStream.SetLength(0);
			}
		}
	}

	internal static bool IsWhiteSpace(char* start, char* end) {
		return CharSpanHelpers.CreateSpan(start, end).IsWhiteSpace();
	}

	internal static bool SkipToMatchingParenthesis(scoped ref char* start, char* end) {
		int parenCount = 0;

		char* reading = start;

		for (; reading < end; reading++) {
			char c = *reading;

			if (c is '"') {
				if (SkipToMatchingQuote(ref reading, end)) continue;
				break;
			} else if (c is '(') {
				parenCount++;
			} else if (c is ')') {
				if (--parenCount is 0) {
					start = reading;
					return true;
				}
			} else if (TestIfCommandEndingToken(reading)) {
				break;
			}
		}

		start = reading;
		return false;
	}

	/// <summary>
	/// start will end pointing to the closing quote
	/// </summary>
	/// <param name="start">This should point to the beginning quotation mark</param>
	/// <param name="end">The end of the entire line</param>
	/// <returns></returns>
	internal static bool SkipToMatchingQuote(scoped ref char* start, char* end) {
		char c;
		char* read = start + 1;
		for (; read < end; read++) {
			c = *read;

			if (c is '"') {
				start = read;
				return true;
			} else if (c is '\\') {
				read++;
				c = *read;
			}

			// this should be checked even if we hit a \
			if (*read is '\n' or BadChar) {
				break;
			}
		}

		start = read;
		return false;
	}

	internal static bool TryReadOutQuotedString(scoped ref char* start, char* end, out CharSpan span) {
		char c;
		char* read = start + 1;

		for (; read < end; read++) {
			c = *read;

			if (c is '"') {
				span = CharSpanHelpers.CreateSpan(start + 1, read);
				start = read + 1;
				return true;
			} else if (c is '\\') {
				read++;
				c = *read;
			}

			// this should be checked even if we hit a \
			if (*read is '\n' or BadChar) {
				break;
			}
		}

		start = read;
		span = default;
		return false;
	}

	internal static bool ColonIsSeparator(char* ptr) {
		return ptr[-1].IsSkippableSpace && ptr[+1].IsSkippableSpace;
	}

	internal static bool TestIfColonAndSeparator(char* ptr) {
		return ptr[0] is ':' && ColonIsSeparator(ptr);
	}

	internal static bool TestIfCommandEndingToken(char* ptr) {
		char c = *ptr;

		return (c is ':' && ColonIsSeparator(ptr)) || c is ';' or NewLine or BadChar;
	}

	internal static int ReadAlphanumericHelper(char* start, char* end) {
		char* read = start;

		while (read < end) {
			char c = *read;

			if (c < AsciiHelpers.AsciiCount) {
				ulong mask = (c < 64) ? AsciiHelpers.NumbersMask : AsciiHelpers.LettersMask;

				if ((mask & (1ul << c)) != 0) {
					read++;
					continue;
				}
			}
			break;
		}

		return (int) (read - start);
	}

	internal static int ReadSublabelBetter(char* start, char* end) {
		char* read = start;

		while (read < end) {
			char c = *read;

			if (c < AsciiHelpers.AsciiCount) {
				ulong mask = (c < 64) ? AsciiHelpers.SublabelsMask : AsciiHelpers.LettersMask;

				if ((mask & (1ul << c)) != 0) {
					read++;
					continue;
				}
			}
			break;
		}

		return (int) (read - start);
	}

	internal static void SkipInlineWhitespace(scoped ref char* start, char* end) {
		char* read = start;

		while (read < end && read->IsSkippableSpace) {
			read++;
		}

		start = read;
		//start = TrimHelper(start, end, TrimType.Head).Start;
	}

	internal static bool SkipInlineWhitespaceWithEndCheck(scoped ref char* start, char* end) {
		SkipInlineWhitespace(ref start, end);
		return start == end;
	}

	internal static void TrimWhiteSpace(scoped ref char* start, scoped ref char* end) {
		//(start, end) = TrimHelper(start, end, TrimType.Both);

		char* thestart = start;
		char* theend = end;

		while (thestart < theend && thestart->IsSkippableSpace) {
			thestart++;
		}

		// test must happen first to avoid reading out of bounds
		while (--theend > thestart && theend->IsSkippableSpace) ;

		start = thestart;
		end = theend + 1;
	}

	internal static bool IsVariableName(char* reading, char* end) {
		return !CharSpanHelpers.CreateSpanUnchecked(reading, end).ContainsAnyExcept(AsciiHelpers.IdentifierSearch);

		//while (reading < end) {
		//	if (reading->IsIdentifierCharacter) {
		//		reading++;
		//	} else {
		//		return false;
		//	}
		//}
		//
		//return true;
	}

	internal static bool IsIdentifier(char* reading, char* end) {
		return IsIdentifier(CharSpanHelpers.CreateSpanUnchecked(reading, end));

		//if (reading->IsLetterOrUnderscore) {
		//	return !.ContainsAnyExcept(AsciiHelpers.IdentifierSearch);
		//	//while (++reading < end) {
		//	//	if (!reading->IsIdentifierCharacter) {
		//	//		return false;
		//	//	}
		//	//}
		//	//
		//	//return true;
		//}
		//
		//return false;
	}

	internal static bool IsIdentifier(CharSpan name) {
		if (name.Length > 0 && name[0].IsLetterOrUnderscore) {
			return !name.ContainsAnyExcept(AsciiHelpers.IdentifierSearch);
		}

		return false;
	}

	internal static bool TryReadIdentifier(scoped ref char* reading, char* end, out CharSpan name) {
		char* wordstart = reading;

		if (!wordstart->IsLetterOrUnderscore) {
			name = [];
			return false;
		}

		int length = ReadAlphanumericHelper(wordstart, end);

		name = new(wordstart, length);

		reading += length;

		return true;

		//		char* read = wordstart;
		//		while (++read < end && read->IsIdentifierCharacter) ;
		//
		//		reading = read;
		//
		//		name = CharSpanHelpers.CreateSpan(wordstart, read);
		//
		//		return true;
	}

	internal static bool TryReadIdentifier(scoped ref char* reading, char* end, out OperandString name) {
		char* wordstart = reading;

		if (!wordstart->IsLetterOrUnderscore) {
			name = default;
			return false;
		}

		int length = ReadAlphanumericHelper(wordstart, end);

		name = new(wordstart, length);

		reading += length;

		return true;

		//char* read = wordstart;
		//while (++read < end && read->IsIdentifierCharacter) ;
		//
		//reading = read;
		//
		//name = new(wordstart, read);
		//
		//return true;
	}

	internal static CharSpan ReadSublabelChunk(scoped ref char* reading, char* end) {
		char* wordstart = reading;

		int length = ReadSublabelBetter(wordstart, end);

		CharSpan ret = new(wordstart, length);

		reading += length;

		return ret;

		//		char* wordstart = reading;
		//
		//		char* read = wordstart;
		//
		//		while (read < end && read->IsSublabelSafe) {
		//			read++;
		//		}
		//
		//		reading = read;
		//
		//		return CharSpanHelpers.CreateSpan(wordstart, read);
	}

	internal static bool TryReadSublabel(scoped ref char* reading, char* end, out CharSpan chunk, out int dots) {
		char* start = reading;

		dots = CountCharacter(start, end, SublabelDelimiter);

		start += dots;

		int length = ReadSublabelBetter(start, end);

		chunk = new(start, length);

		reading = start + length;

		return length is not 0;

		//char* chunkstart = reading;
		//
		//dots = CountCharacter(chunkstart, end, SublabelDelimiter);
		//
		//char* read = chunkstart;
		//
		//while (read < end && read->IsSublabelSafe) {
		//	read++;
		//}
		//
		//chunk = CharSpanHelpers.CreateSpan(chunkstart, read) ;
		//
		//reading = read;
		//return read != chunkstart;
	}

	internal static OperandString ReadAlphaNumeric(scoped ref char* reading, char* end) {
		char* wordstart = reading;
		int length = ReadAlphanumericHelper(wordstart, end);

		reading += length;
		return new(wordstart, length);

		//char* wordstart = reading;
		//char* read = wordstart;
		//
		//while (read < end && read->IsIdentifierCharacter) {
		//	read++;
		//}
		//
		//reading = read;
		//return new(wordstart, read);
	}

	internal static CharSpan ReadAlphaNumericSpan(scoped ref char* reading, char* end) {
		char* wordstart = reading;
		int length = ReadAlphanumericHelper(wordstart, end);

		reading += length;
		return new(wordstart, length);

		//char* wordstart = reading;
		//char* read = wordstart;
		//
		//while (read < end && read->IsIdentifierCharacter) {
		//	read++;
		//}
		//
		//reading = read;
		//return CharSpanHelpers.CreateSpanUnchecked(wordstart, read);
	}

	// don't think these need indexof, since we're expecting very small numbers
	internal static int CountCharacter(char* start, char* end, char test) {
		char* read = start;

		while (*read == test) {
			read++;
		}

		int count = (int) (read - start);
		return count;
	}

	internal static int CountCharacter(scoped ref char* start, char* end, char test) {
		char* read = start;
		char* rstart = read;

		while (*read == test) {
			read++;
		}

		int count = (int) (read - rstart);
		start = read;
		return count;
	}

	// TODO microbenchmark
	internal static void ReadUntilLineEnd(scoped ref char* start, char* end) {
		//start += CharSpanHelpers.CreateSpanUnchecked(start, end).IndexOf(NewLine); #error breaks

		char* read = start;
		
		while (read < end && *read is not NewLine) {
			read++;
		}
		
		start = read;
	}

	internal static decimal ReadDecimal(scoped ref char* reading, char* end) {
		decimal decval = 0;

		char* readit = reading;

		for (; readit < end; readit++) {
			int decdig = *readit - '0';

			if ((uint) decdig <= 9) {
				decval *= 10;
				decval += decdig;
				continue;
			}

			if (decdig is ('_' - '0')) continue;
			if (decdig is ('.' - '0')) goto DecimalFractions;

			break;
		}

		DecimalDone:
		reading = readit;
		return decval;

		DecimalFractions:
		if (++readit == end) {
			goto DecimalDone;
		}

		decimal fracdiv = 0.1M;

		for (; readit < end; readit++) {
			int decdig = *readit - '0';

			if ((uint) decdig <= 9) {
				decval += decdig * fracdiv;
				fracdiv /= 10;
				continue;
			}

			if (decdig is ('_' - '0')) continue;

			break;
		}

		goto DecimalDone;
	}

	internal static bool TryReadInlineDecimalInteger(scoped ref char* reading, char* end, out int value) {
		value = 0;

		char* readit = reading;

		for (; readit < end; readit++) {
			int decdig = *readit - '0';

			if ((uint) decdig <= 9) {
				value *= 10;
				value += decdig;
			}
		}

		if (reading == readit) {
			return false;
		}

		reading = readit;
		return true;
	}

	internal static bool TryReadHexRaw(scoped ref char* reading, char* end, out int hexval) {
		hexval = 0;
		char* readit = reading;

		for (; readit < end; readit++) {
			int hexdig = readit->GetHexValue();

			if (hexdig >= 0) {
				hexval <<= 4;
				hexval |= hexdig;
				continue;
			}

			if (*readit is '_') continue;

			break;
		}

		// if we didn't read any characters, it's invalid
		if (readit == reading) {
			return false;
		}

		reading = readit;
		return true;
	}

	internal static bool TryReadHex(scoped ref char* reading, char* end, out int hexval) {
		// skip over dollar sign
		reading++;

		return TryReadHexRaw(ref reading, end, out hexval);
	}

	internal static bool TryReadBinary(scoped ref char* reading, char* end, out int binval) {
		// skip over the percent sign
		reading++;

		char* readit = reading;

		binval = 0;

		for (; readit < end; readit++) {
			int bindig = *readit;
			bindig ^= '0'; // exclusive or with 0 to get either 0 or 1

			if ((uint) bindig <= 1) { // if it's 0 or 1
				binval <<= 1;
				binval |= bindig;
				continue;
			}

			if (bindig == ('0' ^ '_')) {
				continue;
			}

			break;
		}

		// if we didn't read any characters, invalid
		if (readit == reading) {
			return false;
		}

		reading = readit;
		return true;
	}

	internal static bool IsAllocator(char* read) {
		return AsciiHelpers.FastTest(read, ':', ':');

		//return (read[0] is ':') && (read[1] is ':');
	}
}
