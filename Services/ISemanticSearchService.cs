using RetailMonolith.Models;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace RetailMonolith.Services
{
    public interface ISemanticSearchService
    {
        Task EnsureIndexAsync();
        Task IndexProductsAsync(IEnumerable<Product> products);
        Task<IReadOnlyList<Product>> SemanticSearchAsync(string query);
    }
}
