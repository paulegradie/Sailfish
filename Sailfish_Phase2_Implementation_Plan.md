# Sailfish Statistical Engine - Phase 2 Implementation Plan
## Advanced Statistical Features & Environment Control

**Document Version:** 2.0
**Date:** 2025-11-08
**Status:** In Progress
**Prerequisites:** Phase 1 (Adaptive Sampling & Confidence Intervals) - ✅ COMPLETE

---

## 📋 Executive Summary

This document provides a detailed, agent-ready implementation plan for Phase 2 of the Sailfish Statistical Engine upgrade. Phase 2 focuses on:

1. **Configurable Outlier Strategies** - Extend beyond current Tukey's method
2. **Adaptive Parameter Selection** - Method-specific optimization
3. **Statistical Validation & Warnings** - Quality assurance system
4. **Enhanced T-Distribution** - Proper statistical lookup tables
5. **Performance Regression Tests** - Ensure no degradation
6. **Documentation Updates** - Comprehensive user guides

**Estimated Effort:** 3-5 days (broken into small, agent-friendly tasks)
**Impact:** Medium-High (improves measurement reliability and user experience)

---
## ✅ Progress Update (2025-11-08)

- Task 3.2 (Adapter output integration): Added a "Warnings" section to SailfishConsoleWindowFormatter.cs. Validation warnings are now displayed in IDE test output when present.
- Remaining near-term work for Phase 2 (validation warnings): COMPLETE — markdown integration is done; CSV intentionally excludes warnings (numeric-only export). Proceeding to complete performance tests and migration notes.

- Tier A iPhone-level polish — Environment Health Check ("Sailfish Doctor"): baseline implemented with new probes:
  - Build Mode (Debug vs Release) detection via DebuggableAttribute
  - JIT settings (TieredCompilation, QuickJit, QuickJitForLoops, OSR) via COMPlus flags
  - Summary is surfaced at test session start in the Test Adapter and included in consolidated markdown
  - Unit test added to verify presence of new entries (Build Mode, JIT)


- Documentation & Release Notes updated: Environment Health (Build Mode + JIT) reflected in site docs and RELEASE_NOTES.md; consolidated markdown section documented.

---

## ✅ Progress Update (2025-11-09)

- NxN Method Comparisons completed (adapter + consolidated markdown): Benjamini–Hochberg FDR–adjusted q-values and 95% ratio confidence intervals (computed on the log scale)
- CSV session output parity: added ComparisonGroup, Method1, Method2, Mean1, Mean2, Ratio, CI95_Lower, CI95_Upper, q_value, Label and preserved ChangeDescription for backward compatibility
- Standard error computed from StdDev and sample size where not present in tracking format
- TestAdapter comparison markdown now includes a "Detailed Results" table to satisfy existing tests
- Targeted tests green: Tests.Library Csv* and Tests.TestAdapter comparison markdown tests



## 🎯 Phase 2 Goals

### Primary Objectives
- ✅ Provide flexible outlier handling strategies for different workload types
- ✅ Automatically optimize sampling parameters based on method characteristics
- ✅ Warn users about potentially unreliable measurements
- ✅ Improve statistical accuracy with proper t-distribution values
- ✅ Maintain 100% backward compatibility

### Success Criteria
- All existing tests pass without modification
- New outlier strategies correctly handle edge cases
- Adaptive parameter selection improves convergence speed by 15-25%
- Statistical warnings catch >80% of problematic measurements
- Code coverage remains above current threshold (>80%)

---

## 📁 File Structure Overview

### New Files to Create
```
source/Sailfish/Analysis/
├── OutlierStrategy.cs                    # Enum and strategy definitions
├── IOutlierDetector.cs                   # Enhanced interface
├── ConfigurableOutlierDetector.cs        # Strategy-based implementation
├── AdaptiveParameterSelector.cs          # Parameter optimization
└── StatisticalValidator.cs               # Quality validation

source/Sailfish/Execution/
└── AdaptiveSamplingConfig.cs             # Configuration model

source/Tests.Library/Analysis/
├── ConfigurableOutlierDetectorTests.cs   # Unit tests
├── AdaptiveParameterSelectorTests.cs     # Unit tests
└── StatisticalValidatorTests.cs          # Unit tests
```

