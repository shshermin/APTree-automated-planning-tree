# Logging System Documentation

## Overview

The new logging system writes all console output to both the terminal and a log file simultaneously, making it easier to debug and analyze behavior tree execution.

## Features

- **Dual Output**: All messages are written to both console and log file
- **Timestamped**: Each log entry includes a precise timestamp
- **Color Coded**: Console output uses colors for different log levels
- **Structured**: Log files are organized with headers and sections
- **Configurable**: Can enable/disable console or file output independently

## Log Levels

- **🔍 DEBUG**: Detailed debugging information (gray)
- **📋 INFO**: General information (white)
- **✅ SUCCESS**: Successful operations (green)
- **⚠️ WARNING**: Warning messages (yellow)
- **❌ ERROR**: Error messages (red)

## Usage

### Basic Usage

```csharp
// Initialize the logging service (usually done once at program start)
LoggingService.Initialize("MyApp", enableConsole: true, enableFile: true);

// Log different types of messages
LoggingService.LogInfo("This is an info message");
LoggingService.LogSuccess("Operation completed successfully!");
LoggingService.LogWarning("Something might be wrong");
LoggingService.LogError("An error occurred");
LoggingService.LogDebug("Debug information");
```

### Section Headers

```csharp
// Create section headers for better organization
LoggingService.LogSection("MAIN EXECUTION");
LoggingService.LogSubsection("Subsection Title");
```

### Configuration

```csharp
// Enable/disable console or file output
LoggingService.SetConsoleOutput(false);  // Only log to file
LoggingService.SetFileOutput(false);     // Only log to console

// Get the log file path
string logPath = LoggingService.GetLogFilePath();
```

## Log File Structure

Log files are created in the `logs/` directory with the following naming convention:
```
logs/MyApp_2024-01-15_14-30-25.log
```

### Log File Header
```
================================================================================
BEHAVIOR TREE EXECUTION LOG
Started at: 2024-01-15 14:30:25.123
Log file: C:\path\to\logs\MyApp_2024-01-15_14-30-25.log
================================================================================
```

### Log Entry Format
```
[14:30:25.456] 📋 INFO This is an info message
[14:30:25.789] ✅ SUCCESS Operation completed successfully!
[14:30:26.012] ⚠️ WARNING Something might be wrong
[14:30:26.345] ❌ ERROR An error occurred
```

## Integration with Existing Code

The logging system has been integrated into:

1. **FullTreeTest.cs**: All console output now goes to both console and log file
2. **Program.cs**: Main program initialization uses logging service
3. **Various Services**: PDDL, GOAP, and other planners use the logging service

## Benefits

1. **Complete Trace**: All execution details are preserved in log files
2. **Debugging**: Easy to analyze execution flow and identify issues
3. **Performance**: Can disable console output for better performance
4. **Analysis**: Log files can be processed by external tools
5. **Documentation**: Logs serve as execution documentation

## Example Log Output

```
[14:30:25.123] 📋 INFO 🔍 Testing Neo4j connection...
[14:30:25.456] ✅ SUCCESS ✅ Successfully connected to Neo4j
[14:30:25.789] 📋 INFO 🌳 Creating behavior tree with cassette flow nodes...
[14:30:26.012] ✅ SUCCESS ✅ Created behavior tree instance
[14:30:26.345] ✅ SUCCESS ✅ Created root composite flow node
[14:30:26.678] 📋 INFO 🔧 CallPDDLPlanner: Converting PDDL plan to NodeGraph...
[14:30:26.901] ✅ SUCCESS ✅ CallPDDLPlanner: Parsed 3 action instances
[14:30:27.234] ✅ SUCCESS ✅ CallPDDLPlanner: Generated 2 relations with Sequential configuration
```

## Tips

1. **Use Appropriate Log Levels**: Use DEBUG for detailed info, INFO for general flow, ERROR for problems
2. **Section Headers**: Use LogSection() and LogSubsection() to organize your logs
3. **File Management**: Log files can grow large - consider cleanup strategies
4. **Performance**: Disable console output in production for better performance
5. **Analysis**: Use text editors or log analysis tools to search through log files

## Troubleshooting

- **No Log File Created**: Check if the `logs/` directory exists and is writable
- **Missing Messages**: Ensure LoggingService.Initialize() is called before logging
- **Performance Issues**: Consider disabling console output for large executions
- **File Permissions**: Ensure the application has write permissions to the logs directory
