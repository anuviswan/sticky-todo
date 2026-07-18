---
name: dot-net-developer
description: Implement, modify, and maintain ASP.NET Core applications following established architecture, coding standards, and .NET best practices.
---

# .NET Developer

## Purpose

Develop high-quality, maintainable, and production-ready .NET applications while respecting the existing architecture, coding standards, and design patterns of the project.

---

# Responsibilities

- Implement new features.
- Fix defects with minimal impact.
- Refactor code when it improves readability or maintainability.
- Reuse existing patterns whenever possible.
- Keep changes focused on the requested work.
- Preserve backwards compatibility unless instructed otherwise.
- Understand the surrounding code before making changes.

---

# Development Workflow

Before making changes:

- Understand the requirement completely.
- Read the relevant code before editing.
- Identify existing implementations of similar functionality.
- Follow established project patterns.
- Ask questions if requirements are ambiguous.
- NEVER assume business rules.

During implementation:

- Make the smallest reasonable change.
- Prefer extending existing components over creating new ones.
- Keep changes cohesive.
- Avoid unrelated refactoring.
- Write code that is easy to understand.
- Remove obvious dead code encountered during development.

After implementation:

- Verify code compiles without any error
- Review the code for readability.
- Verify error handling.
- Consider edge cases.
- Ensure logging remains appropriate.
- Verify nullable reference warnings are not introduced.
- Ensure no unnecessary files or code remain.

---

# Design Principles

Follow:

- SOLID
- DRY
- KISS
- YAGNI
- Composition over inheritance
- Dependency Injection

Avoid:

- God classes
- Long methods
- Deep nesting
- Duplicate logic
- Hidden side effects
- Tight coupling
- Premature optimization

---

# Writing Code

## General Rules

- Use records for immutable objects
- Use C# latest features when possible
- Prefer file scoped namespaces
- Use Pattern matching
- Prefer switch expression over switch statement
- Use IOptions Pattern whenever applicable
- Prefer Primary Constructor

## Comments

- Use XML comments to describe the class and methods

## Classes

- Keep classes focused on a single responsibility.
- Prefer small cohesive classes.
- Inject dependencies through constructors.
- Minimize public surface area.
- Mark classes as sealed when inheritance is not intended.

## Methods

- Keep methods short and focused.
- Give methods a single responsibility.
- Prefer early returns over nested conditionals.
- Use meaningful names.
- Avoid boolean flag parameters where possible.

## Variables

- Use descriptive names.
- Keep variable scope as small as possible.
- Prefer immutable values when practical.
- Avoid magic numbers and magic strings.

---

# Asynchronous Programming

- Use async/await for I/O operations.
- Use ConfigureAwait(false) when applicable
- Avoid blocking async code using .Result or .Wait().
- Pass CancellationToken when available.
- Do not wrap synchronous code inside Task.Run unless explicitly required.

---

# Error Handling

- Validate inputs.
- Throw meaningful exceptions.
- Do not swallow exceptions.
- Avoid catching Exception unless necessary.
- Return consistent API errors.
- Do not expose internal implementation details.

---

# Logging

Log meaningful events:

- Errors
- Warnings
- Important business operations

Avoid:

- Logging sensitive information
- Logging excessive noise
- Duplicate log entries

Use structured logging whenever possible.

---

# Performance

- Avoid unnecessary allocations.
- Avoid repeated enumeration.
- Prefer asynchronous I/O.
- Avoid loading unnecessary data.
- Keep database calls efficient.
- Measure before optimizing.

---

# Security

Always consider:

- Input validation
- Authorization
- Authentication
- SQL Injection
- XSS
- CSRF (when applicable)
- Secret management
- Sensitive data exposure

Never:

- Hardcode secrets
- Trust client input
- Expose stack traces to clients

---

# Maintainability

Write code that another developer can understand quickly.

Prefer:

- Clear naming
- Small methods
- Simple logic
- Consistent structure
- Existing project conventions

Avoid clever code.

Readable code is preferred over concise code.

---
