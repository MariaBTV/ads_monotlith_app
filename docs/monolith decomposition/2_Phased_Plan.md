# Phased Decomposition Plan: Checkout Service

This plan outlines the steps to extract the Checkout logic using the Strangler Fig pattern.

## Technical Specifications

### Project Structure
```
RetailMonolith.sln
├── RetailMonolith/                          (Existing Monolith)
├── RetailMonolith.Checkout.Api/             (New API - Port 5100)
│   ├── Controllers/
│   │   └── CheckoutController.cs
│   ├── Program.cs
│   ├── appsettings.json
│   ├── Dockerfile
│   └── RetailMonolith.Checkout.Api.csproj
└── RetailMonolith.Checkout.Tests/           (New Test Project)
    └── CheckoutControllerTests.cs
```

### Port Assignments
- **Monolith:** `https://localhost:5001` / `http://localhost:5000`
- **Checkout API:** `https://localhost:5101` / `http://localhost:5100`

### Key Files to Reference
- Source Logic: `Services/CheckoutService .cs` (note the space in filename)
- Interface: `Services/ICheckoutService.cs`
- Payment Gateway: `Services/IPaymentGateway.cs`
- Models: `Models/Order.cs`, `Models/Cart.cs`, `Models/InventoryItem.cs`
- DbContext: `Data/AppDbContext.cs`

## Phase 1: Scaffold the New API
**Goal:** Create the shell for the new microservice.

### Agent Switch Prompt
> SWITCH AGENT NOW → Use **GPT-5.1 (Primary Build & Code Agent)**.
>
> PROMPT TO ISSUE TO GPT-5.1:
> "Apply the Standard Grounding Prompt. We are at Phase 1 (Scaffold). Confirm phase, list the exact file operations you will perform (project creation, Dockerfile, test project). Do NOT create business logic yet. Stop after meeting all acceptance criteria and produce a Phase 1 validation readiness summary."
>
> OPTIONAL SECONDARY (after validation passes): Use **Claude 4.5** with prompt:
> "Review Phase 1 output (project scaffolding, Dockerfile, health endpoint, initial test). Suggest any documentation or naming improvements ONLY—no code generation unless clearly missing an acceptance criterion."
>
> RATIONALE: GPT-5.1 excels at precise project setup; Claude 4.5 provides clarity and doc polish post-scaffold.

### Steps
1.  Create a new ASP.NET Core Web API project named `RetailMonolith.Checkout.Api` in the solution.
2.  **Container Readiness:** Ensure the project is created with Docker support (or add a `Dockerfile` immediately) to facilitate the next phase of containerisation.
3.  Add references to the shared `RetailMonolith` project (or extract shared models to a `RetailMonolith.Shared` library if circular dependencies arise). *Note: For speed, we may initially link files or reference the project directly.*
4.  Configure Swagger/OpenAPI support.

### Validation Checkpoint
Before proceeding to Phase 2, verify:
```powershell
# From solution root
dotnet build RetailMonolith.Checkout.Api
dotnet build RetailMonolith.Checkout.Tests
dotnet test RetailMonolith.Checkout.Tests
dotnet run --project RetailMonolith.Checkout.Api
# In another terminal:
curl https://localhost:5101/health
# Expected: 200 OK
```

### Acceptance Criteria
- [ ] Solution compiles with both projects.
- [ ] `RetailMonolith.Checkout.Api` starts on port 5100/5101.
- [ ] Health check endpoint `/health` returns 200 OK.
- [ ] A valid `Dockerfile` exists for the new API project.
- [ ] Unit test project `RetailMonolith.Checkout.Tests` is created and compiles.
- [ ] **Test:** Health check endpoint has a corresponding unit test that verifies 200 OK response.
- [ ] **Test:** Test project references xUnit (or NUnit) and has a sample test that passes.

---

## Phase 2: Migrate Business Logic
**Goal:** Move the core logic from `CheckoutService.cs` to the new API.
**Testing Strategy:** Test-Driven Development - Write tests FIRST, then implement.

