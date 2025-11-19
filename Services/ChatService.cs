using Azure.AI.OpenAI;
using Azure;
using Microsoft.EntityFrameworkCore;
using RetailMonolith.Data;
using RetailMonolith.Models;
using System.Collections.Concurrent;
using System.Text;
using System.Text.RegularExpressions;
using OpenAI.Chat;

namespace RetailMonolith.Services;

/// <summary>
/// AI-powered chat service using Azure OpenAI with RAG (Retrieval-Augmented Generation)
/// </summary>
public class ChatService : IChatService
{
    private readonly AppDbContext _db;
    private readonly IConfiguration _config;
    private readonly ILogger<ChatService> _logger;
    private readonly AzureOpenAIClient _aiClient;
    private readonly string _deploymentName;
    private readonly int _maxTokens;
    private readonly float _temperature;
    
    // In-memory conversation history (thread-safe)
    private static readonly ConcurrentDictionary<string, List<Models.ChatMessage>> _conversationHistory = new();
    
    public ChatService(
        AppDbContext db,
        IConfiguration config,
        ILogger<ChatService> logger)
    {
        _db = db;
        _config = config;
        _logger = logger;
        
        // Read Azure OpenAI configuration
        var endpoint = _config["AzureOpenAI:Endpoint"] ?? throw new InvalidOperationException("Azure OpenAI Endpoint not configured");
        var apiKey = _config["AzureOpenAI:ApiKey"] ?? throw new InvalidOperationException("Azure OpenAI ApiKey not configured");
        _deploymentName = _config["AzureOpenAI:DeploymentName"] ?? "gpt-4o";
        _maxTokens = int.Parse(_config["AzureOpenAI:MaxTokens"] ?? "800");
        _temperature = float.Parse(_config["AzureOpenAI:Temperature"] ?? "0.7");
        
        _aiClient = new AzureOpenAIClient(new Uri(endpoint), new AzureKeyCredential(apiKey));
    }
    
