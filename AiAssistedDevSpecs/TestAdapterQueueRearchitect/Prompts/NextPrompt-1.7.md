﻿You are working on the Sailfish Test Adapter Queue Migration project. This is a **CONTINUATION** of previous work by other AI agents.

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

3. **📝 READ THIRD**: `G:/code/Sailfish/source/Sailfish.TestAdapter/Prompts/NEXT_AGENT_PROMPT_TEMPLATE.md`
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
- **Task 1**: Create Queue Message Contract ✅ Completed - TestCompletionQueueMessage.cs created with all required properties, JSON serialization, and comprehensive XML documentation
- **Task 2**: Create Queue Publisher Interface ✅ Completed - ITestCompletionQueuePublisher.cs created with async PublishTestCompletion method, cancellation token support, and comprehensive XML documentation
- **Task 3**: Create Queue Processor Interface ✅ Completed - ITestCompletionQueueProcessor.cs created with async ProcessTestCompletion method, cancellation token support, and comprehensive XML documentation
- **Task 4**: Create Queue Service Interface ✅ Completed - ITestCompletionQueue.cs created with Enqueue, Dequeue, lifecycle management methods, queue status monitoring, and comprehensive XML documentation
- **Task 5**: Create In-Memory Queue Implementation ✅ Completed - InMemoryTestCompletionQueue.cs created using System.Threading.Channels with thread-safe operations, proper lifecycle management, and comprehensive error handling
- **Task 6**: Create Queue Publisher Implementation ✅ Completed - TestCompletionQueuePublisher.cs created implementing ITestCompletionQueuePublisher with proper error handling, logging, and async/await best practices

### 🔄 Current Phase
**Phase 1: Core Infrastructure**
- **Progress**: 6 of 18 tasks completed
- **Status**: IN_PROGRESS

### 🎯 Your Assignment
**Task 7: Create Queue Processor Base Class**

**File to Create**: `G:/code/Sailfish/source/Sailfish.TestAdapter/Queue/Processors/TestCompletionQueueProcessorBase.cs`

**Description**: Create abstract base class for queue processors that provides common functionality for error handling, logging, and template method pattern for processing logic

**Dependencies**: Task 3 (✅ Completed)

**Acceptance Criteria**:
- Abstract base class implementing ITestCompletionQueueProcessor interface
- Common functionality for error handling and logging using existing Sailfish patterns
- Template method pattern for processing logic with protected abstract methods
- Proper cancellation token handling throughout the processing pipeline
- Comprehensive XML documentation for all public and protected APIs
- Thread-safe operations for concurrent processor execution scenarios
- Integration with existing Sailfish logging infrastructure (ILogger)

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

2. **🔍 UNDERSTAND THE QUEUE PROCESSOR BASE CLASS IMPLEMENTATION**:
   - Review Task 7 in the specification for detailed requirements
   - Understand how the base class will be used by concrete processors
   - Consider the template method pattern for extensible processing logic

3. **🔧 EXAMINE EXISTING CODE**:
   - Look at the completed Tasks 1-6 for patterns and conventions
   - Review the ITestCompletionQueueProcessor interface you need to implement
   - Study error handling patterns in the existing Sailfish codebase

4. **📁 CREATE THE IMPLEMENTATION FILE** in the Queue/Processors directory

5. **⚙️ IMPLEMENT THE BASE CLASS** according to acceptance criteria

6. **🧪 BUILD AND VALIDATE** to ensure no compilation errors

### File Structure Context
```
Sailfish.TestAdapter/
├── Queue/                          # 🆕 Queue infrastructure
│   ├── Contracts/                  # 🎯 Interfaces and message contracts
│   │   ├── TestCompletionQueueMessage.cs  # ✅ COMPLETED (Task 1)
│   │   ├── ITestCompletionQueuePublisher.cs  # ✅ COMPLETED (Task 2)
│   │   ├── ITestCompletionQueueProcessor.cs  # ✅ COMPLETED (Task 3)
│   │   └── ITestCompletionQueue.cs  # ✅ COMPLETED (Task 4)
│   ├── Implementation/             # 🎯 Core implementations
│   │   ├── InMemoryTestCompletionQueue.cs  # ✅ COMPLETED (Task 5)
│   │   └── TestCompletionQueuePublisher.cs  # ✅ COMPLETED (Task 6)
│   ├── Processors/                 # 🎯 YOUR TASK (Task 7)
│   │   └── TestCompletionQueueProcessorBase.cs  # 🎯 YOUR TASK (Task 7)
│   ├── Configuration/              # Future tasks (Tasks 9-11, 26, 43-52)
│   ├── Monitoring/                 # Future tasks (Tasks 24-25, 41)
│   └── ErrorHandling/              # Future tasks (Tasks 27-28)
├── Handlers/                       # 🔄 Existing notification handlers
│   ├── TestCaseEvents/            # Test event handlers (Task 20 integration point)
│   └── FrameworkHandlers/         # Framework integration
├── Registrations/                  # 🔄 DI container setup (Task 19)
└── [other existing directories]
```

