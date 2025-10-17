# Next Agent Prompt: Sailfish Adaptive Sampling Implementation

## 🎯 Mission

You are tasked with implementing adaptive sampling functionality in the Sailfish performance testing framework. This feature will allow tests to automatically determine when sufficient samples have been collected for reliable statistical analysis, rather than using fixed sample sizes.

## 📋 Context & Background

**Project**: Sailfish Performance Testing Framework
**Repository**: G:\code\Sailfish
**Branch**: Create new branch `feature/adaptive-sampling` from current branch
**Language**: C# (.NET 8+)
**Architecture**: Dependency injection with Autofac, MediatR for messaging

**Key Existing Components You'll Work With:**
- `TestCaseIterator` - Main test execution loop
- `IExecutionSettings` - Configuration interface
- `SailfishAttribute` - Test class attribute
- `SailfishOutlierDetector` - Statistical analysis (already excellent)
- `PerformanceTimer` - Timing collection

## 📖 Required Reading

**CRITICAL**: Before starting implementation, read these files to understand the current architecture:

1. **Implementation Plan**: `Sailfish_Adaptive_Sampling_Implementation_Plan.md` (comprehensive step-by-step guide)
2. **Current Execution Flow**: `source/Sailfish/Execution/TestCaseIterator.cs`
3. **Configuration System**: `source/Sailfish/Execution/ExecutionSettings.cs`
4. **Statistical Analysis**: `source/Sailfish/Analysis/SailfishOutlierDetector.cs`
5. **Dependency Injection**: `source/Sailfish/Registration/SailfishModuleRegistrations.cs`

## 🎯 Implementation Objectives

### **Primary Goal**
Implement adaptive sampling that continues test iterations until statistical convergence is achieved (coefficient of variation below target threshold).

### **Success Criteria**
1. ✅ **Backward Compatibility**: All existing tests continue to work unchanged
2. ✅ **Opt-in Feature**: Adaptive sampling is disabled by default
3. ✅ **Statistical Rigor**: Uses coefficient of variation for convergence detection
4. ✅ **Safety Mechanisms**: Minimum/maximum sample size limits
5. ✅ **Clear Logging**: Users understand what's happening during execution
6. ✅ **Integration**: Works with existing statistical analysis pipeline

### **Configuration Options**
- `UseAdaptiveSampling`: Enable/disable feature (default: false)
- `TargetCoefficientOfVariation`: Convergence threshold (default: 0.05)
- `MinimumSampleSize`: Minimum iterations (default: 10)
- `MaximumSampleSize`: Maximum iterations (default: 1000)

## 📅 5-Day Implementation Schedule

### **Day 1: Configuration & Statistical Analysis**
**Morning:**
- [ ] Add adaptive sampling properties to `IExecutionSettings` interface
- [ ] Update `ExecutionSettings` class implementation
- [ ] Add adaptive sampling properties to `SailfishAttribute`

**Afternoon:**
- [ ] Create `IStatisticalConvergenceDetector` interface
- [ ] Implement `StatisticalConvergenceDetector` class
- [ ] Create `ConvergenceResult` data class

### **Day 2: Iteration Strategy Pattern**
**Morning:**
- [ ] Create `IIterationStrategy` interface
- [ ] Create `IterationResult` data class
- [ ] Implement `FixedIterationStrategy` (existing behavior)

**Afternoon:**
- [ ] Implement `AdaptiveIterationStrategy` (new behavior)
- [ ] Test both strategies independently

### **Day 3: Integration**
**Morning:**
- [ ] Refactor `TestCaseIterator` to use strategy pattern
- [ ] Update constructor and iteration logic
- [ ] Ensure proper error handling

**Afternoon:**
- [ ] Update dependency injection in `SailfishModuleRegistrations`
- [ ] Update configuration loading methods
- [ ] Test basic integration

### **Day 4: Testing & Validation**
**Morning:**
- [ ] Create unit tests for `StatisticalConvergenceDetector`
- [ ] Create unit tests for iteration strategies
- [ ] Test convergence detection with various data sets

**Afternoon:**
- [ ] Create integration tests with sample test classes
- [ ] Test backward compatibility with existing tests
- [ ] Performance testing and optimization

### **Day 5: Documentation & Polish**
**Morning:**
- [ ] Update README.md with adaptive sampling examples
- [ ] Create migration guide documentation
- [ ] Update API documentation

