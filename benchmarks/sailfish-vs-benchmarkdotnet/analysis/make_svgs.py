#!/usr/bin/env python3
"""Render the benchmark-comparison violin plots as self-contained dark-card SVGs
for the Sailfish docs site.

Usage:
    python3 make_svgs.py [samples.json] [output-dir]

Defaults (relative to this script): ../data/samples-10k-2026-08-22.json and
../../../site/public/benchmark-comparison. The input JSON maps
{workload: {series: [value_ns, ...]}} — produced by merging the two CSVs the
runners emit (see ../README.md).
"""
import json, math, os, sys

HERE = os.path.dirname(os.path.abspath(__file__))
DATA_PATH = sys.argv[1] if len(sys.argv) > 1 else os.path.join(HERE, '..', 'data', 'samples-10k-2026-08-22.json')
OUT_DIR = sys.argv[2] if len(sys.argv) > 2 else os.path.join(HERE, '..', '..', '..', 'site', 'public', 'benchmark-comparison')

DATA = json.load(open(DATA_PATH))

SERIES = [
    ('Sailfish', 'Sailfish', '#3987e5'),
    ('BDN-PerInvocation', 'BDN per-invocation', '#199e70'),
    ('BDN-Default', 'BDN default', '#d95926'),
]
WORKLOADS = [
    ('EfCoreQuery', 'efcorequery'),
    ('CpuHash', 'cpuhash'),
    ('TinyOp', 'tinyop'),
]
SURFACE, INK2, MUTED, GRID, BASELINE = '#1a1a19', '#c3c2b7', '#898781', '#2c2c2a', '#383835'
BORDER = 'rgba(255,255,255,0.10)'
MONO = "ui-monospace, SFMono-Regular, Menlo, Consolas, monospace"
SANS = "system-ui, -apple-system, 'Segoe UI', sans-serif"
W, GUTTER, RIGHT, ROW_H, HALF, AXIS_H, TOP = 820, 168, 28, 78, 26, 40, 18


def quantile(s, p):
    n = len(s); i = p * (n - 1); lo = int(i); hi = min(lo + 1, n - 1); fr = i - lo
    return s[lo] * (1 - fr) + s[hi] * fr


def kde_log(vals):
    xs = sorted(math.log10(v) for v in vals)
    n = len(xs)
    mean = sum(xs) / n
    sd = math.sqrt(sum((x - mean) ** 2 for x in xs) / (n - 1))
    iqr = quantile(xs, 0.75) - quantile(xs, 0.25)
    bw = max(0.9 * min(sd or 1e-3, (iqr or sd or 1e-3) / 1.34) * n ** -0.2, 0.008)
    lo, hi = xs[0] - 2.2 * bw, xs[-1] + 2.2 * bw
    pts = []
    for i in range(161):
        x = lo + (hi - lo) * i / 160
        d = sum(math.exp(-0.5 * ((x - xi) / bw) ** 2) for xi in xs)
        pts.append((x, d / (n * bw)))
    dmax = max(d for _, d in pts)
    return [(x, d / dmax) for x, d in pts]


def fmt(ns):
    def f(v, u):
        d = 0 if v >= 100 else 1 if v >= 10 else 2
        return f'{v:.{d}f} {u}'
    if ns < 1e3: return f(ns, 'ns')
    if ns < 1e6: return f(ns / 1e3, 'µs')
    return f(ns / 1e6, 'ms')