    public async Task<ChatResponse> GetResponseAsync(ChatRequest request, CancellationToken ct = default)
    {
        try
        {
            // Validate input
            if (string.IsNullOrWhiteSpace(request.Message))
            {
                throw new ArgumentException("Message cannot be empty", nameof(request.Message));
            }
            
            _logger.LogInformation("Processing chat request for session {SessionId}", request.SessionId);
            
            // Step 1: Query relevant products using RAG
            var relevantProducts = await QueryRelevantProductsAsync(request.Message, ct);
            
            // Step 2: Build system prompt with product context
            var systemPrompt = BuildSystemPrompt(relevantProducts, request.Message);
            
            // Step 3: Call Azure OpenAI
            var aiResponse = await CallAzureOpenAIAsync(systemPrompt, request.Message, request.SessionId, ct);
            
            // Step 4: Parse product recommendations from AI response
            var recommendations = ParseRecommendations(aiResponse, relevantProducts);
            
            // Add image URLs to recommendations
            if (recommendations != null)
            {
                foreach (var rec in recommendations)
                {
                    rec.ImageUrl = GetProductImageUrl(rec.Category);
                }
            }
            
            // Step 5: Save to conversation history
            SaveToHistory(request.SessionId, request.Message, aiResponse);
            
            return new ChatResponse
            {
                Message = aiResponse,
                Recommendations = recommendations,
                SessionId = request.SessionId
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing chat request");
            return new ChatResponse
            {
                Message = "I'm sorry, I'm having trouble right now. Please try again later.",
                SessionId = request.SessionId
            };
        }
    }
    
    /// <summary>
    /// Query relevant products from database based on user intent (RAG)
    /// </summary>
    private async Task<List<Product>> QueryRelevantProductsAsync(string userMessage, CancellationToken ct)
    {
        var query = _db.Products.Where(p => p.IsActive);
        
        // Extract category
        var category = ExtractCategory(userMessage);
        if (!string.IsNullOrEmpty(category))
        {
            query = query.Where(p => p.Category == category);
            _logger.LogInformation("Filtering by category: {Category}", category);
        }
        
        // Extract budget constraint
        var maxBudget = ExtractBudget(userMessage);
        if (maxBudget.HasValue)
        {
            query = query.Where(p => p.Price <= maxBudget.Value);
            _logger.LogInformation("Filtering by max budget: {MaxBudget}", maxBudget);
        }
        
        // Extract keywords
        var keywords = ExtractKeywords(userMessage);
        if (keywords.Any())
        {
            query = query.Where(p => 
                keywords.Any(k => p.Name.Contains(k) || (p.Description != null && p.Description.Contains(k))));
            _logger.LogInformation("Filtering by keywords: {Keywords}", string.Join(", ", keywords));
        }
        
        // Return top 10 products
        var products = await query.Take(10).ToListAsync(ct);
        
        // If no products found with filters, return some popular products
        if (!products.Any())
        {
            products = await _db.Products
                .Where(p => p.IsActive)
                .OrderBy(p => p.Price)
                .Take(5)
                .ToListAsync(ct);
        }
        
        _logger.LogInformation("Found {Count} relevant products", products.Count);
        return products;
    }
    
    /// <summary>
    /// Extract category from user message
    /// </summary>
    private string? ExtractCategory(string message)
    {
        var lowerMessage = message.ToLowerInvariant();
        var categories = new[] { "Apparel", "Footwear", "Electronics", "Accessories", "Home & Living", "Sports & Outdoors" };
        
        return categories.FirstOrDefault(c => lowerMessage.Contains(c.ToLowerInvariant()));
    }
    
    /// <summary>
    /// Extract budget constraint from user message (e.g., "under £30", "$50", "less than 100")
    /// </summary>
    private decimal? ExtractBudget(string message)
    {
        // Regex patterns for price extraction
        var patterns = new[]
        {
            @"under\s*[£$€]?(\d+(?:\.\d{2})?)",
            @"less\s+than\s*[£$€]?(\d+(?:\.\d{2})?)",
            @"below\s*[£$€]?(\d+(?:\.\d{2})?)",
            @"maximum\s*[£$€]?(\d+(?:\.\d{2})?)",
            @"max\s*[£$€]?(\d+(?:\.\d{2})?)",
            @"[£$€](\d+(?:\.\d{2})?)\s*or\s*less"
        };
        
        foreach (var pattern in patterns)
        {
            var match = Regex.Match(message, pattern, RegexOptions.IgnoreCase);
            if (match.Success && decimal.TryParse(match.Groups[1].Value, out var budget))
            {
                return budget;
            }
        }
        
        return null;
    }
    
    /// <summary>
    /// Extract keywords from user message (filtering common words)
    /// </summary>
    private List<string> ExtractKeywords(string message)
    {
        var commonWords = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "the", "a", "an", "and", "or", "but", "in", "on", "at", "to", "for",
            "of", "with", "by", "from", "up", "about", "into", "through", "during",
            "show", "me", "find", "get", "need", "want", "looking", "buy", "purchase",
            "i", "you", "he", "she", "it", "we", "they", "my", "your", "his", "her",
            "is", "are", "was", "were", "be", "been", "have", "has", "had", "do", "does"
        };
        
        var words = Regex.Split(message, @"\W+")
            .Where(w => w.Length > 2 && !commonWords.Contains(w))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        
        return words;
    }
    
