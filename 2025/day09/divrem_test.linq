<Query Kind="Program">
  <NuGetReference>System.Collections.Immutable</NuGetReference>
  <Namespace>System.Collections.Concurrent</Namespace>
  <Namespace>System.Collections.Immutable</Namespace>
  <Namespace>System.Globalization</Namespace>
  <Namespace>System.Threading.Tasks</Namespace>
  <Namespace>System.Numerics</Namespace>
</Query>

const int INSTRUCTION_LIMIT =
	//1_000_000_000
	10_000
	;

static class ArrayInput
{
	public static ArrayInput<T> Create<T>(params T[] values)
	{
		return new ArrayInput<T>(values, 0);
	}
}
struct ArrayInput<T> : IIntcodeInput<T>
{
	T[] _array;
	int _offset;
	public ArrayInput(T[] array, int offset) { _array = array; _offset = offset; }
	public IIntcodeInput<T> Dequeue(out T element)
	{
		if (_offset >= _array.Length) { element = default(T); return this; }
		element = _array[_offset];
		return new ArrayInput<T>(_array, _offset + 1);
	}
}

static string WorkDir;
void Main()
{
	Util.NewProcess = true;
	var dir = WorkDir = Path.Combine(Path.GetDirectoryName(Util.CurrentQueryPath),
		"."
	);
	var code_file = File.ReadAllText(Path.Combine(dir, "divrem_test.intcode"));
	LoadMemoryMap(Path.Combine(dir, "divrem_test.map"));

	var cpu = ParseInput<long>(new StringReader(
			code_file
		));

	var rng = new Random(1234);
	var dc_tests = new DumpContainer().Dump("Trials");
	var dc_pass1 = new DumpContainer().Dump("Pass (a = qb+r)");
	var dc_pass2 = new DumpContainer().Dump("Pass (q == (a/b) r== (a%b)");
	var dc_fail = new DumpContainer().Dump("Failures");
	var dc_instruction_count = new DumpContainer().Dump("Instructions executed");
	var dc_log2_q_sum = new DumpContainer().Dump("Sum(log2(q))");
	var dc_instruction_rate = new DumpContainer().Dump("Instructions per log2(q)");

	int test_count = 0;
	int pass1_count = 0;
	int pass2_count = 0;
	int fail_count = 0;
	long instruction_count = 0;
	long sum_log2_q = 0;
	int next_update = Environment.TickCount;

	
	DumpContainer dc_laststats = default; //new DumpContainer().Dump("Last execution stats");
	DumpContainer dc_flamegraph = default; //new DumpContainer().Dump("CPU flamegraph");
	long slowest_version = 0;
	for (int i = 0; i < 10_000_000; i++)
	{
		var cpu_work = cpu.Clone();
		var a = rng.Next();
		var b = rng.Next();
		
		/*
		a = 2034237595;
		b = 997536529;
		cpu_work.AddBreakpoint(75, c =>
		{
			Console.WriteLine();
			Console.WriteLine("r (a) = {0}", ReadVariable(c, "fn_divrem_positive__r"));
			Console.WriteLine("~0 = {0}", c.Memory[c.Toc]);
		});
		*/
		
		if (dc_flamegraph != null && a <= b) continue; // while it's important to verify cases where a<=b, they aren't interesting from a flamegraph perspective
		var results = new List<long>();
		cpu_work = cpu_work.WithIO(
			ArrayInput.Create((long)a, (long)b),
			w => results.Add(w)
		);
		IntcodeCpu<long> final_cpu;
		long opcode_count;
		try
		{
			if (true) ExecutionLogFile = new ExecutionLogFileWriter(new StreamWriter(
					Path.Combine(Path.GetDirectoryName(Util.CurrentQueryPath), @"intcode-execlog.txt"), false, new UTF8Encoding(false))
					);
			(final_cpu, opcode_count) = RunUntilHalt(cpu_work, false, instruction_limit: INSTRUCTION_LIMIT, dc_flamegraph);
		}
		finally
		{
			ExecutionLogFile?.Dispose();
		}
		//var (final_cpu, opcode_count) = RunUntilHalt(cpu_work, false, INSTRUCTION_LIMIT);
		if (!final_cpu.IsHalted) { Console.WriteLine("ERROR: did not halt on inputs [{0}, {1}]", a, b); }
		else
		{
			// the surrounding code (rbo + 2x input and 2x output + hlt) is 6 opcodes
			const long PROGRAM_OVERHEAD = 6;
			instruction_count += (opcode_count - PROGRAM_OVERHEAD);
			test_count++;

			var expected_q = (a / b);
			var expected_r = (a % b);
			var q = results[0];
			var r = results[1];

			bool did_pass1 = q * b + r == a;
			bool did_pass2 = q == expected_q && r == expected_r;
			if (did_pass1) { pass1_count++; }
			if (did_pass2) { pass2_count++; }
			else
			{
				fail_count++;
			}
			
			int q_value = BitOperations.Log2((uint)q) + BitOperations.PopCount((uint)q);

			sum_log2_q += q_value;
			//sum_log2_q += BitOperations.Log2((uint)q); //+ BitOperations.PopCount((uint)q);

			if (dc_laststats != null)
			{
				dc_laststats.Content = new
				{
					a,
					b,
					q,
					r,
					expected_q,
					expected_r,
					did_pass1,
					did_pass2,
					q_value,
					opcode_count,
					opcode_count_adjusted = opcode_count - PROGRAM_OVERHEAD,

				};
			}

			if (slowest_version < opcode_count)
			{
				slowest_version = opcode_count;
			}
			if (!did_pass2)
			{
				Console.WriteLine("FAILED on a={0}, b={1}", a, b);
			}
		}
		if ((Environment.TickCount - next_update) > 0)
		{
			dc_tests.Content = test_count;
			dc_pass1.Content = pass1_count;
			dc_pass2.Content = pass2_count;
			dc_fail.Content = fail_count;
			dc_instruction_count.Content = instruction_count;
			dc_log2_q_sum.Content = sum_log2_q;
			dc_instruction_rate.Content = (double)instruction_count / (double)sum_log2_q;

			next_update = Environment.TickCount + 50;
		}
	}
}

