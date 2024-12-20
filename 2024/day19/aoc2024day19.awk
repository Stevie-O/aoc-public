/,/ {
	split($0, patterns, ", ")
	pattern_count = NF
	next
}

$0 {
	# okay, so first, this entire loop is executing for *one* single design string.       
	# clean up after the previous word
	# NOTE: we do *not* clean up memo
    if ($0 in memo) {
        if (memo[$0] > 0) ++p1;
        p2 += memo[$0];
        next
    }

	delete word_pending
	delete word_count
	delete queue_length
	delete queue
    delete words_queued
	# d = design ($0)
	d = $0
	dl = length($fw)
	queue[dl, ++queue_length[dl]] = d
    words_queued[d] = 1
	total_queued = 1
	while (total_queued > 0) {
		# find the shortest subword that we're working on
		for (wl = 1; wl <= dl; wl++) {
			ql = queue_length[wl]
			if (ql > 0) {
				qwe = queue[wl, ql]
				--queue_length[wl]
				--total_queued
				break
			}
		}
		w = qwe
        delete words_queued[w]
    #    print "dequeuing: " w " total_queued=" total_queued
        no_cache=0
        pending_count=0
		# solve for w
		if (w == "") {
            print "this shouldn't have happened"
			w_match_count = 1
            no_cache=1
		} else if (w in memo) {
            print "cache hit!"
			w_match_count = memo[w]
            no_cache=1
		} else {
			# schedule up recursive calls into w
            w_match_count = 0
			for (i = 1; i <= pattern_count; i++) {
				wr = w	# w right
                #print "checking for pattern: " patterns[i]
				if (sub("^" patterns[i], "", wr)) {
                    #print "splitting " w " into " patterns[i] " and " wr
					wrl = length(wr)	# wr length
                    if (wrl == 0) {
                        ++w_match_count
                        #print "word count for " w " is now " word_count[w]
                    } else if (wr in memo) {
                        w_match_count += memo[wr]
                        #print "word count for " wr " memoized as " memo[wr]
                        #print "word count for " w " is now " word_count[w]
                    } else if (wr in words_queued) {
                        pending_count++
                        #print "We've already got " wr " in the works" 
                    } else {
                        pending_count++;
                        words_queued[wr]=1
    					queue[wrl, ++queue_length[wrl]] = wr
		    			++total_queued
			    		++words_pending[w]
                        #print "need to recurse for " w " -> " wr ". pending_count = " pending_count " total_queued " total_queued " words_pending " words_pending[w] 
                    }
                }
            }
			if (pending_count > 0) {
				w_match_count = -1
			}
		}
        if (no_cache) {
            # no-op
        } else if (w_match_count < 0) {
			# we don't have an answer yet, so we'll have to requeue it
        #    print "requeuing: " w " because there are " words_pending[w] " substrings the answer depends on"
			wl = length(w)
            words_queued[w]=1
			queue[wl, ++queue_length[wl]] = w
			++total_queued
		} else {
			word_count[w] += w_match_count
            #print "added " w_match_count " to possibles for " w ", total now " word_count[w]
			if (--word_pending[w] <= 0) {
				# oh! we have our answer!
                #print "memo for " w " -> " word_count[w]
				memo[w] = word_count[w]
			}
		}
	}
	if (memo[d] > 0) {
		p1++
	}
	p2 += memo[d]

    print "line " NR ": (" d ") -> p1=" p1 ", p2=" p2
}

END { print "Part 1: " p1 "\nPart 2: " p2 }
