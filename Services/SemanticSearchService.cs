using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using Azure;
using Azure.Search.Documents;
using Azure.Search.Documents.Indexes;
using Azure.Search.Documents.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using RetailMonolith.Models;

namespace RetailMonolith.Services
{
    public class SemanticSearchService : ISemanticSearchService
    {
        private readonly string _searchEndpoint;
        private readonly string _searchApiKey;
        private readonly string _indexName;
        private readonly string _openAiEndpoint;
        private readonly string _openAiKey;
        private readonly string _embeddingsModel;
        private readonly int _embeddingDimensions;
        private readonly ILogger<SemanticSearchService> _logger;
        private readonly SearchIndexClient _indexClient;
        private readonly SearchClient _searchClient;
        private readonly HttpClient _http;

        public SemanticSearchService(IConfiguration config, ILogger<SemanticSearchService> logger, IHttpClientFactory httpClientFactory)
        {
            _logger = logger;
            _searchEndpoint = config["AzureSearch:Endpoint"]!;
            _searchApiKey = config["AzureSearch:ApiKey"]!;
            _indexName = config["AzureSearch:IndexName"]!;
            _openAiEndpoint = config["AzureOpenAI:Endpoint"]!;
            _openAiKey = config["AzureOpenAI:ApiKey"]!;
            _embeddingsModel = config["AzureOpenAI:EmbeddingsModel"]!;
            _embeddingDimensions = int.Parse(config["AzureOpenAI:EmbeddingDimensions"] ?? "1536");

            var credential = new AzureKeyCredential(_searchApiKey);
            _indexClient = new SearchIndexClient(new Uri(_searchEndpoint), credential);
            _searchClient = new SearchClient(new Uri(_searchEndpoint), _indexName, credential);
            _http = httpClientFactory.CreateClient();
        }

        public async Task EnsureIndexAsync()
        {
            // Index should be created via Azure Portal or REST API with vector + semantic config
            // This method just verifies the index exists
            try
            {
                var existing = await _indexClient.GetIndexAsync(_indexName);
                _logger.LogInformation("Azure AI Search index {Index} exists and is ready", _indexName);
            }
            catch (RequestFailedException ex) when (ex.Status == 404)
            {
                _logger.LogError("Index {Index} not found. Please create it using the REST API as documented in SEMANTIC_SEARCH_SETUP.md", _indexName);
                throw new InvalidOperationException($"Search index '{_indexName}' does not exist. Create it via Azure Portal or REST API first.", ex);
            }
        }

        public async Task IndexProductsAsync(IEnumerable<Product> products)
        {
            var docs = new List<Dictionary<string, object>>();
            foreach (var p in products)
            {
                var combinedText = $"{p.Name} {p.Description} {p.Category}".Trim();
                var embedding = await GetEmbeddingAsync(combinedText);
                
                docs.Add(new Dictionary<string, object>
                {
                    ["id"] = p.Id.ToString(),
                    ["name"] = p.Name,
                    ["description"] = p.Description ?? string.Empty,
                    ["category"] = p.Category ?? string.Empty,
                    ["price"] = Convert.ToDouble(p.Price),
                    ["contentVector"] = embedding
                });
            }
            if (docs.Count == 0) return;
            await _searchClient.IndexDocumentsAsync(IndexDocumentsBatch.Upload(docs));
        }

        public async Task<IReadOnlyList<Product>> SemanticSearchAsync(string query)
        {
            try
            {
                var searchText = string.IsNullOrWhiteSpace(query) ? "*" : query;
                var embedding = await GetEmbeddingAsync(searchText);

                // Extract filters from natural language query
                var filter = BuildFilterFromQuery(query);
                _logger.LogInformation("Query: '{Query}' | Generated filter: '{Filter}'", query, filter ?? "none");

                // Using REST API for hybrid vector + semantic search
                var searchUrl = $"{_searchEndpoint}/indexes/{_indexName}/docs/search?api-version=2024-07-01";
                var requestBody = new
                {
                    search = searchText,
                    filter = filter,
                    vectorQueries = new[]
                    {
                        new
                        {
                            kind = "vector",
                            vector = embedding,
                            fields = "contentVector",
                            k = 10
                        }
                    },
                    queryType = "semantic",
                    semanticConfiguration = "semantic-config",
                    top = 10,
                    select = "id,name,description,category,price"
                };

                using var req = new HttpRequestMessage(HttpMethod.Post, searchUrl)
                {
                    Content = JsonContent.Create(requestBody)
                };
                req.Headers.Add("api-key", _searchApiKey);

                var resp = await _http.SendAsync(req);
                resp.EnsureSuccessStatusCode();

                using var stream = await resp.Content.ReadAsStreamAsync();
                var json = await JsonDocument.ParseAsync(stream);
                var found = new List<Product>();

                foreach (var item in json.RootElement.GetProperty("value").EnumerateArray())
                {
                    found.Add(new Product
                    {
                        Id = item.TryGetProperty("id", out var idProp) && int.TryParse(idProp.GetString(), out var id) ? id : 0,
                        Name = item.TryGetProperty("name", out var nameProp) ? nameProp.GetString() ?? string.Empty : string.Empty,
                        Description = item.TryGetProperty("description", out var descProp) ? descProp.GetString() : null,
                        Category = item.TryGetProperty("category", out var catProp) ? catProp.GetString() : null,
                        Price = item.TryGetProperty("price", out var priceProp) && priceProp.ValueKind == JsonValueKind.Number ? (decimal)priceProp.GetDouble() : 0m,
                        IsActive = true
                    });
                }

                return found;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Semantic search failed for query: {Query}", query);
                // Fallback to lexical search
                var options = new SearchOptions { Size = 10 };
                var results = await _searchClient.SearchAsync<SearchDocument>(string.IsNullOrWhiteSpace(query) ? "*" : query, options);
                var found = new List<Product>();
                await foreach (var r in results.Value.GetResultsAsync())
                {
                    var doc = r.Document;
                    found.Add(new Product
                    {
                        Id = int.TryParse(doc["id"]?.ToString(), out var id) ? id : 0,
                        Name = doc["name"]?.ToString() ?? string.Empty,
                        Description = doc["description"]?.ToString(),
                        Category = doc["category"]?.ToString(),
                        Price = doc.ContainsKey("price") && double.TryParse(doc["price"]?.ToString(), out var price) ? (decimal)price : 0m,
                        IsActive = true
                    });
                }
                return found;
            }
        }

