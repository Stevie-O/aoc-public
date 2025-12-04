#!/usr/bin/env perl
use strict;
use warnings FATAL => 'all';

use Getopt::Long;

my %opt;
die unless GetOptions(\%opt, 'trace', 'labels', 'randomize', 'mapfile=s');

my $asm = do { local $/; <> };
my ($bytecode, $label_addr) = compile($asm,\%opt);
if ($opt{trace}) {
  warn "\n";
}
if ($opt{labels} || defined $opt{mapfile}) {
  my $mapfile;
  if (!defined $opt{mapfile}) { $mapfile = *STDERR; }
  else { open($mapfile, '>', $opt{mapfile}) or die "can't write to $opt{mapfile}: $!"; }
  print $mapfile "$label_addr->{$_}\t$_\n" for sort { $label_addr->{$a} <=> $label_addr->{$b} || $a cmp $b } keys %$label_addr;
  print $mapfile "\n";
}
print join(",", @$bytecode)."\n";

sub flipcoin() { !($opt{randomize}) || rand()<.5 }

sub compile {
  my ($asm, $opt) = @_;

  $opt ||= {};

  my %instr = (
    hlt => { opcode=>99, args=>0, lvals=>[], },
    add => { opcode=> 1, args=>3, lvals=>[0,0,1], },
    mul => { opcode=> 2, args=>3, lvals=>[0,0,1], },
    in  => { opcode=> 3, args=>1, lvals=>[1], },
    out => { opcode=> 4, args=>1, lvals=>[0], },
    jt  => { opcode=> 5, args=>2, lvals=>[0,0], },
    jf  => { opcode=> 6, args=>2, lvals=>[0,0], },
    lt  => { opcode=> 7, args=>3, lvals=>[0,0,1], },
    eq  => { opcode=> 8, args=>3, lvals=>[0,0,1], },
    rbo => { opcode=> 9, args=>1, lvals=>[0], },
  );

  my %str_escape = (
    '\\' => "\\",
    'n'  => "\n",
	'r'  => "\r",
    't'  => "\t",
    '"' => '"',
    "'" => "'",
    's'  => " ", #so tokens like '\s' have no whitespace for regexes like /(\S+)/
  );

  my @compiled;
  #my @lines = grep {/\S/} split(/\n/, $asm);
  my $linenum = 1; 
  my @lines = map { [ $linenum++, $_ ] } split /\n/, $asm;

  my %label_addr;
  my %label_defline;
  my %label_mode;
  my %label_uses;
  my %label_linerefs;
  my $label_prefix = "";
  my %label_prefix_except;

  my %functions;

  my $warn = sub {
    warn "\e[33m[warn]\e[0m $_[0] at line $.\n";
  };

  my $_cur_uid = 0;
  my $gen_uid = sub { ++$_cur_uid };
  
  my $condjump_microcode = sub {
		my ($name, $cmpinstr, $jmpinstr, $argswap) = @_;
		return sub {
			my ($line) = @_;
			die "\@$name line invalid: $line" unless $line =~ /^\@$name\s+(\S+)\s+(\S+)\s+(\S+)\s*$/;
			my ($cmp1, $cmp2, $target) = ($1, $2, $3);
			my $label = "condresult".$gen_uid->();
			if ($argswap) { ($cmp1, $cmp2) = ($cmp2, $cmp1); }
			return (
				"$cmpinstr $cmp1 $cmp2 $label",
				"$jmpinstr $label:0 $target"
			)
		};	
  };

  my %macro = (
    fn => sub {
      my ($line) = @_;
      die '@fn when not in top scope' unless $label_prefix eq '';
      die if %label_prefix_except;

      die "invalid \@fn declaration" unless $line =~ /^\@fn\s+(\d+)\s+(\w+)\(\s*([^\)]*?)\s*\)(?:\s+local\(\s*([^\)]+?)\s*\))?(?:\s+global\(\s*([^\)]+?)\s*\))?\s*$/;
      my ($retnum, $fnname, $arglist, $locallist, $globallist) = ($1, $2, $3, $4, $5);

      die "duplicate function $fnname" if $functions{$fnname};
      die "function label $fnname is already used elsewhere" if exists $label_addr{$fnname} || exists $label_mode{$fnname};

      $label_addr{$fnname} = @compiled;
	  $label_defline{$fnname} = $.;

      my @args    =                       map { lc } split(/\,\s*/, $arglist   );      die "invalid function $fnname arglist"    if grep {!/^(?!\d)\w+$/} @args;
      my @locals  = defined $locallist  ? map { lc } split(/\,\s*/, $locallist ) : (); die "invalid function $fnname locallist"  if grep {!/^(?!\d)\w+$/} @locals;
      my @globals = defined $globallist ? map { lc } split(/\,\s*/, $globallist) : (); die "invalid function $fnname globallist" if grep {!/^(?!\d)\w+$/} @globals;

      my %use_num; $use_num{$_}++ for (@args, @locals, @globals);
      die "duplicate label [", join(' ', grep {$use_num{$_}>1} keys %use_num), "] in \@fn $fnname declaration" if grep {$_>1} values %use_num;

      die if $fnname =~ /__/;
      $label_prefix = "fn_${fnname}__";
      %label_prefix_except = map {$_=>1} (@globals, $fnname);

      # new labels get prefix, but abort if it matches any global
      # label uses get prefix except globals
      # all args become ~-4, ~-3, ...
      # all locals become ..., ~-2, ~-1
      # return0 is ~-4, return1 is ~-3, ...
      #
      # #pass0 is ~1, pass1 is ~2, ...

      my $mk_label = sub {
        my ($label, $addr, $mode) = @_;
        die unless defined $label && defined $addr && defined $mode;
        $label = $label_prefix.$label;
        die "duplicate label $label during function declaration" if exists $label_addr{$label} || exists $label_mode{$label};
        $label_addr{$label} = $addr;
		$label_defline{$label} = $.;
        $label_mode{$label} = $mode;
      };

      my @stack_vars = (@args, @locals);
      for (my $i=0; $i<@stack_vars; $i++) {
        $mk_label->($stack_vars[$i], $i-@stack_vars, 2);
      }

      for (my $i=0; $i<$retnum; $i++) {
        $mk_label->("return$i", $i-@stack_vars, 2); #this is allowed to reach ~0, ~1, ~2, ...
      }

      my $stack_size = 1 + @stack_vars; #retval + stack vars

      $functions{$fnname} = {
        stack_size => $stack_size,
      };

      return (
        "rbo $stack_size",
      );
    },
    endfn => sub {
      my ($line) = @_;
      die unless $line eq '@endfn';
      die '@endfn when not in function' unless $label_prefix =~ /^fn_(\w+?)__/;
      my $fnname = $1;
      die "\@endfn during unknown function $fnname" unless $functions{$fnname};
      $label_prefix = "";
      %label_prefix_except = ();
      return (
        "rbo -$functions{$fnname}{stack_size}",
        '@jmp ~0',
      );
    },
	callt => sub {
      my ($line) = @_;
      die "invalid \@callt syntax: $line" unless $line =~ /^\@callt\s+(\S+\s+)*(\S+)\s*$/;
	  my $fullexpr = "$1$2";
	  my $fnexpr = $2;
      $warn->("\@callt: $fnexpr is not an address") unless $fnexpr =~ /^(?:\w+\:)?\&\w+$/;
      my $ret_label = "retaddr".$gen_uid->();
      return (
        "\@cpy &$ret_label ~0",
        "jt $fullexpr",
        "$ret_label:",
      );
	},
	callf => sub {
      my ($line) = @_;
      die "invalid \@callt syntax: $line" unless $line =~ /^\@callf\s+(\S+\s+)*(\S+)\s*$/;
	  my $fullexpr = "$1$2";
	  my $fnexpr = $2;
      $warn->("\@callt: $fnexpr is not an address") unless $fnexpr =~ /^(?:\w+\:)?\&\w+$/;
      my $ret_label = "retaddr".$gen_uid->();
      return (
        "\@cpy &$ret_label ~0",
        "jf $fullexpr",
        "$ret_label:",
      );
	},
    call => sub {
      my ($line) = @_;
      die "invalid \@call syntax: $line"  unless $line =~ /^\@call\s+(\S+)\s*$/;
      my $fnexpr = $1;
      $warn->("\@call $fnexpr is not an address") unless $fnexpr =~ /^(?:\w+\:)?\&\w+$/;
      my $ret_label = "retaddr".$gen_uid->();
      return (
        "\@cpy &$ret_label ~0",
        "\@jmp $fnexpr",
        "$ret_label:",
      );
    },
    cpy => sub {
      my ($line) = @_;
      die "\@cpy line invalid: $line" unless $line =~ /^\@cpy\s+(\S+)\s+(\S+)\s*$/;
      my ($src, $dst) = ($1, $2);
      my ($op, $arg) = flipcoin() ? ("add",0) : ("mul",1);
      return flipcoin() ? "$op $src $arg $dst" : "$op $arg $src $dst";
    },
    jmp => sub {
      my ($line) = @_;
      die unless $line =~ /^\@jmp\s+(\S+)\s*$/;
      my ($addr) = ($1);
      my ($op, $arg) = flipcoin() ? ("jt",1) : ("jf",0);
      return "$op $arg $addr";
    },
    str => sub {
      my ($line) = @_;
      die "invalid string macro: $line" unless $line =~ /^\@str\s+(?:(\w+)\:\s*)?\"(.*?)\"\s*$/;
      my ($label, $str) = ($1,$2);
      $str =~ s/\\(.)/$str_escape{$1} \/\/ die "unknown str escape for $1"/ge;
      return '@raw '.(defined $label ? "$label:" : "").join(" ", length($str), map {ord($_)} split(//,$str));
    },
	jeq => $condjump_microcode->('jeq', 'eq', 'jt', 0), # jump if a=b
	jne => $condjump_microcode->('jne', 'eq', 'jf', 0), # jump if not (a=b)

	jlt => $condjump_microcode->('jlt', 'lt', 'jt', 0), # jump if a<b
	jgt => $condjump_microcode->('jgt', 'lt', 'jt', 1), # jump if (a>b) means jump if (b<a)

	jge => $condjump_microcode->('jge', 'lt', 'jf', 0), # jump if not(a<b)
	jle => $condjump_microcode->('jle', 'lt', 'jf', 1), # jump if (a <= b) means jump if (b >= a)
  );

  for (my $line_i=0; $line_i<@lines; $line_i++) {
    my $line = $lines[$line_i];
	if (ref $line eq 'ARRAY') {
		$. = $line->[0];
		$line = $line->[1];
	}
	next unless $line =~ /\S/;
    $line =~ s/^\s+|\s+$//g;
    next if $line =~ /^\s*(?:\#.*?)?$/;

    if ($line =~ /^\@(\w+)/ && $macro{$1}) {
      warn "\e[1;31m$line\e[0m\n" if $opt->{trace};
      splice(@lines, $line_i, 1, $macro{$1}($line));
      redo;
    }

    warn "\e[36m", sprintf('%6d ', scalar(@compiled)), "$line\e[0m\n" if $opt->{trace};

    my @in_vals = grep {/\S/} split(/\s+/, $line);
    my @out_vals;

    my $linemode = '';
    if ($in_vals[0] eq '@raw') {
      shift @in_vals;
      $linemode = 'raw';
    }
	elsif ($in_vals[0] eq '@reference') {
	  shift @in_vals;
	  $linemode = 'reference'
	}

    for (my $i=0; $i<@in_vals; $i++) {
      while ($in_vals[$i] =~ s/^((?!\d)\w+)\://) {
        my $label = lc $1;
        die "attempted declaration of prefix-excepted label $label (probably overlaps predeclared global)" if $label_prefix_except{$label};
        die "double underscore in labels is reserved" if $label =~ /__/;
        $label = "$label_prefix$label";
        die "duplicate label $label" if exists $label_addr{$label};
        $label_addr{$label} = @compiled + $i;
		$label_defline{$label} = $.;
      }
      if ($in_vals[$i] !~ /\S/) {
        shift @in_vals;
        last unless @in_vals;
        redo;
      }
      if ($in_vals[$i] =~ /^(-)?\'(?:([^\'\\])|\\(.))\'$/) {
        my ($neg, $literal, $escaped) = ($1, $2, $3);
        if (defined $literal) {
          $in_vals[$i] = ord($literal);
        } elsif (defined $escaped) {
          die "unknown escape sequence \\$escaped in char" unless exists $str_escape{$escaped};
          $in_vals[$i] = ord($str_escape{$escaped});
        } else {
          die 'unreachable';
        }
		if (defined($neg) && $neg eq '-') { $in_vals[$i] *= -1; }
      }

      if ($i == 0 && $linemode ne 'raw' && $linemode ne 'reference') {
        if ($in_vals[0] =~ /^\-?\d+$/) {
          $warn->("using literal value as opcode with no parameter validation in line: $line");
          $out_vals[$i] = $in_vals[0];
		} elsif ($in_vals[0] =~ /^!__reference\s+(\w+)$/) {
        } else {
          die "unknown instruction $in_vals[0] in line: $line" unless exists $instr{$in_vals[0]};
          die "wrong number of args for instruction $in_vals[0] of line: $line" unless @in_vals == $instr{$in_vals[0]}{args}+1;
          die "wrong length lval definition for instruction $in_vals[0]" unless $instr{$in_vals[0]}{args} == @{$instr{$in_vals[0]}{lvals}};
          $out_vals[$i] = $instr{$in_vals[0]}{opcode};

		  $warn->("jump to variable address in line: $line") 
		  if $in_vals[0] =~ /^j[tf]$/ && 	$in_vals[2] !~ /^(?:\w+\:)?(?:\&\w+|\~0)$|^\w+:\*-?\w+$/;
		}
      } else {
        my $addr_mode;

        if ($in_vals[$i] =~ /^([\*\~]?)(\-?\d+)$/) {
          my ($mode_str, $val) = ($1, $2);
          $addr_mode = $mode_str eq '*' ? 0 : $mode_str eq '' ? 1 : $mode_str eq '~' ? 2 : die;
          $out_vals[$i] = $val;
		} elsif ($in_vals[$i] eq '__LINE__') {
			$addr_mode = 1;
			$out_vals[$i] = $.;
        } elsif ($in_vals[$i] =~ /^([\&\*]?)([a-z_]\w*)$/i) {
          my ($mode_str, $label) = ($1, lc $2);
          $label = "$label_prefix$label" unless $label_prefix_except{$label};
          if (exists $label_mode{$label}) {
            die "mode flag illegal on label $label with defined mode $label_mode{$label}" if $mode_str ne '';
            $addr_mode = $label_mode{$label};
          } else {
            $addr_mode = $mode_str eq '&' ? 1 : 0;
          }
          $out_vals[$i] = "LABEL";
		  if ($linemode eq 'reference') {
				push @{$label_uses{$label}}, ();
		  } else {
			push @{$label_uses{$label}}, @compiled + $i;
		  }
		  push @{$label_linerefs{$label}}, $.;
        } else {
				print STDERR "in_vals: ", join(', ', map "[$_]", @in_vals), "\n";
          die "unrecognized value <$in_vals[$i]> at index $i of line: $line";
        }

        if ($linemode eq 'raw') {
          die "values must be immediate in raw line: $line" unless $addr_mode == 1;
	    } elsif ($linemode eq 'reference') {
			@out_vals = ();
        } else {
          die "immediate value in lval parameter in line: $line" if $addr_mode == 1 && $instr{$in_vals[0]} && $instr{$in_vals[0]}{lvals}[$i-1];
          $out_vals[0] += $addr_mode * 10**($i+1);
        }
      }
    }

    die "instruction $in_vals[0]} produced wrong number of out vals in line: $line" if $linemode ne 'raw' && @in_vals && $instr{$in_vals[0]} && @out_vals != $instr{$in_vals[0]}{args}+1;

    push @compiled, @out_vals;
  }

  $label_addr{auto__end} = scalar @compiled;

  my @issues;
  for my $label (keys %label_uses) {
    for my $use_addr (@{$label_uses{$label}}) {
      push @issues, "label $label addr $use_addr somehow outside of compiled bounds" if $use_addr > $#compiled;
      push @issues, "label $label referenced (line " . join(', ', @{$label_linerefs{$label}}) . ") but never defined" unless exists $label_addr{$label};
      $compiled[$use_addr] = $label_addr{$label};
    }
  }
  for my $label (keys %label_addr) {
    push @issues, "label $label defined (line $label_defline{$label}) but never referenced" unless exists $label_uses{$label} || $label =~ /^auto__/;
  }

  if (@issues) {
    warn map{"- $_\n"} sort @issues;
    die +(scalar @issues)." issue(s) found";
  }

  die if grep {$_ eq 'LABEL'} @compiled;

  return (\@compiled, \%label_addr);
}