### Agent Switch Prompt
> SWITCH AGENT NOW → Continue with **GPT-5.1 (TDD & Migration Agent)**.
>
> PROMPT TO ISSUE TO GPT-5.1:
> "Ground yourself (Standard Grounding Prompt). Begin Phase 2. Generate failing unit test stubs (list each). Confirm test names match plan. Then migrate logic from `Services/CheckoutService .cs` into `CheckoutController` using DTOs (request/response). Maintain >80% coverage. Provide interim coverage report and list of remaining failing tests until all green."
>
> AFTER ALL TESTS PASS & COVERAGE MET:
> Switch to **Claude 4.5** with prompt:
> "Audit migrated logic for readability, error handling alignment (coding standards), and DTO boundary correctness. Suggest minimal refactors without changing behaviours or reducing test coverage."
>
> OPTIONAL: Use **Gemini 3** for exploratory performance hints (e.g., batching queries, transaction scope suggestions) but do not implement until a future optimisation phase.
>
> RATIONALE: GPT-5.1 handles multi-step refactor + TDD reliably; Claude sharpens clarity; Gemini offers forward-looking optimisations.

### Steps
1.  **Write Unit Tests First:**
    - Create `CheckoutControllerTests.cs` with test stubs for all scenarios (see Testing Requirements below).
    - Tests should initially fail (Red phase of TDD).
2.  Copy `CheckoutService.cs` logic into a controller or handler in the new API (e.g., `CheckoutController`).
3.  **Configuration:** Ensure connection strings and settings are read from `IConfiguration` (not hardcoded) to support Environment Variable injection in containers.
4.  Register necessary dependencies (`AppDbContext`, `IPaymentGateway`) in the new API's `Program.cs`.
5.  **Health Checks:** Implement a standard `/health` endpoint to allow container orchestrators (Kubernetes/ACA) to monitor service uptime.
6.  Expose a `POST /api/checkout` endpoint that accepts `customerId` and `paymentToken`.
7.  **Implement Logic:** Make the tests pass (Green phase of TDD).
8.  **Refactor:** Clean up code while keeping tests green.

### Testing Requirements (Mandatory)

#### Unit Tests - `CheckoutControllerTests.cs`
1.  **Happy Path:**
    - `Checkout_WithValidCart_ReturnsOrderWithPaidStatus()`
    - `Checkout_WithMultipleItems_CalculatesTotalCorrectly()`
2.  **Validation Failures:**
    - `Checkout_WithEmptyCart_ReturnsBadRequest()`
    - `Checkout_WithInvalidPaymentToken_ReturnsBadRequest()`
3.  **Business Rule Failures:**
    - `Checkout_WithInsufficientStock_ReturnsConflictOrBadRequest()`
4.  **External Dependency Failures:**
    - `Checkout_WhenPaymentGatewayFails_ReturnsOrderWithFailedStatus()`
    - `Checkout_WhenDatabaseUnavailable_Returns503ServiceUnavailable()`

**Minimum Coverage:** 80% code coverage for the checkout logic.
**Mocking:** Use in-memory database (EF Core InMemory) or mock `DbContext`. Mock `IPaymentGateway`.

#### Integration Tests - `CheckoutIntegrationTests.cs`
1.  **End-to-End:**
    - `Checkout_FullFlow_CreatesOrderAndDecrementsInventory()`
    - Test against a real test database (LocalDB or SQL Server container).
    - Verify: Cart cleared, Inventory decremented, Order created.

### Validation Checkpoint
Before proceeding to Phase 3, verify:
```powershell
# Run ALL tests
dotnet test RetailMonolith.Checkout.Tests --logger "console;verbosity=detailed"
# Expected: All tests pass, minimum 80% coverage

# Start the API
dotnet run --project RetailMonolith.Checkout.Api
# Test endpoint (adjust port if needed)
curl -X POST https://localhost:5101/api/checkout `
  -H "Content-Type: application/json" `
  -d '{"customerId":"guest","paymentToken":"tok_test"}'
# Check database for new order
```

