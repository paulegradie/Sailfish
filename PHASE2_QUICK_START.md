# Phase 2 Quick Start Guide for AI Agents

**Last Updated:** 2025-11-08
**Status:** In Progress — Validation warnings complete; Env Health Check (Build Mode + JIT) implemented; continuing iPhone‑level polish

---

## 🎯 Quick Context

You are implementing **Phase 2** of the Sailfish Statistical Engine upgrade:
- **Phase 1** (Adaptive Sampling & Confidence Intervals) is ✅ **COMPLETE**
- **Phase 2** adds: Outlier strategies, adaptive parameters, validation warnings
- **Goal:** Improve measurement reliability without breaking existing functionality

---
## ✅ Progress Update (2025-11-08)

- Added validation warnings display to IDE test output (SailfishConsoleWindowFormatter.cs): prints a "Warnings" section with severity indicators when results include warnings.
- Build is green; change is backward compatible and isolated to output formatting.
- Markdown exporter now includes validation warnings under each affected test group; integration tests cover this.
- CSV output intentionally excludes validation warnings (numeric metrics only); warnings surface in IDE and markdown.
- Implemented Environment Health Check baseline with new probes:
  - Build Mode (Debug vs Release) detection
  - JIT settings (TieredCompilation, QuickJit, QuickJitForLoops, OSR)
- Integrated summary into Test Adapter output and consolidated markdown
- Added unit tests verifying presence of new entries (Build Mode, JIT)

- Next focus: pick the next Tier A iPhone‑level polish item (e.g., Reproducibility Manifest or Anti‑DCE guard rails).


## 🧭 Handoff
- Next Agent Prompt: G:/code/Sailfish/AiAssistedDevSpecs/ImprovedRigor-1/NextAgentPrompt-2.1.md
- Start there for step-by-step instructions, verification commands, and the next Tier A task selection guidance.

---


## 📁 Essential Documents (Read in Order)

1. **This Document** - Quick start guide
2. `Sailfish_Phase2_Implementation_Plan.md` - Detailed implementation spec
3. `Sailfish_Statistical_Engine_Upgrade_Plan.md` - Overall upgrade plan
4. `Sailfish_BenchmarkDotNet_Rigor_Proposal.md` - Feature rationale

---

## 🚀 Getting Started (5 Minutes)

### Step 1: Verify Environment
```bash
cd G:\code\Sailfish\source
pwd  # Should show: G:/code/Sailfish/source
```

### Step 2: Build and Test
```bash
dotnet build Sailfish.sln
# Should succeed with ~145 warnings (nullable refs - OK)

dotnet test --no-build
# All tests should pass
```

### Step 3: Understand Current State
- Phase 1 features are working:
  - Adaptive sampling with CV + CI convergence
  - Confidence intervals in output
  - `StatisticalConvergenceDetector` implemented
  - `AdaptiveIterationStrategy` implemented

---

## 📋 Task Assignment

Pick ONE task to implement:

### Task 1: Outlier Strategies (Day 1)
**Files to Create:**
- `source/Sailfish/Analysis/OutlierStrategy.cs`
- `source/Sailfish/Analysis/IOutlierDetector.cs`
- `source/Sailfish/Analysis/ConfigurableOutlierDetector.cs`
- `source/Tests.Library/Analysis/ConfigurableOutlierDetectorTests.cs`

**Files to Modify:**
- `source/Sailfish/Analysis/SailfishOutlierDetector.cs`

**Key Points:**
- Implement 5 strategies: RemoveUpper, RemoveLower, RemoveAll, DontRemove, Adaptive
- Use Perfolizer's `TukeyOutlierDetector` for fence calculations
- Maintain backward compatibility (default = RemoveUpper)

---

### Task 2: Adaptive Parameter Selection (Day 2)
**Files to Create:**
- `source/Sailfish/Execution/AdaptiveSamplingConfig.cs`
- `source/Sailfish/Analysis/AdaptiveParameterSelector.cs`
- `source/Tests.Library/Analysis/AdaptiveParameterSelectorTests.cs`

