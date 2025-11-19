# AI Copilot Implementation Plan
## Embed a Retail Chat Assistant Powered by Azure OpenAI

**Project Duration:** 6-8 hours  
**Target Completion:** 1-2 days (single developer)  
**Priority:** High  
**Status:** Planning Phase

---

## 📋 Project Overview

Build an intelligent chat assistant that:
- ✅ Recommends products based on natural language queries
- ✅ Understands user intent (budget, category, preferences)
- ✅ Can add products directly to cart
- ✅ Provides conversational product discovery
- ✅ Uses RAG (Retrieval-Augmented Generation) with product catalog

---

## 🎯 Phase 1: Azure OpenAI Setup
**Duration:** 30 minutes  
**Priority:** Critical  
**Status:** ⏳ Not Started

### Tasks

#### 1.1 Create Azure OpenAI Resource
- [ ] Navigate to Azure Portal
- [ ] Create new Azure OpenAI service
- [ ] Select region (East US, West Europe, etc.)
- [ ] Choose pricing tier (Standard)
- [ ] Note the resource name
- **Estimated Time:** 10 minutes
- **Acceptance Criteria:**
  - ✓ Azure OpenAI resource is created and shows "Succeeded" status
  - ✓ Resource is accessible in Azure Portal
  - ✓ Resource name is documented
  - ✓ Region supports required model (gpt-4o or gpt-35-turbo)

#### 1.2 Deploy GPT Model
- [ ] Open Azure OpenAI Studio
- [ ] Navigate to Deployments section
- [ ] Create new deployment
- [ ] Select model: `gpt-4o` (recommended) or `gpt-35-turbo` (cost-effective)
- [ ] Name the deployment (e.g., "chat-model")
- [ ] Configure token limits
- **Estimated Time:** 5 minutes
- **Acceptance Criteria:**
  - ✓ Model deployment shows "Succeeded" status
  - ✓ Deployment name is recorded
  - ✓ Model version is documented
  - ✓ Deployment is accessible via API
  - ✓ Token limits are configured (e.g., 800 max tokens)