    /// <summary>
    /// Build system prompt with product context for AI
    /// </summary>
    private string BuildSystemPrompt(List<Product> products, string userMessage)
    {
        var sb = new StringBuilder();
        
        sb.AppendLine("You are a helpful retail sales assistant for an online electronics store. Your ONLY role is to help customers find and purchase products from our catalog.");
        sb.AppendLine();
        sb.AppendLine("STRICT RULES:");
        sb.AppendLine("1. ONLY discuss products available in our catalog");
        sb.AppendLine("2. ONLY answer questions related to shopping, products, pricing, and purchasing");
        sb.AppendLine("3. If asked about topics unrelated to our products (weather, politics, general knowledge, etc.), politely redirect to product assistance");
        sb.AppendLine("4. Example redirect: 'I'm here to help you find the perfect products from our store. What type of product are you looking for today?'");
        sb.AppendLine();
        sb.AppendLine("Available Products:");
        
        foreach (var product in products)
        {
            sb.AppendLine($"- [{product.Sku}] {product.Name} - {product.Currency}{product.Price:F2} ({product.Category})");
            if (!string.IsNullOrEmpty(product.Description))
            {
                sb.AppendLine($"  Description: {product.Description}");
            }
        }
        
        sb.AppendLine();
        sb.AppendLine("Response Guidelines:");
        sb.AppendLine("- Be conversational, friendly, and helpful");
        sb.AppendLine("- Stay focused on helping customers find products");
        sb.AppendLine("- Ask clarifying questions about product preferences if needed");
        sb.AppendLine("- Recommend 2-4 products maximum per response");
        sb.AppendLine("- IMPORTANT: When recommending a product, you MUST include its SKU in this exact format: [SKU-XXXX]");
        sb.AppendLine("- Example: 'I recommend the [SKU-LAP001] Dell XPS Laptop because it fits your budget.'");
        sb.AppendLine("- Consider any budget constraints mentioned by the customer");
        sb.AppendLine("- Explain why you're recommending specific products");
        sb.AppendLine("- If no suitable products are available, politely explain and suggest alternatives");
        sb.AppendLine();
        sb.AppendLine("CRITICAL: Always wrap product SKUs in square brackets like [SKU-XXXX] to help the system identify recommendations.");
        
        return sb.ToString();
    }
    
    /// <summary>
    /// Call Azure OpenAI API with retry logic
    /// </summary>
    private async Task<string> CallAzureOpenAIAsync(string systemPrompt, string userMessage, string sessionId, CancellationToken ct)
    {
        try
        {
            var chatClient = _aiClient.GetChatClient(_deploymentName);
            
            // Build conversation history
            var messages = new List<Models.ChatMessage>();
            
            // Add system prompt
            messages.Add(new Models.ChatMessage { Role = "system", Content = systemPrompt });
            
            // Add conversation history if exists
            if (_conversationHistory.TryGetValue(sessionId, out var history))
            {
                messages.AddRange(history.TakeLast(5)); // Include last 5 messages for context
            }
            
            // Add current user message
            messages.Add(new Models.ChatMessage { Role = "user", Content = userMessage });
            
            // Convert to OpenAI chat messages
            var chatMessages = messages.Select<Models.ChatMessage, OpenAI.Chat.ChatMessage>(m => 
            {
                return m.Role.ToLowerInvariant() switch
                {
                    "system" => OpenAI.Chat.ChatMessage.CreateSystemMessage(m.Content),
                    "assistant" => OpenAI.Chat.ChatMessage.CreateAssistantMessage(m.Content),
                    _ => OpenAI.Chat.ChatMessage.CreateUserMessage(m.Content)
                };
            }).ToList();
            
            var options = new ChatCompletionOptions
            {
                MaxOutputTokenCount = _maxTokens,
                Temperature = (float)_temperature
            };
            
            _logger.LogInformation("Calling Azure OpenAI with {MessageCount} messages", chatMessages.Count);
            
            var response = await chatClient.CompleteChatAsync(chatMessages, options, ct);
            var responseMessage = response.Value.Content[0].Text;
            
            _logger.LogInformation("Received response from Azure OpenAI ({TokenCount} tokens)", 
                response.Value.Usage.TotalTokenCount);
            
            return responseMessage;
        }
        catch (RequestFailedException ex)
        {
            _logger.LogError(ex, "Azure OpenAI API request failed: {Message}", ex.Message);
            throw new InvalidOperationException("Failed to get AI response. Please try again.", ex);
        }
    }
    
