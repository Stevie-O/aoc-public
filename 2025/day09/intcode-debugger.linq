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
	//"../../puzzle_inputs/2025-09.txt"
	//"hostile-testcase-1.txt"
	//"hostile-testcase-2.txt"
	
	;
const int INSTRUCTION_LIMIT =
	//1_000_000_000
	100_000_000
	;
static (int address, memval_t value)[] memory_patches = {
	//(1, 10), // 1=part1_limit.  for the example, part 1 limit is 10, not 1000
};

static string WorkDir;
void Main()
{
	Util.NewProcess = true;
	ForceHalt = false;
	var dir = WorkDir = Path.Combine(Path.GetDirectoryName(Util.CurrentQueryPath),
		"."
	);
	var code_file = File.ReadAllText(Path.Combine(dir, "a.intcode"));
	LoadMemoryMap(Path.Combine(dir, "a.map"));

	var cpu = ParseInput(new StringReader(
			code_file
		));

	cpu.AddBreakpoint("assertion_failed", c =>
	{
		using var bpout = new BreakpointOutput();
		bpout.WriteLine();
		bpout.WriteLine("ASSERTION FAILURE ON LINE {0}", ReadVariable(c, "assertion_failed_line"));
		bpout.WriteLine();
		ForceHalt = true;
	});

	if (false) cpu.AddBreakpoint("fn_compute_gridh__write_run_length", c =>
	{
		using var bpout = new BreakpointOutput();
		var write_address = c.Toc;
		var grid_start = (int)ReadVariable(c, "grid_start");
		var grid_width = (int)ReadVariable(c, "grid_width");
		var grid_offset = write_address - grid_start;
		var run_length = ReadVariable(c, "fn_compute_gridh__run_length");
		bpout.WriteLine("Writing {0} to gridh row={1}, col={2}",
				run_length,
				grid_offset / grid_width,
				grid_offset % grid_width);
	});

	if (false) cpu.AddBreakpoint("fn_compute_gridv__write_run_length", c =>
	{
		using var bpout = new BreakpointOutput();
		var write_address = c.Toc;
		var grid_start = (int)ReadVariable(c, "grid_start");
		var grid_width = (int)ReadVariable(c, "grid_width");
		var grid_offset = write_address - grid_start;
		var run_length = ReadVariable(c, "fn_compute_gridv__run_length");
		bpout.WriteLine("Writing {0} to gridv row={1}, col={2}",
				run_length,
				grid_offset / grid_width,
				grid_offset % grid_width);
	});

	if (false) cpu.AddBreakpoint("fn_compute_answer__next_j", c =>
	{
		using var bpout = new BreakpointOutput();
		bpout.WriteLine("Checking: ({0}, {1}) - ({2}, {3})",
					ReadVariable(c, "fn_compute_answer__i_x"),
					ReadVariable(c, "fn_compute_answer__i_y"),
					c.Memory[c.Toc + 0],
					c.Memory[c.Toc + 1]
					);
	});
	if (false) cpu.AddBreakpoint("fn_compute_answer__goto_next_j", c =>
	{
		using var bpout = new BreakpointOutput();
		var sb = new StringBuilder();
		var rect_area = ReadVariable(c, "fn_compute_answer__rect_area");
		sb.AppendFormat("Rectangle area was {0}", rect_area);
		if (rect_area == ReadVariable(c, "part1_answer"))
			sb.Append(" (PART1 ANSWER)");
		if (rect_area == ReadVariable(c, "part2_answer"))
			sb.Append(" (PART2 ANSWER)");
		bpout.WriteLine(sb.ToString());
	});
	foreach (var prefix in new string[] { "top_line_", "bottom_line_", "left_line_", "right_line_" })
	{
if (false) 		cpu.AddBreakpoint("fn_compute_answer__" + prefix + "line_loop",
			c =>
			{ 
				using var bpout = new BreakpointOutput();
				var grid_start = (int)ReadVariable(c, "grid_start");
				var grid_width = (int)ReadVariable(c, "grid_width");
				var read_ptr = cpu.Toc + (int)ReadVariable(c, "fn_compute_answer__" + prefix + "grid_read_ptr");
				var grid_offset = read_ptr - grid_start;
				var sb = new StringBuilder();
				sb.AppendFormat(prefix + ": grid_read_ptr will access x={1}, y={0}", grid_offset / grid_width, grid_offset % grid_width);
				var value = cpu.Memory[read_ptr];
				sb.AppendFormat(" (value = {0}, empty_is_green = {1})", value, ReadVariable(c, "fn_compute_answer__empty_is_green"));
				bpout.WriteLine(sb.ToString());
			});
	}

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

	cpu = cpu.Patch(memory_patches);

	var input_text = File.ReadAllText(Path.Combine(dir, INPUT_FILE_PATH));

	cpu = cpu.WithIO(new IntcodeInput(input_text),
		PrintOutput
	);

	try
	{
		if (WRITE_EXECLOG) ExecutionLogFile = new ExecutionLogFileWriter(new StreamWriter(
				Path.Combine(Path.GetDirectoryName(Util.CurrentQueryPath), @"intcode-execlog.txt"), false, new UTF8Encoding(false))
				);
		RunUntilHalt(cpu, instruction_limit: INSTRUCTION_LIMIT);


		(new string[] {
			"red_tiles_size",
			"red_tiles_count",
			"tile_list_start",
			"tile_list_size",
			"x_map_start",
			"grid_width",
			"y_map_start",
			"grid_height",
			"grid_size",
			"grid_start",
			"top_tile_index",
			"perimeter_orientation",
			//"min_input_x_inv",
			//"min_input_y_inv",
			"min_input_x",
			"min_input_y",
			"max_input_x",
			"max_input_y",
		}).Select(var_name => new KeyValuePair<string, memval_t>(var_name, ReadVariable(cpu, var_name)))
			.Dump("Key globals");
		var red_tiles_address = _name2Addr["red_tiles"];
		var red_tiles_size = (int)ReadVariable(cpu, "red_tiles_size");
		cpu.Memory.Skip(red_tiles_address).Take(red_tiles_size).Chunk(2).Select((c, i) => $"[{red_tiles_address + 2 * i}] ({c[0]}, {c[1]})").Dump("tile coordinates");
		var grid_start = (int)ReadVariable(cpu, "grid_start");
		var grid_width = (int)ReadVariable(cpu, "grid_width");
		var grid_height = (int)ReadVariable(cpu, "grid_height");
		if (grid_width > 0)
		{
			var x_map_start = (int)ReadVariable(cpu, "x_map_start");
			cpu.Memory.Skip(x_map_start).Take(grid_width).Dump("X decompression map");
		}
		if (grid_height > 0)
		{
			var y_map_start = (int)ReadVariable(cpu, "y_map_start");
			cpu.Memory.Skip(y_map_start).Take(grid_height).Dump("Y decompression map");
		}
		var sb = new StringBuilder((grid_width + Environment.NewLine.Length) * grid_height);
		for (int r = 0, addr = grid_start; r < grid_height; r++)
		{
			for (int c = 0; c < grid_width; c++, addr++)
			{
				var val = cpu.Memory[addr];
				if (val == 0) sb.Append('.');
				else if (val < 0) sb.Append('E');
				else sb.Append('#');
			}
			sb.AppendLine();
		}
		sb.ToString().Dump("Graph");
		//cpu.Memory.Skip(_name2Addr["boxes"]).Take(size).Dump("boxes data");
	}
	finally
	{
		//string.Join("\r\n", Last1000Instructions).Dump();
		ExecutionLogFile?.Dispose();
	}
}

