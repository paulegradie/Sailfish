# Example: NextPrompt-1.2.md

## 🤖 AI Agent Prompt - Ready to Copy/Paste
**This example shows how to create NextPrompt-1.2.md for Task 2 in Phase 1**

```
You are working on the Sailfish Test Adapter Queue Migration project. This is a continuation of previous work by other AI agents.

## 🚨 CRITICAL FIRST STEP - READ DOCUMENTATION
**BEFORE STARTING ANY WORK, YOU MUST READ THESE FILES IN ORDER:**

1. **📖 READ FIRST**: `G:/code/Sailfish/source/Sailfish.TestAdapter/README.md`
   - Complete project context and current architecture
   - Understanding of test adapter components and integration points
   - Current state tracking and implemented features

2. **📋 READ SECOND**: `G:/code/Sailfish/source/Sailfish.TestAdapter/QUEUE_MIGRATION_SPECIFICATION.md`
   - Detailed migration plan with all 55 tasks
   - Your specific task details and acceptance criteria
   - Architecture diagrams and implementation guidelines

3. **📝 READ THIRD**: `G:/code/Sailfish/source/Sailfish.TestAdapter/NEXT_AGENT_PROMPT_TEMPLATE.md`
   - Template for future agent handoffs
   - Understanding of the handoff process

**⚠️ DO NOT PROCEED WITHOUT READING THESE FILES FIRST ⚠️**

## Project Context
You are implementing a queue-based architecture for the Sailfish Test Adapter to enable asynchronous processing of test completion events. The project is broken down into 55 small tasks across 5 phases.

## Repository Information
- **Repository Root**: G:/code/Sailfish
- **Working Directory**: G:/code/Sailfish/source/Sailfish.TestAdapter
- **Branch**: pg/method-comparisons
- **Project**: Sailfish.TestAdapter (.NET 8.0 and .NET 9.0)

## Current Project Status

### ✅ Completed Tasks
- Task 1: Create Queue Message Contract ✅

### 🔄 Current Phase
**Phase 1: Core Infrastructure**
- **Progress**: 1 of 15 tasks completed
- **Status**: IN_PROGRESS

### 🎯 Your Assignment
**Task 2: Create Queue Publisher Interface**

**File to Create**: `G:/code/Sailfish/source/Sailfish.TestAdapter/Queue/Contracts/ITestCompletionQueuePublisher.cs`

**Description**: Create the message contract for test completion events

**Dependencies**: None (first task)

**Acceptance Criteria**:
- Define TestCompletionQueueMessage class with all required properties
- Include TestCaseId, TestResult, CompletedAt, Metadata, PerformanceMetrics
- Add proper serialization attributes if needed
- Include XML documentation for all properties

## Implementation Guidelines

### Code Quality Requirements
- Follow existing Sailfish coding conventions
- Add comprehensive XML documentation for all public APIs
- Include proper error handling and logging
- Ensure thread-safety where required
- Write unit tests with >90% coverage

### Integration Points
- **Current Notification System**: Uses MediatR with handlers in `Handlers/TestCaseEvents/`
- **DI Container**: Autofac registration in `Registrations/TestAdapterRegistrations.cs`
- **Configuration**: Sailfish run settings system
- **Testing**: Test project at `G:/code/Sailfish/source/Tests.TestAdapter`

### Key Existing Components to Understand
- `TestCaseCompletedNotificationHandler.cs` - Main integration point for queue publishing
- `FrameworkTestCaseEndNotificationHandler.cs` - Final result reporting
- `TestAdapterRegistrations.cs` - DI container setup
- `TestExecutor.cs` - Test execution lifecycle

## Specific Instructions for This Task

### Step-by-Step Approach
1. **📖 READ DOCUMENTATION FIRST** (if you haven't already):
   - README.md for complete project context
   - QUEUE_MIGRATION_SPECIFICATION.md for your specific task details
   - Any additional context files listed above

2. **🔍 UNDERSTAND YOUR TASK**:
   - Locate Task 1 in the specification
   - Review acceptance criteria and dependencies
   - Understand how your task fits into the overall architecture

3. **🔧 EXAMINE EXISTING CODE**:
   - Study the integration points mentioned above
   - Look at similar patterns in the existing codebase
   - Understand the current notification system and DI setup

4. **📁 CREATE THE REQUIRED FILE** following the exact path specified

5. **⚙️ IMPLEMENT THE FUNCTIONALITY** according to acceptance criteria

6. **🧪 ADD COMPREHENSIVE TESTS** if specified in the task

7. **📚 UPDATE DOCUMENTATION** if the task affects public APIs

### File Structure Context
```
Sailfish.TestAdapter/
├── Queue/                          # 🆕 New queue infrastructure (CREATE THIS)
│   ├── Contracts/                  # 🆕 Interfaces and message contracts (CREATE THIS)
│   │   └── TestCompletionQueueMessage.cs  # 🎯 YOUR TASK
│   ├── Implementation/             # Future tasks
│   ├── Processors/                 # Future tasks
│   ├── Configuration/              # Future tasks
│   ├── Monitoring/                 # Future tasks
│   └── ErrorHandling/              # Future tasks
├── Handlers/                       # 🔄 Existing notification handlers
│   ├── TestCaseEvents/            # Test event handlers (integration point)
│   └── FrameworkHandlers/         # Framework integration
├── Registrations/                  # 🔄 DI container setup
└── [other existing directories]
```

### Testing Requirements
This task focuses on creating the message contract. Unit tests will be added in Task 15 (Create Queue Unit Tests) which will test all infrastructure components together.

### Error Handling Requirements
- Use existing Sailfish exception types where appropriate
- Log errors using the existing ILogger interface
- Ensure graceful degradation if queue features fail
- Follow existing error handling patterns in the codebase

## Important Notes

### Backward Compatibility
- ⚠️ **CRITICAL**: Do not break existing functionality
- All queue features must be optional and configurable
- Existing test execution must work unchanged if queue is disabled
- Maintain all existing public APIs

### Performance Considerations
- No performance regression in existing test execution
- Queue operations should be asynchronous and non-blocking
- Proper disposal of resources and memory management
- Consider thread pool usage and async/await best practices

### Configuration Philosophy
- Default to disabled/minimal configuration
- Provide sensible defaults for all settings
- Make features discoverable but not intrusive
- Support both programmatic and file-based configuration

## Validation Steps
After completing your task:

1. **Build the project** - Ensure no compilation errors
2. **Run existing tests** - Verify no regressions
3. **Test your implementation** - Verify acceptance criteria met
4. **Check integration** - Ensure proper DI registration if applicable
5. **Update documentation** - Update README.md current state section if needed

## Handoff Instructions
When you complete your task:

1. **Commit your changes** with a clear commit message: "Task 2: Create Queue Publisher Interface"
2. **Update the specification** - Mark your task as completed
3. **Document any issues** - Note any deviations or problems encountered
4. **Create next agent prompt** - Follow the versioning process below
5. **Test the integration** - Ensure your changes work with existing code

### 🔄 Creating NextPrompt-1.3.md (for Task 3)

**Step 1: Copy Template**
- Use `NEXT_AGENT_PROMPT_TEMPLATE.md` as your base

**Step 2: Update Information**
- Move Task 2 to completed tasks list
- Update progress to "2 of 15 tasks completed"
- Change assignment to Task 3
- Update file path and acceptance criteria for Task 3

**Step 3: Save File**
- Save as `NextPrompt-1.3.md` in `G:/code/Sailfish/source/Sailfish.TestAdapter/Prompts/`

**Step 4: Instruct Human**
```
🤖 NEXT AGENT SETUP:
Please point the next AI agent to: G:/code/Sailfish/source/Sailfish.TestAdapter/Prompts/NextPrompt-1.3.md

