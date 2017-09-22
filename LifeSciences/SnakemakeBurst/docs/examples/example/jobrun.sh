#!/usr/bin/env bash
cd /home/bizdata/fileshare/example ;
 samtools sort -T sorted_reads/B -O bam mapped_reads/B.bam > sorted_reads/B.bam
