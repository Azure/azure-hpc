#!/usr/bin/env bash
sudo rm -r calls
sudo rm -r sorted_reads
sudo rm -r mapped_reads
sudo rm -r report.html
snakemake