### Files to Modify
```
source/Sailfish/Analysis/
├── SailfishOutlierDetector.cs            # Refactor to use new interface
└── IStatisticalConvergenceDetector.cs    # Add validation warnings

source/Sailfish/Execution/
├── ExecutionSettings.cs                  # Add outlier strategy property
└── AdaptiveIterationStrategy.cs          # Integrate parameter selector

source/Sailfish/Attributes/
└── SailfishAttribute.cs                  # Add outlier strategy property

source/Sailfish/Contracts.Public/Models/
└── PerformanceRunResult.cs               # Add validation warnings
```

---

## 🔧 Implementation Tasks

### Task 1: Configurable Outlier Strategies (Day 1)

#### 1.1 Create OutlierStrategy Enum and Models
**File:** `source/Sailfish/Analysis/OutlierStrategy.cs` (NEW)

**Implementation:**
```csharp
namespace Sailfish.Analysis;

/// <summary>
/// Defines strategies for handling outliers in performance test data.
/// </summary>
public enum OutlierStrategy
{
    /// <summary>
    /// Remove only upper outliers (Q3 + 1.5*IQR). Default behavior.
    /// Best for: Most performance tests where upper outliers indicate interference.
    /// </summary>
    RemoveUpper,

    /// <summary>
    /// Remove only lower outliers (Q1 - 1.5*IQR).
    /// Best for: Tests where lower values indicate interference (e.g., cache effects).
    /// </summary>
    RemoveLower,

    /// <summary>
    /// Remove both upper and lower outliers.
    /// Best for: Tests requiring maximum precision, stable environments.
    /// </summary>
    RemoveAll,

    /// <summary>
    /// Keep all data points, no outlier removal.
    /// Best for: Tests where outliers are expected and meaningful.
    /// </summary>
    DontRemove,

    /// <summary>
    /// Automatically choose strategy based on data characteristics.
    /// Analyzes distribution and selects optimal strategy.
    /// </summary>
    Adaptive
}
```

**Acceptance Criteria:**
- Enum compiles without errors
- XML documentation is complete and accurate
- Enum values align with BenchmarkDotNet terminology

---

#### 1.2 Create Enhanced Outlier Detector Interface
**File:** `source/Sailfish/Analysis/IOutlierDetector.cs` (NEW)

**Implementation:**
```csharp
namespace Sailfish.Analysis;

/// <summary>
/// Interface for outlier detection with configurable strategies.
/// </summary>
public interface IOutlierDetector
{
    /// <summary>
    /// Detects outliers using the specified strategy.
    /// </summary>
    /// <param name="originalData">The raw performance measurements</param>
    /// <param name="strategy">The outlier detection strategy to apply</param>
    /// <returns>Processed data with outlier analysis</returns>
    ProcessedStatisticalTestData DetectOutliers(
        IReadOnlyList<double> originalData,
        OutlierStrategy strategy);
}
```

**Acceptance Criteria:**
- Interface compiles without errors
- Method signature matches existing `ISailfishOutlierDetector` pattern
- XML documentation explains each parameter

---

#### 1.3 Implement Configurable Outlier Detector
**File:** `source/Sailfish/Analysis/ConfigurableOutlierDetector.cs` (NEW)

**Key Implementation Points:**
- Implement `IOutlierDetector` interface
- Use Perfolizer's `TukeyOutlierDetector` for fence calculations
- Implement each strategy (RemoveUpper, RemoveLower, RemoveAll, DontRemove)
- Implement Adaptive strategy with heuristics:
  - If skewness > 0.5: Use RemoveUpper (right-skewed distribution)
  - If skewness < -0.5: Use RemoveLower (left-skewed distribution)
  - If |skewness| <= 0.5: Use RemoveAll (symmetric distribution)
- Handle edge cases (N < 4, all values identical)
- Log strategy selection for Adaptive mode

**Acceptance Criteria:**
- All strategies produce correct results
- Adaptive strategy selects appropriate strategy based on distribution
- Edge cases handled gracefully (no exceptions)
- Maintains compatibility with existing `ProcessedStatisticalTestData` model

---

#### 1.4 Update Existing Outlier Detector
**File:** `source/Sailfish/Analysis/SailfishOutlierDetector.cs`

