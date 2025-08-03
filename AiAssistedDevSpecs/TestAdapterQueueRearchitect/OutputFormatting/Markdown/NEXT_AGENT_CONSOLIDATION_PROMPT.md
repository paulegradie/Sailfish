# Next Agent Prompt: Sailfish Markdown Consolidation Fix

## Task Overview
Fix the Sailfish markdown generation feature to create **one consolidated markdown file per test run session** instead of multiple individual files, and implement **NxN comparison tables** for method comparison groups.

## Context & Background
Read the comprehensive handoff document: `G:/code/Sailfish/MARKDOWN_CONSOLIDATION_HANDOFF.md`

The markdown generation feature is partially working but has critical issues:
- ❌ Creating 5+ separate files instead of 1 consolidated file
- ❌ Each file contains only 1 test instead of all tests from session  
- ❌ No comparison group detection or NxN comparison tables
- ❌ Missing session-based consolidation

## Current State
- **Repository**: `G:\code\Sailfish` (branch: `pg/action`)
- **Working Directory**: `G:\code\Sailfish\source`
- **Build Status**: ✅ Compiles successfully
- **Test Output**: `G:\code\Sailfish\source\PerformanceTests\bin\Debug\net9.0\SailfishIDETestOutput`

## Required Implementation

### 1. Session-Based Consolidation (Priority 1)
**Goal**: One markdown file per test run session containing ALL tests with `[WriteToMarkdown]` attribute.

**Current Issue**: `MethodComparisonTestClassCompletedHandler` creates separate files per test class completion.

**Solution**: Replace with `TestRunCompletedNotification` handler that:
- Listens for entire test run completion
- Collects all test results from session
- Generates single consolidated markdown file
- Uses session-based naming: `TestSession_MethodComparisons_{timestamp}.md`

### 2. Comparison Group Detection (Priority 2)  
**Goal**: Detect and group methods with `[SailfishComparison("GroupName")]` attributes.

**Current Issue**: All tests show as "Individual Test Results" - comparison groups not detected.

**Implementation**: 
- Extract comparison group names from `SailfishComparison` attributes
- Group test methods by comparison group name
- Generate separate sections for each group

### 3. NxN Comparison Tables (Priority 3)
**Goal**: Generate performance comparison matrices for each comparison group.

**Format**:
```markdown
## 🔬 Comparison Group: SumCalculation

### Performance Comparison Matrix
|                    | CalculateSum | CalculateSumWithLinq | CalculateSumWithLoop |
|--------------------|--------------|----------------------|----------------------|
| **CalculateSum**   | -            | 2.3x faster          | 1.8x faster          |
| **CalculateSumWithLinq** | 2.3x slower | -                | 1.3x slower          |
| **CalculateSumWithLoop** | 1.8x slower | 1.3x faster      | -                    |
```

## Key Files to Modify

### Primary Files
1. **`source/Sailfish/DefaultHandlers/Sailfish/MethodComparisonTestClassCompletedHandler.cs`**
   - Currently creates multiple files per test class
   - Replace with session-based approach

2. **`source/Sailfish/DefaultHandlers/Sailfish/MethodComparisonMarkdownHandler.cs`**  
   - Handles file writing
   - May need session-aware logic

### Test File
- **`source/PerformanceTests/ExamplePerformanceTests/MethodComparisonExample.cs`**
- Contains `[WriteToMarkdown]` and `[SailfishComparison("GroupName")]` attributes

## Implementation Steps

### Step 1: Analyze Current Flow
1. **Read handoff document**: `G:/code/Sailfish/MARKDOWN_CONSOLIDATION_HANDOFF.md`
2. **Examine current implementation** in `MethodComparisonTestClassCompletedHandler.cs`
3. **Understand notification flow** - find `TestRunCompletedNotification` usage
4. **Identify session completion points**

### Step 2: Implement Session-Based Handler
1. **Create new handler** for `TestRunCompletedNotification`
2. **Collect all test results** from classes with `[WriteToMarkdown]`
3. **Generate single consolidated file** per session
4. **Remove/disable** the current per-class handler

### Step 3: Add Comparison Group Detection
1. **Extract `SailfishComparison` attribute values** from test methods
2. **Group test results** by comparison group name
3. **Separate comparison groups** from individual tests

### Step 4: Generate NxN Comparison Tables
1. **Calculate relative performance** between methods in same group
2. **Generate comparison matrices** showing X times faster/slower
3. **Format as markdown tables** with proper styling

### Step 5: Test & Validate
1. **Clear output directory**: `rm -rf G:\code\Sailfish\source\PerformanceTests\bin\Debug\net9.0\SailfishIDETestOutput\*.md`
2. **Run MethodComparisonExample** test class
3. **Verify single file** generated with all tests
4. **Check comparison groups** and NxN tables

## Success Criteria

### ✅ Must Have
- [ ] **Single markdown file** per test run session
- [ ] **All tests with `[WriteToMarkdown]`** included in same file  
- [ ] **Comparison groups detected** and properly grouped
- [ ] **NxN comparison tables** generated for each group
- [ ] **Build compiles** successfully
- [ ] **No race conditions** or file conflicts

### ✅ Nice to Have
- [ ] **Session metadata** (session ID, total classes, total tests)
- [ ] **Performance summaries** (fastest/slowest in each group)
- [ ] **Error handling** for edge cases
- [ ] **Backward compatibility** maintained

## Testing Commands

```bash
# Navigate to source directory
cd G:\code\Sailfish\source

# Build solution
dotnet build --verbosity quiet

# Clear previous output
rm G:\code\Sailfish\source\PerformanceTests\bin\Debug\net9.0\SailfishIDETestOutput\*.md

# Run tests via IDE or command line
# Check output directory for single consolidated file
```

## Expected Output Structure

```markdown
# 📊 Test Session Results

**Generated:** 2025-08-03 05:13:21 UTC  
**Session ID:** {unique-session-id}
**Total Test Classes:** 1
**Total Test Cases:** 7

## 🔬 Comparison Group: SumCalculation

### Performance Comparison Matrix
[NxN table showing relative performance]

### Detailed Results
[Individual method results for this group]

## 🔬 Comparison Group: SortingAlgorithm

### Performance Comparison Matrix  
[NxN table showing relative performance]

### Detailed Results
[Individual method results for this group]

## 📊 Individual Test Results
[Non-comparison tests]
```

## Important Notes

- **Repository root**: `G:\code\Sailfish`
- **Source directory**: `G:\code\Sailfish\source`  
- **Current branch**: `pg/action`
- **Build must succeed** before testing
- **Clear output directory** before each test run
- **Focus on session-based consolidation first** - this is the highest priority issue

## Questions to Address

1. **How does Sailfish detect test run completion?** (vs individual test completion)
2. **What notification is published when entire test session ends?**
3. **How to access comparison group attributes from test results?**
4. **How to generate unique session IDs for file naming?**

Start by reading the handoff document and examining the current implementation to understand the notification flow.