static bool ForceHalt = false;

void DebugTileCoordinates(string breakpoint_name, IntcodeCpu cpu)
{
	Console.WriteLine();
	Console.WriteLine("breakpoint: {0}", breakpoint_name);
	Console.WriteLine();
	var red_tiles_address = _name2Addr["red_tiles"];
	var red_tiles_size = (int)ReadVariable(cpu, "red_tiles_size");
	var tile_list_address = (int)ReadVariable(cpu, "tile_list_start");
	var tile_list_size = (int)ReadVariable(cpu, "tile_list_size");
	cpu.Memory.Skip(red_tiles_address).Take(red_tiles_size).Chunk(2).Select((c, i) => $"[{red_tiles_address + 2 * i}] ({c[0]}, {c[1]})").Dump("tile coordinates");


	cpu.Memory.Skip(tile_list_address).Take(tile_list_size)
		.Select((a, i) => new
		{
			list_index = i,
			list_item_address = tile_list_address + i,
			tile_address = a,
			tile_x = cpu.Memory[red_tiles_address + a],
			tile_y = cpu.Memory[red_tiles_address + a + 1],
		})
		.Dump("tile list");

	var grid_width = (int)ReadVariable(cpu, "grid_width");
	var grid_height = (int)ReadVariable(cpu, "grid_height");
	if (grid_width > 0)
	{
		var x_map_start = (int)ReadVariable(cpu, "x_map_start");
		cpu.Memory.Skip(x_map_start).Take(grid_width).Dump("X decompression map");
	}
	if (grid_height > 0)
	{
		var y_map_start = (int)ReadVariable(cpu, "y_map_start");
		cpu.Memory.Skip(y_map_start).Take(grid_height).Dump("Y decompression map");
	}

}

