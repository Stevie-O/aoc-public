<Query Kind="Program">
  <NuGetReference>System.Collections.Immutable</NuGetReference>
  <Namespace>memval_t = System.Int64</Namespace>
  <Namespace>System.Collections.Concurrent</Namespace>
  <Namespace>System.Collections.Immutable</Namespace>
  <Namespace>System.Threading.Tasks</Namespace>
  <Namespace>System.Globalization</Namespace>
</Query>

const bool WRITE_EXECLOG = false;

void Main()
{
	var dir = Path.Combine( Path.GetDirectoryName(Util.CurrentQueryPath), @"../2024/day22");
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
//"1101,0,0,3,109,801,1106,0,31,0,1,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,21101,0,11,1,21101,0,21,2,21101,46,0,0,1105,1,233,2101,0,1,9,21101,0,21,1,21101,0,31,2,21102,65,1,0,1106,0,233,2008,9,1,70,1106,0,693,21001,11,0,1,21001,21,0,2,21101,0,87,0,1106,0,120,202,1,10,10,101,-1,9,9,101,1,73,73,101,1,77,77,1005,9,72,21001,10,0,1,21102,117,1,0,1105,1,551,104,10,99,109,11,21201,-9,1,-8,22202,-10,-10,-7,22102,-4,-9,-6,22201,-7,-6,1,21102,145,1,0,1106,0,359,21202,1,-1,-5,22201,-10,-5,1,21102,1,160,0,1105,1,422,21201,1,0,-4,1202,-4,-1,170,21201,-10,0,-3,22202,-4,-3,-2,2207,-2,-8,181,1106,0,190,21201,-4,1,-4,1106,0,164,21201,-3,0,-1,1202,-1,-1,200,21201,-10,0,-3,22202,-1,-3,-2,2207,-2,-8,211,1105,0,220,21201,-1,1,-1,1106,0,194,21202,-4,-1,-10,22201,-1,-10,-10,109,-11,2105,1,0,109,7,21101,0,0,-3,2101,0,-6,308,1101,0,247,319,21101,0,0,-4,21101,0,0,-2,203,-1,1208,-1,10,262,1105,0,287,1207,-1,48,269,1105,0,278,1207,-1,58,276,1105,0,320,1005,1,255,1206,-2,255,1106,0,298,2201,-3,-2,292,1106,0,255,1101,0,350,319,2007,308,-5,303,1106,0,686,2101,0,-4,0,101,1,308,308,22101,1,-3,-3,1106,0,247,1201,-4,0,341,22101,1,-2,-2,22101,-48,-1,-1,22102,10,-4,-4,22201,-4,-1,-4,2107,0,-4,345,1105,0,255,1106,0,679,22101,0,-3,-6,109,-7,2105,1,0,109,4,1207,-3,2,366,1105,0,417,21201,-3,0,1,21101,0,379,0,1106,0,422,21101,386,0,0,1105,1,422,21102,1,393,0,1105,1,359,21202,1,2,-1,21201,-1,1,-1,22202,-1,-1,-2,2207,-3,-2,415,1002,415,-1,415,21201,-1,0,-3,109,-4,2105,1,0,109,2,1207,-1,2,429,1106,0,438,21101,0,0,-1,1106,0,457,22101,0,-1,1,21101,0,1,2,21101,453,0,0,1105,1,462,22101,0,2,-1,109,-2,2106,0,0,109,6,22201,-4,-4,2,1206,-4,679,2207,-5,2,476,1106,0,493,21201,-5,0,1,21101,0,0,2,21101,1,0,3,1106,0,504,21201,-5,0,1,21101,504,0,0,1105,1,462,1205,3,511,22201,2,-4,2,21201,2,0,-2,22207,1,-4,-1,1205,-1,530,1202,-4,-1,528,21201,1,0,1,21201,1,0,-3,21201,-3,0,-5,21201,-2,0,-4,21201,-1,0,-3,109,-6,2105,1,0,109,2,1206,-1,574,22101,0,-1,1,21101,0,1,2,21101,571,0,0,1106,0,581,1106,0,576,104,48,109,-2,2105,1,0,109,5,22101,0,-4,1,2207,-4,-3,592,1105,0,637,21202,-3,10,2,21101,0,605,0,1106,0,581,21201,1,0,-4,21202,-3,-1,-1,21101,48,0,-2,2207,-4,-3,622,1105,0,635,21201,-2,1,-2,22201,-4,-1,-4,1106,0,617,204,-2,21201,-4,0,-4,109,-5,2106,0,0,0,0,0,101,0,646,654,1001,0,0,647,101,1,646,662,1001,0,0,648,1001,662,1,662,4,648,1001,647,-1,647,1005,647,661,99,1101,700,0,646,1106,0,649,1101,727,0,646,1106,0,649,1101,749,0,646,1106,0,649,26,73,110,116,101,103,101,114,32,111,118,101,114,102,108,111,119,32,100,101,116,101,99,116,101,100,10,21,84,111,111,32,109,97,110,121,32,114,97,99,101,115,32,105,110,112,117,116,10,51,78,117,109,98,101,114,32,111,102,32,116,105,109,101,115,32,100,111,101,115,32,110,111,116,32,109,97,116,99,104,32,110,117,109,98,101,114,32,111,102,32,100,105,115,116,97,110,99,101,115,10"
//"1101,0,0,3,109,801,1105,1,31,0,1,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,21101,11,0,1,21101,21,0,2,21101,46,0,0,1105,1,233,1201,1,0,9,21101,21,0,1,21101,31,0,2,21101,65,0,0,1105,1,233,2008,9,1,70,1106,0,693,21001,11,0,1,21001,21,0,2,21101,87,0,0,1105,1,120,202,1,10,10,101,-1,9,9,101,1,73,73,101,1,77,77,1005,9,72,21001,10,0,1,21101,117,0,0,1105,1,551,104,10,99,109,11,21201,-9,1,-8,22202,-10,-10,-7,22102,-4,-9,-6,22201,-7,-6,1,21101,145,0,0,1105,1,359,21202,1,-1,-5,22201,-10,-5,1,21101,160,0,0,1105,1,422,21201,1,0,-4,1202,-4,-1,170,21201,-10,0,-3,22202,-4,-3,-2,2207,-2,-8,181,1106,0,190,21201,-4,1,-4,1105,1,164,21201,-3,0,-1,1202,-1,-1,200,21201,-10,0,-3,22202,-1,-3,-2,2207,-2,-8,211,1105,0,220,21201,-1,1,-1,1105,1,194,21202,-4,-1,-10,22201,-1,-10,-10,109,-11,2105,1,0,109,7,21101,0,0,-3,1201,-6,0,308,1101,247,0,319,21101,0,0,-4,21101,0,0,-2,203,-1,1208,-1,10,262,1105,0,287,1207,-1,48,269,1105,0,278,2107,57,-1,276,1106,0,320,1005,1,255,1206,-2,255,1105,1,298,2201,-3,-2,292,1106,0,255,1101,350,0,319,2007,308,-5,303,1106,0,686,1201,-4,0,0,101,1,308,308,22101,1,-3,-3,1105,1,247,1201,-4,0,341,22101,1,-2,-2,22101,-48,-1,-1,22102,10,-4,-4,22201,-4,-1,-4,2107,0,-4,345,1106,0,679,1105,1,255,21201,-3,0,-6,109,-7,2105,1,0,109,4,1207,-3,2,366,1105,0,417,21201,-3,0,1,21101,379,0,0,1105,1,422,21101,386,0,0,1105,1,422,21101,393,0,0,1105,1,359,21202,1,2,-1,21201,-1,1,-1,22202,-1,-1,-2,2207,-3,-2,415,1002,415,-1,415,21201,-1,0,-3,109,-4,2105,1,0,109,2,1207,-1,2,429,1106,0,438,21101,0,0,-1,1105,1,457,21201,-1,0,1,21101,1,0,2,21101,453,0,0,1105,1,462,21201,2,0,-1,109,-2,2105,1,0,109,6,22201,-4,-4,2,1206,-4,679,2207,-5,2,476,1106,0,493,21201,-5,0,1,21101,0,0,2,21101,1,0,3,1105,1,504,21201,-5,0,1,21101,504,0,0,1105,1,462,1205,3,511,22201,2,-4,2,21201,2,0,-2,22207,1,-4,-1,1205,-1,530,1202,-4,-1,528,21201,1,0,1,21201,1,0,-3,21201,-3,0,-5,21201,-2,0,-4,21201,-1,0,-3,109,-6,2105,1,0,109,2,1206,-1,574,21201,-1,0,1,21101,1,0,2,21101,571,0,0,1105,1,581,1105,1,576,104,48,109,-2,2105,1,0,109,5,21201,-4,0,1,2207,-4,-3,592,1105,0,637,21202,-3,10,2,21101,605,0,0,1105,1,581,21201,1,0,-4,21202,-3,-1,-1,21101,48,0,-2,2207,-4,-3,622,1105,0,635,21201,-2,1,-2,22201,-4,-1,-4,1105,1,617,204,-2,21201,-4,0,-4,109,-5,2105,1,0,0,0,0,1001,646,0,654,1001,0,0,647,101,1,646,662,1001,0,0,648,1001,662,1,662,4,648,1001,647,-1,647,1005,647,661,99,1101,700,0,646,1105,1,649,1101,727,0,646,1105,1,649,1101,749,0,646,1105,1,649,26,73,110,116,101,103,101,114,32,111,118,101,114,102,108,111,119,32,100,101,116,101,99,116,101,100,10,21,84,111,111,32,109,97,110,121,32,114,97,99,101,115,32,105,110,112,117,116,10,51,78,117,109,98,101,114,32,111,102,32,116,105,109,101,115,32,100,111,101,115,32,110,111,116,32,109,97,116,99,104,32,110,117,109,98,101,114,32,111,102,32,100,105,115,116,97,110,99,101,115,10"
//@"1101,1,0,3,109,1777,1101,13,0,623,1105,1,582,203,0,1207,0,48,20,1105,0,1360,2107,55,0,27,1105,0,1360,109,1,1001,1776,1,1776,203,0,1208,0,44,42,1105,0,13,1208,0,13,49,1105,0,35,1208,0,10,56,1105,0,68,2107,0,0,63,1106,0,68,1105,1,1360,21101,0,0,0,109,1,21001,1605,0,1,21001,1602,0,2,21101,89,0,0,1105,1,1134,1201,1,0,1606,21001,1607,0,1,21001,1603,0,2,21101,108,0,0,1105,1,1134,1201,1,0,1608,21001,1609,0,1,21001,1604,0,2,21101,127,0,0,1105,1,1134,1201,1,0,1610,21001,1605,0,1,21001,1606,0,2,21101,146,0,0,1105,1,1247,204,1,21001,1607,0,1,21001,1608,0,2,21101,163,0,0,1105,1,1247,204,1,21001,1609,0,1,21001,1610,0,2,21101,180,0,0,1105,1,1247,204,1,21101,189,0,0,1105,1,218,104,10,99,109,2,1106,0,199,104,44,21201,-1,48,-1,204,-1,1101,1,0,195,21101,1,0,-1,109,-2,2105,1,0,109,3,21101,1,0,-2,7,1775,1776,229,1106,0,361,101,1777,1775,236,21001,0,0,-2,1001,1775,1,1775,101,1777,1775,248,21001,0,0,-2,1001,1775,1,1775,2107,3,-1,260,1106,0,336,1208,-2,1,267,1105,0,336,1208,-2,3,274,1105,0,336,1208,-2,4,281,1105,0,336,2107,6,-1,288,1105,0,1367,21201,-1,-4,-1,21202,-1,2,-1,2101,1605,-1,307,1001,307,2,311,21001,0,0,1,21001,0,0,2,21101,321,0,0,1105,1,1247,21201,1,0,-1,1105,1,336,343,355,353,356,354,357,346,349,2101,328,-2,342,1105,1,1367,1105,1,352,1105,1,352,1105,1,352,99,99,99,99,99,99,1105,1,220,109,-3,2105,1,0,21101,1582,0,1,21101,1592,0,2,21101,381,0,0,1105,1,718,2008,1570,1,386,1106,0,1339,21001,1572,0,1,21001,1582,0,2,21101,403,0,0,1105,1,436,202,1,1571,1571,101,-1,1570,1570,101,1,389,389,101,1,393,393,1005,1570,388,21001,1571,0,1,21101,433,0,0,1105,1,1039,104,10,99,109,11,21201,-9,1,-8,22202,-10,-10,-7,22102,-4,-9,-6,22201,-7,-6,1,1207,1,1,459,1105,0,1346,21101,468,0,0,1105,1,847,21202,1,-1,-5,22201,-10,-5,1,21101,483,0,0,1105,1,910,21201,1,0,-4,1202,-4,-1,493,21201,-10,0,-3,22202,-4,-3,-2,2207,-2,-8,504,1106,0,513,21201,-4,1,-4,1105,1,487,21201,-3,0,-1,1202,-1,-1,523,21201,-10,0,-3,22202,-1,-3,-2,2207,-2,-8,534,1105,0,543,21201,-1,1,-1,1105,1,517,21202,-4,-1,-10,22201,-1,-10,-10,109,-11,2105,1,0,82,101,103,105,115,116,101,114,32,65,58,32,0,80,114,111,103,114,97,109,58,32,0,0,0,0,1101,556,0,595,1101,0,0,581,1101,0,0,674,1001,0,0,579,1006,579,617,3,580,8,579,580,608,1106,0,1360,1001,595,1,595,1105,1,594,1008,595,578,622,1105,0,1353,3,580,1008,580,13,631,1105,0,624,1008,580,10,638,1105,0,673,1007,580,48,645,1105,0,1360,107,57,580,652,1105,0,1360,1101,1,0,674,101,-48,580,580,1002,581,10,581,1,581,580,581,1105,1,624,1106,0,1360,1001,581,0,1602,1001,679,1,679,1001,565,1,565,107,67,565,693,1106,0,582,3,580,1008,580,13,702,1105,0,695,1008,580,10,709,1106,0,1360,1101,569,0,595,1105,1,594,109,7,21101,0,0,-3,1201,-6,0,796,1101,732,0,807,21101,0,0,-4,21101,0,0,-2,203,-1,1208,-1,10,747,1105,0,772,1207,-1,48,754,1105,0,763,2107,57,-1,761,1106,0,808,1005,1,740,1206,-2,740,1105,1,786,2201,-3,-2,777,1106,0,740,1206,-2,838,1101,838,0,807,2007,796,-5,791,1106,0,1332,1201,-4,0,0,101,1,796,796,22101,1,-3,-3,1105,1,732,1201,-4,0,829,22101,1,-2,-2,22101,-48,-1,-1,22102,10,-4,-4,22201,-4,-1,-4,2107,0,-4,833,1106,0,1325,1105,1,740,21201,-3,0,-6,109,-7,2105,1,0,109,4,1207,-3,2,854,1105,0,905,21201,-3,0,1,21101,867,0,0,1105,1,910,21101,874,0,0,1105,1,910,21101,881,0,0,1105,1,847,21202,1,2,-1,21201,-1,1,-1,22202,-1,-1,-2,2207,-3,-2,903,1002,903,-1,903,21201,-1,0,-3,109,-4,2105,1,0,109,2,1207,-1,2,917,1106,0,926,21101,0,0,-1,1105,1,945,21201,-1,0,1,21101,1,0,2,21101,941,0,0,1105,1,950,21201,2,0,-1,109,-2,2105,1,0,109,6,22201,-4,-4,2,1206,-4,1325,2207,-5,2,964,1106,0,981,21201,-5,0,1,21101,0,0,2,21101,1,0,3,1105,1,992,21201,-5,0,1,21101,992,0,0,1105,1,950,1205,3,999,22201,2,-4,2,21201,2,0,-2,22207,1,-4,-1,1205,-1,1018,1202,-4,-1,1016,21201,1,0,1,21201,1,0,-3,21201,-3,0,-5,21201,-2,0,-4,21201,-1,0,-3,109,-6,2105,1,0,109,2,1206,-1,1062,21201,-1,0,1,21101,1,0,2,21101,1059,0,0,1105,1,1069,1105,1,1064,104,48,109,-2,2105,1,0,109,5,21201,-4,0,1,2207,-4,-3,1080,1105,0,1125,21202,-3,10,2,21101,1093,0,0,1105,1,1069,21201,1,0,-4,21202,-3,-1,-1,21101,48,0,-2,2207,-4,-3,1110,1105,0,1123,21201,-2,1,-2,22201,-4,-1,-4,1105,1,1105,204,-2,21201,-4,0,-4,109,-5,2105,1,0,109,3,21201,-2,0,1,21201,-1,0,2,21101,1,0,3,21001,1601,0,4,21101,1159,0,0,1105,1,1168,21201,1,0,-2,109,-3,2105,1,0,109,6,21101,0,0,-1,2207,-4,-3,1179,1105,0,1238,1206,-2,1325,2207,-3,-4,1189,1106,0,1222,22101,1,-5,1,21201,-4,0,2,21202,-3,2,3,21201,-2,-1,4,21101,1214,0,0,1105,1,1168,22201,-1,1,-1,21201,2,0,-4,1201,-5,0,1233,2207,-4,-3,1231,1108,0,0,0,21201,-1,1,-1,21201,-1,0,-5,109,-6,2105,1,0,109,4,1201,-3,0,1265,21101,0,0,-3,21101,1,0,-1,1206,-2,1287,2002,0,-1,1270,21201,-3,0,-3,21202,-1,2,-1,1001,1265,1,1265,21201,-2,-1,-2,1205,-2,1264,109,-4,2105,1,0,0,0,0,1001,1292,0,1300,1001,0,0,1293,101,1,1292,1308,1001,0,0,1294,1001,1308,1,1308,4,1294,1001,1293,-1,1293,1005,1293,1307,99,1101,1374,0,1292,1105,1,1295,1101,1401,0,1292,1105,1,1295,1101,1423,0,1292,1105,1,1295,1101,1475,0,1292,1105,1,1295,1101,1502,0,1292,1105,1,1295,1101,1518,0,1292,1105,1,1295,1101,1537,0,1292,1105,1,1295,26,73,110,116,101,103,101,114,32,111,118,101,114,102,108,111,119,32,100,101,116,101,99,116,101,100,10,21,84,111,111,32,109,97,110,121,32,114,97,99,101,115,32,105,110,112,117,116,10,51,78,117,109,98,101,114,32,111,102,32,116,105,109,101,115,32,100,111,101,115,32,110,111,116,32,109,97,116,99,104,32,110,117,109,98,101,114,32,111,102,32,100,105,115,116,97,110,99,101,115,10,26,85,110,115,111,108,118,97,98,108,101,32,105,110,112,117,116,32,100,101,116,101,99,116,101,100,10,15,73,110,116,101,114,110,97,108,32,101,114,114,111,114,10,18,73,110,112,117,116,32,112,97,114,115,101,32,101,114,114,111,114,10,32,73,110,118,97,108,105,100,32,111,112,99,111,100,101,32,111,114,32,99,111,109,98,111,32,111,112,101,114,97,110,100,10,0,1,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,2,192,1607,1613,1609,1612,1608,1614,1611,64,0,0,0,1615,0,1695,0,1655,0,1735,0,1695,1735,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0"
			code_file
		));


	cpu = cpu.WithIO(new IntcodeInput(
/*
@"Register A: 4611686018427387903
Register B: 1048576
Register C: 1

Program: 5,2
")*/


/*
@"Register A: 2024
Register B: 0
Register C: 0

Program: 0,3,5,4,3,0
"
*/
@"123
"
/*
@"Register A: 729
Register B: 0
Register C: 0

Program: 0,1,5,4,3,0
"*/),
		//o => o.Dump("output")
		PrintOutput
	);

	try
	{
	//	cpu = cpu.Patch((1, 1));
		
		if (WRITE_EXECLOG) ExecutionLogFile = new StreamWriter(
				Path.Combine( Path.GetDirectoryName(Util.CurrentQueryPath), @"intcode-execlog.txt"), false, new UTF8Encoding(false));
		Console.WriteLine("PART 1");
		RunUntilHalt(cpu);
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


// ALL THIS FUCKING WORK because BinarySearch doesn't just let you pass a callback routine
static class DelegateComparer {
	public static DelegateComparer<T>  Create<T>(Func<T?, T?, int> cb) => new DelegateComparer<T>(cb);
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
	if (address > obj.address) { sb.Append('+').Append((address - obj.address).ToString());}
	sb.Append(')');
	return sb.ToString();
}

const int MaxTraceLog = 40_000;
static int instr_num = 0;
static Queue<string> Last1000Instructions = new Queue<string>();
static TextWriter ExecutionLogFile;
static void LogExecutionMessage(string message)
{
	if (ExecutionLogFile == null) return;

	++instr_num;
	var full_message = "{" + instr_num + "} " + message;
	ExecutionLogFile?.WriteLine(full_message);
	Last1000Instructions.Enqueue(full_message);
	if (Last1000Instructions.Count > MaxTraceLog) Last1000Instructions.Dequeue();
}

void PrintOutput(memval_t value)
{
	if (value == '\n') Console.WriteLine();
	else if (value < 0x20 || value >= 0x7F) value.Dump("Dumped value");
	else Console.Write((char)value);
}

static HashSet<int> Breakpoints= new HashSet<int>() { 
		//528, 798,
	//93, 
	};

static HashSet<int> MemoryBreakPoints = new HashSet<int>()
{
	//2166,
};

IntcodeCpu RunUntilHalt(IntcodeCpu cpu)
{
	int limit = 10_000_000;
	while (!cpu.IsHalted)
	{
		if (Breakpoints.Contains(cpu.Pc))
		{
			ExecutionLogFile?.Flush();
			Console.WriteLine("Breakpoint at PC={0}", cpu.Pc);
			Util.Break();
		}
		if (--limit <= 0) throw new Exception("cpu rlimit exceeded");
		if (ExecutionLogFile != null && _addr2Name.TryGetValue(cpu.Pc, out var name))
			ExecutionLogFile.WriteLine("## {0}", name);
		//cpu.Dump();
		cpu = cpu.ExecuteInstruction();
		//cpu.Dump();
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

		if (false)
			Console.WriteLine("Read character: {0}",
					(value < 0x20) ? $"0x{value:x2}" : "'" + char.ConvertFromUtf32((int)value) + "'"
				);

		return new IntcodeInput(_input, offset);
	}
}

struct IntcodeCpu
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

	public readonly ImmutableArray<memval_t> Memory;
	//public readonly ImmutableQueue<memval_t> Input;
	public readonly IntcodeInput Input;
	public readonly Action<memval_t> Output;
	public readonly int Toc;
	public readonly int Pc;
	public bool IsHalted => Pc < 0;

	readonly ExecutionTrace _trace;

	public IntcodeCpu(ImmutableArray<memval_t> memory, IntcodeInput input, Action<memval_t> output, int pc, int toc) : this(memory, input, output, pc, toc, null) { }

	public IntcodeCpu Patch(params (int offset, memval_t value)[] patches)
	{
		var mem = Memory;
		foreach (var patch in patches)
		{
			mem = mem.SetItem(patch.offset, patch.value);
		}
		return new IntcodeCpu(mem, Input, Output, Pc, Toc, _trace);
	}

	//public IntcodeCpu WithInput(params int[] input) => WithInput(ImmutableQueue.CreateRange(input));

	public IntcodeCpu WithIO(IntcodeInput input, Action<memval_t> output)
	{
		return new IntcodeCpu(Memory, input, output, Pc, Toc, _trace);
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

	IntcodeCpu(ImmutableArray<memval_t> memory, IntcodeInput input, Action<memval_t> output, int pc, int toc, ExecutionTrace trace)
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
		var newPc = Pc + 1;
		return new IntcodeCpu(Memory, Input, Output, (-1 - Pc), Toc, trace);
	}

	IntcodeCpu SetTocOp(ExecutionTrace trace, AddressingModeData directMask)
	{
		var pc = Pc + 1;
		var pa = Memory[pc++];

		var (a, a_ptr) = ReadValue(directMask[0], pa);
		var newToc = (int)(Toc + a);
		trace.SetArguments(newToc);

		LogExecutionMessage($"New TOC = {newToc}");

		return new IntcodeCpu(Memory, Input, Output, pc, newToc, trace);
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

	IntcodeCpu WriteMemory(int newPc, ImmutableArray<memval_t> newMemory, ExecutionTrace trace)
	{
		return new IntcodeCpu(newMemory, Input, Output, newPc, Toc, trace);
	}


	IntcodeCpu WriteMemory(int newPc, int dest, memval_t value, ExecutionTrace trace = null)
	//=> WriteMemory(newPc, Memory.SetItem((int)dest, value), trace);
	{
		LogExecutionMessage(string.Format("Storing {0} at address {1}", value, DescribeAddress(dest, Toc)));
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
			var moreMem = Memory.ToBuilder();
			moreMem.AddRange(new memval_t[1024 + dest]);
			Console.WriteLine("resizing from {0} to {1}", Memory.Length, moreMem.Count);
			mem = moreMem.ToImmutable();
		}
		return WriteMemory(newPc, mem.SetItem(dest, value), trace);
	}

	IntcodeCpu ConsumeInput(int newPc, ImmutableArray<memval_t> newMemory, IntcodeInput newInput, ExecutionTrace trace)
	{
		return new IntcodeCpu(newMemory, newInput, Output, newPc, Toc, trace);
	}

	IntcodeCpu ConsumeInput(int newPc, int dest, ExecutionTrace trace = null)
	{
		var newInput = Input.Dequeue(out var value);
		var newMemory = Memory.SetItem((int)dest, value);
		return new IntcodeCpu(newMemory, newInput, Output, newPc, Toc, trace);
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

		return new IntcodeCpu(
				Memory,
				Input,
				Output,
				pc,
				Toc,
				trace
				);
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

		return new IntcodeCpu(Memory, Input, Output, pc, Toc, trace);
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
		LogExecutionMessage(sb.ToString());
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
		var builder = ImmutableArray.CreateBuilder<memval_t>();
		builder.AddRange(memory_text.Split(',').Select(n => memval_t.Parse(n)));
		builder.AddRange(new memval_t[1024]);
		var memory = builder.ToImmutable();
		

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