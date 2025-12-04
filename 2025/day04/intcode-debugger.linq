<Query Kind="Program">
  <NuGetReference>System.Collections.Immutable</NuGetReference>
  <Namespace>memval_t = System.Int64</Namespace>
  <Namespace>System.Collections.Concurrent</Namespace>
  <Namespace>System.Collections.Immutable</Namespace>
  <Namespace>System.Threading.Tasks</Namespace>
  <Namespace>System.Globalization</Namespace>
</Query>

const bool WRITE_EXECLOG = true;
const bool TRACE_INPUT = true;
const bool WRITE_OUTPUT_LOG = false;

static string WorkDir;
void Main()
{
	Util.NewProcess = true;
	var dir = WorkDir = Path.Combine(Path.GetDirectoryName(Util.CurrentQueryPath), 
		"." //@"../2025/day02"
	);
	var code_file = File.ReadAllText(Path.Combine(dir, "a.intcode"));
	_memoryMap = File.ReadAllLines(Path.Combine(dir, "a.map"))
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

	var cpu = ParseInput(new StringReader(
			code_file
		));

	StreamWriter stdout_file;
	if (WRITE_OUTPUT_LOG)
	{
		stdout_file = new StreamWriter(Path.Combine(dir, "output.txt"), false);
		OutputTextWriter = stdout_file;
		OutputLiterals = true;
	}
	else
	{
		stdout_file = null;
	}
	using var tmp = stdout_file;

	cpu = cpu.Patch(
		//(6, 1)   // debug mode
		//,(9, 1)  // with line numbers
		);
	//cpu = cpu.Patch( (4, 1), (6, 1995), (7, 1));
	//	cpu = cpu.Patch( (4, 1), (6, 1995), (7, 1));

	var day3_input = File.ReadAllText(
		//Path.Combine(dir, "example.txt")
		//Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), @"LINQPad Queries\advent-of-code\2025\day03.txt")
		Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), @"aoc-gh\2025\day03.txt")
		);
	
	//day3_input = "2444352216545122355224492942447515152244455161432542324549291845752525553324354454533245436254745426\n";

	cpu = cpu.WithIO(new IntcodeInput(day3_input),
		PrintOutput
	);

	try
	{
		//	cpu = cpu.Patch((1, 1));

		if (WRITE_EXECLOG) ExecutionLogFile = new ExecutionLogFileWriter(new StreamWriter(
				Path.Combine(Path.GetDirectoryName(Util.CurrentQueryPath), @"intcode-execlog.txt"), false, new UTF8Encoding(false))
				);
		//Console.WriteLine("PART 1");
		RunUntilHalt(cpu, instruction_limit: 1_000_000_000);
		/*
		Console.WriteLine("-----------------");
		cpu = cpu.Patch((1, 1));
		Console.WriteLine("PART 2");
		RunUntilHalt(cpu);
		*/
	}
	finally
	{
		//string.Join("\r\n", Last1000Instructions).Dump();
		ExecutionLogFile?.Dispose();
	}
}

