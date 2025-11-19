using System.ComponentModel.DataAnnotations;

namespace RetailMonolith.Models;

/// <summary>
/// Request model for chat messages
/// </summary>
public class ChatRequest
{
    [Required(ErrorMessage = "Message is required")]
    [MaxLength(500, ErrorMessage = "Message cannot exceed 500 characters")]
    public string Message { get; set; } = string.Empty;
    
    public string SessionId { get; set; } = string.Empty;
    
    public string CustomerId { get; set; } = "guest";
}
