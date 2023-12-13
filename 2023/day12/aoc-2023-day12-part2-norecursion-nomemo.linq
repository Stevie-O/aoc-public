<Query Kind="Program">
  <Reference Relative="day12.txt">day12.txt</Reference>
  <Namespace>System.Collections.Immutable</Namespace>
</Query>

const bool USE_SAMPLE_INPUT = false;
const bool RUN_PART2 = true;

void Main()
{
	using (var tr = USE_SAMPLE_INPUT ? GetSampleInput() : GetPuzzleInput("day12.txt"))
	{
		var springsList = RegexInputParser(tr, new Regex(@"^([#.?]+)\s+(?:(\d+)(?:,|$))+$"), m =>
		{
			var pattern = m.Groups[1].Value;
			var lengths = m.Groups[2].Captures.Select(n => int.Parse(n.ValueSpan)).ToArray();

			if (RUN_PART2)
			{
				pattern = pattern + "?" + pattern + "?" + pattern + "?" + pattern + "?" + pattern;
				lengths = (lengths.Concat(lengths).Concat(lengths).Concat(lengths).Concat(lengths)).ToArray();
			}

			var lastHashIndex = pattern.LastIndexOf('#');
			// after @lastHashIndex it's possible to just be all '.'s
			return new SpringData(pattern, lastHashIndex, lengths);
		}).ToList();

		springsList
			.Select(springs => new { springs.report, lengths = string.Join(", ", springs.lengths), solutions = CountSolutions(springs) })
			//.ToList().Dump()
			.Select(s => s.solutions).Sum()
			.Dump("answer to " + (RUN_PART2 ? "part 2" : "part 1"))
			;
	}
}

// there are only two key parameters defining each state: 
// 1. the offset into the report
// 2. the number of springs successfully placed
record struct MatchState(int inputOffset, int springIndex);

record class SpringData(string report, int lastHash, int[] lengths)
{
	public int SpringCount => lengths.Length;
	/// <summary>Checks whether or not the specified state has successfully placed all of the springs.</summary>
	public bool IsFinished(MatchState state) => state.inputOffset == report.Length && state.springIndex == SpringCount;
}

/// <summary>Queue of states to proceed from (BFS)</summary>
class SolutionQueue
{
	CheapPriorityQueue<int, MatchState> _queue = new CheapPriorityQueue<int, MatchState>();
	Dictionary<MatchState, long> _counts = new Dictionary<MatchState, long>();

	public long Count => _queue.Count;
	public (MatchState state, long count) Dequeue()
	{
		var position = _queue.Dequeue();
		if (!_counts.Remove(position, out var count)) throw new Exception("internal data consistenty failure");
		return (position, count);
	}

	public void Enqueue(MatchState state, long count)
	{
		if (!_counts.TryGetValue(state, out long existingCount))
		{
			// we've already found at least one way to end up in the specified state. now we have @count more ways to do so.
			existingCount = 0;
			_queue.Enqueue(state.inputOffset, state);
		}
		_counts[state] = existingCount + count;
	}
}

/// <summary>The actual answer the puzzle is asking for</summary>
long CountSolutions(SpringData springs)
{
	// find out what our starting position is
	// for example, if the report is begins with "#?????" and the first spring is 3, all valid states begin with "###."
	// if the report begins with '?' then we can't skip anything. (actually there are some situations where we *could* skip something,
	// but those cases will be handled in the main loop.)
	var start = GetStart(springs);
	var queue = new SolutionQueue();
	queue.Enqueue(start, 1);

	long solved = 0;
	while (queue.Count > 0)
	{
		var (state, count) = queue.Dequeue();
		// @state is a reachable state
		// @count is the number of ways we can reach that state.
		if (springs.IsFinished(state))
		{
			// woo! we have placed all springs successfully!
			solved += count;
		}
		else
		{
			foreach (var newState in FindInterpretations(springs, state))
			{
				// newState should point to an '?' (or it should be one past the end of the string if it finishes up)
				var report = springs.report;
				if (newState.inputOffset < report.Length && report[newState.inputOffset] != '?') throw new Exception("internal error");
				// there are @count (more) ways to reach @newState
				queue.Enqueue(newState, count);
			}
		}
	}
	return solved;
}

/// <summary>Try to place the spring at index @springIndex beginning at report offset @offset</summary>
MatchState? TryPlaceSpring(SpringData springs, int offset, int springIndex, bool speculative)
{
	// @speculative=true if we should treat report[offset] as if it were '#' even if it isn't
	var report = springs.report;
	if (offset >= report.Length) throw new ArgumentException("offset should be less than report.Length", nameof(offset));
	while (offset < report.Length && (speculative || report[offset] == '#'))
	{
		speculative = false;
		// the next spring.lengths[springIndex] characters must be '#'s, and the character after that must be a '.'
		if (springIndex >= springs.SpringCount) return null; // we cannot place a spring here because there are no springs less
		var sprlen = springs.lengths[springIndex++];
		if (offset > report.Length - sprlen) return null; // we cannot place a spring here because there is no room
		for (int i = 0; i < sprlen; i++)
			if (report[offset++] == '.') return null; // we cannot place a spring here because we know there is a gap 
		if (offset < report.Length && report[offset++] == '#') return null; // we cannot place a spring here because there is no gap _after_ the spring

		// okay, we fully consumed this spring.
		// if there are any hardwired '.'s after this then skip past them
		while (offset < report.Length && report[offset] == '.') offset++;
	}
	// at this point, we should either be past the end of @report or be pointed at a '?'.
	if (offset < report.Length && report[offset] != '?') throw new Exception($"internal error -- trymatch should end at end-of-string or at a ? - report='{report}' offset={offset} char='{report[offset]}'");
	return new MatchState(offset, springIndex);
}

