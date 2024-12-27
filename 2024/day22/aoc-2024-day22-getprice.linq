<Query Kind="Statements" />

Enumerable.Range(0, 24).Select(n => (1<<n) % 10).Sum().Dump("maximum value at end of run");
for (int i = 0, j = 1; i < 24; i++, j <<= 1)
{
	if (i == 0)
	{
		Console.WriteLine("mul xs00 1 accum");
	}
	else
	{
		Console.WriteLine("mul xs{0:00} {1} tmp", i, (j % 10));
		Console.WriteLine("add accum tmp accum");
	}
}