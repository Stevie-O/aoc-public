Intcode Assembler
=================

The intcode assembler (compiler.pl) is based on topaz's aoc2019-intcode here: https://github.com/topaz/aoc2019-intcode

I've made several tweaks to accommodate my personal needs:

1. The `--trace` option (which prints out instructions as they are processed) has been modified to prefix each line with its address, to simplify debugging
2. The `--labels` option prints labels in address order instead of alphabetical order, although I probably don't need that anymore now that I made the --trace change
3. The original compiler accepted syntax like `'X'` to represent the ASCII value of X.  I extended the parser so that `-'X'` is understood to be the negative of the ASCII value of X.