**Afternoon:**
- [ ] Create example test classes demonstrating the feature
- [ ] Final testing and bug fixes
- [ ] Prepare for code review

## 🔧 Technical Implementation Notes

### **Key Design Patterns**
1. **Strategy Pattern**: For iteration logic (fixed vs adaptive)
2. **Dependency Injection**: All new components should be properly registered
3. **Interface Segregation**: Clean interfaces for testability
4. **Single Responsibility**: Each class has one clear purpose

### **Critical Integration Points**
1. **TestCaseIterator.Iterate()**: Main execution method to modify
2. **ExecutionSettings**: Configuration container
3. **SailfishAttribute**: User-facing configuration
4. **PerformanceTimer**: Source of timing data for convergence analysis

### **Statistical Approach**
- Use **Coefficient of Variation** (CV = standard deviation / mean) for convergence
- Require minimum sample size before checking convergence
- Stop when CV falls below target threshold
- Respect maximum sample size to prevent infinite loops

## 🚨 Critical Requirements

### **Backward Compatibility**
- **MUST NOT** break existing tests
- **MUST NOT** change default behavior
- **MUST** maintain existing API contracts
- **MUST** preserve existing performance characteristics

### **Error Handling**
- Graceful handling of convergence detection failures
- Proper exception handling in iteration strategies
- Clear error messages for configuration issues
- Fallback to fixed sampling if adaptive fails

### **Logging**
- Log convergence progress during adaptive sampling
- Log final convergence status (converged/max iterations reached)
- Use existing `ILogger` infrastructure
- Appropriate log levels (Information for progress, Warning for issues)

## 📊 Testing Strategy

### **Unit Tests Required**
1. `StatisticalConvergenceDetectorTests` - Test convergence logic
2. `FixedIterationStrategyTests` - Test existing behavior
3. `AdaptiveIterationStrategyTests` - Test new behavior
4. Configuration loading tests

### **Integration Tests Required**
1. End-to-end adaptive sampling with sample test classes
2. Backward compatibility with existing test suites
3. Performance impact measurement
4. Edge case handling (very fast/slow methods)

### **Test Data Scenarios**
- Low variability data (should converge quickly)
- High variability data (should reach max iterations)
- Insufficient samples (should require minimum)
- Edge cases (empty data, single sample, etc.)

## 🎯 Deliverables

### **Code Deliverables**
1. All new classes and interfaces as specified in implementation plan
2. Modified `TestCaseIterator` with strategy pattern
3. Updated configuration classes and attributes
4. Comprehensive unit and integration test suite
5. Updated dependency injection registrations

### **Documentation Deliverables**
1. Updated README.md with adaptive sampling examples
2. Migration guide for users
3. API documentation for new interfaces
4. Example test classes demonstrating usage

## 🔍 Quality Checklist

Before marking complete, verify:
- [ ] All existing tests still pass
- [ ] New unit tests achieve >90% code coverage
- [ ] Integration tests demonstrate feature working end-to-end
- [ ] No performance regression in existing functionality
- [ ] Clear, helpful logging messages
- [ ] Documentation is complete and accurate
- [ ] Code follows existing Sailfish patterns and conventions

## 🚀 Getting Started

1. **Read the implementation plan thoroughly**
2. **Explore the existing codebase** to understand current patterns
3. **Create feature branch**: `git checkout -b feature/adaptive-sampling`
4. **Start with Day 1 tasks** - configuration and statistical analysis
5. **Test each component** before moving to the next
6. **Commit frequently** with clear, descriptive messages

## 📞 Success Indicators

You'll know you're successful when:
- Existing Sailfish tests run unchanged
- New adaptive sampling tests converge appropriately
- Users can enable adaptive sampling via simple attribute configuration
- Test output clearly shows convergence progress and results
- Performance impact is minimal (<5% overhead)

## 🎯 Final Note

This is a high-impact feature that will significantly improve Sailfish's statistical rigor. Take your time to understand the existing architecture before making changes. The implementation plan provides detailed step-by-step guidance - follow it closely for best results.

**Remember**: Sailfish already has excellent statistical infrastructure. You're enhancing it, not replacing it. Leverage the existing `SailfishOutlierDetector`, statistical tests, and execution pipeline.

Good luck! 🚀
