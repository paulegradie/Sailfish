#!/usr/bin/env python3
"""
Analyze Cobertura coverage files and identify files below 80% coverage
that are in PR #213.
"""

import xml.etree.ElementTree as ET
import os
import glob
from pathlib import Path
from collections import defaultdict

# PR #213 files (from GitHub API response)
PR_FILES = {
    ".github/workflows/build-v3.0.yml",
    "AiAssistedDevSpecs/ImprovedRigor-1/AntiDCEAnalyzers-Design-v1.md",
    "AiAssistedDevSpecs/ImprovedRigor-1/HANDOFF_SUMMARY-2.2.md",
    "AiAssistedDevSpecs/ImprovedRigor-1/HANDOFF_SUMMARY-2.5.md",
    "AiAssistedDevSpecs/ImprovedRigor-1/HANDOFF_SUMMARY-2.6.md",
    "AiAssistedDevSpecs/ImprovedRigor-1/HANDOFF_SUMMARY-2.9.md",
    "AiAssistedDevSpecs/ImprovedRigor-1/NextAgentPrompt-2.1.md",
    "AiAssistedDevSpecs/ImprovedRigor-1/NextAgentPrompt-2.2.md",
    "AiAssistedDevSpecs/ImprovedRigor-1/NextAgentPrompt-2.3.md",
    "AiAssistedDevSpecs/ImprovedRigor-1/NextAgentPrompt-2.4.md",
    "AiAssistedDevSpecs/ImprovedRigor-1/NextAgentPrompt-2.5.md",
    "AiAssistedDevSpecs/ImprovedRigor-1/NextAgentPrompt-2.6.md",
    "AiAssistedDevSpecs/ImprovedRigor-1/NextAgentPrompt-2.7.md",
    "AiAssistedDevSpecs/ImprovedRigor-1/NextAgentPrompt-2.8.md",
    "AiAssistedDevSpecs/ImprovedRigor-1/NextAgentPrompt-2.9.md",
    "AiAssistedDevSpecs/ImprovedRigor-1/NextAgentPrompt-2.10.md",
    "AiAssistedDevSpecs/ImprovedRigor-1/NextAgentPrompt-2.11.md",
    "README.md",
    "RELEASE_NOTES.md",
    "Sailfish_Phase2_Implementation_Plan.md",
    "site/src/components/Layout.jsx",
    "site/src/components/Navigation.jsx",
    "site/src/pages/docs/1/adaptive-sampling.md",
    "site/src/pages/docs/1/anti-dce.md",
    "site/src/pages/docs/1/csv-output.md",
    "site/src/pages/docs/1/environment-health.md",
    "site/src/pages/docs/1/iteration-tuning.md",
}

def parse_coverage_file(xml_path):
    """Parse a Cobertura XML file and return file coverage data."""
    try:
        tree = ET.parse(xml_path)
        root = tree.getroot()
        
        file_coverage = {}
        for package in root.findall('.//package'):
            for cls in package.findall('.//class'):
                filename = cls.get('filename')
                if filename:
                    line_rate = float(cls.get('line-rate', 0))
                    lines_valid = int(cls.get('lines-valid', 0))
                    lines_covered = int(cls.get('lines-covered', 0))
                    
                    # Normalize path
                    if filename.startswith('G:\\code\\Sailfish\\source\\'):
                        filename = filename.replace('G:\\code\\Sailfish\\source\\', '')
                    
                    if filename not in file_coverage:
                        file_coverage[filename] = {
                            'line_rate': line_rate,
                            'lines_valid': lines_valid,
                            'lines_covered': lines_covered
                        }
        
        return file_coverage
    except Exception as e:
        print(f"Error parsing {xml_path}: {e}")
        return {}

def main():
    # Find all coverage files
    coverage_dir = "Tests.Analyzers/TestResults"
    coverage_files = glob.glob(f"{coverage_dir}/**/coverage.cobertura.xml", recursive=True)

    print(f"Found {len(coverage_files)} coverage files")

    # Aggregate coverage data
    all_coverage = defaultdict(lambda: {'line_rate': 0, 'lines_valid': 0, 'lines_covered': 0})

    for cov_file in coverage_files:
        coverage = parse_coverage_file(cov_file)
        for filename, data in coverage.items():
            if filename in all_coverage:
                # Accumulate coverage data
                entry = all_coverage[filename]
                entry['lines_valid'] += data['lines_valid']
                entry['lines_covered'] += data['lines_covered']
                if entry['lines_valid'] > 0:
                    entry['line_rate'] = entry['lines_covered'] / entry['lines_valid']
            else:
                # First time seeing this file
                all_coverage[filename] = data

    # First, show all source files in coverage
    print("\n" + "="*80)
    print("ALL SOURCE FILES IN COVERAGE REPORT")
    print("="*80)

    source_files = sorted([f for f in all_coverage.keys() if f.endswith('.cs')])
    for f in source_files[:20]:  # Show first 20
        data = all_coverage[f]
        print(f"{f}: {data['line_rate']*100:.2f}%")

    if len(source_files) > 20:
        print(f"... and {len(source_files) - 20} more files")

    # Find all files below 80% that are in PR #213
    below_80_all = []
    for filename, data in all_coverage.items():
        if not filename.endswith('.cs'):
            continue

        normalized = Path(filename).as_posix()
        if normalized not in PR_FILES:
            continue

        line_rate = data['line_rate']
        if line_rate < 0.80:
            below_80_all.append({
                'filename': normalized,
                'coverage': line_rate * 100,
                'lines_covered': data['lines_covered'],
                'lines_valid': data['lines_valid']
            })

    # Sort by coverage (lowest first)
    below_80_all.sort(key=lambda x: x['coverage'])

    print("\n" + "="*80)
    print("ALL SOURCE FILES BELOW 80% COVERAGE")
    print("="*80)

    if below_80_all:
        for item in below_80_all[:30]:  # Show first 30
            print(f"\n{item['filename']}")
            print(f"  Coverage: {item['coverage']:.2f}%")
            print(f"  Lines: {item['lines_covered']}/{item['lines_valid']}")
        if len(below_80_all) > 30:
            print(f"\n... and {len(below_80_all) - 30} more files below 80%")
    else:
        print("\nNo source files below 80% coverage found!")

    print("\n" + "="*80)
    print(f"SUMMARY: {len(below_80_all)} source files below 80% coverage")
    print("="*80)

if __name__ == '__main__':
    main()

