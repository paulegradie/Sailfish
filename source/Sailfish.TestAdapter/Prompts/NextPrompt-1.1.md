﻿You are working on the Sailfish Test Adapter Queue Migration project. This is a **FRESH START** with the updated intercepting queue architecture.

## 🚨 CRITICAL FIRST STEP - READ DOCUMENTATION
**BEFORE STARTING ANY WORK, YOU MUST READ THESE FILES IN ORDER:**

1. **📖 READ FIRST**: `G:/code/Sailfish/source/Sailfish.TestAdapter/README.md`
   - Complete project context and current architecture
   - Understanding of test adapter components and integration points
   - Current state tracking and implemented features
   - **NEW**: Intercepting Queue Architecture section

2. **📋 READ SECOND**: `G:/code/Sailfish/source/Sailfish.TestAdapter/QUEUE_MIGRATION_SPECIFICATION.md`
   - **UPDATED**: Detailed migration plan with all 62 tasks
   - Your specific task details and acceptance criteria
   - **NEW**: Intercepting architecture diagrams and implementation guidelines

3. **📝 READ THIRD**: `G:/code/Sailfish/source/Sailfish.TestAdapter/ARCHITECTURE_UPDATE_SUMMARY.md`
   - **NEW**: Summary of architectural changes and rationale
   - Understanding of the intercepting vs. side-channel approach
   - Critical success factors and risk mitigation

4. **📝 READ FOURTH**: `G:/code/Sailfish/source/Sailfish.TestAdapter/Prompts/NEXT_AGENT_PROMPT_TEMPLATE.md`
   - Template for future agent handoffs
   - Understanding of the handoff process

**⚠️ DO NOT PROCEED WITHOUT READING THESE FILES FIRST ⚠️**

## Project Context
You are implementing an **intercepting queue architecture** for the Sailfish Test Adapter to enable batch processing and cross-test-case analysis before results reach the VS Test Platform. This is a **FUNDAMENTAL ARCHITECTURAL CHANGE** from the original side-channel approach.

## Repository Information
- **Repository Root**: G:/code/Sailfish
- **Working Directory**: G:/code/Sailfish/source/Sailfish.TestAdapter
- **Branch**: pg/rearch-3
- **Project**: Sailfish.TestAdapter (.NET 8.0 and .NET 9.0)

## Current Project Status

### ✅ Completed Tasks
- **Architecture Review**: ✅ Completed - Specification updated for intercepting architecture
- **Documentation Update**: ✅ Completed - README and specification aligned

### 🔄 Current Phase
**Phase 1: Core Infrastructure**
- **Progress**: 0 of 18 tasks completed (FRESH START)
- **Status**: READY_TO_BEGIN

### 🎯 Your Assignment
**Task 1: Create Queue Message Contract**

**File to Create**: `G:/code/Sailfish/source/Sailfish.TestAdapter/Queue/Contracts/TestCompletionQueueMessage.cs`

**Description**: Create the message contract for test completion data in the queue system

**Dependencies**: None (Starting task)

**Acceptance Criteria**:
- Define TestCompletionQueueMessage class with all required properties
- Include TestCaseId, TestResult, CompletedAt, Metadata, PerformanceMetrics
- Add proper JSON serialization attributes
- Include comprehensive XML documentation
- Follow existing Sailfish coding conventions

## **CRITICAL ARCHITECTURE UNDERSTANDING** 🎯

### **Intercepting Queue Architecture**
This is **NOT** a side-channel queue. The queue **intercepts** framework publishing to enable batch processing:

