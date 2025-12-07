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

const string OUTPUT_LOG_NAME = "output.txt";
const string INPUT_FILE_PATH =
	"example.txt"
	//	"../../puzzle_inputs/2025-06.txt"
	;
const int INSTRUCTION_LIMIT =
	//1_000_000_000
	50_000
	;

static string WorkDir;
void Main()
{
	Util.NewProcess = true;
	var dir = WorkDir = Path.Combine(Path.GetDirectoryName(Util.CurrentQueryPath),
		"."
	);
	var code_file = File.ReadAllText(Path.Combine(dir, "a.intcode"));
	LoadMemoryMap(Path.Combine(dir, "a.map"));

	var cpu = ParseInput(new StringReader(
			code_file
		));

	cpu.AddBreakpoint("loop_add", _ =>
	{
		using var bpout = new BreakpointOutput();
		int first_row_address = _name2Addr["first_row"];
		int num_columns = (int)ReadVariable(cpu, "num_columns");
		int table_size = (int)ReadVariable(cpu, "table_size");
		bpout.WriteLine("loop_add: adding table[{0}] = {1} to p1_accum", cpu.Toc - first_row_address - 2 * num_columns, cpu.Memory[cpu.Toc]);
	});
	cpu.AddBreakpoint("loop_mul", _ =>
	{
		using var bpout = new BreakpointOutput();
		int first_row_address = _name2Addr["first_row"];
		int num_columns = (int)ReadVariable(cpu, "num_columns");
		int table_size = (int)ReadVariable(cpu, "table_size");
		bpout.WriteLine("loop_mul: multiplying table[{0}] = {1} to p1_accum", cpu.Toc - first_row_address - 2 * num_columns, cpu.Memory[cpu.Toc]);
	});


	cpu.AddBreakpoint("scan_first_row", _ =>
{
	using var bpout = new BreakpointOutput();
	bpout.WriteLine("Processing column {0}", ReadVariable(cpu, "num_columns") - ReadVariable(cpu, "r0_chars_left"));
	bpout.WriteLine("rb = {0}", cpu.Toc);
	bpout.WriteLine("Character: '{0}'", (char)cpu.Memory[cpu.Toc]);
});

	//cpu.AddBreakpoint("debug_checkpoint_1", c => Year2025Day6_PrintState(c, "debug_checkpoint_1"));
	cpu.AddBreakpoint("scan_next_row", c => Year2025Day6_PrintState(c, "scan_next_row"));
	cpu.AddBreakpoint("found_operator_row", c => Year2025Day6_PrintState(c, "found_operator_row"));

	StreamWriter stdout_file;
	if (WRITE_OUTPUT_LOG)
	{
		stdout_file = new StreamWriter(Path.Combine(dir, OUTPUT_LOG_NAME), false);
		OutputTextWriter = stdout_file;
		OutputLiterals = true;
	}
	else
	{
		stdout_file = null;
	}
	using var tmp = stdout_file;

	cpu = cpu.Patch(
		//(3, 1) // raw output mode
		//,(5, 1) // debug_range_build
		);

	var day3_input = File.ReadAllText(Path.Combine(dir, INPUT_FILE_PATH));

	cpu = cpu.WithIO(new IntcodeInput(day3_input),
		PrintOutput
	);

	try
	{
		if (WRITE_EXECLOG) ExecutionLogFile = new ExecutionLogFileWriter(new StreamWriter(
				Path.Combine(Path.GetDirectoryName(Util.CurrentQueryPath), @"intcode-execlog.txt"), false, new UTF8Encoding(false))
				);
		RunUntilHalt(cpu, instruction_limit: INSTRUCTION_LIMIT);
	}
	finally
	{
		//string.Join("\r\n", Last1000Instructions).Dump();
		ExecutionLogFile?.Dispose();
	}
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

}

memval_t ReadVariable(IntcodeCpu cpu, string name)
{
	return cpu.Memory[_name2Addr[name]];
}

void Year2025Day6_PrintState(IntcodeCpu cpu, string breakpointName)
{
	//cpu.Memory[cpu.Pc] = 99; // HLT
	var bpout = new BreakpointOutput();
	bpout.WriteLine("{0} breakpoint hit", breakpointName);
	bpout.WriteLine("");
	int first_row_address = _name2Addr["first_row"];
	int num_columns = (int)ReadVariable(cpu, "num_columns");
	int table_size = (int)ReadVariable(cpu, "table_size");
	foreach (var var_name in new string[] {
				"num_operations",
				"num_columns",
				"num_columns_inv",
				"table_size",
				"table_size_inv",
				"num_rows",
				})
		bpout.WriteLine("{0} = {1}", var_name, ReadVariable(cpu, var_name));
	bpout.WriteLine("");
	bpout.WriteLine("Part 2 column status: {0}",
				string.Join(" ", cpu.Memory.Skip(first_row_address).Take(num_columns)));
	bpout.WriteLine("Part 2 accumulators: {0}",
				string.Join(" ", cpu.Memory.Skip(first_row_address + num_columns).Take(num_columns)));
	bpout.WriteLine("Part 1 table: {0}",
				string.Join(" ", cpu.Memory.Skip(first_row_address + 2 * num_columns + 1).Take(table_size)));

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
static Dictionary<string, int> _name2Addr;

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
	if (print_exectime_info)
	{
		instrcount.Dump("Number of instructions executed");
		_instructionHitCount.OrderBy(x => x.Key).Select(x => new { pc = x.Key, pc_desc = DescribeAddress(x.Key, cpu.Toc), count = x.Value.Value })
			.Dump("PC flamegraph", collapseTo: 0);
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

	public bool SingleStepMode { get; set; }

	public IntcodeCpu Clone()
	{
		return new IntcodeCpu((memval_t[])(Memory.Clone()), Input, Output, Pc, Toc);
	}

	Dictionary<int, Action<IntcodeCpu>> _breakpoints = new Dictionary<int, System.Action<UserQuery.IntcodeCpu>>();
	public void AddBreakpoint(string name, Action<IntcodeCpu> callback)
		=> AddBreakpoint(_name2Addr[name], callback);

	public void AddBreakpoint(int address, Action<IntcodeCpu> callback)
	{
		_breakpoints.Add(address, callback);
	}

	void TriggerBreakpoints()
	{
		if (_breakpoints.TryGetValue(Pc, out var callback))
			callback(this);
		if (SingleStepMode)
		{
			ExecutionLogFile?.Flush();
			Util.Break();
		}
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
				if (addr >= Memory.Length)
					return (default(memval_t), addr);
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
		TriggerBreakpoints();

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
