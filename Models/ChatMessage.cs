using System.ComponentModel.DataAnnotations;

namespace RetailMonolith.Models;

/// <summary>
/// Represents a chat message in a conversation
/// </summary>
public class ChatMessage
{
    public int Id { get; set; }
    
    [Required]
    [MaxLength(100)]
    public string SessionId { get; set; } = Guid.NewGuid().ToString();
    
    [Required]
    [MaxLength(50)]
    public string Role { get; set; } = "user"; // user, assistant, system
    
    [Required]
    [MaxLength(2000)]
    public string Content { get; set; } = string.Empty;
    
    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
}
