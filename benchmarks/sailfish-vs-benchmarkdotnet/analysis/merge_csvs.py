#!/usr/bin/env python3
"""Merge the two runner CSVs (samples_sailfish.csv, samples_bdn.csv) into the
samples.json consumed by make_svgs.py, and print summary statistics per series.

Usage:
    python3 merge_csvs.py <output-dir-containing-csvs> [out.json]
"""
import csv, json, math, os, sys

out_dir = sys.argv[1]
out_json = sys.argv[2] if len(sys.argv) > 2 else os.path.join(out_dir, 'samples.json')

data = {}
for fn in ('samples_sailfish.csv', 'samples_bdn.csv'):
    with open(os.path.join(out_dir, fn)) as f:
        for r in csv.DictReader(f):
            tool = r['tool'].split('(')[0]  # strip BDN job-parameter suffix
            data.setdefault(r['workload'], {}).setdefault(tool, []).append(round(float(r['value_ns']), 1))

json.dump(data, open(out_json, 'w'))
print(f'wrote {out_json}')


def quantile(s, p):
    n = len(s); i = p * (n - 1); lo = int(i); hi = min(lo + 1, n - 1); fr = i - lo
    return s[lo] * (1 - fr) + s[hi] * fr


for wl, series in data.items():
    for tool, vals in series.items():
        s = sorted(vals)
        n = len(s)
        mean = sum(s) / n
        sd = math.sqrt(sum((x - mean) ** 2 for x in s) / (n - 1))
        print(f'{wl:12s} {tool:18s} n={n:6d} median={quantile(s, .5):10.0f} ns '
              f'p95={quantile(s, .95):10.0f} p99={quantile(s, .99):10.0f} cv={sd / mean * 100:6.1f}%')
