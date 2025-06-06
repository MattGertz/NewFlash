# Copilot Instructions

<!-- Use this file to provide workspace-specific custom instructions to Copilot. For more details, visit https://code.visualstudio.com/docs/copilot/copilot-customization#_use-a-githubcopilotinstructionsmd-file -->

## Project Overview
This is a C# class library project that provides file synchronization functionality between directories based on regex patterns.

## Development Guidelines
- Use .NET 8.0 features and modern C# syntax
- Implement async/await patterns for I/O operations
- Use thread-safe collections and operations
- Implement proper error handling and logging
- Follow SOLID principles and clean architecture
- Use dependency injection where appropriate
- Write comprehensive unit tests using xUnit

## Key Features
- Asynchronous file synchronization between origin and destination directories
- Regex-based file filtering using semicolon-separated patterns
- File comparison based on modification times
- Thread-safe operations with controlled concurrency
- Comprehensive error handling and progress reporting

## Performance Considerations
- Use async I/O operations to avoid blocking threads
- Implement semaphore or similar mechanisms to control concurrent operations
- Use memory-efficient file reading for large files
- Implement proper disposal patterns for file handles

## Build Considerations
- We always use PowerShell for building and running the project
- Never use '&&' in PowerShell commands; use semicolons for concatenation instead
- Always ask before building so we can verify that files got saved to disk, since autosave can have issues with keeping up

## Code Formatting Rules
- **CRITICAL**: Always ensure proper line breaks between code elements:
  - After closing braces `}` before the next statement
  - After `#region` statements before method signatures
  - Between method definitions
  - After `#endregion` statements
- When using replace_string_in_file or insert_edit_into_file tools, always double-check that line breaks are preserved
- Never concatenate closing braces with subsequent code on the same line
- Always put each `#region` and `#endregion` on its own line
- Verify that each method definition starts on a new line after the previous method's closing brace