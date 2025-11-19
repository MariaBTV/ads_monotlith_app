using System.Net.Http.Json;
using System.Text.Json;
using RetailMonolith.Models;

namespace RetailMonolith.Services
{
    /// <summary>
    /// Proxy implementation of CheckoutService that forwards requests to the Checkout API microservice.
    /// This is part of the Strangler Fig pattern migration.
    /// </summary>
    public class CheckoutService : ICheckoutService
    {
        private readonly HttpClient _httpClient;
        private readonly JsonSerializerOptions _jsonOptions;

        public CheckoutService(HttpClient httpClient)
        {
            _httpClient = httpClient;
            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };
        }

        public async Task<Order> CheckoutAsync(string customerId, string paymentToken, CancellationToken ct = default)
        {
            // Construct request to Checkout API
            var request = new
            {
                customerId,
                paymentToken
            };

            try
            {
                // Call the Checkout API microservice
                var response = await _httpClient.PostAsJsonAsync("/api/checkout", request, ct);

                // Handle different response codes
                if (response.StatusCode == System.Net.HttpStatusCode.BadRequest)
                {
                    var errorContent = await response.Content.ReadAsStringAsync(ct);
                    throw new InvalidOperationException($"Checkout validation failed: {errorContent}");
                }

                if (response.StatusCode == System.Net.HttpStatusCode.ServiceUnavailable)
                {
                    throw new HttpRequestException("Checkout service temporarily unavailable");
                }

                // Ensure success or throw
                response.EnsureSuccessStatusCode();

                // Parse API response
                var apiResponse = await response.Content.ReadFromJsonAsync<CheckoutApiResponse>(_jsonOptions, ct)
                    ?? throw new InvalidOperationException("Empty response from Checkout API");

                // Map API response to Order model expected by Razor Pages
                var order = new Order
                {
                    Id = apiResponse.OrderId,
                    CustomerId = customerId,
                    Status = apiResponse.Status,
                    Total = apiResponse.Total,
                    CreatedUtc = apiResponse.CreatedUtc
                };

                return order;
            }
            catch (HttpRequestException ex)
            {
                // Wrap HTTP errors for consistent error handling
                throw new HttpRequestException($"Failed to communicate with Checkout API: {ex.Message}", ex);
            }
            catch (TaskCanceledException ex) when (ex.CancellationToken == ct || !ct.IsCancellationRequested)
            {
                // Timeout occurred
                throw new TaskCanceledException("Checkout API request timed out", ex);
            }
        }

        private class CheckoutApiResponse
        {
            public int OrderId { get; set; }
            public string Status { get; set; } = string.Empty;
            public decimal Total { get; set; }
            public DateTime CreatedUtc { get; set; }
        }
    }
}
