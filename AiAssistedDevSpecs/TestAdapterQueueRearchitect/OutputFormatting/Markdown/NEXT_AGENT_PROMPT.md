# 🤖 Next Agent Prompt - Method Comparison Markdown Generation

## 📋 Your Mission

Implement automatic markdown file generation for method comparison tests that use the `[WriteToMarkdown]` attribute in the Sailfish performance testing framework.

## 🎯 Specific Task

**Problem**: The test file `G:\code\Sailfish\source\PerformanceTests\ExamplePerformanceTests\MethodComparisonExample.cs` has the `[WriteToMarkdown]` attribute but doesn't generate markdown files.

**Goal**: Make method comparison tests honor the `[WriteToMarkdown]` attribute and generate professional markdown files.

## 📚 Context Documents

**IMPORTANT**: Read these documents first to understand the full context:

1. **`NEXT_AGENT_CONTEXT_HANDOFF.md`** - Complete context and background
2. **`METHOD_COMPARISON_MARKDOWN_GENERATION_DESIGN.md`** - Detailed implementation specification
3. **`METHOD_COMPARISON_MARKDOWN_ISSUE_ANALYSIS.md`** - Root cause analysis

## 🏗️ Architecture Summary

**The Issue**: Method comparison tests bypass the normal markdown generation pipeline:
- **Regular tests**: `SailfishExecutor` → `WriteToMarkDownNotification` → Markdown file ✅
- **Method comparison tests**: `TestAdapter Queue` → `MethodComparisonProcessor` → IDE output only ❌

**Your Solution**: Add markdown generation to the TestAdapter workflow using the notification pattern.

## 🔧 Implementation Steps

### **Step 1: Create Notification Infrastructure**
```csharp
// File: source/Sailfish/Contracts.Private/WriteMethodComparisonMarkdownNotification.cs
public class WriteMethodComparisonMarkdownNotification : INotification
{
    public string TestClassName { get; set; }
    public string MarkdownContent { get; set; }
    public string OutputDirectory { get; set; }
    public DateTime Timestamp { get; set; }
}
```

### **Step 2: Create Handler**
```csharp
// File: source/Sailfish/DefaultHandlers/Sailfish/MethodComparisonMarkdownHandler.cs
public class MethodComparisonMarkdownHandler : INotificationHandler<WriteMethodComparisonMarkdownNotification>
{
    // Generate filename: {TestClassName}_MethodComparisons_{timestamp}.md
    // Write markdown content to file
}
```

### **Step 3: Enhance MethodComparisonProcessor**
```csharp
// In: source/Sailfish.TestAdapter/Queue/Processors/MethodComparisonProcessor.cs
// Add after line 284: await _batchProcessor.ProcessBatch(batch, cancellationToken);

await GenerateMarkdownIfRequested(batch, cancellationToken);

private async Task GenerateMarkdownIfRequested(TestCaseBatch batch, CancellationToken cancellationToken)
{
    // 1. Check for WriteToMarkdown attribute
    // 2. Generate markdown using unified formatter
    // 3. Publish notification
}
```

### **Step 4: Register in DI**
```csharp
// In: source/Sailfish/Registration/SailfishModuleRegistrations.cs
builder.RegisterType<MethodComparisonMarkdownHandler>()
    .As<INotificationHandler<WriteMethodComparisonMarkdownNotification>>();
```

## 🧪 Test Case

**File**: `G:\code\Sailfish\source\PerformanceTests\ExamplePerformanceTests\MethodComparisonExample.cs`

**Expected Result**: After implementation, running this test should create a markdown file:
- **Location**: Default output directory
- **Name**: `MethodComparisonExample_MethodComparisons_{timestamp}.md`
- **Content**: Professional markdown with performance analysis using unified formatter

## 🚨 Important Constraints

1. **Use notification pattern** - Don't do direct file I/O in TestAdapter (analyzer restrictions)
2. **Leverage existing unified formatter** - `ISailDiffUnifiedFormatter` with `OutputContext.Markdown`
3. **No breaking changes** - Maintain all existing functionality
4. **Follow existing patterns** - Study how `WriteToMarkDownNotification` works

## 🎯 Success Criteria

- ✅ Method comparison tests with `[WriteToMarkdown]` generate markdown files
- ✅ Generated files use unified formatter for professional output
- ✅ No compilation errors or analyzer violations
- ✅ No impact on existing functionality
- ✅ Test case `MethodComparisonExample.cs` creates markdown file

## 🚀 Getting Started

1. **Read the context documents** (especially `NEXT_AGENT_CONTEXT_HANDOFF.md`)
2. **Examine the test case** to understand current behavior
3. **Study existing notification pattern** (`WriteToMarkDownNotification`)
4. **Implement the solution** following the design specification
5. **Test with the example** to verify it works

## 📁 Key Files

**Create**:
- `source/Sailfish/Contracts.Private/WriteMethodComparisonMarkdownNotification.cs`
- `source/Sailfish/DefaultHandlers/Sailfish/MethodComparisonMarkdownHandler.cs`

**Modify**:
- `source/Sailfish.TestAdapter/Queue/Processors/MethodComparisonProcessor.cs`
- `source/Sailfish/Registration/SailfishModuleRegistrations.cs`

**Test**:
- `source/PerformanceTests/ExamplePerformanceTests/MethodComparisonExample.cs`

---

**Status**: 📋 **READY FOR IMPLEMENTATION**  
**Estimated Time**: 4-6 hours  
**All context provided** - Read the handoff documents for complete details!