This file contains the complete prompt for Task 3 with all current context and instructions.
```

## Questions or Issues
If you encounter any issues:
- Check the existing codebase for similar patterns
- Review the QUEUE_MIGRATION_SPECIFICATION.md for clarification
- Look at completed tasks for implementation examples
- Ask for clarification if requirements are unclear

## Success Criteria for This Session
- [ ] Task 2 completed according to acceptance criteria
- [ ] All existing tests still pass
- [ ] New code follows project conventions
- [ ] Documentation updated if required
- [ ] NextPrompt-1.3.md created for next agent
- [ ] Ready for next agent to continue with Task 3

---

**Current Date**: 2025-01-27
**Session Goal**: Complete Task 2 and prepare NextPrompt-1.3.md for Task 3
**Estimated Time**: 30-45 minutes
```

---

## How to Use This Example

This example demonstrates the versioned prompt system:

### 📁 File Structure After This Task:
```
G:/code/Sailfish/source/Sailfish.TestAdapter/Prompts/
├── NEXT_AGENT_PROMPT_TEMPLATE.md    # Template for creating new prompts
├── EXAMPLE_NEXT_AGENT_PROMPT.md     # This example file
├── README.md                        # System documentation
├── NextPrompt-1.1.md               # Task 1 prompt (completed)
├── NextPrompt-1.2.md               # Task 2 prompt (this example)
└── NextPrompt-1.3.md               # Task 3 prompt (created by agent)
```

### 🔄 Workflow Demonstration:
1. **Agent receives**: `NextPrompt-1.2.md` (this file)
2. **Agent completes**: Task 2
3. **Agent creates**: `NextPrompt-1.3.md` using the template
4. **Human points next agent to**: `NextPrompt-1.3.md`

This versioning system ensures each agent has complete context and creates a clear handoff trail.
