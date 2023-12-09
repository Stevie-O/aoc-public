Console.WriteLine(
File.ReadAllLines("day01.txt")
.Select(input => Regex.Matches(input, "(?=([1-9]|one|two|three|four|five|six|seven|eight|nine))")
.Select(m => (len: m.Groups[1].Length, dig: m.Groups[1].Value switch
{
	"1" or "one" => 1,
	"2" or "two" => 2,
	"3" or "three" => 3,
	"4" or "four" => 4,
	"5" or "five" => 5,
	"6" or "six" => 6,
	"7" or "seven" => 7,
	"8" or "eight" => 8,
	"9" or "nine" => 9,
	_ => throw new Exception(),
}))
.Aggregate<(int len, int dig), (int p1d1, int p1d2, int p2d1, int p2d2)>
((-1, -1, -1, -1), (s, m) => m.len == 1 ?
		(
			(s.p2d1 < 0) ? (m.dig, m.dig, m.dig, m.dig) :
			(s.p1d1 < 0) ? (m.dig, m.dig, s.p2d1, m.dig) :
			(s.p1d1, m.dig, s.p2d1, m.dig)
		) :
		(
			(s.p2d1 < 0) ? (s.p1d1, s.p1d2, m.dig, m.dig) :
							(s.p1d1, s.p1d2, s.p2d1, m.dig)
		)
)
)
.Select(agg => (part1: agg.p1d1 * 10 + agg.p1d2, part2: agg.p2d1 * 10 + agg.p2d2))
.Aggregate( (part1: 0, part2: 0), (acc, x) => (acc.part1 + x.part1, acc.part2 + x.part2))
.ToString()
);
