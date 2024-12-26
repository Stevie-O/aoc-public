# 2024 Day 22 intcode notes

### 2024-12-25

Man, this is SLOW.

When I finally put it all together and had it answering the example correctly, it took 2.78s.

When I fed it my actual input, it took 32 minutes and 17 seconds and _gave the wrong answer for part 2_, which I gotta say was a bit disheartening.  (For the record, it executed 3,588,081,552 CPU instructions.)

It gave the right answer for part 1, however, so I believe that at minimum, my Xorshift implementation is working.

### 2024-12-26 2pm

The next thing to check is the price conversions -- modulo 10 ain't easy in intcode.

Fortunately, I have a debugging mode (MODE2) that just prints out the prices for monkeys.

There are 2313 monkeys in my puzzle input.  That means generating 2001 \* 2313 = 4,628,313 prices.

This took 26 minutes and 9 seconds, and executed 3,507,884,395 CPU instructions.

This means that only about 6 minutes and 81M of the instructions I executed on my first real attempt were for computing the price differences.

### Comparison results

I compared the output from MODE2 to my C# implementation for day 22 part 2, and all of the prices matched.
So I suppose the good news is that the problem is somewhere else in the code.  Unfortunately, I'm not sure of a good way to test that faster...


### 2024-12-26 3:30pm

Okay, I have verified that my pattern ID calculations match those of my C# code.  So the problem must lie elsewhere.