static string ToScaleAndOffset(int offset, int array_element_size)
{
	var count = offset / array_element_size;
	var remainder = offset % array_element_size;
	return $"{count} * {array_element_size} + {remainder} = {offset}";
}

class BreakpointOutput : IDisposable
{
	public BreakpointOutput()
	{
		ExecutionLogFile?.WriteLine("");
	}

	public void WriteLine(string message) { Console.WriteLine(message); ExecutionLogFile?.WriteLine(message); }
	public void WriteLine(string format, params object[] args) { Console.WriteLine(format, args); ExecutionLogFile?.WriteLine(format, args); }

	public void Dispose()
	{
		ExecutionLogFile?.WriteLine("");
	}
}

void LoadMemoryMap(string path)
{
	_memoryMap = File.ReadAllLines(path)
		.Where(l => l.Trim().Length > 0)
		.Select(l =>
		{
			var m = Regex.Match(l, @"^(-?[0-9]+)\s+(\S+)$");
			if (!m.Success) throw new Exception("Invalid input in mapfile: '" + l + "'");
			return (addr: int.Parse(m.Groups[1].Value), name: m.Groups[2].Value);
		})
		.ToArray();
	_addr2Name = _memoryMap.Where(x => x.address >= 0)
		.GroupBy(x => x.address)
		.ToDictionary(x => x.Key, x => x.First().name);
	_name2Addr = _memoryMap.Where(x => x.address >= 0).ToDictionary(x => x.name, x => x.address);
	var allFunctions = new List<(int start, int end, string name)>();
	foreach (var entry in _memoryMap.Where(x => x.address >= 0 && x.name.IndexOf("__") < 0))
	{
		var end_name = "fn_" + entry.name + "__auto__endfn";
		if (_name2Addr.TryGetValue(end_name, out var end_address))
		{
			allFunctions.Add((entry.address, end_address, entry.name));
		}
	}
	allFunctions.Sort((a, b) => a.start.CompareTo(b.start));
	_functionMap = allFunctions.ToArray();
}

memval_t ReadVariable<memval_t>(IntcodeCpu<memval_t> cpu, string name)
	where memval_t : IComparable<memval_t>,
					IEquatable<memval_t>,
					IAdditionOperators<memval_t, memval_t, memval_t>,
					IMultiplyOperators<memval_t, memval_t, memval_t>,
					// I don't actually need full INumberBase but that's the only way to get conversion
					INumberBase<memval_t>
{
	return cpu.Memory[_name2Addr[name]];
}

