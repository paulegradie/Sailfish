# Next Agent Prompt Template - Sailfish Queue Migration

## Template Instructions
**For Human Use**: Copy the template below, fill in the bracketed placeholders, and use as the prompt for the next AI agent.

**Update After Each Task**: Modify the completed tasks, current status, and next task information.

---

## 🤖 AI Agent Prompt Template

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

3. **📝 READ THIRD**: `[ADDITIONAL_CONTEXT_FILES]`
   - Any additional context files created during migration
   - Previous agent notes and implementation decisions

**⚠️ DO NOT PROCEED WITHOUT READING THESE FILES FIRST ⚠️**

## Project Context
You are implementing a queue-based architecture for the Sailfish Test Adapter to enable asynchronous processing of test completion events. The project is broken down into 55 small tasks across 5 phases.

## Repository Information
- **Repository Root**: G:/code/Sailfish
- **Working Directory**: G:/code/Sailfish/source/Sailfish.TestAdapter
- **Branch**: [CURRENT_BRANCH_NAME]
- **Project**: Sailfish.TestAdapter (.NET 8.0 and .NET 9.0)

## Current Project Status

### ✅ Completed Tasks
[LIST_COMPLETED_TASKS_WITH_NUMBERS]
Example:
- Task 1: Create Queue Message Contract ✅
- Task 2: Create Queue Publisher Interface ✅
- Task 3: Create Queue Processor Interface ✅

### 🔄 Current Phase
**Phase [CURRENT_PHASE_NUMBER]: [PHASE_NAME]**
- **Progress**: [X] of [Y] tasks completed
- **Status**: [IN_PROGRESS/COMPLETED]

### 🎯 Your Assignment
**Task [NEXT_TASK_NUMBER]: [TASK_NAME]**

**File to Create/Modify**: `[FILE_PATH]`

**Description**: [TASK_DESCRIPTION]

**Dependencies**: [DEPENDENCY_TASKS] (✅ All completed)

**Acceptance Criteria**:
[ACCEPTANCE_CRITERIA_LIST]

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
   - Locate Task [NEXT_TASK_NUMBER] in the specification
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
├── Queue/                          # 🆕 New queue infrastructure
│   ├── Contracts/                  # Interfaces and message contracts
│   ├── Implementation/             # Core implementations
│   ├── Processors/                 # Background processors
│   ├── Configuration/              # Settings and config
│   ├── Monitoring/                 # Health checks and metrics
│   └── ErrorHandling/              # Retry and error handling
├── Handlers/                       # 🔄 Existing notification handlers
│   ├── TestCaseEvents/            # Test event handlers (integration point)
│   └── FrameworkHandlers/         # Framework integration
├── Registrations/                  # 🔄 DI container setup
└── [other existing directories]
```

### Testing Requirements
[TESTING_REQUIREMENTS_FOR_TASK]

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

1. **Commit your changes** with a clear commit message: "Task [NUMBER]: [TASK_NAME]"
2. **Update the specification** - Mark your task as completed
3. **Document any issues** - Note any deviations or problems encountered
4. **Prepare next prompt** - Update this template for the next agent
5. **Test the integration** - Ensure your changes work with existing code

## Questions or Issues
If you encounter any issues:
- Check the existing codebase for similar patterns
- Review the QUEUE_MIGRATION_SPECIFICATION.md for clarification
- Look at completed tasks for implementation examples
- Ask for clarification if requirements are unclear

## Success Criteria for This Session
- [ ] Task [NEXT_TASK_NUMBER] completed according to acceptance criteria
- [ ] All existing tests still pass
- [ ] New code follows project conventions
- [ ] Documentation updated if required
- [ ] Ready for next agent to continue with Task [NEXT_TASK_NUMBER + 1]

---

**Current Date**: [CURRENT_DATE]
**Session Goal**: Complete Task [NEXT_TASK_NUMBER] and prepare for Task [NEXT_TASK_NUMBER + 1]
**Estimated Time**: [ESTIMATED_TIME] (most tasks should take 30-60 minutes)
```

---

## Template Usage Instructions

### For Each New Agent Session:

1. **Copy the template above**
2. **Fill in all bracketed placeholders**:
   - `[CURRENT_BRANCH_NAME]` - Current git branch
   - `[ADDITIONAL_CONTEXT_FILES]` - Any new context files created
   - `[LIST_COMPLETED_TASKS_WITH_NUMBERS]` - List of completed tasks
   - `[CURRENT_PHASE_NUMBER]` and `[PHASE_NAME]` - Current phase info
   - `[NEXT_TASK_NUMBER]` and `[TASK_NAME]` - Next task to complete
   - `[FILE_PATH]` - Exact file path for the task
   - `[TASK_DESCRIPTION]` - Description from specification
   - `[DEPENDENCY_TASKS]` - Required dependencies
   - `[ACCEPTANCE_CRITERIA_LIST]` - Acceptance criteria from specification
   - `[TESTING_REQUIREMENTS_FOR_TASK]` - Specific testing needs
   - `[CURRENT_DATE]` - Current date
   - `[ESTIMATED_TIME]` - Time estimate for the task

3. **Review and customize** any specific instructions for the task
4. **Use as the complete prompt** for the next AI agent

### Maintenance Notes:
- Update this template if new patterns or requirements emerge
- Add new sections if additional context becomes necessary
- Keep the template focused and actionable
- Ensure all file paths are absolute and correct

**Template Version**: 1.0  
**Created**: 2025-01-27  
**Last Updated**: 2025-01-27
