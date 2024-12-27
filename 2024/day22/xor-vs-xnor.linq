<Query Kind="Program" />

void Main()
{
	var truth = new[] { 0, 1 };
	(
	from a in truth
	from b in truth
	from c in truth
	select new {
		a,
		b,
		c,
		xor3 = (a^b^c),
		xnor3 = xnor(a, xnor(b, c)),
	}).Dump();
	
}



int xnor(int a, int b) => (a == b) ? 1 : 0;