// ALL THIS FUCKING WORK because BinarySearch doesn't just let you pass a callback routine
static class DelegateComparer
{
	public static DelegateComparer<T> Create<T>(Func<T?, T?, int> cb) => new DelegateComparer<T>(cb);
}
class DelegateComparer<T> : IComparer<T>
{
	readonly Func<T, T, int> _cb;
	public DelegateComparer(Func<T, T, int> cb) { _cb = cb; }

	public int Compare(T x, T y) => _cb(x, y);
}

static IComparer<(int address, string name)> AddressComparer = DelegateComparer.Create<(int address, string name)>(
		 (a, b) => a.address.CompareTo(b.address));

static (int start, int end, string name)[] _functionMap;
static (int address, string name)[] _memoryMap;
static Dictionary<int, string> _addr2Name;
static Dictionary<string, int> _name2Addr;

static string DescribeAddress(int address, int rb)
{
	if (_memoryMap == null || _memoryMap.Length == 0) return address.ToString();
	if (address == 70)
	{
		Util.Break();
	}
	if (address >= _memoryMap[_memoryMap.Length - 1].address)
	{
		var rb_diff = address - rb;
		if (Math.Abs(rb_diff) <= 20)
		{
			//return new StringBuilder().Append(address.ToString()).Append("(rb").Append(rb_diff.ToString("+0;-#", null)).Append(')').ToString();
			return new StringBuilder().AppendFormat("{0}(~{1})", address, rb_diff).ToString();
		}
		return address.ToString();
	}
	var index = PartitionPoint(_memoryMap, a => a.address < address);
	// NOTE: index < _memoryMap.Length ddue to the above check
	var addr_str = address.ToString();
	var obj = _memoryMap[index];
	if (obj.address > address) return addr_str;
	var sb = new StringBuilder(addr_str);
	sb.Append("(&").Append(obj.name);
	if (address > obj.address) { sb.Append('+').Append((address - obj.address).ToString()); }
	sb.Append(')');
	return sb.ToString();
}

const int MaxTraceLog = 40_000;
static ExecutionLogFileWriter ExecutionLogFile = null;
class ExecutionLogFileWriter : IDisposable
{
	int instr_num = 0;
	Queue<string> Last1000Instructions = new Queue<string>();
	TextWriter ExecutionLogFile => _logFile;

	TextWriter _logFile;
	public void Dispose() { _logFile.Dispose(); }
	public void Flush() { _logFile.Flush(); }
	public ExecutionLogFileWriter(TextWriter logFile) { _logFile = logFile ?? throw new ArgumentNullException(nameof(logFile)); }
	public void WriteLine(string message) => _logFile.WriteLine(message);
	public void WriteLine(string format, params object[] args) => _logFile.WriteLine(format, args);
	public void LogExecutionMessage(string message)
	{
		++instr_num;
		var full_message = "{" + instr_num + "} " + message;
		ExecutionLogFile?.WriteLine(full_message);
		Last1000Instructions.Enqueue(full_message);
		if (Last1000Instructions.Count > MaxTraceLog) Last1000Instructions.Dequeue();
	}
}

TextWriter OutputTextWriter = Console.Out;
bool OutputLiterals = true;

bool dle_received = false;
const byte DLE = 16;
void PrintOutput(long value)
{
	if (dle_received)
	{
		dle_received = false;
		if (OutputLiterals) OutputTextWriter.Write(value);
		else value.Dump("Dumped value");
		return;
	}
	if (value == '\n') OutputTextWriter.WriteLine();
	else if (value == DLE) { dle_received = true; }
	else if (value < 0x20 || value >= 0x7F)
	{
		if (OutputLiterals) OutputTextWriter.WriteLine("<{0} / 0x{0:X}>", value);
		else value.Dump("Nonprintable");
	}
	else OutputTextWriter.Write((char)value);
}

static HashSet<int> MemoryBreakPoints = new HashSet<int>()
{
	//2166,
};

class Indirect<T> { public T Value; }

