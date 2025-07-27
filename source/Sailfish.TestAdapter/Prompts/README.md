# Sailfish Test Adapter - AI Agent Prompt System

## 📋 Overview

This directory contains the versioned prompt system for AI agent handoffs during the Sailfish Test Adapter Queue Migration project. The system ensures seamless continuity between AI agents working on different tasks.

## 📁 Directory Structure

```
G:/code/Sailfish/source/Sailfish.TestAdapter/Prompts/
├── README.md                        # This documentation file
├── NEXT_AGENT_PROMPT_TEMPLATE.md    # Template for creating new prompts
├── EXAMPLE_NEXT_AGENT_PROMPT.md     # Example showing versioning in practice
├── NextPrompt-1.1.md               # Phase 1, Task 1 prompt
├── NextPrompt-1.2.md               # Phase 1, Task 2 prompt
├── NextPrompt-1.3.md               # Phase 1, Task 3 prompt
├── ...                             # Additional versioned prompts
├── NextPrompt-2.1.md               # Phase 2, Task 1 prompt
└── NextPrompt-5.X.md               # Final phase prompts
```

## 🔢 Versioning Scheme

### Format: `NextPrompt-{Phase}.{Task}.md`

- **Phase**: Major version (1-5 corresponding to project phases)
- **Task**: Minor version (task number within that phase)

### Examples:
- `NextPrompt-1.1.md` → Phase 1, Task 1
- `NextPrompt-1.15.md` → Phase 1, Task 15 (last task in Phase 1)
- `NextPrompt-2.1.md` → Phase 2, Task 1 (first task in Phase 2)
- `NextPrompt-5.10.md` → Phase 5, Task 10

### Phase Breakdown:
- **Phase 1**: Core Infrastructure (Tasks 1-15)
- **Phase 2**: Queue Implementation (Tasks 16-25)
- **Phase 3**: Integration Layer (Tasks 26-35)
- **Phase 4**: Configuration & Monitoring (Tasks 36-45)
- **Phase 5**: Testing & Documentation (Tasks 46-55)

## 🔄 Workflow Process

### For AI Agents:

1. **Receive Assignment**: Human points you to specific prompt file (e.g., `NextPrompt-1.5.md`)
2. **Complete Task**: Follow instructions in the prompt file
3. **Create Next Prompt**: Use `NEXT_AGENT_PROMPT_TEMPLATE.md` to create next version
4. **Save Versioned File**: Save as `NextPrompt-{Phase}.{Task+1}.md`
5. **Instruct Human**: Provide path to next prompt file

### For Humans:

1. **Point Agent to Specific File**: Always use versioned files, not templates
2. **Wait for Completion**: Agent will create next prompt file
3. **Point Next Agent**: Use the path provided by previous agent

## 📝 File Descriptions

### Core Files:

- **`NEXT_AGENT_PROMPT_TEMPLATE.md`**: Master template used by agents to create new prompts
- **`EXAMPLE_NEXT_AGENT_PROMPT.md`**: Shows the versioning system in practice
- **`README.md`**: This documentation file

### Versioned Prompt Files:

- **`NextPrompt-X.Y.md`**: Complete, ready-to-use prompts for specific tasks
- Each contains full context, task details, and handoff instructions
- Created by AI agents using the template

## 🎯 Key Benefits

1. **Complete Context**: Each prompt contains full project context
2. **Clear Versioning**: Easy to track progress and task sequence
3. **Seamless Handoffs**: No information loss between agents
4. **Audit Trail**: Complete history of task progression
5. **Parallel Work**: Multiple agents can work on different phases

## 🚀 Getting Started

### For Humans:
1. Start with `NextPrompt-1.1.md` for the first task
2. Point each subsequent agent to the file created by the previous agent
3. Never edit versioned prompt files manually

### For AI Agents:
1. Read the specific prompt file you're assigned
2. Follow all instructions in that file
3. Create the next prompt file before completing your session
4. Provide clear handoff instructions to the human

## 🔧 Maintenance

### Adding New Phases:
- Update the phase breakdown in this README
- Update the template with new phase information
- Ensure version numbering continues logically

### Template Updates:
- Modify `NEXT_AGENT_PROMPT_TEMPLATE.md` for system-wide changes
- Update version number and changelog in template
- Consider impact on existing versioned prompts

### Quality Assurance:
- Verify each prompt file contains complete context
- Ensure version numbers follow the established pattern
- Check that handoff instructions are clear and actionable

## 📞 Support

If you encounter issues with the prompt system:
1. Check this README for guidance
2. Review the template and example files
3. Ensure you're using the correct versioning scheme
4. Verify file paths are absolute and correct

---

**System Version**: 2.0  
**Created**: 2025-01-27  
**Last Updated**: 2025-01-27  
**Maintainer**: Sailfish Development Team