### Acceptance Criteria
- [ ] `POST /api/checkout` endpoint responds with correct status codes.
- [ ] `/health` endpoint returns 200 OK.
- [ ] Calling the endpoint creates an order in the database.
- [ ] **Unit Tests:** ALL 7+ unit tests pass (see Testing Requirements).
- [ ] **Integration Test:** At least 1 end-to-end integration test passes.
- [ ] **Coverage:** Code coverage report shows ≥80% for checkout logic.
- [ ] Logs appear in console (not files).

---

## Phase 3: Refactor Monolith to Proxy
**Goal:** Reroute Monolith traffic to the new API.
**Testing Strategy:** Write proxy tests before refactoring.

### Agent Switch Prompt
> SWITCH AGENT NOW → Use **GPT-5.1 (Proxy & Resilience Agent)**.
>
> PROMPT TO ISSUE TO GPT-5.1:
> "Ground (Standard Grounding Prompt). Begin Phase 3. Create proxy unit test stubs first. Refactor monolith `CheckoutService` to pure HTTP client logic calling the new API. Implement resilience (retry/circuit breaker) if time, gated behind config flag. Ensure all proxy tests + E2E pass. Provide summary showing business logic fully removed."
>
> AFTER SUCCESSFUL VALIDATION:
> Use **Claude 4.5** with prompt:
> "Review HTTP error mapping, exception messages, and resilience clarity. Suggest improvements to logging and failure semantics without altering working code paths."
>
> OPTIONAL: Use **Gemini 3** for alternative resilience strategies or latency measurement suggestions (do NOT implement now—record for backlog).
>
> RATIONALE: GPT-5.1 is precise at transforming service layers; Claude improves semantic clarity; Gemini proposes future enhancements.

### Steps
1.  **Write Tests First:** Create `CheckoutServiceProxyTests.cs` in the Monolith's test project (create if missing).
2.  Modify the existing `CheckoutService` in the Monolith.
3.  Replace the internal logic with an `HttpClient` call to `http://localhost:5100/api/checkout`.
4.  Map the API response back to the `Order` model expected by the Razor Page.
5.  **Implement Resilience:** Add retry logic (Polly) or circuit breaker for HTTP calls (optional but recommended).
6.  Make the tests pass.

### Testing Requirements (Mandatory)

#### Unit Tests - `CheckoutServiceProxyTests.cs` (in Monolith test project)
1.  **Success Scenarios:**
    - `CheckoutAsync_WhenApiReturns200_ReturnsOrderObject()`
    - `CheckoutAsync_WithValidResponse_MapsFieldsCorrectly()`
2.  **Failure Scenarios:**
    - `CheckoutAsync_WhenApiReturns400_ThrowsInvalidOperationException()`
    - `CheckoutAsync_WhenApiReturns500_ThrowsHttpRequestException()`
    - `CheckoutAsync_WhenApiUnavailable_ThrowsOrRetries()`
3.  **Timeout Scenarios:**
    - `CheckoutAsync_WhenApiTimesOut_ThrowsTimeoutException()`

**Mocking:** Use `HttpMessageHandler` mock or a library like `MockHttp` to simulate API responses.

#### Integration Tests - `CheckoutE2ETests.cs`
1.  **Full Stack Test:**
    - Start both services (Monolith + Checkout API).
    - Use `WebApplicationFactory<Program>` to test in-memory.
    - Simulate: POST to `/Checkout` page → verify redirect to `/Orders/Details`.

### Validation Checkpoint
Before proceeding to Phase 4, verify:
```powershell
# Run Monolith tests
dotnet test RetailMonolith.Tests --logger "console;verbosity=detailed"
# Expected: All proxy tests pass

# Start BOTH services
Start-Process -NoNewWindow -FilePath "dotnet" -ArgumentList "run","--project","RetailMonolith"
Start-Process -NoNewWindow -FilePath "dotnet" -ArgumentList "run","--project","RetailMonolith.Checkout.Api"
# Test full flow via browser
# Navigate to: https://localhost:5001/Products
# Add item, go to Cart, proceed to Checkout, submit
```