(IntcodeCpu<memval_t>, long instruction_count) RunUntilHalt<memval_t>(IntcodeCpu<memval_t> cpu, bool print_exectime_info = true, int instruction_limit = 0, DumpContainer dc_flamegraph = null)
	where memval_t : IComparable<memval_t>,
					IEquatable<memval_t>,
					IAdditionOperators<memval_t, memval_t, memval_t>,
					IMultiplyOperators<memval_t, memval_t, memval_t>,
					// I don't actually need full INumberBase but that's the only way to get conversion
					INumberBase<memval_t>
{
	var _instructionHitCount = new Dictionary<int, Indirect<long>>();
	
	long limit = long.MaxValue;// 1_000_000_000;
	long instrcount = 0;
	while (!cpu.IsHalted)
	{
		// this would've been a oneliner in Rust. meh.
		if (!_instructionHitCount.TryGetValue(cpu.Pc, out var holder))
			holder = _instructionHitCount[cpu.Pc] = new();
		holder.Value++;

		/*if (Breakpoints.Contains(cpu.Pc))
		{
			ExecutionLogFile?.Flush();
			Console.WriteLine("Breakpoint at PC={0}", cpu.Pc);
			Util.Break();
		}
		*/
		if (instruction_limit > 0 && --instruction_limit == 0)
		{
			break;
		}
		if (--limit <= 0) throw new Exception("cpu rlimit exceeded");
		if (ExecutionLogFile != null && _addr2Name.TryGetValue(cpu.Pc, out var name))
			ExecutionLogFile.WriteLine("## {0}", name);
		cpu = cpu.ExecuteInstruction();
		instrcount++;
	}
	if (print_exectime_info || dc_flamegraph != null)
	{
		var pc_flamegraph = _instructionHitCount.OrderBy(x => x.Key).Select(x => new { pc = x.Key, pc_desc = DescribeAddress(x.Key, cpu.Toc), count = x.Value.Value });
		if (print_exectime_info)
		{
			instrcount.Dump("Number of instructions executed");
			pc_flamegraph
				.Dump("PC flamegraph", collapseTo: 0);
			_instructionHitCount.Select(x =>
					new
					{
						function_name = GetFunctionName(x.Key) ?? "(no function)",
						hit_count = x.Value.Value
					})
				.GroupBy(i => i.function_name, i => i.hit_count)
				.Select(g => new { function_name = g.Key, hit_count = g.Sum() })
				//.OrderBy(f => f.function_name)
				.OrderByDescending(f => f.hit_count)
				.Dump("Function flamegraph");
		}
		else
		{
			dc_flamegraph.Content = pc_flamegraph;
		}


	}
	return (cpu, instrcount);
}

string GetFunctionName(int address)
{
	var func_idx = PartitionPoint(_functionMap, f => f.end <= address);
	if (func_idx < _functionMap.Length && address >= _functionMap[func_idx].start)
		return _functionMap[func_idx].name;
	return null;
}

static int PartitionPoint<T>(T[] array, Func<T, bool> predicate) =>
	PartitionPoint(array.AsSpan(), predicate);

/// <summary>Works like Rust's partition_point().
/// </summary>
/// <remarks>
/// Given that there is an index _i_ in @span such that:
/// - predicate(span[j]) == true for all 0 <= j < i, and
/// - predicate(span[j]) == false for all j >= i,
/// 
/// returns i.
/// 
/// As a consequence, if predicate is false for all elements, returns 0,
/// while if predicate is true for all elements, returns span.Length.
/// </remarks>
static int PartitionPoint<T>(ReadOnlySpan<T> span, Func<T, bool> predicate)
{
	var size = span.Length;
	if (size == 0) return size;
	int b = 0;
	// based on Rust's slice.binary_search_by
	while (size > 1)
	{
		var half = size / 2;
		var mid = b + half;
		var cmp = predicate(span[mid]) ? -1 : 1;
		b = (cmp > 0) ? b : mid;
		size -= half;
	}
	// note: size == 1
	Debug.Assert(size == 1);
	var final_cmp = predicate(span[b]) ? -1 : 1;
	var final_result = b + ((final_cmp < 0) ? 1 : 0);
	return final_result;
}