### Implementation Requirements
Based on the intercepting architecture, the base class must include:

1. **ITestCompletionQueueProcessor implementation** with abstract ProcessTestCompletion method
2. **Template method pattern** for extensible processing logic
3. **Error handling infrastructure** for processor execution failures
4. **Logging integration** using existing Sailfish logging patterns
5. **Cancellation token support** for graceful shutdown scenarios
6. **Thread-safe operations** for concurrent processor execution
7. **Comprehensive XML documentation** explaining the base class architecture

## Important Notes

### Backward Compatibility
- ⚠️ **CRITICAL**: Do not break existing functionality
- All queue features must be optional and configurable
- Existing test execution must work unchanged if queue is disabled
- Maintain all existing public APIs

### Performance Considerations
- Implementation should support async operations to avoid blocking
- Consider memory usage for processor execution scenarios
- Use appropriate async patterns for queue operations
- Design for high-throughput test execution scenarios

### Future Integration Points
- Base class will be extended by concrete processors (Tasks 13, 17, 31-36)
- Must support processor pipeline integration (Task 40)
- Will be registered in DI container (Task 19)
- Configuration system will control processor behavior (Tasks 43-52)

## Validation Steps
After completing your task:

1. **Build the project** - Ensure no compilation errors
2. **Run existing tests** - Verify no regressions
3. **Validate implementation** - Ensure all interface methods are implemented
4. **Check documentation** - Verify XML documentation is comprehensive
5. **Review integration points** - Ensure implementation supports future tasks

## Handoff Instructions
When you complete your task:

1. **Test your implementation** - Build and run tests
2. **Document any issues** - Note any deviations or problems encountered
3. **Create next agent prompt** - Follow the versioning process below
4. **Prepare for Task 8** - Ensure foundation is solid for queue consumer service implementation

### 🔄 Creating NextPrompt-1.8.md (for Task 8)

**Step 1: Copy This Template**
- Use this file as your base structure

**Step 2: Update Information**
- Move Task 7 to completed tasks list
- Update progress to "7 of 18 tasks completed"
- Change assignment to Task 8: Create Queue Consumer Service
- Update file path and acceptance criteria for Task 8

**Step 3: Save File**
- Save as `NextPrompt-1.8.md` in `G:/code/Sailfish/source/Sailfish.TestAdapter/Prompts/`

**Step 4: Instruct Human**
```
🤖 NEXT AGENT SETUP:
Please point the next AI agent to: G:/code/Sailfish/source/Sailfish.TestAdapter/Prompts/NextPrompt-1.8.md

This file contains the complete prompt for Task 8 with all current context and instructions.
```

## Questions or Issues
If you encounter any issues:
- Check the existing codebase for similar patterns
- Review the QUEUE_MIGRATION_SPECIFICATION.md for clarification
- Look at the completed Tasks 1-6 for implementation examples
- Ask for clarification if requirements are unclear

## Success Criteria for This Session
- [ ] Task 7 completed according to acceptance criteria
- [ ] All existing tests still pass
- [ ] New code follows project conventions
- [ ] Implementation supports future queue processor requirements
- [ ] NextPrompt-1.8.md created for next agent
- [ ] Ready for next agent to continue with Task 8

---

**Current Date**: 2025-01-27
**Session Goal**: Complete Task 7 with intercepting architecture and prepare NextPrompt-1.8.md for Task 8
**Architecture**: Intercepting Queue with Batch Processing
**Estimated Time**: 30-45 minutes
