using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using Moq.Protected;
using RetailMonolith.Models;
using RetailMonolith.Services;
using Xunit;

namespace RetailMonolith.Tests;

public class CheckoutServiceProxyTests
{
    // Success Scenarios
    [Fact]
    public async Task CheckoutAsync_WhenApiReturns200_ReturnsOrderObject()
    {
        // Arrange
        var mockHandler = new Mock<HttpMessageHandler>();
        var apiResponse = new
        {
            orderId = 42,
            status = "Paid",
            total = 99.99m,
            createdUtc = DateTime.UtcNow
        };
        
        mockHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(
                    JsonSerializer.Serialize(apiResponse),
                    Encoding.UTF8,
                    "application/json")
            });

        var httpClient = new HttpClient(mockHandler.Object)
        {
            BaseAddress = new Uri("http://localhost:5100")
        };
        
        var service = new CheckoutService(httpClient);

        // Act
        var result = await service.CheckoutAsync("test-customer", "tok_test");

        // Assert
        Assert.NotNull(result);
        Assert.Equal(42, result.Id);
        Assert.Equal("Paid", result.Status);
        Assert.Equal(99.99m, result.Total);
    }

    [Fact]
    public async Task CheckoutAsync_WithValidResponse_MapsFieldsCorrectly()
    {
        // Arrange
        var mockHandler = new Mock<HttpMessageHandler>();
        var apiResponse = new
        {
            orderId = 123,
            status = "Failed",
            total = 150.50m,
            createdUtc = new DateTime(2025, 11, 19, 14, 30, 0, DateTimeKind.Utc)
        };
        
        mockHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(
                    JsonSerializer.Serialize(apiResponse),
                    Encoding.UTF8,
                    "application/json")
            });

        var httpClient = new HttpClient(mockHandler.Object)
        {
            BaseAddress = new Uri("http://localhost:5100")
        };
        
        var service = new CheckoutService(httpClient);

        // Act
        var result = await service.CheckoutAsync("customer-123", "tok_abc");

        // Assert
        Assert.Equal(123, result.Id);
        Assert.Equal("Failed", result.Status);
        Assert.Equal(150.50m, result.Total);
        Assert.Equal("customer-123", result.CustomerId);
        Assert.Equal(new DateTime(2025, 11, 19, 14, 30, 0, DateTimeKind.Utc), result.CreatedUtc);
    }

    // Failure Scenarios
    [Fact]
    public async Task CheckoutAsync_WhenApiReturns400_ThrowsInvalidOperationException()
    {
        // Arrange
        var mockHandler = new Mock<HttpMessageHandler>();
        mockHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.BadRequest,
                Content = new StringContent("{\"error\":\"Cart not found or empty\"}")
            });

        var httpClient = new HttpClient(mockHandler.Object)
        {
            BaseAddress = new Uri("http://localhost:5100")
        };
        
        var service = new CheckoutService(httpClient);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.CheckoutAsync("empty-cart", "tok_test"));
    }

    [Fact]
    public async Task CheckoutAsync_WhenApiReturns500_ThrowsHttpRequestException()
    {
        // Arrange
        var mockHandler = new Mock<HttpMessageHandler>();
        mockHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.InternalServerError,
                Content = new StringContent("{\"error\":\"Internal server error\"}")
            });

        var httpClient = new HttpClient(mockHandler.Object)
        {
            BaseAddress = new Uri("http://localhost:5100")
        };
        
        var service = new CheckoutService(httpClient);

        // Act & Assert
        await Assert.ThrowsAsync<HttpRequestException>(
            () => service.CheckoutAsync("test-customer", "tok_test"));
    }

    [Fact]
    public async Task CheckoutAsync_WhenApiUnavailable_ThrowsHttpRequestException()
    {
        // Arrange
        var mockHandler = new Mock<HttpMessageHandler>();
        mockHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ThrowsAsync(new HttpRequestException("Connection refused"));

        var httpClient = new HttpClient(mockHandler.Object)
        {
            BaseAddress = new Uri("http://localhost:5100")
        };
        
        var service = new CheckoutService(httpClient);

        // Act & Assert
        await Assert.ThrowsAsync<HttpRequestException>(
            () => service.CheckoutAsync("test-customer", "tok_test"));
    }

    // Timeout Scenarios
    [Fact]
    public async Task CheckoutAsync_WhenApiTimesOut_ThrowsTaskCanceledException()
    {
        // Arrange
        var mockHandler = new Mock<HttpMessageHandler>();
        mockHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ThrowsAsync(new TaskCanceledException("Request timeout"));

        var httpClient = new HttpClient(mockHandler.Object)
        {
            BaseAddress = new Uri("http://localhost:5100"),
            Timeout = TimeSpan.FromSeconds(1)
        };
        
        var service = new CheckoutService(httpClient);

        // Act & Assert
        await Assert.ThrowsAsync<TaskCanceledException>(
            () => service.CheckoutAsync("test-customer", "tok_test"));
    }
}
