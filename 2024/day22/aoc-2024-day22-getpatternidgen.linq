<Query Kind="Statements" />

string[] deltas = { "pb0", "pb1", "pb2", "pb3" };

for (int i = 0; i < 4; i++)
{
	GenerateGetPatternIdFunction(i, deltas.Skip(i).Concat(deltas.Take(i)).ToArray());
}

void GenerateGetPatternIdFunction(int i, string[] deltas)
{
	Console.WriteLine("@fn 1 get_pattern{0}_id() global({1})", i, string.Join(", ", deltas));
	Console.WriteLine();

	Console.WriteLine("add {0} 9 return0", deltas[0]);
	for (int i = 1; i < 4; i++)
	{
		
	}
}