    /// <summary>
    /// Parse product recommendations from AI response
    /// </summary>
    private List<ProductRecommendation>? ParseRecommendations(string aiResponse, List<Product> availableProducts)
    {
        // Extract SKUs using regex pattern [SKU-XXXX]
        var skuPattern = @"\[SKU-([^\]]+)\]";
        var matches = Regex.Matches(aiResponse, skuPattern);
        
        if (!matches.Any())
        {
            return null;
        }
        
        var recommendations = new List<ProductRecommendation>();
        var processedSkus = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        
        foreach (Match match in matches)
        {
            var sku = match.Groups[1].Value;
            
            // Skip duplicates
            if (processedSkus.Contains(sku))
                continue;
            
            processedSkus.Add(sku);
            
            // Find product in available products
            var product = availableProducts.FirstOrDefault(p => 
                p.Sku.Equals(sku, StringComparison.OrdinalIgnoreCase));
            
            if (product != null)
            {
                // Extract reason (text around the SKU mention)
                var skuIndex = aiResponse.IndexOf(match.Value, StringComparison.Ordinal);
                var contextStart = Math.Max(0, skuIndex - 100);
                var contextLength = Math.Min(200, aiResponse.Length - contextStart);
                var context = aiResponse.Substring(contextStart, contextLength).Trim();
                
                recommendations.Add(new ProductRecommendation
                {
                    ProductId = product.Id,
                    Sku = product.Sku,
                    Name = product.Name,
                    Price = product.Price,
                    Currency = product.Currency,
                    Category = product.Category ?? string.Empty,
                    Reason = context
                });
            }
        }
        
        _logger.LogInformation("Parsed {Count} product recommendations", recommendations.Count);
        return recommendations.Any() ? recommendations : null;
    }
    
    /// <summary>
    /// Save conversation to history
    /// </summary>
    private void SaveToHistory(string sessionId, string userMessage, string assistantMessage)
    {
        var history = _conversationHistory.GetOrAdd(sessionId, _ => new List<Models.ChatMessage>());
        
        lock (history)
        {
            history.Add(new Models.ChatMessage
            {
                SessionId = sessionId,
                Role = "user",
                Content = userMessage,
                CreatedUtc = DateTime.UtcNow
            });
            
            history.Add(new Models.ChatMessage
            {
                SessionId = sessionId,
                Role = "assistant",
                Content = assistantMessage,
                CreatedUtc = DateTime.UtcNow
            });
            
            // Keep only last 20 messages to avoid memory issues
            if (history.Count > 20)
            {
                history.RemoveRange(0, history.Count - 20);
            }
        }
    }
    
    public async Task<List<Models.ChatMessage>> GetChatHistoryAsync(string sessionId, CancellationToken ct = default)
    {
        await Task.CompletedTask; // Make async
        
        if (_conversationHistory.TryGetValue(sessionId, out var history))
        {
            lock (history)
            {
                return history.ToList();
            }
        }
        
        return new List<Models.ChatMessage>();
    }
    
    public async Task ClearSessionAsync(string sessionId, CancellationToken ct = default)
    {
        await Task.CompletedTask; // Make async
        _conversationHistory.TryRemove(sessionId, out _);
        _logger.LogInformation("Cleared conversation history for session {SessionId}", sessionId);
    }
    
    /// <summary>
    /// Get product image URL based on category
    /// </summary>
    private string GetProductImageUrl(string category)
    {
        // Map categories to placeholder images or use category-specific images
        var categoryLower = category?.ToLowerInvariant() ?? "";
        
        return categoryLower switch
        {
            var c when c.Contains("laptop") || c.Contains("computer") => "https://images.unsplash.com/photo-1496181133206-80ce9b88a853?w=400",
            var c when c.Contains("phone") || c.Contains("smartphone") => "https://images.unsplash.com/photo-1511707171634-5f897ff02aa9?w=400",
            var c when c.Contains("tablet") => "https://images.unsplash.com/photo-1561154464-82e9adf32764?w=400",
            var c when c.Contains("camera") => "https://images.unsplash.com/photo-1526170375885-4d8ecf77b99f?w=400",
            var c when c.Contains("headphone") || c.Contains("audio") => "https://images.unsplash.com/photo-1505740420928-5e560c06d30e?w=400",
            var c when c.Contains("watch") || c.Contains("smartwatch") => "https://images.unsplash.com/photo-1523275335684-37898b6baf30?w=400",
            var c when c.Contains("tv") || c.Contains("television") || c.Contains("monitor") => "https://images.unsplash.com/photo-1593359677879-a4bb92f829d1?w=400",
            var c when c.Contains("speaker") => "https://images.unsplash.com/photo-1608043152269-423dbba4e7e1?w=400",
            var c when c.Contains("gaming") || c.Contains("console") => "https://images.unsplash.com/photo-1486401899868-0e435ed85128?w=400",
            _ => "https://images.unsplash.com/photo-1505740420928-5e560c06d30e?w=400" // Default tech product image
        };
    }
}
