using RetailMonolith.Models;

namespace RetailMonolith.Services;

/// <summary>
/// Service interface for AI-powered chat functionality
/// </summary>
public interface IChatService
{
    /// <summary>
    /// Gets an AI-powered response to a user's chat message
    /// </summary>
    /// <param name="request">The chat request containing the user's message</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Chat response with AI message and product recommendations</returns>
    Task<ChatResponse> GetResponseAsync(ChatRequest request, CancellationToken ct = default);
    
    /// <summary>
    /// Retrieves the chat history for a specific session
    /// </summary>
    /// <param name="sessionId">The session identifier</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>List of chat messages in the session</returns>
    Task<List<Models.ChatMessage>> GetChatHistoryAsync(string sessionId, CancellationToken ct = default);
    
    /// <summary>
    /// Clears the conversation history for a specific session
    /// </summary>
    /// <param name="sessionId">The session identifier</param>
    /// <param name="ct">Cancellation token</param>
    Task ClearSessionAsync(string sessionId, CancellationToken ct = default);
}