#### 1.3 Get Credentials
- [ ] Navigate to Keys and Endpoint section
- [ ] Copy Endpoint URL
- [ ] Copy API Key (Key 1)
- [ ] Copy Deployment Name
- **Estimated Time:** 5 minutes
- **Acceptance Criteria:**
  - ✓ Endpoint URL is in correct format (https://*.openai.azure.com/)
  - ✓ API Key is securely stored (not in plain text)
  - ✓ Deployment name matches created deployment
  - ✓ Credentials are accessible by development team
  - ✓ Backup key (Key 2) location is documented

#### 1.4 Configure Application Settings
- [ ] Add configuration to `appsettings.json`
- [ ] Set up user secrets for development: `dotnet user-secrets set "AzureOpenAI:ApiKey" "your-key"`
- [ ] Verify configuration loads correctly
- **Estimated Time:** 10 minutes
- **Acceptance Criteria:**
  - ✓ `appsettings.json` contains AzureOpenAI section with placeholder values
  - ✓ User secrets are initialized and contain actual API key
  - ✓ No API keys are committed to source control
  - ✓ Configuration can be read in `Program.cs` without errors
  - ✓ Development and production configurations are separate

**Configuration Structure:**
```json
{
  "AzureOpenAI": {
    "Endpoint": "https://your-resource.openai.azure.com/",
    "ApiKey": "your-api-key-here",
    "DeploymentName": "gpt-4o",
    "MaxTokens": 800,
    "Temperature": 0.7
  }
}
```

---

## 🔧 Phase 2: Backend Service Layer
**Duration:** 3-4 hours  
**Priority:** Critical  
**Status:** ⏳ Not Started

### Tasks

#### 2.1 Install NuGet Packages
- [ ] Install `Azure.AI.OpenAI` (version 2.1.0)
- [ ] Install `System.Text.Json` (version 9.0.0) if not present
- [ ] Restore packages
- **Command:** `dotnet add package Azure.AI.OpenAI --version 2.1.0`
- **Estimated Time:** 5 minutes
- **Acceptance Criteria:**
  - ✓ Package references appear in `RetailMonolith.csproj`
  - ✓ `dotnet restore` completes without errors
  - ✓ No version conflicts with existing packages
  - ✓ Solution builds successfully
  - ✓ Package versions are compatible with .NET 9.0

#### 2.2 Create Data Models
**Location:** `Models/`

##### Create `Models/ChatMessage.cs`
- [ ] Define `ChatMessage` class
  - Properties: `Id`, `SessionId`, `Role`, `Content`, `CreatedUtc`
  - Add data annotations
- **Estimated Time:** 15 minutes
- **Acceptance Criteria:**
  - ✓ Class compiles without errors
  - ✓ All properties have appropriate data types
  - ✓ `SessionId` has default value (new Guid)
  - ✓ `Role` has default value ("user")
  - ✓ `CreatedUtc` has default value (DateTime.UtcNow)
  - ✓ Required properties have validation attributes
  - ✓ String properties have MaxLength attributes

##### Create `Models/ChatRequest.cs`
- [ ] Define `ChatRequest` class
  - Properties: `Message`, `SessionId`, `CustomerId`
  - Add validation attributes
- **Estimated Time:** 10 minutes
- **Acceptance Criteria:**
  - ✓ Class compiles without errors
  - ✓ `Message` property has [Required] attribute
  - ✓ `Message` has [MaxLength(500)] constraint
  - ✓ `SessionId` has default empty string value
  - ✓ `CustomerId` has default value "guest"
  - ✓ Properties use appropriate nullability annotations

##### Create `Models/ChatResponse.cs`
- [ ] Define `ChatResponse` class
  - Properties: `Message`, `Recommendations`, `SessionId`
- **Estimated Time:** 5 minutes
- **Acceptance Criteria:**
  - ✓ Class compiles without errors
  - ✓ `Message` property is non-nullable string
  - ✓ `Recommendations` is nullable List<ProductRecommendation>
  - ✓ `SessionId` is non-nullable string
  - ✓ Class can be serialized to JSON
  - ✓ Properties have XML documentation comments

##### Create `Models/ProductRecommendation.cs`
- [ ] Define `ProductRecommendation` class
  - Properties: `ProductId`, `Sku`, `Name`, `Price`, `Currency`, `Category`, `Reason`
- **Estimated Time:** 10 minutes
- **Acceptance Criteria:**
  - ✓ Class compiles without errors
  - ✓ `ProductId` is int type
  - ✓ `Price` is decimal type
  - ✓ `Currency` has default value "GBP"
  - ✓ All string properties are non-nullable with default empty string
  - ✓ `Reason` property can contain AI explanation (max 500 chars)
  - ✓ Class matches Product model structure

#### 2.3 Create Service Interface
**File:** `Services/IChatService.cs`

- [ ] Define `IChatService` interface
- [ ] Add method: `Task<ChatResponse> GetResponseAsync(ChatRequest request, CancellationToken ct)`
- [ ] Add method: `Task<List<ChatMessage>> GetChatHistoryAsync(string sessionId, CancellationToken ct)`
- [ ] Add method: `Task ClearSessionAsync(string sessionId, CancellationToken ct)`
- **Estimated Time:** 10 minutes
- **Acceptance Criteria:**
  - ✓ Interface compiles without errors
  - ✓ All methods return Task for async operations
  - ✓ CancellationToken has default parameter value
  - ✓ Method signatures match expected usage
  - ✓ XML documentation comments describe each method
  - ✓ Interface follows naming conventions (I prefix)

#### 2.4 Implement Chat Service
**File:** `Services/ChatService.cs`

##### Core Structure Setup
- [ ] Create `ChatService` class implementing `IChatService`
- [ ] Add constructor with dependencies:
  - `AppDbContext`
  - `IConfiguration`
  - `HttpClient`
  - `ILogger<ChatService>`
- [ ] Add private fields for conversation history (Dictionary)
- **Estimated Time:** 30 minutes
- **Acceptance Criteria:**
  - ✓ Class implements all IChatService methods
  - ✓ Constructor uses dependency injection
  - ✓ All dependencies are readonly fields
  - ✓ Conversation history uses thread-safe ConcurrentDictionary
  - ✓ Class compiles without errors
  - ✓ Azure OpenAI configuration is read from IConfiguration
  - ✓ Logger is configured for all log levels

##### Implement RAG - Product Retrieval
- [ ] Create `QueryRelevantProducts` method
- [ ] Implement keyword extraction from user message
- [ ] Implement budget extraction (regex for prices)
- [ ] Implement category detection (match against known categories)
- [ ] Query database with filters:
  - Active products only
  - Category filter
  - Price range filter
  - Keyword matching in name/description
- [ ] Return top 10 relevant products
- **Estimated Time:** 45 minutes
- **Acceptance Criteria:**
  - ✓ Method returns List<Product>
  - ✓ Keyword extraction handles common words (the, and, etc.)
  - ✓ Budget extraction recognizes £30, $50, under 100 formats
  - ✓ Category matching is case-insensitive
  - ✓ Only active products are returned
  - ✓ Results are limited to 10 products maximum
  - ✓ Empty query returns popular/featured products
  - ✓ Database query is efficient (uses indexes)
  - ✓ Method handles null/empty input gracefully

##### Implement Prompt Engineering
- [ ] Create `BuildSystemPrompt` method
- [ ] Design system prompt template:
  - Role definition
  - Available products context
  - Guidelines for recommendations
  - Format instructions (include SKU markers)
- [ ] Inject product data dynamically
- [ ] Add user message context
- **Estimated Time:** 30 minutes
- **Acceptance Criteria:**
  - ✓ System prompt is clear and comprehensive
  - ✓ Product data includes: SKU, Name, Price, Category
  - ✓ Prompt instructs AI to use [SKU-XXXX] format
  - ✓ Guidelines limit recommendations to 2-4 products
  - ✓ Prompt encourages conversational tone
  - ✓ Budget constraints are mentioned in prompt
  - ✓ Prompt stays under token limits (< 2000 tokens)
  - ✓ Dynamic data injection is safe (no injection attacks)

**System Prompt Template:**
```text
You are a helpful retail assistant for an online store. Your role is to recommend products based on customer needs.

Available Products:
{PRODUCT_LIST}

Guidelines:
- Be conversational and friendly
- Ask clarifying questions if intent is unclear
- Recommend 2-4 products maximum per response
- Include product SKU in format [SKU-XXXX] when recommending
- Consider budget constraints mentioned by customer
- Explain why you're recommending specific products

Current customer query: {USER_MESSAGE}
```

##### Implement Azure OpenAI Integration
- [ ] Create `CallAzureOpenAI` method
- [ ] Use `Azure.AI.OpenAI` SDK
- [ ] Build chat completion request
- [ ] Configure parameters (temperature, max tokens)
- [ ] Handle API errors and retries
- [ ] Log API calls for debugging
- **Estimated Time:** 40 minutes
- **Acceptance Criteria:**
  - ✓ Method successfully calls Azure OpenAI API
  - ✓ Temperature is set to 0.7 (configurable)
  - ✓ Max tokens is set to 800 (configurable)
  - ✓ API errors return meaningful error messages
  - ✓ Timeout is configured (30 seconds)
  - ✓ Retry logic handles transient failures (3 retries)
  - ✓ API calls are logged with request/response size
  - ✓ Sensitive data (API key) is not logged
  - ✓ CancellationToken is respected

##### Implement Response Parsing
- [ ] Create `ParseRecommendations` method
- [ ] Extract product SKUs from AI response using regex
- [ ] Match SKUs to database products
- [ ] Create `ProductRecommendation` objects
- [ ] Add recommendation reasons from AI response
- **Estimated Time:** 30 minutes
- **Acceptance Criteria:**
  - ✓ Regex pattern correctly extracts [SKU-XXXX] format
  - ✓ Method handles responses with no recommendations
  - ✓ Invalid SKUs are ignored gracefully
  - ✓ Database lookup is efficient (single query)
  - ✓ Reason text is extracted and trimmed
  - ✓ ProductRecommendation objects have all fields populated
  - ✓ Method returns empty list if no matches found
  - ✓ Duplicate SKUs are handled (deduplicated)

##### Orchestrate Main Flow
- [ ] Implement `GetResponseAsync` method
- [ ] Coordinate all helper methods:
  1. Validate input
  2. Query relevant products
  3. Build system prompt
  4. Call Azure OpenAI
  5. Parse recommendations
  6. Save to conversation history
  7. Return `ChatResponse`
- [ ] Add error handling and logging
- **Estimated Time:** 10 minutes
- **Acceptance Criteria:**
  - ✓ Method executes all steps in correct order
  - ✓ Input validation throws ArgumentException for invalid data
  - ✓ All exceptions are caught and logged
  - ✓ User-friendly error messages are returned
  - ✓ Conversation history is updated correctly
  - ✓ ChatResponse contains AI message and recommendations
  - ✓ Method completes within 5 seconds (typical)
  - ✓ CancellationToken is honored at each step
  - ✓ SessionId is persisted in response

#### 2.5 Register Services
**File:** `Program.cs`

- [ ] Register `IChatService` as scoped service
- [ ] Add HttpClient for ChatService
- [ ] Configure antiforgery for AJAX calls
- **Estimated Time:** 5 minutes
- **Acceptance Criteria:**
  - ✓ Service is registered with correct lifetime (Scoped)
  - ✓ HttpClient is configured with timeout and retry policies
  - ✓ Antiforgery token header name is set correctly
  - ✓ Application builds successfully
  - ✓ Service can be resolved from DI container
  - ✓ No circular dependencies detected

---

## 🎨 Phase 3: Frontend - Razor Page & UI
**Duration:** 2-3 hours  
**Priority:** High  
**Status:** ⏳ Not Started

### Tasks

#### 3.1 Create Page Structure
- [ ] Create `Pages/Chat/` folder
- [ ] Create `Pages/Chat/Index.cshtml`
- [ ] Create `Pages/Chat/Index.cshtml.cs`
- **Estimated Time:** 2 minutes
- **Acceptance Criteria:**
  - ✓ Folder structure exists under Pages directory
  - ✓ Files are created with correct naming convention
  - ✓ cshtml file has @page directive
  - ✓ cshtml.cs file has correct namespace
  - ✓ Files are included in project

#### 3.2 Implement Page Model
**File:** `Pages/Chat/Index.cshtml.cs`

- [ ] Create `IndexModel` class
- [ ] Inject `IChatService` and `ICartService`
- [ ] Add properties:
  - `[BindProperty] SessionId`
  - `ChatHistory` list
- [ ] Implement `OnGetAsync` (initialize session)
- [ ] Implement `OnPostSendMessageAsync` (AJAX handler)
  - Accept JSON body
  - Call chat service
  - Return JSON response
- [ ] Implement `OnPostAddToCartAsync` (AJAX handler)
  - Accept product ID
  - Add to cart
  - Return success response
- **Estimated Time:** 30 minutes
- **Acceptance Criteria:**
  - ✓ IndexModel inherits from PageModel
  - ✓ Services are injected via constructor
  - ✓ SessionId generates new GUID on first load
  - ✓ OnGetAsync returns Page() result
  - ✓ OnPostSendMessageAsync accepts [FromBody] ChatRequest
  - ✓ OnPostSendMessageAsync returns JsonResult
  - ✓ OnPostAddToCartAsync validates product exists
  - ✓ All handlers have try-catch error handling
  - ✓ Handlers return appropriate HTTP status codes
  - ✓ Model validation is performed

#### 3.3 Create Chat UI
**File:** `Pages/Chat/Index.cshtml`

##### HTML Structure
- [ ] Add page model directive
- [ ] Create chat container (Bootstrap)
- [ ] Create message display area (scrollable)
- [ ] Create message input section
  - Text input
  - Send button
  - Loading spinner
- [ ] Add product recommendation card template
- [ ] Add empty state message
- **Estimated Time:** 45 minutes
- **Acceptance Criteria:**
  - ✓ Page uses correct @page and @model directives
  - ✓ Layout is responsive (mobile and desktop)
  - ✓ Chat container has fixed height with scroll
  - ✓ Input is at bottom and stays visible
  - ✓ Send button is visually distinct
  - ✓ Loading spinner appears during API calls
  - ✓ Product cards display image, name, price, button
  - ✓ Empty state shows friendly welcome message
  - ✓ HTML validates without errors
  - ✓ Accessibility attributes are present (ARIA labels)

**Key Components:**
```html
<div class="chat-container">
  <div id="chatMessages" class="chat-messages"></div>
  <div class="product-recommendations" id="productRecommendations"></div>
  <div class="chat-input">
    <input type="text" id="messageInput" placeholder="Ask me about products..." />
    <button id="sendButton">Send</button>
  </div>
</div>
```

#### 3.4 Create JavaScript Interactions
**File:** `wwwroot/js/chat.js`

##### Core Functionality
- [ ] Initialize on page load
- [ ] Implement `sendMessage` function
  - Get message from input
  - Validate not empty
  - Show loading state
  - AJAX POST to `/Chat/Index?handler=SendMessage`
  - Handle response
  - Display user message
  - Display AI response
  - Display product recommendations
- [ ] Implement `displayMessage` function
  - Create message bubble
  - Apply styling (user vs assistant)
  - Append to chat container
  - Auto-scroll to bottom
- [ ] Implement `displayRecommendations` function
  - Clear previous recommendations
  - Create product cards
  - Add "Add to Cart" buttons
  - Attach event handlers
- [ ] Implement `addToCart` function
  - AJAX POST to `/Chat/Index?handler=AddToCart`
  - Show success notification
  - Update cart badge
- [ ] Add Enter key support for sending messages
- [ ] Add error handling for failed requests
- **Estimated Time:** 60 minutes
- **Acceptance Criteria:**
  - ✓ JavaScript file loads without errors
  - ✓ Send button click triggers sendMessage
  - ✓ Empty messages are prevented
  - ✓ Loading spinner shows/hides correctly
  - ✓ AJAX requests include antiforgery token
  - ✓ User messages appear immediately
  - ✓ AI responses render with proper formatting
  - ✓ Product cards render with all data
  - ✓ Add to cart button works for each product
  - ✓ Enter key sends message (not Shift+Enter)
  - ✓ Chat auto-scrolls to latest message
  - ✓ Error messages display to user
  - ✓ Network failures are handled gracefully
  - ✓ No console errors during operation

#### 3.5 Create CSS Styling
**File:** `wwwroot/css/chat.css`

- [ ] Style chat container (full height, flex layout)
- [ ] Style message bubbles
  - User messages: right-aligned, blue
  - AI messages: left-aligned, gray
  - Rounded corners, padding
- [ ] Style product cards
  - Grid/flex layout
  - Image, title, price, button
  - Hover effects
- [ ] Style input section (sticky bottom)
- [ ] Add animations (fade-in for messages)
- [ ] Make responsive (mobile-friendly)
- **Estimated Time:** 30 minutes
- **Acceptance Criteria:**
  - ✓ Chat container fills available height
  - ✓ Message bubbles have distinct styling (user vs AI)
  - ✓ User messages are blue (#007bff) and right-aligned
  - ✓ AI messages are gray (#6c757d) and left-aligned
  - ✓ Bubbles have border-radius of 15px
  - ✓ Product cards display in grid (2-3 columns)
  - ✓ Cards have hover effect (shadow/scale)
  - ✓ Input section stays at bottom (position: sticky)
  - ✓ Fade-in animation duration is 300ms
  - ✓ Mobile layout stacks cards vertically
  - ✓ Text is readable on all backgrounds
  - ✓ No horizontal scrolling on mobile

#### 3.6 Update Navigation
**File:** `Pages/Shared/_Layout.cshtml`

- [ ] Add "Chat Assistant" link to navbar
- [ ] Add icon (🤖 or chat bubble)
- [ ] Position in appropriate menu location
- **Estimated Time:** 15 minutes
- **Acceptance Criteria:**
  - ✓ Chat link appears in main navigation
  - ✓ Link uses correct route (/Chat)
  - ✓ Icon is visible and recognizable
  - ✓ Link is styled consistently with other nav items
  - ✓ Active state highlights when on chat page
  - ✓ Link is accessible on mobile (hamburger menu)
  - ✓ Text is clear ("Chat Assistant" or "AI Chat")

---

## 🔗 Phase 4: Integration & Configuration
**Duration:** 1 hour  
**Priority:** High  
**Status:** ⏳ Not Started

### Tasks

#### 4.1 Update Program.cs
- [ ] Register ChatService with DI container
- [ ] Add HttpClient configuration
- [ ] Configure CORS if needed
- [ ] Add antiforgery token configuration
- **Estimated Time:** 10 minutes
- **Acceptance Criteria:**
  - ✓ ChatService is registered correctly (AddScoped)
  - ✓ HttpClient has 30-second timeout
  - ✓ HttpClient has retry policy (3 attempts)
  - ✓ CORS allows necessary origins (if required)
  - ✓ Antiforgery HeaderName is "X-CSRF-TOKEN"
  - ✓ Application builds without errors
  - ✓ Service can be resolved at runtime

```csharp
builder.Services.AddHttpClient<IChatService, ChatService>();
builder.Services.AddScoped<IChatService, ChatService>();
```

#### 4.2 Update Configuration Files
- [ ] Add Azure OpenAI section to `appsettings.json`
- [ ] Add configuration to `appsettings.Development.json`
- [ ] Set up user secrets for API key
- [ ] Document configuration in README
- **Estimated Time:** 15 minutes
- **Acceptance Criteria:**
  - ✓ appsettings.json has AzureOpenAI section with placeholders
  - ✓ Development config overrides for local testing
  - ✓ User secrets initialized (dotnet user-secrets init)
  - ✓ API key stored in user secrets
  - ✓ No secrets in appsettings.json
  - ✓ README documents configuration steps
  - ✓ README includes example configuration structure
  - ✓ Configuration loads successfully at runtime

#### 4.3 Database Migration (Optional)
- [ ] Add `ChatMessages` DbSet to `AppDbContext` (if persisting history)
- [ ] Create migration: `dotnet ef migrations add AddChatMessages`
- [ ] Update database: `dotnet ef database update`
- **Estimated Time:** 15 minutes
- **Note:** Can be skipped if using in-memory conversation history
- **Acceptance Criteria:**
  - ✓ DbSet<ChatMessage> added to AppDbContext
  - ✓ Migration file created successfully
  - ✓ Migration includes ChatMessages table
  - ✓ Table has appropriate indexes (SessionId)
  - ✓ Database updates without errors
  - ✓ Table schema matches ChatMessage model
  - ✓ Rollback script tested

#### 4.4 Test Integration
- [ ] Run application
- [ ] Test chat page loads
- [ ] Test sending a message
- [ ] Verify Azure OpenAI connection
- [ ] Test product recommendations appear
- [ ] Test add to cart functionality
- **Estimated Time:** 20 minutes
- **Acceptance Criteria:**
  - ✓ Application starts without errors
  - ✓ Chat page accessible at /Chat
  - ✓ Page loads within 2 seconds
  - ✓ Sending "Hello" returns AI greeting
  - ✓ Azure OpenAI logs show successful API call
  - ✓ Product query "show me electronics" returns recommendations
  - ✓ Recommendation cards display correctly
  - ✓ Add to cart button adds product to cart
  - ✓ Cart count updates after adding product
  - ✓ No errors in browser console
  - ✓ No errors in application logs

---

## 🧪 Phase 5: Testing
**Duration:** 2-3 hours  
**Priority:** Medium  
**Status:** ⏳ Not Started

### Unit Tests

#### 5.1 Setup Test Project
- [ ] Create test project: `RetailMonolith.Tests`
- [ ] Install NuGet packages:
  - `xUnit` (2.9.0)
  - `Moq` (4.20.72)
  - `Microsoft.EntityFrameworkCore.InMemory` (9.0.0)
- [ ] Create test folder structure
- **Estimated Time:** 15 minutes
- **Acceptance Criteria:**
  - ✓ Test project created with correct SDK (Microsoft.NET.Sdk)
  - ✓ All packages installed without conflicts
  - ✓ Project references main RetailMonolith project
  - ✓ Test project targets .NET 9.0
  - ✓ Folder structure mirrors main project (Services/, Models/)
  - ✓ `dotnet test` command recognizes project
  - ✓ Test explorer discovers test project

#### 5.2 ChatService Unit Tests
**File:** `Tests/Services/ChatServiceTests.cs`

- [ ] Test `GetResponseAsync` with valid message returns response
- [ ] Test `GetResponseAsync` with empty message throws validation exception
- [ ] Test `QueryRelevantProducts` with category filter returns filtered products
- [ ] Test `QueryRelevantProducts` with budget constraint returns products under budget
- [ ] Test `BuildSystemPrompt` includes product data
- [ ] Test `ParseRecommendations` extracts SKUs correctly
- [ ] Test `ParseRecommendations` handles no recommendations
- [ ] Test error handling when Azure OpenAI fails
- **Estimated Time:** 1.5 hours
- **Acceptance Criteria:**
  - ✓ All tests pass (green)
  - ✓ Test coverage > 80% for ChatService
  - ✓ Tests use in-memory database
  - ✓ Mocks are used for external dependencies
  - ✓ Each test follows Arrange-Act-Assert pattern
  - ✓ Test names clearly describe what is tested
  - ✓ Edge cases are covered (null, empty, invalid)
  - ✓ Async tests use proper async/await
  - ✓ Tests are isolated (no shared state)
  - ✓ Tests run quickly (< 5 seconds total)

#### 5.3 Model Unit Tests
**File:** `Tests/Models/ChatMessageTests.cs`

- [ ] Test `ChatMessage` default values are set correctly
- [ ] Test `SessionId` generation is unique
- [ ] Test `ProductRecommendation` properties
- **Estimated Time:** 15 minutes
- **Acceptance Criteria:**
  - ✓ All model tests pass
  - ✓ Default values are verified
  - ✓ SessionId uniqueness is confirmed
  - ✓ Property types are validated
  - ✓ Tests are simple and focused
  - ✓ No external dependencies required

### Integration Tests

#### 5.4 Setup Integration Tests
- [ ] Create test project: `RetailMonolith.IntegrationTests`
- [ ] Install NuGet packages:
  - `xUnit`
  - `Microsoft.AspNetCore.Mvc.Testing` (9.0.0)
- [ ] Create `WebApplicationFactory` setup
- **Estimated Time:** 20 minutes
- **Acceptance Criteria:**
  - ✓ Integration test project created
  - ✓ WebApplicationFactory configured correctly
  - ✓ Test host uses in-memory database
  - ✓ Azure OpenAI calls can be mocked
  - ✓ Test server starts successfully
  - ✓ HTTP client can make requests
  - ✓ Projects references are correct

#### 5.5 Chat Endpoint Integration Tests
**File:** `Tests/Integration/ChatEndpointTests.cs`

- [ ] Test POST `/Chat/Index?handler=SendMessage` returns 200 OK
- [ ] Test chat response contains AI message
- [ ] Test product recommendations are returned
- [ ] Test add to cart endpoint
- [ ] Test invalid requests return appropriate errors
- [ ] Test session persistence
- **Estimated Time:** 45 minutes
- **Acceptance Criteria:**
  - ✓ All integration tests pass
  - ✓ Tests use actual HTTP requests
  - ✓ Response status codes are verified
  - ✓ Response content is validated (JSON structure)
  - ✓ Error scenarios return 400/500 as appropriate
  - ✓ Tests clean up data after execution
  - ✓ Tests can run in parallel
  - ✓ Test execution time is reasonable (< 30 seconds)
  - ✓ Tests work with mocked Azure OpenAI

### Manual Testing Scenarios

#### 5.6 Functional Testing
- [ ] **Scenario 1:** Basic conversation flow
  - User: "Hi, I need help finding products"
  - Expected: Friendly greeting, ask about preferences
  - **Acceptance:** AI responds within 3 seconds with conversational tone
- [ ] **Scenario 2:** Category-based search
  - User: "Show me electronics"
  - Expected: List of electronics products with prices
  - **Acceptance:** Returns 2-4 electronics products with correct prices
- [ ] **Scenario 3:** Budget filtering
  - User: "Show me footwear under £30"
  - Expected: Only footwear products below £30
  - **Acceptance:** All recommended products are footwear AND price < £30
- [ ] **Scenario 4:** Add to cart from recommendations
  - User receives recommendations
  - User clicks "Add to Cart"
  - Expected: Product added, confirmation shown
  - **Acceptance:** Cart badge increments, success toast appears, no errors
- [ ] **Scenario 5:** Multi-turn conversation
  - User: "Show me apparel"
  - AI shows apparel
  - User: "What about something cheaper?"
  - Expected: AI remembers context, shows cheaper options
  - **Acceptance:** Second response refers to apparel, prices are lower
- [ ] **Scenario 6:** Error handling
  - Simulate Azure OpenAI unavailable
  - Expected: Graceful error message
  - **Acceptance:** User sees friendly error, no stack trace, can retry
- [ ] **Scenario 7:** Empty/invalid input
  - User submits empty message
  - Expected: Validation message
  - **Acceptance:** Send button disabled OR validation message shown
- [ ] **Scenario 8:** Mobile responsiveness
  - Test on mobile viewport (375px width)
  - Expected: UI adapts correctly
  - **Acceptance:** Chat readable, input usable, no horizontal scroll
- **Estimated Time:** 30 minutes

---

## 📦 Deliverables Checklist

### Code Files
- [ ] `Models/ChatMessage.cs`
- [ ] `Models/ChatRequest.cs`
- [ ] `Models/ChatResponse.cs`
- [ ] `Models/ProductRecommendation.cs`
- [ ] `Services/IChatService.cs`
- [ ] `Services/ChatService.cs`
- [ ] `Pages/Chat/Index.cshtml`
- [ ] `Pages/Chat/Index.cshtml.cs`
- [ ] `wwwroot/js/chat.js`
- [ ] `wwwroot/css/chat.css`

### Updated Files
- [ ] `Program.cs` (service registration)
- [ ] `appsettings.json` (Azure OpenAI config)
- [ ] `appsettings.Development.json` (dev config)
- [ ] `Pages/Shared/_Layout.cshtml` (navigation)
- [ ] `RetailMonolith.csproj` (NuGet packages)

### Test Files
- [ ] `Tests/Services/ChatServiceTests.cs`
- [ ] `Tests/Models/ChatMessageTests.cs`
- [ ] `Tests/Integration/ChatEndpointTests.cs`

### Documentation
- [ ] Configuration instructions
- [ ] API key setup guide
- [ ] User guide for chat feature
- [ ] Developer notes

---

## 🎯 Success Criteria

### Functional Requirements
- ✅ User can access chat interface from navigation
- ✅ User can send messages to AI assistant
- ✅ AI responds within 3 seconds
- ✅ Product recommendations are relevant to user query
- ✅ User can add recommended products to cart
- ✅ Chat interface is responsive and user-friendly
- ✅ Conversation maintains context across messages

### Technical Requirements
- ✅ Azure OpenAI integration working
- ✅ RAG implementation retrieves relevant products
- ✅ Error handling implemented (API failures, timeouts)
- ✅ Logging configured for debugging
- ✅ Unit test coverage > 80%
- ✅ All integration tests passing
- ✅ No console errors in browser
- ✅ API costs stay under budget ($10/month)

### Quality Requirements
- ✅ Code follows project conventions
- ✅ No security vulnerabilities (API key protected)
- ✅ UI/UX is intuitive and polished
- ✅ Performance meets targets (< 3s response time)
- ✅ Mobile responsive design

---

## 🚀 Implementation Timeline

| Phase | Duration | Start | End |
|-------|----------|-------|-----|
| **Phase 1:** Azure OpenAI Setup | 30 min | Day 1, 9:00 AM | Day 1, 9:30 AM |
| **Phase 2:** Backend Service Layer | 4 hours | Day 1, 9:30 AM | Day 1, 1:30 PM |
| **Break** | 30 min | Day 1, 1:30 PM | Day 1, 2:00 PM |
| **Phase 3:** Frontend UI | 3 hours | Day 1, 2:00 PM | Day 1, 5:00 PM |
| **Phase 4:** Integration | 1 hour | Day 2, 9:00 AM | Day 2, 10:00 AM |
| **Phase 5:** Testing | 3 hours | Day 2, 10:00 AM | Day 2, 1:00 PM |
| **Buffer/Polish** | 1 hour | Day 2, 1:00 PM | Day 2, 2:00 PM |

**Total Estimated Time:** 8 hours over 2 days

---

## 💰 Cost Estimation

### Azure OpenAI Pricing (GPT-4o)
- **Input tokens:** $0.005 per 1K tokens
- **Output tokens:** $0.015 per 1K tokens
- **Average conversation:**
  - Input: ~500 tokens (system prompt + user message)
  - Output: ~300 tokens (AI response)
  - Cost per conversation: ~$0.0075

### Monthly Projections
- **100 conversations/month:** $0.75
- **1,000 conversations/month:** $7.50
- **10,000 conversations/month:** $75.00

### Cost Optimization Tips
- Start with GPT-3.5-Turbo ($0.0015 input, $0.002 output) - 10x cheaper
- Cache system prompts where possible
- Set token limits (MaxTokens: 800)
- Implement rate limiting per user
- Monitor usage in Azure Portal

---

## 🔒 Security Considerations

### API Key Management
- [ ] Store API key in Azure Key Vault (production)
- [ ] Use User Secrets for development
- [ ] Never commit keys to source control
- [ ] Rotate keys regularly
- [ ] Implement key expiration monitoring

### Input Validation
- [ ] Sanitize user messages
- [ ] Implement message length limits (max 500 chars)
- [ ] Prevent prompt injection attacks
- [ ] Validate session IDs
- [ ] Rate limit requests per user/IP

### Data Privacy
- [ ] Don't send PII to Azure OpenAI
- [ ] Implement conversation data retention policy
- [ ] Add user consent for data processing
- [ ] Log minimal information
- [ ] Comply with GDPR/privacy regulations

### Application Security
- [ ] Use HTTPS only
- [ ] Implement CSRF protection
- [ ] Validate all AJAX requests
- [ ] Sanitize HTML in chat display
- [ ] Add authentication (Phase 2: Entra ID integration)

---

## 🐛 Known Issues & Future Enhancements

### Known Limitations
- In-memory conversation history (lost on restart)
- No authentication (guest users only)
- No conversation export
- Limited to text-only interactions
- No multi-language support

### Future Enhancements (Phase 6+)
- [ ] **Streaming Responses:** Real-time typing indicator
- [ ] **Voice Input:** Speech-to-text integration
- [ ] **Image Support:** Product image recognition
- [ ] **Conversation Persistence:** Database storage
- [ ] **User Accounts:** Link to Entra ID authentication
- [ ] **Analytics Dashboard:** Track popular queries
- [ ] **A/B Testing:** Test different prompts
- [ ] **Feedback Loop:** Rate responses
- [ ] **Multi-language:** Support multiple languages
- [ ] **Semantic Search Integration:** Phase 3 requirement
- [ ] **Context Awareness:** Remember past purchases
- [ ] **Proactive Recommendations:** Suggest based on browse history

---

## 📚 References & Resources

### Azure OpenAI Documentation
- [Azure OpenAI Service Documentation](https://learn.microsoft.com/azure/ai-services/openai/)
- [Quickstart: Chat with Azure OpenAI](https://learn.microsoft.com/azure/ai-services/openai/chatgpt-quickstart)
- [Azure OpenAI SDK for .NET](https://github.com/Azure/azure-sdk-for-net/tree/main/sdk/openai/Azure.AI.OpenAI)

### RAG Pattern Resources
- [Retrieval Augmented Generation Pattern](https://learn.microsoft.com/azure/architecture/ai-ml/architecture/rag-pattern)
- [Prompt Engineering Guide](https://learn.microsoft.com/azure/ai-services/openai/concepts/prompt-engineering)

### ASP.NET Core
- [Razor Pages Documentation](https://learn.microsoft.com/aspnet/core/razor-pages/)
- [AJAX with Razor Pages](https://learn.microsoft.com/aspnet/core/razor-pages/javascript)

### Testing
- [Unit Testing in ASP.NET Core](https://learn.microsoft.com/aspnet/core/test/unit-tests)
- [Integration Tests in ASP.NET Core](https://learn.microsoft.com/aspnet/core/test/integration-tests)

---

## 👥 Roles & Responsibilities

| Role | Responsibilities | Person |
|------|-----------------|--------|
| **Developer** | Full implementation, testing | TBD |
| **Azure Admin** | Create Azure resources, manage keys | TBD |
| **QA Engineer** | Manual testing, test case design | TBD |
| **Product Owner** | Requirements validation, UAT | TBD |
| **DevOps** | Deployment, monitoring setup | TBD |

---

## 📝 Change Log

| Date | Version | Changes | Author |
|------|---------|---------|--------|
| 2025-11-19 | 1.0 | Initial implementation plan created | GitHub Copilot |

---

## ✅ Sign-off

- [ ] Plan reviewed and approved
- [ ] Azure resources provisioned
- [ ] Development environment ready
- [ ] Ready to begin implementation

**Approved by:** ___________________  
**Date:** ___________________

---

**Next Steps:** Begin Phase 1 - Azure OpenAI Setup