void DebugTileHeap(string breakpoint_name, IntcodeCpu cpu, BreakpointOutput bpout = null)
{
	using var bpout2 = (bpout == null ? new BreakpointOutput() : null);
	if (bpout == null) bpout = bpout2;
	bpout.WriteLine();
	bpout.WriteLine("breakpoint: {0}", breakpoint_name);
	bpout.WriteLine();
	var red_tiles_address = _name2Addr["red_tiles"];
	var red_tiles_size = (int)ReadVariable(cpu, "red_tiles_size");
	var tile_list_address = (int)ReadVariable(cpu, "tile_list_start");
	var tile_list_size = (int)ReadVariable(cpu, "tile_list_size");
	//cpu.Memory.Skip(red_tiles_address).Take(red_tiles_size).Chunk(2).Select(c => $"({c[0]}, {c[1]})").Dump("tile coordinates");
	var tiles_heap = cpu.Memory.AsSpan(tile_list_address, tile_list_size);
	VerifyHeap(tiles_heap,
		(a, b) =>
		cpu.Memory[red_tiles_address + a] <= cpu.Memory[red_tiles_address + b]
		).Dump("Valid heap? (X)");
	VerifyHeap(tiles_heap,
		(a, b) =>
		{
			var cmp = cpu.Memory[red_tiles_address + a + 1].CompareTo(cpu.Memory[red_tiles_address + b + 1]);
			if (cmp == 0) cmp = a.CompareTo(b);
			return cmp <= 0;
		}).Dump("Valid heap? (X)");
	cpu.Memory.Skip(tile_list_address).Take(tile_list_size)
		.Select((a, i) => new
		{
			list_index = i,
			list_item_address = tile_list_address + i,
			tile_address = a,
			tile_x = cpu.Memory[red_tiles_address + a],
			tile_y = cpu.Memory[red_tiles_address + a + 1],
		})
		.Dump("tile list");
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

	public void WriteLine() => WriteLine("");
	public void WriteLine(string message) { Console.WriteLine(message); ExecutionLogFile?.WriteLine(message); }
	public void WriteLine(string format, params object[] args) { Console.WriteLine(format, args); ExecutionLogFile?.WriteLine(format, args); }

	public void Dispose()
	{
		ExecutionLogFile?.WriteLine("");
	}
}