```
CURRENT (Direct Framework Publishing):
TestCaseCompletedNotification 
    ↓ (TestCaseCompletedNotificationHandler)
    ↓ FrameworkTestCaseEndNotification
    ↓ (FrameworkTestCaseEndNotificationHandler)  
    ↓ VS Test Platform (ITestFrameworkWriter)

TARGET (Intercepting Queue Architecture):
TestCaseCompletedNotification 
    ↓ (TestCaseCompletedNotificationHandler)
    ↓ TestCompletionQueueMessage → In-Memory Queue
    ↓ Queue Processors (Batching, Comparison, Analysis)
    ↓ Enhanced FrameworkTestCaseEndNotification(s)
    ↓ (FrameworkTestCaseEndNotificationHandler)  
    ↓ VS Test Platform (ITestFrameworkWriter)
```

### **Key Architectural Principles**:
1. **NO direct framework publishing** from TestCaseCompletedNotificationHandler (Task 20)
2. **Queue processors are responsible** for publishing FrameworkTestCaseEndNotification
3. **Batching enables cross-test-case analysis** before framework reporting
4. **Enhanced results** with comparison data sent to framework
5. **Fallback mechanism** ensures tests never hang if queue fails

### **Why This Architecture?**
- **Cross-Test-Case Analysis**: Compare multiple test methods within a single test run
- **Batch Processing**: Group related tests for statistical analysis
- **Enhanced Results**: Enrich test results with comparison rankings and insights
- **Framework Integration**: Results appear normally in VS Test Platform with enhanced data

## Implementation Guidelines

### Code Quality Requirements
- Follow existing Sailfish coding conventions
- Add comprehensive XML documentation for all public APIs
- Include proper error handling and logging
- Ensure thread-safety where required
- Use System.Text.Json for serialization consistency

### Integration Points
- **Current Notification System**: Uses MediatR with handlers in `Handlers/TestCaseEvents/`
- **DI Container**: Autofac registration in `Registrations/TestAdapterRegistrations.cs`
- **Configuration**: Sailfish run settings system
- **Testing**: Test project at `G:/code/Sailfish/source/Tests.TestAdapter`

### Key Existing Components to Understand
- `TestCaseCompletedNotificationHandler.cs` - **WILL BE MODIFIED** in Task 20 to use queue
- `FrameworkTestCaseEndNotificationHandler.cs` - Final result reporting (unchanged)
- `TestAdapterRegistrations.cs` - DI container setup
- `TestExecutor.cs` - Test execution lifecycle

## Specific Instructions for This Task

### Step-by-Step Approach
1. **📖 READ ALL DOCUMENTATION FIRST** (critical for understanding the architecture)

2. **🔍 UNDERSTAND THE MESSAGE CONTRACT**:
   - Review existing notification classes for patterns
   - Understand what data needs to be captured for batch processing
   - Consider serialization requirements for queue storage

3. **🔧 EXAMINE EXISTING CODE**:
   - Look at `TestCaseCompletedNotification` to understand source data
   - Review `ClassExecutionSummaryTrackingFormat` for performance data
   - Study `TestInstanceContainerExternal` for test case metadata

4. **📁 CREATE THE DIRECTORY STRUCTURE**:
   - Create `Queue/Contracts/` directory if it doesn't exist
   - Follow the established project structure patterns

5. **⚙️ IMPLEMENT THE MESSAGE CONTRACT**:
   - Create TestCompletionQueueMessage class
   - Add all required properties per acceptance criteria
   - Include nested classes for TestExecutionResult and PerformanceMetrics
   - Add JSON serialization attributes
   - Include comprehensive XML documentation

6. **🧪 VALIDATE THE IMPLEMENTATION**:
   - Build the project to ensure no compilation errors
   - Run existing tests to ensure no regressions

### File Structure Context
```
Sailfish.TestAdapter/
├── Queue/                          # 🆕 NEW - Queue infrastructure
│   ├── Contracts/                  # 🎯 YOUR TASK - Interfaces and message contracts
│   │   └── TestCompletionQueueMessage.cs  # 🎯 CREATE THIS FILE
│   ├── Implementation/             # Future tasks (Tasks 3-6)
│   ├── Processors/                 # Future tasks (Tasks 7, 17, 31-36)
│   ├── Configuration/              # Future tasks (Tasks 9-11, 43-52)
│   ├── Monitoring/                 # Future tasks (Tasks 24-25, 41)
│   └── ErrorHandling/              # Future tasks (Tasks 27-28)
├── Handlers/                       # 🔄 Existing notification handlers
│   ├── TestCaseEvents/            # Test event handlers (Task 20 integration point)
│   └── FrameworkHandlers/         # Framework integration
├── Registrations/                  # 🔄 DI container setup (Task 19)
└── [other existing directories]
```