        private string? BuildFilterFromQuery(string query)
        {
            if (string.IsNullOrWhiteSpace(query))
                return null;

            var filters = new List<string>();
            var lowerQuery = query.ToLowerInvariant();

            // Extract category filter
            var categories = new[] { "beauty", "apparel", "footwear", "home", "accessories", "electronics" };
            foreach (var category in categories)
            {
                if (lowerQuery.Contains(category))
                {
                    filters.Add($"category eq '{char.ToUpper(category[0]) + category.Substring(1)}'");
                    break;
                }
            }

            // Extract price filters
            var underMatch = System.Text.RegularExpressions.Regex.Match(lowerQuery, @"under\s+\$?(\d+)");
            var belowMatch = System.Text.RegularExpressions.Regex.Match(lowerQuery, @"below\s+\$?(\d+)");
            var lessMatch = System.Text.RegularExpressions.Regex.Match(lowerQuery, @"less\s+than\s+\$?(\d+)");
            
            if (underMatch.Success && double.TryParse(underMatch.Groups[1].Value, out var maxPrice))
            {
                filters.Add($"price lt {maxPrice}");
            }
            else if (belowMatch.Success && double.TryParse(belowMatch.Groups[1].Value, out maxPrice))
            {
                filters.Add($"price lt {maxPrice}");
            }
            else if (lessMatch.Success && double.TryParse(lessMatch.Groups[1].Value, out maxPrice))
            {
                filters.Add($"price lt {maxPrice}");
            }

            var overMatch = System.Text.RegularExpressions.Regex.Match(lowerQuery, @"over\s+\$?(\d+)");
            var aboveMatch = System.Text.RegularExpressions.Regex.Match(lowerQuery, @"above\s+\$?(\d+)");
            var moreMatch = System.Text.RegularExpressions.Regex.Match(lowerQuery, @"more\s+than\s+\$?(\d+)");
            
            if (overMatch.Success && double.TryParse(overMatch.Groups[1].Value, out var minPrice))
            {
                filters.Add($"price gt {minPrice}");
            }
            else if (aboveMatch.Success && double.TryParse(aboveMatch.Groups[1].Value, out minPrice))
            {
                filters.Add($"price gt {minPrice}");
            }
            else if (moreMatch.Success && double.TryParse(moreMatch.Groups[1].Value, out minPrice))
            {
                filters.Add($"price gt {minPrice}");
            }

            return filters.Count > 0 ? string.Join(" and ", filters) : null;
        }

        private async Task<IReadOnlyList<float>> GetEmbeddingAsync(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
            {
                return Enumerable.Repeat(0f, _embeddingDimensions).ToList();
            }

            var url = $"{_openAiEndpoint}openai/deployments/{_embeddingsModel}/embeddings?api-version=2024-02-15-preview";
            using var req = new HttpRequestMessage(HttpMethod.Post, url)
            {
                Content = JsonContent.Create(new
                {
                    input,
                    model = _embeddingsModel
                })
            };
            req.Headers.Add("api-key", _openAiKey);
            
            var resp = await _http.SendAsync(req);
            resp.EnsureSuccessStatusCode();
            
            using var stream = await resp.Content.ReadAsStreamAsync();
            var json = await JsonDocument.ParseAsync(stream);
            var vector = new List<float>();
            
            foreach (var v in json.RootElement.GetProperty("data")[0].GetProperty("embedding").EnumerateArray())
            {
                vector.Add(v.GetSingle());
            }
            
            return vector;
        }
    }
}