static string SelectBestName(IEnumerable<string> names)
{
	string best_name = "???";
	foreach (var name in names)
	{
		best_name = name;
		if (!name.Contains("auto__")) break;
	}
	return best_name;
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
	_addr2Name = _memoryMap.Where(x => x.address >= 0
		&& !x.name.EndsWith("__auto__endfn")
		// debugger breakpoints
		//&& !x.name.StartsWith("debugbp_")
		)
		.GroupBy(x => x.address, x => x.name)
		.ToDictionary(x => x.Key, x => SelectBestName(x));
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

memval_t ReadVariable(IntcodeCpu cpu, string name)
{
	return cpu.Memory[_name2Addr[name]];
}

void Year2025Day8_DebugPhase2Pointers(IntcodeCpu cpu, string breakpointName)
{
	var bpout = new BreakpointOutput();
	bpout.WriteLine("{0} breakpoint hit", breakpointName);
	bpout.WriteLine("");
	bpout.WriteLine("rb = {0} ({1})", cpu.Toc, cpu.DescribeToc(cpu, cpu.Toc));
	foreach (var var_name in new string[] {
				"box1_id", "box2_id",
				"box1_to_pair_table",
				"pair_table_to_next_box1",
				"box2_to_pair_table",
				"pair_table_to_next_box2"
				})
	{
		string effective_name = var_name;
		var scoped_name = "fn_build_pairs__" + var_name;
		if (_name2Addr.ContainsKey(scoped_name))
			effective_name = scoped_name;
		bpout.WriteLine("{0} = {1}", var_name, ReadVariable(cpu, effective_name));
	}
	bpout.WriteLine("");
}


void Year2025Day8_DebugProcessNextPair(IntcodeCpu cpu, string breakpointName)
{
	var bpout = new BreakpointOutput();
	bpout.WriteLine("{0} breakpoint hit", breakpointName);
	bpout.WriteLine("");
	int box1 = (int)cpu.Memory[cpu.Toc];
	int box2 = (int)cpu.Memory[cpu.Toc + 1];

	bpout.WriteLine("Pairs left: {0}", ReadVariable(cpu, "pair_count"));
	bpout.WriteLine("# of unconnected boxes: {0}", ReadVariable(cpu, "unconnected_box_count"));
	// disjoint_circuit_count is off by 1 so that it is equal to 0 when we have solved the problem
	bpout.WriteLine("# of disjoint circuits: {0}", 1 + ReadVariable(cpu, "disjoint_circuit_count"));
	bpout.WriteLine("Upcoming boxes: {0} and {1}", box1, box2);
	//bpout.WriteLine("RB = {0}", cpu.Toc);
	int boxes_address = _name2Addr["boxes"];
	var circuit_pointers_address = (int)ReadVariable(cpu, "circuit_pointers_address");
	var circuits_address = (int)ReadVariable(cpu, "circuits_address");
	int box_count = (int)ReadVariable(cpu, "box_count");

	bpout.WriteLine("");
	for (int i = 0; i < box_count; i++)
	{
		var sb = new StringBuilder();
		sb.AppendFormat("boxes[{0,3}] ", i);
		if (i == box1) sb.Append('1');
		else if (i == box2) sb.Append('2');
		else sb.Append(' ');
		sb.Append(' ');
		int circuit_ptr = (int)cpu.Memory[boxes_address + i * 4 + 3];
		sb.AppendFormat("circuit_ptr = {0,3}", circuit_ptr);
		if (circuit_ptr >= 0)
		{
			var circuit_address = (int)cpu.Memory[circuit_pointers_address + circuit_ptr];
			sb.AppendFormat(" -> {0,3}", circuit_address);
			int circuit_box_count, circuit_merge_ptr;
			int loop_limit = 10;
			do
			{
				circuit_box_count = (int)cpu.Memory[circuits_address + circuit_address + 0];
				circuit_merge_ptr = (int)cpu.Memory[circuits_address + circuit_address + 1];
				sb.AppendFormat(" => (count = {0,3}, merged = {1,3})", circuit_box_count, circuit_merge_ptr);
				circuit_address = circuit_merge_ptr;
			} while (circuit_address >= 0 && (--loop_limit > 0));
		}
		bpout.WriteLine(sb.ToString());
	}
	bpout.WriteLine("");

	//	var pairsTable = pairsMemory.Take(pair_count * 3).Chunk(3).ToArray();
	//	bpout.WriteLine("Pairs table is valid heap? {0}", VerifyHeap<memval_t[]>(pairsTable, (a, b) => a[2] <= b[2]));
}

void Year2025Day8_DebugPairAdded(IntcodeCpu cpu, string breakpointName)
{
	// halt after heapify_pairs_done
	if (breakpointName == "heapify_pairs_done") { cpu.Memory[cpu.Pc] = 99; }
	var bpout = new BreakpointOutput();
	bpout.WriteLine("{0} breakpoint hit", breakpointName);
	bpout.WriteLine("");
	//bpout.WriteLine("RB = {0}", cpu.Toc);
	int boxes_address = _name2Addr["boxes"];
	int box_count = (int)ReadVariable(cpu, "box_count");
	int box_table_size = (int)ReadVariable(cpu, "box_table_size");
	int pairs_address = boxes_address + box_table_size;
	int pair_count = (int)ReadVariable(cpu, "pair_count");
	int pair_table_size = (int)ReadVariable(cpu, "pair_table_size");
	int heap_size = (int)ReadVariable(cpu, "heap_size");
	bpout.WriteLine("rb = pair_table + {0}", cpu.Toc - pairs_address);
	foreach (var var_name in new string[] {
				"fn_build_pairs__box1_id", "fn_build_pairs__box2_id",
				"box_count",
				"box_table_size",
				"box_table_size_inv",
				"heap_size",
				"heap_size_inv",
				"pair_count",
				"pair_table_size",
				"pair_table_size_inv",
				})
		bpout.WriteLine("var {0} = {1}", var_name, ReadVariable(cpu, var_name));
	//bpout.WriteLine("");
	bpout.WriteLine("Address of pairs table: {0}", pairs_address);
	//bpout.WriteLine("Base of stack segment:  {0}", boxes_address + heap_size);
	var pairsMemory = cpu.Memory.Skip(pairs_address).Take(pair_table_size);
	//bpout.WriteLine("Pairs table: {0}", string.Join(" ", pairsMemory));

	/*
		bpout.WriteLine("Pairs table:");
		foreach (var pair in FormatPairsTable(pairsMemory, pair_count)) bpout.WriteLine("\t{0}", pair);
		bpout.WriteLine("");
		*/

	var pairsTable = pairsMemory.Take(pair_count * 3).Chunk(3).ToArray();
	bpout.WriteLine("Pairs table is valid heap? {0}", VerifyHeap<memval_t[]>(pairsTable, (a, b) => a[2] <= b[2]));
}

bool VerifyHeap<T>(Span<T> span, Func<T, T, bool> valid_parent_for)
{
	for (int parent = 0; parent < span.Length; parent++)
	{
		var child1 = parent * 2 + 1;
		var child2 = child1 + 1;
		if (child1 >= span.Length) break;
		if (!valid_parent_for(span[parent], span[child1])) return false;
		if (child2 >= span.Length) break;
		if (!valid_parent_for(span[parent], span[child2])) return false;
	}
	return true;
}

IEnumerable<string> FormatPairsTable(IEnumerable<memval_t> src, int pair_count)
{
	return src.Chunk(3).Select((chunk, i) =>
		((i == pair_count) ? " | " : "") +
		$"{chunk[0]} {chunk[1]} {(chunk[2])}");
}

void Year2025Day8_PrintBoxTable(IntcodeCpu cpu, string breakpointName)
{
	var bpout = new BreakpointOutput();
	bpout.WriteLine("{0} breakpoint hit", breakpointName);
	bpout.WriteLine("");
	int boxes_address = _name2Addr["boxes"];
	int box_count = (int)ReadVariable(cpu, "box_count");
	int box_table_size = (int)ReadVariable(cpu, "box_table_size");
	int heap_size = (int)ReadVariable(cpu, "heap_size");
	foreach (var var_name in new string[] {
				"box_count",
				"box_table_size",
				"box_table_size_inv",
				"heap_size",
				"heap_size_inv",
				//"num_rows",
				})
		bpout.WriteLine("{0} = {1}", var_name, ReadVariable(cpu, var_name));
	bpout.WriteLine("");
	bpout.WriteLine("Address of boxes table: {0}", boxes_address);
	bpout.WriteLine("Base of stack segment:  {0}", boxes_address + heap_size);
	bpout.WriteLine("Boxes table: {0}",
				string.Join(" ", cpu.Memory.Skip(boxes_address).Take(box_table_size)));
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
	bpout.WriteLine("Address of column-status: {0}", first_row_address);
	bpout.WriteLine("Address of part 2 accumulators: {0}", first_row_address + num_columns);
	bpout.WriteLine("Address of part 1 table: {0}", first_row_address + 2 * num_columns + 1);
	bpout.WriteLine("Part 2 column status: {0}",
				string.Join(" ", cpu.Memory.Skip(first_row_address).Take(num_columns)));
	bpout.WriteLine("Part 2 accumulators: {0}",
				string.Join(", ", cpu.Memory.Skip(first_row_address + num_columns).Take(num_columns)));
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

static (int start, int end, string name)[] _functionMap;
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
		cpu.Toc.Dump("Final RB");
		instrcount.Dump("Number of instructions executed");
		_instructionHitCount.OrderBy(x => x.Key).Select(x => new { pc = x.Key, pc_desc = DescribeAddress(x.Key, cpu.Toc), count = x.Value.Value })
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
	return cpu;
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

	public Func<IntcodeCpu, int, string> DescribeToc;
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
		if (ForceHalt)
		{
			Console.WriteLine("CPU forcibly halted");
			return this.HaltOp(default, default);
		}

		if (Pc < 0 || Pc >= Memory.Length) throw new Exception("PC out of bounds: " + Pc);
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
