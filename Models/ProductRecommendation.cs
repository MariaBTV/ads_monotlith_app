namespace RetailMonolith.Models;

/// <summary>
/// Represents a product recommendation from the AI chat assistant
/// </summary>
public class ProductRecommendation
{
    public int ProductId { get; set; }
    
    public string Sku { get; set; } = string.Empty;
    
    public string Name { get; set; } = string.Empty;
    
    public decimal Price { get; set; }
    
    public string Currency { get; set; } = "GBP";
    
    public string Category { get; set; } = string.Empty;
    
    /// <summary>
    /// URL to product image
    /// </summary>
    public string ImageUrl { get; set; } = string.Empty;
    
    /// <summary>
    /// AI-generated reason for recommending this product
    /// </summary>
    public string Reason { get; set; } = string.Empty;
}