**Files to Modify:**
- `source/Sailfish/Execution/AdaptiveIterationStrategy.cs`

**Key Points:**
- Analyze pilot samples to determine method speed
- Select optimal parameters based on timing profile
- 5 categories: Ultra-Fast, Fast, Medium, Slow, Very Slow

---

### Task 3: Statistical Validation (Day 3)
**Files to Create:**
- `source/Sailfish/Analysis/StatisticalValidator.cs`
- `source/Tests.Library/Analysis/StatisticalValidatorTests.cs`

**Files to Modify:**
- `source/Sailfish/Contracts.Public/Models/PerformanceRunResult.cs`
- `source/Sailfish.TestAdapter/Display/TestOutputWindow/SailfishConsoleWindowFormatter.cs`

**Key Points:**
- Validate sample size, outlier ratio, distribution, variability
- Generate actionable warnings
- Display warnings in console output

---

### Task 4: T-Distribution & Config (Day 4)
**Files to Create:**
- `source/Sailfish/Analysis/TDistributionTable.cs`

**Files to Modify:**
- `source/Sailfish/Analysis/StatisticalConvergenceDetector.cs`
- `source/Sailfish/Execution/ExecutionSettings.cs`
- `source/Sailfish/Attributes/SailfishAttribute.cs`
- `source/Sailfish/Extensions/Methods/ExecutionExtensionMethods.cs`

**Key Points:**
- Complete t-table for accurate confidence intervals
- Add OutlierStrategy property to settings
- Maintain backward compatibility

---

### Task 5: Testing & Documentation (Day 5)
**Files to Create:**
- `source/Tests.Library/Integration/Phase2IntegrationTests.cs`
- `source/Tests.Library/Performance/Phase2PerformanceTests.cs`
- `PHASE2_MIGRATION_GUIDE.md`

**Files to Modify:**
- `README.md`
- All source files (XML documentation)

**Key Points:**
- Comprehensive test coverage (> 85%)
- Integration tests verify end-to-end
- Performance tests ensure < 10% overhead
- Documentation is clear and complete

---

## 🔍 Code Patterns to Follow

### Pattern 1: Backward Compatibility
```csharp
// ✅ GOOD: Optional parameter with default
public void Method(OutlierStrategy strategy = OutlierStrategy.RemoveUpper)

// ❌ BAD: Required parameter (breaks existing code)
public void Method(OutlierStrategy strategy)
```

### Pattern 2: Dependency Injection
```csharp
// ✅ GOOD: Constructor injection
public class MyClass
{
    private readonly IDependency dependency;
    public MyClass(IDependency dependency) => this.dependency = dependency;
}
```

### Pattern 3: Unit Testing
```csharp
// ✅ GOOD: Clear test name, arrange-act-assert
[Fact]
public void RemoveUpper_WithUpperOutliers_RemovesOnlyUpper()
{
    // Arrange
    var data = new[] { 1.0, 2.0, 3.0, 100.0 };
    var detector = new ConfigurableOutlierDetector();

    // Act
    var result = detector.DetectOutliers(data, OutlierStrategy.RemoveUpper);

    // Assert
    result.UpperOutliers.Should().Contain(100.0);
    result.LowerOutliers.Should().BeEmpty();
}
```

---

## ⚠️ Common Mistakes to Avoid

1. **Breaking Backward Compatibility**
   - ❌ Changing existing method signatures
   - ❌ Removing public APIs
   - ❌ Changing default behavior
   - ✅ Adding optional parameters
   - ✅ Adding new methods/classes

2. **Skipping Tests**
   - ❌ Implementing without tests
   - ❌ Only testing happy path
   - ✅ Write tests first (TDD)
   - ✅ Test edge cases

3. **Poor Error Handling**
   - ❌ Throwing generic exceptions
   - ❌ Swallowing exceptions
   - ✅ Validate inputs
   - ✅ Throw specific exceptions with clear messages

4. **Ignoring Performance**
   - ❌ O(n²) algorithms on large datasets
   - ❌ Unnecessary allocations
   - ✅ Profile critical paths
   - ✅ Use efficient data structures

