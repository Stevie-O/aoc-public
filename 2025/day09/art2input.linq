<Query Kind="Statements" />

// drawn with https://asciiflow.com/#/

string Art = @"
┌─────┐┌───────────┐
│     ││           │
│  ┌──┘└─────────┐ │
│  │             │ │
│  │             │ │
│  │             │ │
│  │             │ │
│  └─────────────┘ │
│                  │
└──────────────────┘
".Trim();

const char CORNER_DN_RT = '┌';
const char CORNER_DN_LT = '┐';
const char CORNER_UP_RT = '└';
const char CORNER_UP_LT = '┘';

var lines = Art.Split(Environment.NewLine);
//lines.Dump();
var (start_r, start_c) =
	(from r in Enumerable.Range(0, lines.Length)
	 from c in Enumerable.Range(0, lines[0].Length)
	 where lines[r][c] == '┌'
	 select (r, c)
	).First();

Console.WriteLine("{0},{1}", start_c, start_r);

const int NORTH = 0, EAST = 1, SOUTH = 2, WEST = 3;
var dir_dydx = new (int dy, int dx)[] { (-1, 0), (0, 1), (1, 0), (0, -1) };
string[] dir_names = { "NORTH", "EAST", "SOUTH", "WEST" };
const string LINE_SYMS = "│─│─";

var dir = EAST;
var (dy, dx) = dir_dydx[dir];
var line_sym = LINE_SYMS[dir];


var cur_r = start_r + dy;
var cur_c = start_c + dx;

while ((cur_r, cur_c) != (start_r, start_c))
{
	var cur_sym = lines[cur_r][cur_c];
	if (cur_sym != line_sym)
	{
		var new_dir = dir switch
		{
			NORTH => cur_sym switch
			{
				CORNER_DN_RT => EAST,
				CORNER_DN_LT => WEST,
				_ => throw new Exception($"Unexpected character '{cur_sym}' at row={cur_r}, col={cur_c} while traveling {dir_names[dir]}"),
			},
			SOUTH => cur_sym switch
			{
				CORNER_UP_RT => EAST,
				CORNER_UP_LT => WEST,
				_ => throw new Exception($"Unexpected character '{cur_sym}' at row={cur_r}, col={cur_c} while traveling {dir_names[dir]}"),
			},
			EAST => cur_sym switch
			{
				CORNER_UP_LT => NORTH,
				CORNER_DN_LT => SOUTH,
				_ => throw new Exception($"Unexpected character '{cur_sym}' at row={cur_r}, col={cur_c} while traveling {dir_names[dir]}"),
			},
			WEST => cur_sym switch
			{
				CORNER_UP_RT => NORTH,
				CORNER_DN_RT => SOUTH,
				_ => throw new Exception($"Unexpected character '{cur_sym}' at row={cur_r}, col={cur_c} while traveling {dir_names[dir]}"),
			},
			_ => throw new Exception("invalid direction " + dir + "???"),
		};
		Console.WriteLine("{0},{1}", cur_c, cur_r);
		dir = new_dir;
		(dy, dx) = dir_dydx[dir];
		line_sym = LINE_SYMS[dir];
	}
	(cur_r, cur_c) = (cur_r + dy, cur_c + dx);
}
