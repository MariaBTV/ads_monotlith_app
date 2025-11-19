# Best Practice Coding Standards

To ensure consistency and quality during the decomposition, adhere to the following standards.

## 1. Language & Terminology
- **British English:** Use British spelling for all documentation, comments, and UI text (e.g., `Colour`, `Behaviour`, `Catalogue`, `Unauthorised`).
- **Code:** Keep variable names and class names in standard US English if they are part of the framework (e.g., `Color` struct in System.Drawing), but prefer British English for domain concepts if consistent with the team's ubiquitous language (e.g., `CatalogueService`). *For this project, we will stick to the existing naming conventions in code to minimise diff noise, but comments must be British English.*

## 2. C# Coding Conventions
- **Naming:**
  - Classes/Methods: `PascalCase`
  - Private Fields: `_camelCase`
  - Parameters/Locals: `camelCase`
- **Async/Await:** Always use `async` and `await` for I/O-bound operations. Suffix async methods with `Async` (e.g., `CheckoutAsync`).
- **Dependency Injection:** Use constructor injection. Avoid `IServiceLocator` patterns.
- **Namespace:** Follow the folder structure. New API namespaces should start with `RetailMonolith.Checkout.Api`.

## 3. API Design Standards
- **RESTful Principles:**
  - Use nouns for resources (e.g., `/orders`, `/checkout`).
  - Use correct HTTP verbs (`GET`, `POST`, `PUT`, `DELETE`).
- **Status Codes:**
  - `200 OK`: Success with content.
  - `201 Created`: Resource created (return `Location` header).
  - `400 Bad Request`: Validation failure.
  - `404 Not Found`: Resource does not exist.
  - `500 Internal Server Error`: Unhandled exception.
- **DTOs:** Always use Data Transfer Objects (DTOs) for API requests and responses. Do not expose Entity Framework models directly over the API boundary.

## 4. Testing Standards

### Test Structure
- **Pattern:** Arrange, Act, Assert (AAA) - keep these sections clearly separated.
- **Naming:** `MethodName_StateUnderTesting_ExpectedBehavior` (e.g., `Checkout_WithValidCart_ReturnsOrder`).
- **One Assert Per Test:** Each test should verify one behaviour (exceptions: related assertions like status code + response body).

### Test Organisation
```csharp
public class CheckoutControllerTests
{
    // Group related tests in nested classes
    public class CheckoutAsyncMethod
    {
        [Fact]
        public async Task WithValidCart_ReturnsOrderWithPaidStatus()
        {
            // Arrange: Set up mocks and test data
            var mockPaymentGateway = new Mock<IPaymentGateway>();
            mockPaymentGateway.Setup(x => x.ChargeAsync(It.IsAny<PaymentRequest>(), default))
                .ReturnsAsync(new PaymentResult { Succeeded = true });
            
            // Act: Call the method under test
            var result = await controller.CheckoutAsync(request);
            
            // Assert: Verify expectations
            Assert.NotNull(result);
            Assert.Equal("Paid", result.Status);
        }
    }
}
```

### Mocking Guidelines
- **Use Mocks For:** External dependencies (HTTP, Payment Gateway, Message Bus).
- **Use In-Memory DB For:** Simple data access tests (EF Core InMemory provider).
- **Use Real DB For:** Integration tests (SQL Server container or LocalDB).
- **Never Mock:** The class under test itself.

### Test Data
- **Use Builders:** Create test data builders for complex objects (e.g., `OrderBuilder`, `CartBuilder`).
- **Avoid Magic Numbers:** Use constants (e.g., `const decimal TestPrice = 99.99m`).
- **Realistic Data:** Use realistic product names, prices (avoid "test", "foo", "bar" unless demonstrating errors).

### Coverage Requirements
- **Minimum:** 80% line coverage for business logic.
- **Focus Areas:** All public methods, all business rules, all error paths.
- **Exclusions:** DTOs, Program.cs, auto-generated code.
- **Tool:** Run `dotnet test --collect:"XPlat Code Coverage"` to generate reports.

### Test Execution
- **CI/CD:** All tests must pass before merging.
- **Speed:** Unit tests should complete in < 1 second each.
- **Isolation:** Tests must not depend on execution order.
- **Cleanup:** Integration tests must clean up test data.

## 5. "Make it Work" Philosophy
- **Prioritise Functionality:** Focus on getting the happy path working first.
- **Refactor Later:** Do not over-engineer the initial extraction. It is acceptable to duplicate code (like EF Models) temporarily to break the dependency chain.

## 6. Error Handling & Resilience
All API endpoints must handle failures gracefully:

- **Database Unavailable:** Return `503 Service Unavailable` with retry-after header.
- **Payment Gateway Failure:** Return `502 Bad Gateway` and log the error.
- **Validation Errors:** Return `400 Bad Request` with clear error messages in the response body.
- **Unexpected Exceptions:** Catch at the top level, log with full stack trace, return `500 Internal Server Error` with a generic message (never expose internal details to clients).

### Example Pattern
```csharp
try
{
    // Business logic
}
catch (InvalidOperationException ex)
{
    _logger.LogWarning(ex, "Validation failed: {Message}", ex.Message);
    return BadRequest(new { error = ex.Message });
}
catch (Exception ex)
{
    _logger.LogError(ex, "Unhandled exception in CheckoutAsync");
    return StatusCode(500, new { error = "An error occurred processing your request" });
}
```

## 7. Tool Selection for Agents

- **Creating new projects:** Use `dotnet new webapi -n ProjectName` for consistency.
- **Adding project references:** Use `dotnet add reference` rather than manually editing `.csproj`.
- **File creation:** Use the `create_file` tool for new files, `replace_string_in_file` for edits.
- **Terminal commands:** Always use PowerShell syntax (e.g., backtick for line continuation).

## 8. Cloud-Native & Container Readiness
To prepare for the upcoming containerisation phase, all new services must adhere to these principles:

- **Configuration:**
  - Never hardcode secrets or connection strings.
  - Use `IConfiguration` for everything.
  - Ensure all settings can be overridden by Environment Variables (e.g., `ConnectionStrings__DefaultConnection`).
- **Logging:**
  - Do not log to local files. Containers are ephemeral, and local files will be lost.
  - Log to `Console` (stdout/stderr). This allows container orchestrators and tools like Application Insights to scrape logs easily.
- **Statelessness:**
  - The API must be stateless. Do not store session data in memory.
  - Any persistence must be in the external SQL database or a future distributed cache (Redis).
- **Health Checks:**
  - Every service must expose a lightweight `/health` endpoint.
  - This is critical for Kubernetes/Container Apps to know if the container is ready to receive traffic.
