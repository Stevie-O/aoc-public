<Query Kind="Program" />

void Main()
{
	//ComputeMasks();
	// ComputeMasks tells me that without storing intermediates, I need 128 total XOR operations to directly compute the output.
	// In contrast, I need (24-6) + (24-5) + (24-11) = 50 total XORs if I store the intermediates.

	GenerateXorShifter3(true, 6, 5, 11);

	//GenerateXorShifter2(true, 0x1c001001, 6, 5, 11);

}

void GenerateXorShifter(bool shl, params int[] shifts)
{
	string[] bitval = Enumerable.Range(0, 24).Select(n => "xs" + n.ToString("00")).ToArray();
	string[] tmpval = Array.ConvertAll(bitval, s => "t" + s.Substring(2));
	bool[] inverted = new bool[bitval.Length];

	int[] last_read = new int[bitval.Length];
	int[] last_write = new int[bitval.Length];
	var cmds = new List<string>();
	int steps = 0;
	foreach (var shift in shifts)
	{
		int src_num, delta;
		if (shl) { src_num = bitval.Length - 1; delta = -1; }
		else { src_num = 0; delta = 1; }
		int xor_num = src_num + delta * shift;
		int count = bitval.Length - shift;
		for (int i = 0; i < count; i++)
		{
			last_write[src_num] = cmds.Count;
			last_read[xor_num] = cmds.Count;
			cmds.Add(string.Format("{2} = XNOR {0} with {1}", bitval[xor_num], bitval[src_num], tmpval[src_num]));

			bitval[src_num] = tmpval[src_num];
			inverted[src_num] = !(inverted[src_num] ^ inverted[xor_num]);
			src_num += delta;
			xor_num += delta;
			steps++;
		}
		//Console.WriteLine();
		cmds.Add("");
		shl = !shl;
	}
	//	Console.WriteLine("number of steps: {0}", steps);
	// cmds.Dump("executions");
	cmds.Select((cmd, n) =>
		new
		{
			cmd,
			last_read_for = Enumerable.Range(0, bitval.Length).Where(bitn => last_read[bitn] == n).ToArray(),
			last_write_for = Enumerable.Range(0, bitval.Length).Where(bitn => last_write[bitn] == n).ToArray(),
		}
		).Dump();

	inverted.Select((f, i) => (i, f)).Dump("inversions");
}

void GenerateXorShifter2(bool shl, ulong invmask, params int[] shifts)
{
	string[] bitname = Enumerable.Range(0, 24).Select(n => "xs" + n.ToString("00")).ToArray();
	bool[] inverted = new bool[bitname.Length];

int steps = 0;
	foreach (var shift in shifts)
	{
		int src_num, delta;
		if (shl) { src_num = bitname.Length - 1; delta = -1; }
		else { src_num = 0; delta = 1; }
		int xor_num = src_num + delta * shift;
		int count = bitname.Length - shift;
		for (int i = 0; i < count; i++)
		{
			Console.WriteLine("eq {0} {1} {0}", bitname[src_num], bitname[xor_num]);
			inverted[src_num] = !(inverted[src_num] ^ inverted[xor_num]);
			if ((invmask & (1UL << steps)) != 0)
			{
				Console.WriteLine("eq {0} 0 {0}", bitname[src_num]);
				inverted[src_num] = !inverted[src_num];
			}
			//if (inverted[src_num]) Console.WriteLine("# {0} is inverted from its correct value", bitname[src_num]);
			src_num += delta;
			xor_num += delta;
			steps++;
		}
		Console.WriteLine();
		shl = !shl;
	}
	Console.WriteLine("# Fix inverted registers");
	for (int i = 0; i < inverted.Length; i++)
	{
		if (inverted[i])
		{
			Console.WriteLine("eq {0} 0 {0}", bitname[i]);
		}
	}
	//inverted.Select((f, i) => (i, f)).Dump("inversions");
}

static int Popcnt(ulong value) => (int)System.Runtime.Intrinsics.X86.Popcnt.X64.PopCount(value);
static ulong Lowbit(ulong value) => value & (ulong)(unchecked(-(long)value));

void GenerateXorShifter3(bool shl, params int[] shifts)
{
	string[] bitname = Enumerable.Range(0, 24).Select(n => "xs" + n.ToString("00")).ToArray();
	bool[] inverted = new bool[bitname.Length];

	int best_inv_count = 24;

	for (ulong inv_mask = 0; inv_mask < (1UL << 50); inv_mask++)
	{
		if (Popcnt(inv_mask) >= best_inv_count)
		{
			do
			{
				inv_mask += Lowbit(inv_mask);
			} while (Popcnt(inv_mask) >= best_inv_count);
		}
		int inv_count = Popcnt(inv_mask);
		Array.Clear(inverted);

		shl = true;
		int steps = 0;
		foreach (var shift in shifts)
		{
			int src_num, delta;
			if (shl) { src_num = bitname.Length - 1; delta = -1; }
			else { src_num = 0; delta = 1; }
			int xor_num = src_num + delta * shift;
			int count = bitname.Length - shift;
			for (int i = 0; i < count; i++)
			{
				bool add_inversion = (inv_mask & (1UL << steps)) != 0;
				inverted[src_num] = !(inverted[src_num] ^ inverted[xor_num] ^ add_inversion);

				src_num += delta;
				xor_num += delta;
				steps++;
			}
			//Console.WriteLine();
			shl = !shl;
		}

		var inv_left = inverted.Where(e => e).Count();
		int inv_total = inv_count + inv_left;
		if (inv_total <= best_inv_count)
		{
			Console.WriteLine("invmask 0x{0:x} forces {1} inversions, requiring {2} at the end, for a total of {3} inversions",
					inv_mask, inv_count, inv_left, inv_total);
			best_inv_count = inv_total;
		}

	}
}


void ComputeMasks()
{
	var word = Enumerable.Range(0, 24).Select(n => "xs" + n.ToString("00")).ToArray();
	string.Join(", ", word /* .Concat(word.Select(w => w.Replace("xs", "t"))) */ ).Dump();
	// word ^= ((word << 6) & mask))
	for (int i = 6; i < word.Length; i++)
	{
		word[i] = xor(word[i], word[i - 6]);
	}
	// word ^= ((word >> 5) & mask));
	for (int i = 0; i < word.Length - 5; i++)
	{
		word[i] = xor(word[i], word[i + 5]);
	}
	// word ^= ((word << 11) & mask));
	for (int i = 11; i < word.Length; i++)
	{
		word[i] = xor(word[i], word[i - 11]);
	}
	word.Dump();

}

// Define other methods and classes here

string xor(string a, string b) //=> a + "^" + b;
{
	var a_w = a.Split('^').ToHashSet();
	var b_w = b.Split('^').ToHashSet();
	return string.Join("^", a_w.Union(b_w).Where(w => a_w.Contains(w) ^ b_w.Contains(w)).OrderBy(w => w));
}