void DebugDay22PatternMemory(IntcodeCpu initial_cpu, string day22_prices)
{
	using var stdout_file = new StreamWriter(Path.Combine(WorkDir, "pattern_mem.txt"), false);

	int counter = 0;
	var pricelists = day22_prices.Split(Environment.NewLine + Environment.NewLine);
	foreach (var pricelist in pricelists)
	{
		if (WRITE_EXECLOG) ExecutionLogFile = new ExecutionLogFileWriter(new StreamWriter(
				Path.Combine(Path.GetDirectoryName(Util.CurrentQueryPath), @"intcode-execlog.txt"), false, new UTF8Encoding(false))
				);

		var cpu = initial_cpu.Clone().WithIO(
				new IntcodeInput(pricelist),
				PrintOutput
		);

		RunUntilHalt(cpu, false);
		for (int i = 0; i < 260642; i++)
		{
			stdout_file.WriteLine(cpu.Memory[1267 + i]);
		}
		stdout_file.WriteLine();
		stdout_file.Flush();
		counter++;
		ExecutionLogFile?.Dispose();
		Console.WriteLine("Wrote {0} pattern tables", counter);
	}
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

static (int address, string name)[] _memoryMap;
static Dictionary<int, string> _addr2Name;

static string DescribeAddress(int address, int rb)
{
	if (_memoryMap == null || _memoryMap.Length == 0) return address.ToString();
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
	var index = Array.BinarySearch(_memoryMap, 0, _memoryMap.Length, (address, default), AddressComparer);
	if (index < 0) index = (~index) - 1;
	var addr_str = address.ToString();
	if (index < 0) return addr_str;
	var sb = new StringBuilder(addr_str);
	var obj = _memoryMap[index];
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
void PrintOutput(memval_t value)
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

static HashSet<int> Breakpoints = new HashSet<int>()
{
	//528, 798,
	//93, 
	//147,
};

static HashSet<int> MemoryBreakPoints = new HashSet<int>()
{
	//2166,
};

class Indirect<T> { public T Value; }
static Dictionary<int, Indirect<long>> _instructionHitCount = new Dictionary<int, Indirect<long>>();

IntcodeCpu RunUntilHalt(IntcodeCpu cpu, bool print_exectime_info = true, int instruction_limit = 0)
{
	long limit = long.MaxValue;// 1_000_000_000;
	long instrcount = 0;
	while (!cpu.IsHalted)
	{
		// this would've been a oneliner in Rust. meh.
		if (!_instructionHitCount.TryGetValue(cpu.Pc, out var holder))
			holder = _instructionHitCount[cpu.Pc] = new();
		holder.Value++;

		if (Breakpoints.Contains(cpu.Pc))
		{
			ExecutionLogFile?.Flush();
			Console.WriteLine("Breakpoint at PC={0}", cpu.Pc);
			Util.Break();
		}
		if (instruction_limit > 0 && --instruction_limit == 0)
		{
			break;
		}
		if (--limit <= 0) throw new Exception("cpu rlimit exceeded");
		if (ExecutionLogFile != null && _addr2Name.TryGetValue(cpu.Pc, out var name))
			ExecutionLogFile.WriteLine("## {0}", name);
		//cpu.Dump();
		cpu = cpu.ExecuteInstruction();
		instrcount++;
		//cpu.Dump();
	}
	if (print_exectime_info)
	{
		instrcount.Dump("Number of instructions executed");
		_instructionHitCount.OrderBy(x => x.Key).Select(x => new { pc = x.Key, pc_desc = DescribeAddress(x.Key, cpu.Toc), count = x.Value.Value }).Dump("PC flamegraph");
	}
	return cpu;
}


readonly struct IntcodeInput
{
	readonly string _input;
	readonly int _offset;

	public IntcodeInput(string input, int offset = 0) { _input = input; _offset = offset; }

	public static IntcodeInput Empty => new IntcodeInput("", 0);

	public IntcodeInput Dequeue(out memval_t value)
	{
		var offset = _offset;

		do
		{
			if (offset >= _input.Length) { value = 0; break; }
			value = _input[offset++];
		} while (value == '\0');

		if (TRACE_INPUT)
			ExecutionLogFile?.WriteLine("Read character: {0}",
					(value < 0x20) ? $"0x{value:x2}" : "'" + char.ConvertFromUtf32((int)value) + "'"
				);

		return new IntcodeInput(_input, offset);
	}
}

class IntcodeCpu
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

	public IntcodeCpu Clone()
	{
		return new IntcodeCpu((memval_t[])(Memory.Clone()), Input, Output, Pc, Toc);
	}

	public memval_t[] Memory { get; private set; }
	public IntcodeInput Input { get; private set; }
	public Action<memval_t> Output { get; private set; }
	public int Toc { get; private set; }
	public int Pc { get; private set; }
	public bool IsHalted => Pc < 0;

	ExecutionTrace _trace;

	public IntcodeCpu(memval_t[] memory, IntcodeInput input, Action<memval_t> output, int pc, int toc) : this(memory, input, output, pc, toc, null) { }

	public IntcodeCpu Patch(params (int offset, memval_t value)[] patches)
	{
		var mem = Memory;
		foreach (var patch in patches)
		{
			mem[patch.offset] = patch.value;
		}
		return this;
	}

	public IntcodeCpu WithIO(IntcodeInput input, Action<memval_t> output)
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

	IntcodeCpu(memval_t[] memory, IntcodeInput input, Action<memval_t> output, int pc, int toc, ExecutionTrace trace)
	{
		Memory = memory;
		Input = input;
		Output = output;
		Pc = pc;
		Toc = toc;

		_trace = trace;
	}

	IntcodeCpu HaltOp(ExecutionTrace trace, AddressingModeData directMask)
	{
		this.Pc = -1 - Pc;
		this._trace = trace;
		return this;
	}

	IntcodeCpu SetTocOp(ExecutionTrace trace, AddressingModeData directMask)
	{
		var pc = Pc + 1;
		var pa = Memory[pc++];

		var (a, a_ptr) = ReadValue(directMask[0], pa);
		var newToc = (int)(Toc + a);
		trace.SetArguments(newToc);

		ExecutionLogFile?.LogExecutionMessage($"New TOC = {newToc}");

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
		var n = (int)_n;
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

	int GetAddress(AddressingMode mode, memval_t ins_value)
	{
		switch (mode)
		{
			case AddressingMode.Relative: return (int)(Toc + ins_value);
			case AddressingMode.Absolute:
			default:
				return (int)ins_value;
		}
	}

	(memval_t value, int address) ReadValue(AddressingMode mode, memval_t ins_value)
	{
		switch (mode)
		{
			default:
			case AddressingMode.Absolute:
				return (Memory[(int)ins_value], (int)ins_value);
			case AddressingMode.Immediate:
				return (ins_value, -1);
			case AddressingMode.Relative:
				int addr = (int)(ins_value + Toc);
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

	IntcodeCpu WriteMemory(int newPc, int dest, memval_t value, ExecutionTrace trace = null)
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

	IntcodeCpu ConsumeInput(int newPc, int dest, ExecutionTrace trace = null)
	{
		Input = Input.Dequeue(out var value);
		Memory[dest] = value;
		Pc = newPc;
		this._trace = trace;
		return this;
	}

	// instruction arguments are: (&in1|in1), (&in2|in2), &dest
	IntcodeCpu Alu3(ExecutionTrace trace, AddressingModeData direct, Func<memval_t, memval_t, memval_t> calc)
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
	IntcodeCpu ReadInput(ExecutionTrace trace, AddressingModeData direct)
	{
		var pc = Pc + 1;
		var pr = GetAddress(direct[0], Memory[pc++]);

		trace?.SetArguments(-1, -1, pr);

		return ConsumeInput(pc, pr, trace);
	}

	// instruction arguments are: &dest
	IntcodeCpu WriteOutput(ExecutionTrace trace, AddressingModeData direct)
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
	IntcodeCpu BoolOp(ExecutionTrace trace, AddressingModeData direct, bool expected_truth)
	{
		var pc = Pc + 1;
		var pa = Memory[pc++];
		var pdest = Memory[pc++];

		var (a, a_ptr) = ReadValue(direct[0], pa);

		trace.SetArguments(a_ptr, -1, GetAddress(direct[1], pdest));

		if (expected_truth == (a != 0))
		{
			var (newPc, _) = ReadValue(direct[1], pdest);
			pc = (int)newPc;
		}

		Pc = pc;
		_trace = trace;
		return this;
	}

	// instruction arguments are: (@arg1, @arg2, @dest)
	IntcodeCpu CompareOp(ExecutionTrace trace, AddressingModeData direct, Func<memval_t, memval_t, bool> cmp)
	{
		var pc = Pc + 1;
		var pa = Memory[pc++];
		var pb = Memory[pc++];
		var pdest = Memory[pc++];

		var (a, a_ptr) = ReadValue(direct[0], pa);
		var (b, b_ptr) = ReadValue(direct[1], pb);
		var dest_ptr = GetAddress(direct[2], pdest);


		trace.SetArguments(a_ptr, b_ptr, dest_ptr);

		var value = cmp(a, b) ? 1 : 0;
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
					case AddressingMode.Relative: sb.Append("[rb").Append(value.ToString("+#;-#;+#")).Append(']'); break;
					default: throw new NotImplementedException();
				}
			}
		}
		//Console.WriteLine(sb.ToString());
		ExecutionLogFile?.LogExecutionMessage(sb.ToString());
	}

	public IntcodeCpu ExecuteInstruction()
	{
		var ins_arg0 = Memory[Pc];
		var trace = new ExecutionTrace();
		var (directMask, ins) = DecodeArg0(ins_arg0);
		//if (ins == OP_SETTOC)
		TraceInstruction(directMask, ins);
		switch (ins)
		{
			case OP_ADD: return Alu3(trace, directMask, (a, b) => unchecked(a + b));
			case OP_MULT: return Alu3(trace, directMask, (a, b) => unchecked(a * b));
			case OP_INPUT: return ReadInput(trace, directMask);
			case OP_OUTPUT: return WriteOutput(trace, directMask);
			case OP_HALT: return HaltOp(trace, directMask);
			case OP_JTRUE: return BoolOp(trace, directMask, true);
			case OP_JFALSE: return BoolOp(trace, directMask, false);
			case OP_JLT: return CompareOp(trace, directMask, (a, b) => a < b);
			case OP_JE: return CompareOp(trace, directMask, (a, b) => a == b);
			case OP_SETTOC: return SetTocOp(trace, directMask);
			default:
				throw new InvalidOperationException($"Invalid CPU opcode {ins} at pc={Pc}");
		}

	}
}


// Define other methods and classes here

IntcodeCpu ParseInput(TextReader tr)
{
	using (tr)
	{
		var memory_text = tr.ReadToEnd();

		//var memory = ImmutableArray.CreateRange(memory_text.Split(',').Select(n => memval_t.Parse(n)));
		var builder = new List<memval_t>();
		builder.AddRange(memory_text.Split(',').Select(n => memval_t.Parse(n)));
		builder.EnsureCapacity(builder.Count + 1024);
		for (int i = 0; i < 1024; i++) builder.Add(default(memval_t));
		var memory = builder.ToArray();


		return new IntcodeCpu(memory, IntcodeInput.Empty, null, 0, 0);
	}
}


StreamReader OpenDataFile()
{
	var queryName = Util.CurrentQueryPath;
	string dayNumber = Regex.Match(queryName, @"(day\d+)").Groups[1].Value;
	var dir = Path.GetDirectoryName(queryName);
	var data_file = Path.Combine(dir, dayNumber + ".txt");
	return new StreamReader(data_file);
}


const string EXAMPLE_1 =
	@"109,1,204,-1,1001,100,1,100,1008,100,16,101,1006,101,0,99";
const string EXAMPLE_2 =
	@"1102,34915192,34915192,7,4,7,99,0";
const string EXAMPLE_3 =
	@"104,1125899906842624,99";

IEnumerable<TextReader> GetSampleInputs()
{
	foreach (var input in new[] {
					EXAMPLE_1,
					EXAMPLE_2,
					EXAMPLE_3,
				})
		yield return new StringReader(input);
}

IEnumerable<TextReader> GetPuzzleInputs()
{
	using (var tr = OpenDataFile())
	{
		yield return tr;
	}
}