### Data Requirements for Message Contract
Based on the intercepting architecture, the message must include:

1. **TestCaseId** (string): Unique identifier for the test case
2. **TestResult** (TestExecutionResult): Success/failure status and exception details
3. **CompletedAt** (DateTime): Timestamp when test completed
4. **Metadata** (Dictionary<string, object>): Flexible metadata for batching and grouping
5. **PerformanceMetrics** (PerformanceMetrics): Performance data for analysis

## Important Notes

### Backward Compatibility
- ⚠️ **CRITICAL**: Do not break existing functionality
- All queue features must be optional and configurable
- Existing test execution must work unchanged if queue is disabled
- Maintain all existing public APIs

### Performance Considerations
- No performance regression in existing test execution
- Message contract should be lightweight and efficient
- Consider memory usage for batch processing scenarios
- Use appropriate data types for serialization

### Future Integration Points
- Message will be used by batching service (Task 16)
- Framework publishing processor will consume these messages (Task 17)
- Comparison processors will analyze batched messages (Task 31)
- Configuration system will control message handling (Tasks 43-52)

## Validation Steps
After completing your task:

1. **Build the project** - Ensure no compilation errors
2. **Run existing tests** - Verify no regressions
3. **Validate message structure** - Ensure all required properties are included
4. **Check serialization** - Verify JSON attributes are properly applied
5. **Review documentation** - Ensure XML documentation is comprehensive

## Handoff Instructions
When you complete your task:

1. **Test your implementation** - Build and run tests
2. **Document any issues** - Note any deviations or problems encountered
3. **Create next agent prompt** - Follow the versioning process below
4. **Prepare for Task 2** - Ensure foundation is solid for queue publisher interface

### 🔄 Creating NextPrompt-1.2-RESTART.md (for Task 2)

**Step 1: Copy This Template**
- Use this file as your base structure

**Step 2: Update Information**
- Move Task 1 to completed tasks list
- Update progress to "1 of 18 tasks completed"
- Change assignment to Task 2: Create Queue Publisher Interface
- Update file path and acceptance criteria for Task 2

**Step 3: Save File**
- Save as `NextPrompt-1.2-RESTART.md` in `G:/code/Sailfish/source/Sailfish.TestAdapter/Prompts/`

**Step 4: Instruct Human**
```
🤖 NEXT AGENT SETUP:
Please point the next AI agent to: G:/code/Sailfish/source/Sailfish.TestAdapter/Prompts/NextPrompt-1.2-RESTART.md

This file contains the complete prompt for Task 2 with all current context and instructions.
```

## Questions or Issues
If you encounter any issues:
- Check the existing codebase for similar patterns
- Review the QUEUE_MIGRATION_SPECIFICATION.md for clarification
- Look at the ARCHITECTURE_UPDATE_SUMMARY.md for context
- Ask for clarification if requirements are unclear

## Success Criteria for This Session
- [ ] Task 1 completed according to acceptance criteria
- [ ] All existing tests still pass
- [ ] New code follows project conventions
- [ ] Message contract supports future batching and analysis requirements
- [ ] NextPrompt-1.2-RESTART.md created for next agent
- [ ] Ready for next agent to continue with Task 2

---

**Current Date**: 2025-01-27
**Session Goal**: Complete Task 1 with intercepting architecture and prepare NextPrompt-1.2-RESTART.md for Task 2
**Architecture**: Intercepting Queue with Batch Processing
**Estimated Time**: 45-60 minutes