---

## 🧪 Testing Workflow

### After Each Code Change:
```bash
# 1. Build
dotnet build Sailfish.sln

# 2. Run affected tests
dotnet test --filter "FullyQualifiedName~ConfigurableOutlierDetector"

# 3. Run all tests (before commit)
dotnet test --no-build

# 4. Check coverage (if available)
dotnet test --collect:"XPlat Code Coverage"
```

### Before Marking Task Complete:
- [ ] All unit tests pass
- [ ] All integration tests pass
- [ ] Code coverage > 85% for new code
- [ ] Documentation updated
- [ ] Backward compatibility verified

---

## 📚 Key Files Reference

### Study These (Understand Current Implementation):
```
source/Sailfish/Analysis/
├── SailfishOutlierDetector.cs          # Current outlier detection
├── StatisticalConvergenceDetector.cs   # Convergence logic
└── ProcessedStatisticalTestData.cs     # Data model

source/Sailfish/Execution/
├── ExecutionSettings.cs                # Configuration
├── AdaptiveIterationStrategy.cs        # Adaptive execution
└── FixedIterationStrategy.cs           # Fixed execution

source/Tests.Library/Analysis/
└── StatisticalConvergenceDetectorTests.cs  # Test examples
```

### Modify These (Your Implementation):
```
source/Sailfish/Analysis/
├── OutlierStrategy.cs                  # NEW: Enum
├── IOutlierDetector.cs                 # NEW: Interface
├── ConfigurableOutlierDetector.cs      # NEW: Implementation
├── AdaptiveParameterSelector.cs        # NEW: Parameter selection
├── StatisticalValidator.cs             # NEW: Validation
└── TDistributionTable.cs               # NEW: T-table

source/Tests.Library/Analysis/
├── ConfigurableOutlierDetectorTests.cs # NEW: Tests
├── AdaptiveParameterSelectorTests.cs   # NEW: Tests
└── StatisticalValidatorTests.cs        # NEW: Tests
```

---

## 💡 Pro Tips

1. **Use Existing Code as Template**
   - Copy structure from `SailfishOutlierDetector.cs`
   - Follow naming conventions
   - Match code style

2. **Test-Driven Development**
   - Write test first
   - Implement minimal code to pass
   - Refactor
   - Repeat

3. **Commit Frequently**
   - Small, focused commits
   - Clear commit messages
   - Easy to review and rollback

4. **Ask for Help**
   - Don't guess on requirements
   - Clarify ambiguities
   - Verify assumptions

---

## 🎯 Success Criteria

### For Each Task:
- ✅ Code compiles without errors
- ✅ All tests pass (existing + new)
- ✅ Code coverage > 85%
- ✅ Documentation complete
- ✅ Backward compatible

### For Phase 2 Overall:
- ✅ All 5 tasks complete
- ✅ Performance overhead < 10%
- ✅ User documentation clear
- ✅ Migration guide complete

---

## 🚨 When You Get Stuck

1. **Build Errors:**
   - Read error message carefully
   - Check for missing using statements
   - Verify file paths are correct

2. **Test Failures:**
   - Read test output carefully
   - Use debugger to step through
   - Check test data and expectations

3. **Design Questions:**
   - Review `Sailfish_Phase2_Implementation_Plan.md`
   - Look at similar existing code
   - Ask user for clarification

4. **Performance Issues:**
   - Profile the code
   - Check for O(n²) algorithms
   - Optimize hot paths only

---

## 📞 Need Help?

**Ask the user if:**
- Requirements are unclear
- Design decision needed
- Breaking change seems necessary
- Stuck for > 15 minutes

**Don't ask the user if:**
- Syntax error (Google it)
- Test failure (debug it)
- Code style question (follow existing patterns)
- Implementation detail (use your judgment)

---

## ✅ Ready to Start?

1. Pick a task (start with Task 1 if unsure)
2. Read the detailed spec in `Sailfish_Phase2_Implementation_Plan.md`
3. Set up your environment (build + test)
4. Start implementing!

**Good luck! 🚀**