**Changes:**
- Keep existing `ISailfishOutlierDetector` interface for backward compatibility
- Internally delegate to `ConfigurableOutlierDetector` with `OutlierStrategy.RemoveUpper`
- Add XML comment noting this is legacy interface

**Acceptance Criteria:**
- All existing tests pass without modification
- Behavior identical to current implementation
- No breaking changes to public API

---

### Task 2: Adaptive Parameter Selection (Day 2)

#### 2.1 Create Adaptive Sampling Configuration Model
**File:** `source/Sailfish/Execution/AdaptiveSamplingConfig.cs` (NEW)

**Implementation:**
```csharp
namespace Sailfish.Execution;

/// <summary>
/// Configuration for adaptive sampling optimized for specific method characteristics.
/// </summary>
public class AdaptiveSamplingConfig
{
    public int MinimumSampleSize { get; set; } = 10;
    public int MaximumSampleSize { get; set; } = 1000;
    public double TargetCoefficientOfVariation { get; set; } = 0.05;
    public double MaxConfidenceIntervalWidth { get; set; } = 0.20;
    public OutlierStrategy OutlierStrategy { get; set; } = OutlierStrategy.RemoveUpper;
    public string SelectionReason { get; set; } = string.Empty;
}
```

**Acceptance Criteria:**
- Model compiles without errors
- Properties have sensible defaults
- XML documentation explains each property

---

#### 2.2 Implement Adaptive Parameter Selector
**File:** `source/Sailfish/Analysis/AdaptiveParameterSelector.cs` (NEW)

**Key Implementation Points:**
- Analyze pilot samples (first 10-20 iterations)
- Calculate median time, CV, and distribution characteristics
- Select optimal parameters based on method timing profile:

**Timing Categories:**
1. **Ultra-Fast Methods** (< 1ms median):
   - MinSampleSize: 50
   - MaxSampleSize: 2000
   - TargetCV: 0.10 (relaxed - high noise expected)
   - OutlierStrategy: RemoveUpper

2. **Fast Methods** (1-10ms median):
   - MinSampleSize: 20
   - MaxSampleSize: 1000
   - TargetCV: 0.05
   - OutlierStrategy: RemoveUpper

3. **Medium Methods** (10-100ms median):
   - MinSampleSize: 15
   - MaxSampleSize: 500
   - TargetCV: 0.03
   - OutlierStrategy: RemoveAll

4. **Slow Methods** (100-1000ms median):
   - MinSampleSize: 10
   - MaxSampleSize: 200
   - TargetCV: 0.02
   - OutlierStrategy: RemoveAll

5. **Very Slow Methods** (> 1000ms median):
   - MinSampleSize: 5
   - MaxSampleSize: 50
   - TargetCV: 0.02
   - OutlierStrategy: DontRemove (outliers likely meaningful)

**Variability Adjustments:**
- If initial CV > 0.20: Increase MaxSampleSize by 50%
- If initial CV < 0.02: Decrease MinSampleSize by 30%

**Acceptance Criteria:**
- Correctly categorizes methods by timing
- Adjusts parameters based on variability
- Logs selection reasoning
- Returns valid `AdaptiveSamplingConfig`

---

#### 2.3 Integrate Parameter Selector into Execution Pipeline
**File:** `source/Sailfish/Execution/AdaptiveIterationStrategy.cs`

**Changes:**
1. Add optional parameter selector injection
2. After minimum iterations, analyze pilot data
3. If parameter selector available, adjust remaining execution:
   - Update target CV
   - Update max iterations
   - Update outlier strategy
4. Log parameter adjustments

**Acceptance Criteria:**
- Backward compatible (works without parameter selector)
- Parameter adjustments logged clearly
- Adjusted parameters improve convergence speed
- No breaking changes to existing behavior

---

### Task 3: Statistical Validation & Warnings (Day 3)

#### 3.1 Create Statistical Validator
**File:** `source/Sailfish/Analysis/StatisticalValidator.cs` (NEW)

**Key Validation Checks:**

1. **Sample Size Adequacy**
   - Warning if N < 10: "Very small sample size"
   - Warning if N < 5: "Critically small sample size - results unreliable"

