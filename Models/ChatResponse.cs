namespace RetailMonolith.Models;

/// <summary>
/// Response model for chat messages
/// </summary>
public class ChatResponse
{
    /// <summary>
    /// The AI assistant's response message
    /// </summary>
    public string Message { get; set; } = string.Empty;
    
    /// <summary>
    /// Product recommendations extracted from the AI response
    /// </summary>
    public List<ProductRecommendation>? Recommendations { get; set; }
    
    /// <summary>
    /// Session identifier for conversation tracking
    /// </summary>
    public string SessionId { get; set; } = string.Empty;
}
