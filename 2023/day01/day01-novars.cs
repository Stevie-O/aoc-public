Console.WriteLine(
File.ReadAllLines("day01.txt")
.Select(input => Regex.Matches(input, "(?=([1-9]|one|two|three|four|five|six|seven|eight|nine))")
.Select(m => m.Groups[1].Value switch
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
})
.Aggregate<int, (int d1, int d2)>
((-1, -1), (s, m) => (s.d1 < 0 ? (m, m) : (s.d1, m)))
)
.Select(agg => agg.d1 * 10 + agg.d2)
.Sum()
);