2. **Outlier Ratio**
   - Warning if outliers > 20%: "High outlier ratio - investigate environment"
   - Warning if outliers > 40%: "Excessive outliers - results may be unreliable"

3. **Bimodal Distribution Detection**
   - Use Hartigan's dip test or simple heuristic
   - Warning if bimodal: "Distribution appears bimodal - may indicate mode switching"

4. **Extreme Variability**
   - Warning if CV > 0.50: "Very high variability - consider longer warmup"
   - Warning if CV > 1.0: "Extreme variability - results unreliable"

5. **Convergence Quality**
   - Warning if converged at max iterations: "Reached maximum iterations without full convergence"
   - Info if converged early: "Converged quickly - stable performance"

**Return Model:**
```csharp
public class ValidationResult
{
    public bool IsReliable { get; set; }
    public List<string> Warnings { get; set; } = new();
    public List<string> Recommendations { get; set; } = new();
    public ValidationSeverity Severity { get; set; }
}

public enum ValidationSeverity
{
    None,      // No issues
    Info,      // Informational
    Warning,   // Potential issues
    Critical   // Serious reliability concerns
}
```

**Acceptance Criteria:**
- All validation checks implemented
- Warnings are clear and actionable
- Recommendations help users improve tests
- No false positives on known-good data

---

#### 3.2 Integrate Validator into Results Pipeline
**File:** `source/Sailfish/Contracts.Public/Models/PerformanceRunResult.cs`

**Changes:**
1. Add `ValidationResult? ValidationWarnings` property
2. In `ConvertWithOutlierAnalysis`, call validator
3. Include validation warnings in result

**File:** `source/Sailfish.TestAdapter/Display/TestOutputWindow/SailfishConsoleWindowFormatter.cs`

**Changes:**
1. Display validation warnings after statistics table
2. Use color coding (if supported):
   - Info: Blue
   - Warning: Yellow
   - Critical: Red

**Acceptance Criteria:**
- Warnings displayed in console output
- Warnings included in tracking files
- Warnings don't break existing output parsers

---

### Task 4: Enhanced T-Distribution (Day 4)

#### 4.1 Create Comprehensive T-Distribution Table
**File:** `source/Sailfish/Analysis/TDistributionTable.cs` (NEW)

**Implementation:**
- Complete t-table for df 1-30, then 40, 60, 80, 100, 120, ∞
- Support confidence levels: 0.90, 0.95, 0.99, 0.999
- Use interpolation for intermediate df values
- Reference: Standard statistical tables

**Acceptance Criteria:**
- Values match published t-tables
- Interpolation produces reasonable estimates
- Performance: O(1) lookup time

---

#### 4.2 Update Statistical Convergence Detector
**File:** `source/Sailfish/Analysis/StatisticalConvergenceDetector.cs`

**Changes:**
- Replace `GetTValue` method with `TDistributionTable` lookup
- Remove approximations and hardcoded values
- Add unit tests comparing old vs new values

**Acceptance Criteria:**
- More accurate confidence intervals
- All existing tests pass
- Improved precision for small sample sizes

---

### Task 5: Configuration & Attributes (Day 4)

#### 5.1 Update Execution Settings
**File:** `source/Sailfish/Execution/ExecutionSettings.cs`

**Add Properties:**
```csharp
public OutlierStrategy OutlierStrategy { get; set; } = OutlierStrategy.RemoveUpper;
public bool UseAdaptiveParameterSelection { get; set; } = false;
public bool EnableStatisticalValidation { get; set; } = true;
```

**Acceptance Criteria:**
- Properties have sensible defaults
- Backward compatible (existing code works unchanged)
- XML documentation complete

---

#### 5.2 Update Sailfish Attribute
**File:** `source/Sailfish/Attributes/SailfishAttribute.cs`

**Add Properties:**
```csharp
public OutlierStrategy OutlierStrategy { get; set; } = OutlierStrategy.RemoveUpper;
public bool UseAdaptiveParameterSelection { get; set; } = false;
```

**Acceptance Criteria:**
- Attribute properties flow through to ExecutionSettings
- Existing attributes work unchanged
- XML documentation explains new properties

---

#### 5.3 Update Extension Methods
**File:** `source/Sailfish/Extensions/Methods/ExecutionExtensionMethods.cs`

