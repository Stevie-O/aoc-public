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

### 2024-12-26 4:30pm

After several grueling hours, I have finally identified the bug in my code: `process_monkey` incorrectly initialized a variable
to 0.  As a result, the answer it gave for part 2 only included patterns that the final monkey in the input could generate.
Removing that assignment statement gave the correct answer.

Now, to deal with the fact that the implementation that runs on the raw input takes 26 minutes to execute on my machine...

(also: before I was inadvertently running in 32-bit mode.  So I cut out 6 minutes of execution time by switching to 64-bit mode.)

Instructions executed: 3_588_060_717

### 2024-12-26 7:30pm

After unrolling the xorshift_step code, the new execution stats are as follows:

- Intcode cycles: 2_089_236_717
- Execution time: 21 minutes 3 seconds

### 2024-12-26 8:40pm

Some code in my intcode implementation meant for diagnostic tracing was running even when the tracing was disabled.

New execution time for my unrolled version: under 8 minutes.

### 2024-12-26 9:00pm

Okay, I unrolled the loop for price extraction and optimized the mod_10 logic, and that took the instruction count down by over 50%!

- Intcode cycles: 950_671_719
- Execution time: 3 minutes 53 seconds.

I should be able to nock another 70M cycles off of that, however, with one last unrolling.

### 2024-12-26 10:00pm

I unrolled the loop for pattern extraction and now it's at:

- Intcode cycles: 811_741_374
- Execution time: 2 minutes 27 seconds.

This is as good as I can reasonably hope for; time to post it to Reddit!