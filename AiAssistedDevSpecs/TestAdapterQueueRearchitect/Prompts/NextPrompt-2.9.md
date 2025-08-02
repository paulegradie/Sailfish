# Next Agent Prompt - Task 27: Queue Configuration Loader

## Task Overview
**Task 27: Queue Configuration Loader**
- **File**: `G:/code/Sailfish/source/Sailfish.TestAdapter/Queue/Configuration/QueueConfigurationLoader.cs`
- **Description**: Create a configuration loader that integrates with the existing Sailfish settings system to load queue configuration from .sailfish.json files, environment variables, and command-line arguments.

## Context
Task 26 (Queue Metrics Collection) has been **COMPLETED SUCCESSFULLY**. The following components have been implemented:

### Completed in Task 26:
1. **IQueueMetrics Interface** (`G:/code/Sailfish/source/Sailfish.TestAdapter/Queue/Contracts/IQueueMetrics.cs`)
   - Comprehensive interface for queue metrics collection
   - Methods for recording messages published, processed, failed
   - Queue depth monitoring and batch statistics tracking
   - Async methods for retrieving metrics snapshots

2. **QueueMetrics Implementation** (`G:/code/Sailfish/source/Sailfish.TestAdapter/Queue/Monitoring/QueueMetrics.cs`)
   - Thread-safe metrics collection service
   - Historical data tracking with automatic cleanup
   - Real-time processing rate calculations
   - Comprehensive metrics aggregation and reporting

3. **Metrics Data Types** (`G:/code/Sailfish/source/Sailfish.TestAdapter/Queue/Contracts/QueueMetricsTypes.cs`)
   - QueueMetricsSnapshot, ProcessingRateMetrics, QueueDepthMetrics, BatchMetrics
   - Supporting data structures for comprehensive metrics reporting

4. **DI Registration** (Updated `G:/code/Sailfish/source/Sailfish.TestAdapter/Registrations/TestAdapterRegistrations.cs`)
   - QueueMetrics service registered as singleton in DI container
   - Proper integration with existing queue service registration patterns

### Build Status:
- ✅ All code compiles successfully
- ✅ No compilation errors
- ✅ DI registration properly configured
- ✅ Service ready for integration with queue components

## Task 27 Requirements

### Primary Objective
Create a QueueConfigurationLoader service that integrates with the existing Sailfish settings system to provide flexible configuration loading for the queue system.

### Acceptance Criteria
1. **Configuration Sources Integration**
   - Load from .sailfish.json configuration files
   - Support environment variable overrides
   - Handle command-line argument overrides
   - Provide fallback to safe defaults

2. **Settings System Integration**
   - Integrate with existing Sailfish settings infrastructure
   - Follow established configuration patterns
   - Support configuration validation and error handling

3. **Configuration Hierarchy**
   - Implement proper configuration precedence (CLI > ENV > File > Defaults)
   - Support partial configuration overrides
   - Maintain backward compatibility

4. **Validation and Error Handling**
   - Validate configuration values and ranges
   - Provide meaningful error messages for invalid configurations
   - Graceful fallback to defaults for missing or invalid settings

### Implementation Details

#### File Structure
```
G:/code/Sailfish/source/Sailfish.TestAdapter/Queue/Configuration/
├── QueueConfiguration.cs (existing)
├── QueueConfigurationLoader.cs (to be created)
└── IQueueConfigurationLoader.cs (to be created)
```

#### Key Components to Implement
1. **IQueueConfigurationLoader Interface**
   - Contract for configuration loading operations
   - Methods for loading from different sources
   - Configuration validation methods

2. **QueueConfigurationLoader Implementation**
   - Integration with Sailfish settings system
   - Multi-source configuration loading
   - Configuration validation and error handling

3. **Configuration Integration**
   - Update TestAdapterRegistrations to use loader
   - Replace hardcoded configuration with dynamic loading
   - Maintain backward compatibility

### Integration Points
- **Existing Settings System**: Integrate with current Sailfish configuration infrastructure
- **DI Container**: Register loader service appropriately
- **Queue Services**: Update registration to use loaded configuration
- **Error Handling**: Follow existing error handling patterns

## Required Reading
Before starting, read these files to understand the current architecture:

1. **Queue Migration Specification**: `G:/code/Sailfish/source/Sailfish.TestAdapter/QUEUE_MIGRATION_SPECIFICATION.md`
2. **Project README**: `G:/code/Sailfish/source/Sailfish.TestAdapter/README.md`
3. **Current Configuration**: `G:/code/Sailfish/source/Sailfish.TestAdapter/Queue/Configuration/QueueConfiguration.cs`
4. **Registration Patterns**: `G:/code/Sailfish/source/Sailfish.TestAdapter/Registrations/TestAdapterRegistrations.cs`

## Implementation Steps
1. **Analyze Existing Settings System**
   - Study current Sailfish configuration patterns
   - Understand .sailfish.json file structure
   - Review environment variable handling

2. **Create Interface Contract**
   - Define IQueueConfigurationLoader interface
   - Specify configuration loading methods
   - Include validation and error handling contracts

3. **Implement Configuration Loader**
   - Create QueueConfigurationLoader class
   - Implement multi-source configuration loading
   - Add configuration validation logic

4. **Update DI Registration**
   - Register configuration loader service
   - Update queue service registration to use loader
   - Maintain backward compatibility

5. **Test and Validate**
   - Ensure configuration loading works correctly
   - Verify fallback behavior
   - Test configuration validation

## Success Criteria
- [ ] IQueueConfigurationLoader interface created with comprehensive contract
- [ ] QueueConfigurationLoader implementation with multi-source loading
- [ ] Integration with existing Sailfish settings system
- [ ] Configuration validation and error handling
- [ ] Updated DI registration using configuration loader
- [ ] Backward compatibility maintained
- [ ] All code compiles without errors
- [ ] Configuration loading tested and working

## Notes
- Follow existing Sailfish configuration patterns and conventions
- Ensure thread safety for configuration loading operations
- Maintain comprehensive error handling and logging
- Consider performance implications of configuration loading
- Document configuration options and precedence rules

## Next Steps After Completion
After completing Task 27, create `NextPrompt-2.10.md` for the next task in the queue migration project.