**Changes:**
- Map new attribute properties to ExecutionSettings
- Maintain backward compatibility

**Acceptance Criteria:**
- New properties correctly mapped
- All existing tests pass
- No breaking changes

---

### Task 6: Comprehensive Testing (Day 5)

#### 6.1 Unit Tests for Outlier Strategies
**File:** `source/Tests.Library/Analysis/ConfigurableOutlierDetectorTests.cs` (NEW)

**Test Cases:**
1. `RemoveUpper_WithUpperOutliers_RemovesOnlyUpper`
2. `RemoveLower_WithLowerOutliers_RemovesOnlyLower`
3. `RemoveAll_WithBothOutliers_RemovesBoth`
4. `DontRemove_WithOutliers_KeepsAll`
5. `Adaptive_WithRightSkew_SelectsRemoveUpper`
6. `Adaptive_WithLeftSkew_SelectsRemoveLower`
7. `Adaptive_WithSymmetric_SelectsRemoveAll`
8. `EdgeCase_SmallSample_HandlesGracefully`
9. `EdgeCase_AllIdentical_NoOutliers`
10. `EdgeCase_EmptyData_ThrowsArgumentException`

**Acceptance Criteria:**
- All test cases pass
- Code coverage > 90% for ConfigurableOutlierDetector
- Edge cases handled correctly

---

#### 6.2 Unit Tests for Adaptive Parameter Selector
**File:** `source/Tests.Library/Analysis/AdaptiveParameterSelectorTests.cs` (NEW)

**Test Cases:**
1. `UltraFastMethod_SelectsHighSampleSize`
2. `FastMethod_SelectsStandardParameters`
3. `MediumMethod_SelectsBalancedParameters`
4. `SlowMethod_SelectsLowSampleSize`
5. `VerySlowMethod_SelectsMinimalSampling`
6. `HighVariability_IncreasesMaxSampleSize`
7. `LowVariability_DecreasesMinSampleSize`
8. `SelectionReason_IsPopulated`

**Acceptance Criteria:**
- All test cases pass
- Code coverage > 85% for AdaptiveParameterSelector
- Parameter selection logic validated

---

#### 6.3 Unit Tests for Statistical Validator
**File:** `source/Tests.Library/Analysis/StatisticalValidatorTests.cs` (NEW)

**Test Cases:**
1. `SmallSample_GeneratesWarning`
2. `HighOutlierRatio_GeneratesWarning`
3. `BimodalDistribution_GeneratesWarning`
4. `HighVariability_GeneratesWarning`
5. `GoodData_NoWarnings`
6. `CriticalIssues_SetsCriticalSeverity`
7. `Recommendations_AreActionable`

**Acceptance Criteria:**
- All test cases pass
- Code coverage > 85% for StatisticalValidator
- Warnings trigger correctly

---

#### 6.4 Integration Tests
**File:** `source/Tests.Library/Integration/Phase2IntegrationTests.cs` (NEW)

**Test Scenarios:**
1. End-to-end test with each outlier strategy
2. Adaptive parameter selection in real execution
3. Statistical validation warnings in output
4. Backward compatibility with Phase 1 tests
5. Performance regression check (overhead < 10%)

**Acceptance Criteria:**
- All integration tests pass
- No performance degradation
- Backward compatibility verified

---

#### 6.5 Performance Regression Tests
**File:** `source/Tests.Library/Performance/Phase2PerformanceTests.cs` (NEW)

**Benchmarks:**
1. Outlier detection overhead (should be < 1ms for 1000 samples)
2. Parameter selection overhead (should be < 5ms)
3. Validation overhead (should be < 2ms)
4. Total execution overhead (should be < 10% vs Phase 1)

**Acceptance Criteria:**
- All performance benchmarks pass
- No significant overhead introduced
- Execution time remains acceptable

---

### Task 7: Documentation Updates (Day 5)

#### 7.1 Update README.md
**File:** `README.md`

**Add Sections:**
1. **Outlier Strategies** - Explain each strategy and when to use
2. **Adaptive Parameter Selection** - How it works and benefits
3. **Statistical Validation** - What warnings mean and how to address
4. **Migration from Phase 1** - Any breaking changes (none expected)