MatchState GetStart(SpringData springs)
{
	var report = springs.report;
	int offset = 0;
	int springIndex = 0;
	// skip hard '.' at start of string
	while (offset < report.Length && report[offset] == '.') offset++;
	if (report[offset] == '#')
	{
		return TryPlaceSpring(springs, offset, springIndex, false) ?? throw new Exception("Unsolvable");
	}
	else
	{
		return new MatchState(offset, springIndex);
	}
}

/// <summary>find interpretations for the report beginning at <paramref name='startFrom' /></summary>
IEnumerable<MatchState> FindInterpretations(SpringData springs, MatchState startFrom)
{
	var report = springs.report;
	int offset = startFrom.inputOffset;
	if (startFrom.springIndex >= springs.SpringCount)
	{
		// the entire rest of the string must be '.'s
		// if there are any hard '#'s, this is a dead-end.  any '?'s can be interpreted as '.' and still match, however.
		if (offset > springs.lastHash)
			yield return startFrom with { inputOffset = report.Length };  // interpret all remaining '?'s as '.' and zoom to the end
		yield break;
	}
	// if we're past the end of the report and we have unplaced springs, the input state is unsolvable.
	if (offset >= report.Length) yield break;
	// sanity check -- this had better be uncdertain
	if (report[offset] != '?') throw new Exception("internal error -- " + nameof(FindInterpretations) + " called but not pointed at a '?'");

	// okay, we now have a choice: this '?' is either a '.' or a '#'
	// can we treat this as '#'?
	var treatAsSpring = TryPlaceSpring(springs, offset, startFrom.springIndex, true);
	if (treatAsSpring != null) yield return treatAsSpring.Value;

	// okay, try treating it as a '.'
	offset++;
	// skip past any other '.'s
	while (offset < report.Length && report[offset] == '.') offset++;
	if (offset < report.Length && report[offset] != '?')  // note: if report[offset] is not ? it should be a #
	{
		// well, there's definitely a spring here. try to place one.
		// (if we _can't_ place one, then we're at a dead-end)
		var realign = TryPlaceSpring(springs, offset, startFrom.springIndex, false);
		if (realign != null) yield return realign.Value;
	}
	else
	{
		// either we're past the end of the report or we're pointed at a '?'
		yield return new MatchState(offset, startFrom.springIndex);
	}
}

#region Input handling

static IEnumerable<T> RegexInputParser<T>(TextReader tr, Regex re, Func<Match, T> conversion)
{
	string line;
	while ((line = tr.ReadLine()) != null)
	{
		if (line.Length == 0) continue;
		var m = re.Match(line);
		if (!m.Success) throw new Exception("Invalid input: " + line);
		yield return conversion(m);
	}
}

TextReader GetPuzzleInput(string path)
{
	return new StreamReader(path);
}

const string EXAMPLE_1 = @"
???.### 1,1,3
.??..??...?##. 1,1,3
?#?#?#?#?#?#?#? 1,3,1,6
????.#...#... 4,1,1
????.######..#####. 1,6,5
?###???????? 3,2,1
";

TextReader GetSampleInput()
{
	return new StringReader(EXAMPLE_1);
}

#endregion

#region CheapPriorityQueue
class CheapPriorityQueue<TPriority, TValue>
{
	class ShimComparer : IComparer<(TPriority key, long sequence)>
	{
		IComparer<TPriority> _keyComparer;
		public ShimComparer(IComparer<TPriority> keyComparer)
		{
			_keyComparer = keyComparer;
		}

		public int Compare((TPriority key, long sequence) x, (TPriority key, long sequence) y)
		{
			int cmp = _keyComparer.Compare(x.key, y.key);
			if (cmp != 0) return cmp;
			return x.sequence.CompareTo(y.sequence);
		}
	}

	SortedDictionary<(TPriority key, long sequence), TValue> _dict;
	long _sequence;

	public CheapPriorityQueue() : this(Comparer<TPriority>.Default)
	{
	}

	public CheapPriorityQueue(IComparer<TPriority> comparer)
	{
		_dict = new SortedDictionary<(TPriority, long), TValue>(new ShimComparer(comparer));
	}

	public int Count => _dict.Count;
	public TValue Dequeue()
	{
		var firstEntry = _dict.First();
		_dict.Remove(firstEntry.Key);
		return firstEntry.Value;
	}
	public void Enqueue(TPriority priority, TValue value)
	{
		var sequence = _sequence++;
		_dict[(priority, sequence)] = value;
	}
}
#endregion