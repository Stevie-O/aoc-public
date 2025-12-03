#!/usr/bin/env -Sawk --posix -f
# additional options: -vprint_invalid_ids=1
BEGIN { RS = ","; FS = "-"; part1_answer = 0; part2_answer = 0; 

# divisors[len,divisor_id,X]
#	X=1: number of partitions
#	X=2: length of each partitions
#   X=3: scalar
#   X=4: string repeat helper

max_length = split(";;2,1,11,xx;;3,1,111,xxx;;2,2,101,xx;4,1,1111,xxxx;;5,1,11111,xxxxx;;2,3,1001,xx;3,2,10101,xxx;6,1,111111,xxxxxx;;7,1,1111111,xxxxxxx;;2,4,10001,xx;4,2,1010101,xxxx;8,1,11111111,xxxxxxxx;;3,3,1001001,xxx;9,1,111111111,xxxxxxxxx;;2,5,100001,xx;5,2,101010101,xxxxx;10,1,1111111111,xxxxxxxxxx", length_table, ";;");
for (len=2; len<=max_length; len++) {
	num_divisors = split(length_table[len],divisor_list,";")
	
	divisors[len,0,0] = num_divisors
	divisors[len,0,1] = (10 ^ len) - 1
	for (i=1; i<=num_divisors; i++)
	{
		num_fields = split(divisor_list[i], divisor_info, ",")
		for (j = 1; j <= num_fields; j++)
		{
			value = divisor_info[j];
			if (j != 4) value += 0;
			divisors[len,i,j] = value
		}
	}
}
}

function init() {
   id_len = length($1);
   current_id = $1 + 0;
   range_end = $2 + 0;
   current_id_limit = divisors[id_len,0,1];
   split("", counters);
   num_divisors = divisors[id_len,0,0];
   for (i=1; i<=num_divisors; i++) {
	 prefix_len = divisors[id_len,i,2];
	 scalar = divisors[id_len,i,3];
	 initial_counter = divisors[id_len,i,4];
	 gsub("x", substr($1, 1, prefix_len), initial_counter);
	 counters[i] = initial_counter + 0;
   }
}

function seek_invalid_id() {
   next_invalid_id = range_end + 1;
   next_invalid_id_segment_count = 0
   for (i=1; i<=num_divisors; i++) {
	while (counters[i] < current_id) {
		counters[i] += divisors[id_len,i,3]
    }
	if (counters[i] < next_invalid_id) {
		next_invalid_id = counters[i]
		next_invalid_id_segment_count = divisors[id_len, i, 1]
	}
   }
   current_id = next_invalid_id
   current_id_segment_count = next_invalid_id_segment_count   
}

function find_all_invalid_ids() {
   init();
   seek_invalid_id();
   while (current_id <= range_end && current_id <= current_id_limit) {
		if (print_invalid_ids) {
			print current_id_segment_count " " current_id
		}
		part2_answer += current_id;
		if (current_id_segment_count == 2)
			part1_answer += current_id;
		current_id++;
		# needed to avoid issues with seek_invalid_id()
		if (current_id > current_id_limit) break;
		seek_invalid_id();
	}
	if (current_id > range_end) {
		return 1;
	} else {
		ofs_save=OFS; OFS=FS
		$1 = current_id "";
		OFS=ofs_save
		return 0;
	}
}

# fixup needed for the example input, which contains embedded newlines
/^[[:space:]]|[[:space:]]$/ { sub(/^[[:space:]]+/, ""); sub(/[[:space:]]+$/, ""); }

# { print "test: $0 = '" $0 "' $1 = " $1 " $2 = " $2 }

/^[0-9]-[0-9]+$/    { ofs_save=OFS; OFS=FS; $1 = "11"; OFS=ofs_save; }
/^[0-9]{2}-[0-9]+$/ { if (find_all_invalid_ids()) next; }
/^[0-9]{3}-[0-9]+$/ { if (find_all_invalid_ids()) next; }
/^[0-9]{4}-[0-9]+$/ { if (find_all_invalid_ids()) next; }
/^[0-9]{5}-[0-9]+$/ { if (find_all_invalid_ids()) next; }
/^[0-9]{6}-[0-9]+$/ { if (find_all_invalid_ids()) next; }
/^[0-9]{7}-[0-9]+$/ { if (find_all_invalid_ids()) next; }
/^[0-9]{8}-[0-9]+$/ { if (find_all_invalid_ids()) next; }
/^[0-9]{9}-[0-9]+$/ { if (find_all_invalid_ids()) next; }
/^[0-9]{10}-[0-9]+$/ { if (find_all_invalid_ids()) next; }
{ print "Unrecognized input at record " NR ": " $0; failed_exit = 1; exit 1 }

END {
	if (failed_exit) exit failed_exit;
	print "Part 1: " part1_answer
	print "Part 2: " part2_answer
}



function join(array, start, end, sep,    result, i)
{
    if (sep == "")
       sep = " "
    else if (sep == SUBSEP) # magic value
       sep = ""
    result = array[start]
    for (i = start + 1; i <= end; i++)
        result = result sep array[i]
    return result
}