**Example:**
```markdown
## Outlier Strategies

Sailfish provides flexible outlier handling:

### RemoveUpper (Default)
Removes only upper outliers. Best for most performance tests.
```csharp
[Sailfish(OutlierStrategy = OutlierStrategy.RemoveUpper)]
```

### RemoveAll
Removes both upper and lower outliers. Best for stable environments.
```csharp
[Sailfish(OutlierStrategy = OutlierStrategy.RemoveAll)]
```

### Adaptive
Automatically selects strategy based on data distribution.
```csharp
[Sailfish(OutlierStrategy = OutlierStrategy.Adaptive)]
```
```

**Acceptance Criteria:**
- Documentation is clear and comprehensive
- Examples are correct and runnable
- Migration guide addresses common questions

---

#### 7.2 Create Phase 2 Migration Guide
**File:** `PHASE2_MIGRATION_GUIDE.md` (NEW)

**Contents:**
1. What's new in Phase 2
2. Breaking changes (none expected)
3. New configuration options
4. How to enable new features
5. Troubleshooting common issues
6. Performance considerations

**Acceptance Criteria:**
- Guide is comprehensive
- Examples are tested and working
- Common questions addressed

---

#### 7.3 Update API Documentation
**Files:** All modified source files

**Requirements:**
- All public APIs have XML documentation
- Examples in XML docs are correct
- Parameter descriptions are clear
- Return value documentation is complete

**Acceptance Criteria:**
- No XML documentation warnings
- API docs build successfully
- Examples compile and run

---

## 🧪 Testing Strategy

### Unit Test Coverage Requirements
- **ConfigurableOutlierDetector**: > 90%
- **AdaptiveParameterSelector**: > 85%
- **StatisticalValidator**: > 85%
- **TDistributionTable**: > 95%

### Integration Test Requirements
- All Phase 1 tests pass unchanged
- New features work end-to-end
- Backward compatibility verified
- Performance overhead < 10%

### Manual Testing Checklist
- [ ] Run full test suite on Windows
- [ ] Run full test suite on Linux (if available)
- [ ] Verify console output formatting
- [ ] Check markdown output includes warnings
- [ ] Verify CSV output unchanged
- [ ] Test with .NET 8 and .NET 9

---

## 🔄 Backward Compatibility Checklist

### Must Maintain
- [ ] All existing tests pass without modification
- [ ] Default behavior unchanged (RemoveUpper outlier strategy)
- [ ] Existing attributes work without new properties
- [ ] Console output format compatible
- [ ] Tracking file format compatible
- [ ] CSV export format compatible

### Allowed Changes
- ✅ Add new optional properties to attributes
- ✅ Add new optional configuration settings
- ✅ Add new output sections (warnings)
- ✅ Improve statistical accuracy

---

## 📊 Success Metrics

### Quantitative Metrics
1. **Code Coverage**: Maintain > 80% overall
2. **Performance Overhead**: < 10% increase
3. **Convergence Speed**: 15-25% improvement with adaptive parameters
4. **Warning Accuracy**: > 80% of problematic measurements flagged

### Qualitative Metrics
1. **User Experience**: Clear, actionable warnings
2. **Documentation Quality**: Comprehensive and accurate
3. **API Design**: Intuitive and consistent
4. **Backward Compatibility**: 100% maintained

---

## 🚀 Deployment Strategy

### Pre-Deployment Checklist
- [ ] All unit tests pass
- [ ] All integration tests pass
- [ ] Performance tests pass
- [ ] Documentation complete
- [ ] Code review completed
- [ ] Backward compatibility verified

### Deployment Steps
1. Merge Phase 2 branch to main
2. Update version number (semantic versioning)
3. Create release notes
4. Publish NuGet packages
5. Update documentation website
6. Announce release

---

## 👥 Agent Handoff Instructions

### For Next Agent: Getting Started

1. **Read Prerequisites:**
   - Review `Sailfish_Statistical_Engine_Upgrade_Plan.md`
   - Review `Sailfish_BenchmarkDotNet_Rigor_Proposal.md`
   - Verify Phase 1 is complete (run tests)

2. **Set Up Environment:**
   ```bash
   cd G:\code\Sailfish\source
   dotnet build Sailfish.sln
   dotnet test --no-build
   ```