readonly struct IntcodeStringInput<memval_t> : IIntcodeInput<memval_t>
	where memval_t : INumberBase<memval_t>
{
	readonly string _input;
	readonly int _offset;

	public IntcodeStringInput(string input, int offset = 0) { _input = input; _offset = offset; }

	public IIntcodeInput<memval_t> Dequeue(out memval_t value)
	{
		var offset = _offset;

		char ch = default;
		do
		{
			if (offset >= _input.Length) { value = default; break; }
			ch = _input[offset++];
		} while (ch == '\0');
		value = memval_t.CreateChecked((ushort)ch);

		/*
				if (TRACE_INPUT)
					ExecutionLogFile?.WriteLine("Read character: {0}",
							(value < 0x20) ? $"0x{value:x2}" : "'" + char.ConvertFromUtf32((int)value) + "'"
						);
						*/

		return new IntcodeStringInput<memval_t>(_input, offset);
	}
}

interface IIntcodeInput<memval_t>
{
	IIntcodeInput<memval_t> Dequeue(out memval_t value);
}

class IntcodeCpu<memval_t>
	where memval_t : IComparable<memval_t>,
					IEquatable<memval_t>,
					IAdditionOperators<memval_t, memval_t, memval_t>,
					IMultiplyOperators<memval_t, memval_t, memval_t>,
					// I don't actually need full INumberBase but that's the only way to get conversion (via CreateChecked)
					INumberBase<memval_t>
{
	const int OP_ADD = 1;
	const int OP_MULT = 2;
	const int OP_INPUT = 3;
	const int OP_OUTPUT = 4;
	const int OP_JTRUE = 5;
	const int OP_JFALSE = 6;
	const int OP_JLT = 7;
	const int OP_JE = 8;
	const int OP_SETTOC = 9;

	const int OP_HALT = 99;

	public bool SingleStepMode { get; set; }

	public IntcodeCpu<memval_t> Clone()
	{
		return new IntcodeCpu<memval_t>((memval_t[])(Memory.Clone()), Input, Output, Pc, Toc);
	}

	public Func<IntcodeCpu<memval_t>, int, string> DescribeToc;
	Dictionary<int, Action<IntcodeCpu<memval_t>>> _breakpoints = new Dictionary<int, Action<IntcodeCpu<memval_t>>>();
	public void AddBreakpoint(string name, Action<IntcodeCpu<memval_t>> callback)
		=> AddBreakpoint(_name2Addr[name], callback);

	public void AddBreakpoint(int address, Action<IntcodeCpu<memval_t>> callback)
	{
		_breakpoints.Add(address, callback);
	}

	void TriggerBreakpoints()
	{
		if (_breakpoints.TryGetValue(Pc, out var callback))
		{
			ExecutionLogFile?.Flush();
			callback(this);
			ExecutionLogFile?.Flush();
		}
		if (SingleStepMode)
		{
			ExecutionLogFile?.Flush();
			Util.Break();
		}
	}

	public memval_t[] Memory { get; private set; }
	public IIntcodeInput<memval_t> Input { get; private set; }
	public Action<memval_t> Output { get; private set; }
	public int Toc { get; private set; }
	public int Pc { get; private set; }
	public bool IsHalted => Pc < 0;

	ExecutionTrace _trace;

	public IntcodeCpu(memval_t[] memory, IIntcodeInput<memval_t> input, Action<memval_t> output, int pc, int toc) : this(memory, input, output, pc, toc, null) { }

	// really wish I had a Self pseudo-type like Rust does
	public IntcodeCpu<memval_t> Patch(params (int offset, memval_t value)[] patches)
	{
		var mem = Memory;
		foreach (var patch in patches)
		{
			mem[patch.offset] = patch.value;
		}
		return this;
	}

	public IntcodeCpu<memval_t> WithIO(IIntcodeInput<memval_t> input, Action<memval_t> output)
	{
		this.Input = input;
		this.Output = output;
		return this;
	}

	public object ToDump()
	{
		var sb = new StringBuilder("<pre>");
		if (IsHalted) sb.Append("[H] ");

		var lastIns = _trace?.lastIns ?? (-1, -1, -1);

		int idx = 0;
		foreach (var value in Memory)
		{
			var i = idx++;
			if (i > 0) sb.Append(',');
			var isSpan = false;
			if (i == Pc || i == lastIns.a || i == lastIns.b || i == lastIns.c)
			{
				isSpan = true;
				sb.Append("<span class='");
				if (i == Pc) sb.Append("pc ");
				if (i == Toc) sb.Append("toc ");
				if (i == lastIns.a || i == lastIns.b) sb.Append("src ");
				if (i == lastIns.c) sb.Append("dest ");

				sb.Append("'>");
			}
			sb.Append(value);
			if (isSpan) sb.Append("</span>");
		}
		sb.Append("</pre>");

		return Util.RawHtml(sb.ToString());
	}

	IntcodeCpu(memval_t[] memory, IIntcodeInput<memval_t> input, Action<memval_t> output, int pc, int toc, ExecutionTrace trace)
	{
		Memory = memory;
		Input = input;
		Output = output;
		Pc = pc;
		Toc = toc;

		_trace = trace;
	}

	IntcodeCpu<memval_t> HaltOp(ExecutionTrace trace, AddressingModeData directMask)
	{
		this.Pc = -1 - Pc;
		this._trace = trace;
		return this;
	}

	IntcodeCpu<memval_t> SetTocOp(ExecutionTrace trace, AddressingModeData directMask)
	{
		var pc = Pc + 1;
		var pa = Memory[pc++];

		var (a, a_ptr) = ReadValue(directMask[0], pa);
		var a_int = int.CreateChecked(a);
		int newToc = Toc + a_int;
		trace.SetArguments(newToc);

		if (ExecutionLogFile != null)
		{
			var sb = new StringBuilder();
			sb.Append("New RB = ").Append(newToc);
			var toc_description = DescribeToc?.Invoke(this, newToc);
			if (toc_description != null)
				sb.Append(" (").Append(toc_description).Append(")");

			ExecutionLogFile.LogExecutionMessage(sb.ToString());
		}

		this.Pc = pc;
		this.Toc = newToc;
		this._trace = trace;
		return this;
	}


	[Flags]
	enum ParamName
	{
		None = 0,
		Param1 = 1,
		Param2 = 2,
		Param3 = 4,
	}

	(AddressingModeData amd, int op) DecodeArg0(memval_t _n)
	{
		var n = int.CreateChecked(_n);
		int am_mask = 0;
		int op = n % 100;
		n /= 100;
		int shift = 0;
		while (n > 0)
		{
			int mode = n % 10;
			am_mask |= (mode << shift);
			shift += 2;
			n /= 10;
		}
		return (new AddressingModeData(am_mask), op);
	}

	enum AddressingMode
	{
		Absolute = 0,
		Immediate = 1,
		Relative = 2,
	}

	readonly struct AddressingModeData
	{
		readonly int _value;
		public AddressingModeData(int value)
		{
			_value = value;
		}
		public AddressingMode this[int paramIndex]
		{
			get
			{
				var shift = paramIndex * 2;
				return (AddressingMode)((_value >> shift) & 0x03);
			}
		}
	}

	int GetAddress(AddressingMode mode, memval_t ins_value) => GetAddress(mode, int.CreateChecked(ins_value));

	int GetAddress(AddressingMode mode, int ins_value)
	{
		switch (mode)
		{
			case AddressingMode.Relative: return Toc + ins_value;
			case AddressingMode.Absolute:
			default:
				return ins_value;
		}
	}

	(memval_t value, int address) ReadValue(AddressingMode mode, memval_t ins_value)
	{
		if (mode == AddressingMode.Immediate)
			return (ins_value, -1);
		int addr = int.CreateChecked(ins_value);
		switch (mode)
		{
			case AddressingMode.Relative:
				addr += Toc;
				goto default;
			default:
			case AddressingMode.Absolute:
				if (addr >= Memory.Length)
					return (default(memval_t), addr);
				if (addr < 0) throw new Exception("attempt to access negative memory address");
				return (Memory[addr], addr);
		}
	}

	class ExecutionTrace
	{
		public (int a, int b, int c) lastIns;
		public void SetArguments(int a, int b = -1, int r = -1)
		{
			lastIns = (a, b, r);
		}
	}

	IntcodeCpu<memval_t> WriteMemory(int newPc, int dest, memval_t value, ExecutionTrace trace = null)
	{
		ExecutionLogFile?.LogExecutionMessage(string.Format("Storing {0} at address {1}", value, DescribeAddress(dest, Toc)));
		if (dest < 0) throw new Exception("attempt to write to negative memory address");
		if (MemoryBreakPoints.Contains(dest))
		{
			ExecutionLogFile?.Flush();
			Console.WriteLine("Write to memory address {0}", dest);
			Util.Break();
		}
		var mem = Memory;
		if (dest >= Memory.Length)
		{
			// resize memory
			var moreMem = new memval_t[dest + 1024];
			Array.Copy(Memory, moreMem, mem.Length);
			//Console.WriteLine("resizing from {0} to {1}", Memory.Length, moreMem.Length);
			Memory = mem = moreMem;
		}

		mem[dest] = value;
		this.Pc = newPc;
		this._trace = trace;
		return this;
	}

	IntcodeCpu<memval_t> ConsumeInput(int newPc, int dest, ExecutionTrace trace = null)
	{
		Input = Input.Dequeue(out var value);
		return WriteMemory(newPc, dest, value, trace);
		/*
		Memory[dest] = value;
		Pc = newPc;
		this._trace = trace;
		return this;
		*/
	}

	// instruction arguments are: (&in1|in1), (&in2|in2), &dest
	IntcodeCpu<memval_t> Alu3(ExecutionTrace trace, AddressingModeData direct, Func<memval_t, memval_t, memval_t> calc)
	{
		var pc = Pc + 1;
		var pa = Memory[pc++];
		var pb = Memory[pc++];
		var pr = GetAddress(direct[2], Memory[pc++]);

		var (a, a_ptr) = ReadValue(direct[0], pa);
		var (b, b_ptr) = ReadValue(direct[1], pb);

		trace?.SetArguments(a_ptr, b_ptr, pr);

		var c = calc(a, b);

		return WriteMemory(pc, pr, c, trace);
	}

	// instruction arguments are: &dest
	IntcodeCpu<memval_t> ReadInput(ExecutionTrace trace, AddressingModeData direct)
	{
		var pc = Pc + 1;
		var pr = GetAddress(direct[0], Memory[pc++]);

		trace?.SetArguments(-1, -1, pr);

		return ConsumeInput(pc, pr, trace);
	}

	// instruction arguments are: &dest
	IntcodeCpu<memval_t> WriteOutput(ExecutionTrace trace, AddressingModeData direct)
	{
		var pc = Pc + 1;
		var pa = Memory[pc++];

		var (a, a_ptr) = ReadValue(direct[0], pa);

		trace.SetArguments(a_ptr);

		ExecutionLogFile?.WriteLine("Output: {0}", a);

		//a.Dump("OUTPUT");
		//		_last_output = a;
		Output(a);

		Pc = pc;
		_trace = trace;
		return this;
	}

	// instruction arguments are: (@test_value, @newpc)
	IntcodeCpu<memval_t> BoolOp(ExecutionTrace trace, AddressingModeData direct, bool expected_truth)
	{
		var pc = Pc + 1;
		var pa = Memory[pc++];
		var pdest = Memory[pc++];

		var (a, a_ptr) = ReadValue(direct[0], pa);

		trace.SetArguments(a_ptr, -1, GetAddress(direct[1], pdest));

		if (expected_truth == !a.Equals(memval_t.Zero))
		{
			var (newPc, _) = ReadValue(direct[1], pdest);
			pc = int.CreateChecked(newPc);
		}

		Pc = pc;
		_trace = trace;
		return this;
	}

	// instruction arguments are: (@arg1, @arg2, @dest)
	IntcodeCpu<memval_t> CompareOp(ExecutionTrace trace, AddressingModeData direct, Func<memval_t, memval_t, bool> cmp)
	{
		var pc = Pc + 1;
		var pa = Memory[pc++];
		var pb = Memory[pc++];
		var pdest = Memory[pc++];

		var (a, a_ptr) = ReadValue(direct[0], pa);
		var (b, b_ptr) = ReadValue(direct[1], pb);
		var dest_ptr = GetAddress(direct[2], pdest);


		trace.SetArguments(a_ptr, b_ptr, dest_ptr);

		var value = cmp(a, b) ? memval_t.One : memval_t.Zero;
		return WriteMemory(pc, dest_ptr, value, trace);
	}

	static readonly Dictionary<int, (string opname, int argcount)> OpDecodeData = new Dictionary<int, (string, int)>(){
			{ OP_ADD, ("add", 3) },
			{ OP_MULT, ("mul", 3) },
			{ OP_INPUT, ("in", 1) },
			{ OP_OUTPUT, ("out", 1) },
			{ OP_HALT, ("hlt", 0) },
			{ OP_JTRUE, ("jt", 2) },
			{ OP_JFALSE, ("jf", 2) },
			{ OP_JLT, ("lt", 3) },
			{ OP_JE, ("eq", 3) },
			{ OP_SETTOC, ("rbo", 1) },
		};

	void TraceInstruction(AddressingModeData mask, int ins)
	{
		var sb = new StringBuilder();
		sb.Append('[').Append(Pc.ToString("00000")).Append("] ");
		if (!OpDecodeData.TryGetValue(ins, out var opInfo))
		{
			sb.Append("(ERROR: INVALID OPCODE ").Append(ins).Append(")");
		}
		else
		{
			sb.Append(opInfo.opname);
			for (int i = 0; i < opInfo.argcount; i++)
			{
				memval_t value = Memory[Pc + 1 + i];
				sb.Append(' ');
				switch (mask[i])
				{
					case AddressingMode.Absolute: sb.Append('[').Append(value).Append(']'); break;
					case AddressingMode.Immediate: sb.Append('#').Append(value); break;
					case AddressingMode.Relative: sb.Append("[rb").Append(value.ToString("+#;-#;+#", CultureInfo.InvariantCulture)).Append(']'); break;
					default: throw new NotImplementedException();
				}
			}
		}
		//Console.WriteLine(sb.ToString());
		ExecutionLogFile?.LogExecutionMessage(sb.ToString());
	}

	public IntcodeCpu<memval_t> ExecuteInstruction()
	{
		TriggerBreakpoints();

		if (Pc < 0 || Pc >= Memory.Length) throw new Exception("PC out of bounds: " + Pc);
		var ins_arg0 = Memory[Pc];
		var trace = new ExecutionTrace();
		var (directMask, ins) = DecodeArg0(ins_arg0);
		//if (ins == OP_SETTOC)
		TraceInstruction(directMask, ins);
		switch (ins)
		{
			case OP_ADD: return Alu3(trace, directMask, (a, b) => checked(a + b));
			case OP_MULT: return Alu3(trace, directMask, (a, b) => checked(a * b));
			case OP_INPUT: return ReadInput(trace, directMask);
			case OP_OUTPUT: return WriteOutput(trace, directMask);
			case OP_HALT: return HaltOp(trace, directMask);
			case OP_JTRUE: return BoolOp(trace, directMask, true);
			case OP_JFALSE: return BoolOp(trace, directMask, false);
			case OP_JLT: return CompareOp(trace, directMask, (a, b) => a.CompareTo(b) < 0);
			case OP_JE: return CompareOp(trace, directMask, (a, b) => a.Equals(b));
			case OP_SETTOC: return SetTocOp(trace, directMask);
			default:
				throw new InvalidOperationException($"Invalid CPU opcode {ins} at pc={Pc}");
		}

	}
}

IntcodeCpu<memval_t> ParseInput<memval_t>(TextReader tr)
	where memval_t : IComparable<memval_t>,
					IEquatable<memval_t>,
					IAdditionOperators<memval_t, memval_t, memval_t>,
					IMultiplyOperators<memval_t, memval_t, memval_t>,
					// I don't actually need full INumberBase but that's the only way to get conversion
					INumberBase<memval_t>
{
	using (tr)
	{
		var memory_text = tr.ReadToEnd();

		//var memory = ImmutableArray.CreateRange(memory_text.Split(',').Select(n => memval_t.Parse(n)));
		var builder = new List<memval_t>();
		builder.AddRange(memory_text.Split(',').Select(n => memval_t.Parse(n, NumberStyles.Integer, CultureInfo.InvariantCulture)));
		builder.EnsureCapacity(builder.Count + 1024);
		for (int i = 0; i < 1024; i++) builder.Add(default(memval_t));
		var memory = builder.ToArray();


		return new IntcodeCpu<memval_t>(memory, new IntcodeStringInput<memval_t>(""), null, 0, 0);
	}
}
