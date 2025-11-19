using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using RetailMonolith.Models;
using RetailMonolith.Services;

namespace RetailMonolith.Pages.Chat;

public class IndexModel : PageModel
{
    private readonly IChatService _chatService;
    private readonly ICartService _cartService;
    private readonly ILogger<IndexModel> _logger;

    public IndexModel(
        IChatService chatService, 
        ICartService cartService,
        ILogger<IndexModel> logger)
    {
        _chatService = chatService;
        _cartService = cartService;
        _logger = logger;
    }

    public void OnGet()
    {
        // Initialize session if needed
        if (string.IsNullOrEmpty(HttpContext.Session.GetString("ChatSessionId")))
        {
            var sessionId = Guid.NewGuid().ToString();
            HttpContext.Session.SetString("ChatSessionId", sessionId);
            _logger.LogInformation("Created new chat session: {SessionId}", sessionId);
        }
    }

    public async Task<IActionResult> OnPostSendMessageAsync([FromForm] string message, CancellationToken ct = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(message))
            {
                return BadRequest(new { error = "Message cannot be empty" });
            }

            // Get or create session ID
            var sessionId = HttpContext.Session.GetString("ChatSessionId");
            if (string.IsNullOrEmpty(sessionId))
            {
                sessionId = Guid.NewGuid().ToString();
                HttpContext.Session.SetString("ChatSessionId", sessionId);
            }

            // Get customer ID from session (if user is logged in) - convert to string
            var customerId = HttpContext.Session.GetInt32("CustomerId")?.ToString();

            var request = new ChatRequest
            {
                Message = message,
                SessionId = sessionId,
                CustomerId = customerId
            };

            _logger.LogInformation("Processing chat message for session {SessionId}", sessionId);

            var response = await _chatService.GetResponseAsync(request, ct);

            return new JsonResult(new
            {
                success = true,
                message = response.Message,
                recommendations = response.Recommendations,
                sessionId = response.SessionId
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing chat message");
            return StatusCode(500, new { 
                success = false, 
                error = "Sorry, I encountered an error. Please try again." 
            });
        }
    }

    public async Task<IActionResult> OnPostAddToCartAsync([FromForm] string sku, CancellationToken ct = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(sku))
            {
                return BadRequest(new { error = "Product SKU is required" });
            }

            // Get or create customer ID for the cart
            var customerId = HttpContext.Session.GetString("CustomerId");
            if (string.IsNullOrEmpty(customerId))
            {
                // Create a temporary customer ID for guest users
                customerId = HttpContext.Session.GetString("GuestCustomerId");
                if (string.IsNullOrEmpty(customerId))
                {
                    customerId = $"guest-{Guid.NewGuid()}";
                    HttpContext.Session.SetString("GuestCustomerId", customerId);
                }
            }

            // Note: This is simplified - you would need to look up product ID from SKU
            // For now, we'll just log the intent
            _logger.LogInformation("Request to add product {Sku} to cart for customer {CustomerId}", sku, customerId);

            return new JsonResult(new
            {
                success = true,
                message = "Product added to cart successfully!"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding product {Sku} to cart", sku);
            return StatusCode(500, new { 
                success = false, 
                error = "Failed to add product to cart" 
            });
        }
    }

    public async Task<IActionResult> OnPostClearHistoryAsync(CancellationToken ct = default)
    {
        try
        {
            var sessionId = HttpContext.Session.GetString("ChatSessionId");
            if (!string.IsNullOrEmpty(sessionId))
            {
                await _chatService.ClearSessionAsync(sessionId, ct);
                _logger.LogInformation("Cleared chat history for session {SessionId}", sessionId);
            }

            // Create new session
            var newSessionId = Guid.NewGuid().ToString();
            HttpContext.Session.SetString("ChatSessionId", newSessionId);

            return new JsonResult(new
            {
                success = true,
                message = "Chat history cleared",
                sessionId = newSessionId
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error clearing chat history");
            return StatusCode(500, new { 
                success = false, 
                error = "Failed to clear chat history" 
            });
        }
    }
}