for wl_key, out_name in WORKLOADS:
    series = []
    for key, label, color in SERIES:
        vals = DATA[wl_key][key]
        s = sorted(vals)
        series.append(dict(key=key, label=label, color=color, vals=vals,
                           n=len(vals), median=quantile(s, 0.5)))
    all_vals = [v for s in series for v in s['vals']]
    lo_log = math.log10(min(all_vals)) - 0.06
    hi_log = math.log10(max(all_vals)) + 0.06
    H = TOP + len(series) * ROW_H + AXIS_H

    def x_of(lg):
        return GUTTER + (lg - lo_log) / (hi_log - lo_log) * (W - GUTTER - RIGHT)

    out = [f'<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 {W} {H}" '
           f'font-family="{SANS}" role="img" '
           f'aria-label="Violin plot of {wl_key} measurements: Sailfish vs BenchmarkDotNet">']
    out.append(f'<rect x="0.5" y="0.5" width="{W-1}" height="{H-1}" rx="12" fill="{SURFACE}" stroke="{BORDER}"/>')

    ticks = []
    for d in range(int(math.floor(lo_log)) - 1, int(math.ceil(hi_log)) + 2):
        for m in (1, 2, 5):
            lg = d + math.log10(m)
            if lo_log <= lg <= hi_log:
                ticks.append((lg, m == 1))
    shown = [t for t in ticks if t[1]] if len(ticks) > 8 else ticks

    plot_bottom = TOP + len(series) * ROW_H
    for lg, _ in shown:
        x = x_of(lg)
        out.append(f'<line x1="{x:.1f}" y1="{TOP}" x2="{x:.1f}" y2="{plot_bottom}" stroke="{GRID}" stroke-width="1"/>')
        out.append(f'<text x="{x:.1f}" y="{plot_bottom+22}" text-anchor="middle" fill="{MUTED}" '
                   f'font-size="11.5" font-family="{MONO}">{fmt(10**lg)}</text>')
    out.append(f'<line x1="{GUTTER-8}" y1="{plot_bottom}" x2="{W-RIGHT}" y2="{plot_bottom}" stroke="{BASELINE}" stroke-width="1"/>')

    for i, s in enumerate(series):
        cy = TOP + i * ROW_H + ROW_H / 2
        pts = kde_log(s['vals'])
        top_path, bot_path = [], []
        for lg, dens in pts:
            x = x_of(max(lo_log, min(hi_log, lg)))
            top_path.append(f'{x:.1f},{cy - dens*HALF:.1f}')
        for lg, dens in reversed(pts):
            x = x_of(max(lo_log, min(hi_log, lg)))
            bot_path.append(f'{x:.1f},{cy + dens*HALF:.1f}')
        d_attr = 'M' + ' L'.join(top_path) + ' L' + ' L'.join(bot_path) + ' Z'
        out.append(f'<path d="{d_attr}" fill="{s["color"]}" fill-opacity="0.20" '
                   f'stroke="{s["color"]}" stroke-width="2" stroke-linejoin="round"/>')

        step = max(1, math.ceil(s['n'] / 250))
        max_idx = s['vals'].index(max(s['vals']))
        for j, v in enumerate(s['vals']):
            if j % step != 0 and j != max_idx:
                continue
            jit = ((j * 0.6180339887) % 1 - 0.5) * HALF * 1.1
            out.append(f'<circle cx="{x_of(math.log10(v)):.1f}" cy="{cy+jit:.1f}" r="2.1" '
                       f'fill="{s["color"]}" fill-opacity="0.45"/>')

        mx = x_of(math.log10(s['median']))
        out.append(f'<line x1="{mx:.1f}" y1="{cy-HALF-3}" x2="{mx:.1f}" y2="{cy+HALF+3}" stroke="{SURFACE}" stroke-width="6"/>')
        out.append(f'<line x1="{mx:.1f}" y1="{cy-HALF-3}" x2="{mx:.1f}" y2="{cy+HALF+3}" stroke="{s["color"]}" stroke-width="2.5"/>')
        out.append(f'<text x="{mx+6:.1f}" y="{cy-HALF-7}" fill="{INK2}" font-size="11.5" '
                   f'font-family="{MONO}">{fmt(s["median"])}</text>')

        out.append(f'<text x="10" y="{cy-2:.1f}" fill="{INK2}" font-size="13" font-weight="600">{s["label"]}</text>')
        note = ' (batch means)' if s['key'] == 'BDN-Default' else ''
        out.append(f'<text x="10" y="{cy+15:.1f}" fill="{MUTED}" font-size="11.5" '
                   f'font-family="{MONO}">n={s["n"]:,}{note}</text>')

    out.append('</svg>')
    path = os.path.join(OUT_DIR, f'{out_name}.svg')
    os.makedirs(os.path.dirname(path), exist_ok=True)
    with open(path, 'w') as f:
        f.write('\n'.join(out))
    print(out_name, 'written,', os.path.getsize(path), 'bytes')