3. **Start with Task 1:**
   - Create `OutlierStrategy.cs` enum
   - Follow implementation details in Task 1.1
   - Run tests after each subtask
   - Commit frequently with clear messages

4. **Task Execution Pattern:**
   - Read task description thoroughly
   - Implement code following specifications
   - Write unit tests immediately
   - Run tests and verify
   - Update documentation
   - Commit changes
   - Move to next subtask

5. **When Stuck:**
   - Review existing similar code (e.g., `SailfishOutlierDetector.cs`)
   - Check test examples in `Tests.Library`
   - Ask user for clarification
   - Don't guess - verify assumptions

6. **Quality Checks:**
   - Run `dotnet build` - must succeed
   - Run `dotnet test` - all tests must pass
   - Check code coverage - must meet thresholds
   - Verify backward compatibility

### Task Breakdown for Multiple Agents

**Agent 1 (Day 1):** Task 1 - Configurable Outlier Strategies
**Agent 2 (Day 2):** Task 2 - Adaptive Parameter Selection
**Agent 3 (Day 3):** Task 3 - Statistical Validation & Warnings
**Agent 4 (Day 4):** Task 4 & 5 - T-Distribution & Configuration
**Agent 5 (Day 5):** Task 6 & 7 - Testing & Documentation

### Critical Files Reference

**Current Implementation (Study These):**
- `source/Sailfish/Analysis/SailfishOutlierDetector.cs` - Current outlier detection
- `source/Sailfish/Analysis/StatisticalConvergenceDetector.cs` - Convergence logic
- `source/Sailfish/Execution/AdaptiveIterationStrategy.cs` - Adaptive execution
- `source/Sailfish/Execution/ExecutionSettings.cs` - Configuration model

**Test Examples (Use as Templates):**
- `source/Tests.Library/Analysis/StatisticalConvergenceDetectorTests.cs`
- `source/Tests.Library/Integration/AdaptiveSamplingIntegrationTests.cs`

---

## 📝 Implementation Notes

### Design Decisions

1. **Why Enum for OutlierStrategy?**
   - Simple, type-safe, easy to serialize
   - Matches BenchmarkDotNet pattern
   - Easy to extend in future

2. **Why Separate AdaptiveParameterSelector?**
   - Single Responsibility Principle
   - Testable in isolation
   - Optional feature (can be disabled)

3. **Why ValidationResult Model?**
   - Structured warnings
   - Severity levels for filtering
   - Extensible for future checks

4. **Why Keep ISailfishOutlierDetector?**
   - Backward compatibility
   - Gradual migration path
   - No breaking changes

### Common Pitfalls to Avoid

1. **Don't break backward compatibility** - All existing tests must pass
2. **Don't skip unit tests** - Write tests as you implement
3. **Don't hardcode values** - Use constants and configuration
4. **Don't ignore edge cases** - Handle empty data, small samples, etc.
5. **Don't forget documentation** - Update XML docs and README

### Performance Considerations

1. **Outlier Detection:** Already fast (< 1ms for 1000 samples)
2. **Parameter Selection:** Run once per test (< 5ms acceptable)
3. **Validation:** Run once per test (< 2ms acceptable)
4. **Total Overhead:** Target < 10% increase

---

## 🎯 Definition of Done

A task is complete when:
- [ ] Code implemented according to specification
- [ ] Unit tests written and passing (> 85% coverage)
- [ ] Integration tests passing
- [ ] Documentation updated (XML docs + README)
- [ ] Code review completed (if applicable)
- [ ] Backward compatibility verified
- [ ] Performance benchmarks pass
- [ ] Committed with clear message

Phase 2 is complete when:
- [ ] All 7 tasks complete
- [ ] All tests passing (unit + integration)
- [ ] Documentation complete and accurate
- [ ] Performance overhead < 10%
- [ ] Code coverage > 80%
- [ ] User acceptance testing passed

---

## 📞 Support and Questions

**For Implementation Questions:**
- Review this document thoroughly
- Check existing code patterns
- Ask user for clarification

**For Technical Issues:**
- Check build errors carefully
- Review test failures
- Verify dependencies are installed

**For Design Decisions:**
- Follow patterns in this document
- Maintain consistency with Phase 1
- Ask user before major deviations

---

**End of Phase 2 Implementation Plan**