### Acceptance Criteria
- [ ] The Monolith's `CheckoutService` contains **no** business logic, only HTTP client code.
- [ ] The UI flow (Checkout Page → Submit) works without errors.
- [ ] Network traffic shows call from Monolith to `localhost:5100/api/checkout`.
- [ ] **Unit Tests:** ALL 6+ proxy unit tests pass.
- [ ] **Integration Test:** E2E test passes with both services running.
- [ ] Old `CheckoutService` logic is deleted (not commented out).

---

## Phase 4: Verification & Cleanup
**Goal:** Ensure stability and remove dead code.

### Agent Switch Prompt
> SWITCH AGENT NOW → Use **Claude 4.5 (Final Audit & Documentation Agent)**.
>
> PROMPT TO ISSUE TO CLAUDE 4.5:
> "Perform a final audit of decomposition: confirm acceptance criteria in Phases 1–3, identify any lingering direct checkout logic, verify British English in docs, and suggest minimal cleanup tasks. Do not propose new features."
>
> FOR TARGETED CODE DELETIONS / MINOR REFACTOR:
> Switch briefly to **GPT-5.1** with prompt:
> "Remove identified dead code and ensure build + tests remain green. Report deletions." 
>
> OPTIONAL: Use **Gemini 3** for suggestions on container start optimisation or future observability hooks—log as backlog items.
>
> RATIONALE: Claude excels at holistic review; GPT-5.1 executes precise surgical cleanup; Gemini informs future roadmap.

### Steps
1.  Run full end-to-end manual tests: Add item -> Checkout -> Verify Order in DB.
2.  **Container Verification:** Verify the new API can run on a different port and that logs are visible in the console (stdout).
3.  Remove the old `IPaymentGateway` and direct database checkout logic from the Monolith (if no longer used by other parts).

### Acceptance Criteria
- [ ] User can complete a purchase successfully.
- [ ] Order appears in the "Orders" list.
- [ ] API logs are visible in the console window.
- [ ] Codebase is clean of commented-out legacy logic.

---

## Testing Strategy

### Testing Philosophy
**Non-Negotiable:** No code is merged without passing tests. Decoupling introduces network boundaries, making comprehensive testing critical.

### Test Pyramid
1.  **Unit Tests (70%):** Fast, isolated, mock all dependencies.
2.  **Integration Tests (20%):** Test with real database, verify data changes.
3.  **End-to-End Tests (10%):** Full stack, both services running.

### Unit Tests (xUnit recommended)
- **Framework:** xUnit (preferred) or NUnit.
- **Mocking:** Moq or NSubstitute for interfaces, `HttpMessageHandler` for HTTP clients.
- **Coverage Tool:** `dotnet test --collect:"XPlat Code Coverage"` + Coverlet.
- **Target:** Minimum 80% code coverage for business logic.

#### Checkout API Tests (`RetailMonolith.Checkout.Tests`)
- Test all controller endpoints with mocked dependencies.
- Test business rules: insufficient stock, payment failures.
- Test validation: empty cart, invalid tokens.

#### Monolith Proxy Tests (`RetailMonolith.Tests`)
- Test HTTP client wrapper handles all response codes.
- Test timeout and retry scenarios.
- Test response mapping (API DTO → Domain Model).

### Integration Tests
- **Database:** Use SQL Server container or LocalDB test instance.
- **Scope:** Verify database transactions (cart cleared, inventory decremented, order created).
- **Cleanup:** Each test should reset database state (e.g., delete test data or use transactions).

### End-to-End Tests
- **Scope:** Start both services, simulate user journey.
- **Tool:** `WebApplicationFactory<Program>` for in-process testing, or Playwright for UI testing.
- **Minimum:** 1 happy path test (Add to Cart → Checkout → Order Created).

### Manual Verification (Before Phase Sign-off)
- **Scenario:** "Guest" user adds "Product A" to cart and checks out.
- **Expected:** Redirected to Order Details page; Order status is "Paid".
- **Verify:** Check database directly for order record and inventory decrement.
