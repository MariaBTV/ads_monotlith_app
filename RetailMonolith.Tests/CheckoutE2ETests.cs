using System.Net;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using RetailMonolith;
using RetailMonolith.Data;
using RetailMonolith.Models;
using Xunit;

namespace RetailMonolith.Tests;

public class CheckoutE2ETests : IClassFixture<WebApplicationFactory<ProgramEntry>>
{
    private readonly WebApplicationFactory<ProgramEntry> _factory;

    public CheckoutE2ETests(WebApplicationFactory<ProgramEntry> factory)
    {
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                // Ensure the monolith uses an in-memory database for this test
                var sp = services.BuildServiceProvider();
                using var scope = sp.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                db.Database.EnsureDeleted();
                db.Database.EnsureCreated();

                // Seed a simple product and cart for the UI flow
                var product = new Product
                {
                    Sku = "E2E-001",
                    Name = "E2E Test Product",
                    Price = 19.99m,
                    Currency = "USD",
                    Category = "Test"
                };
                db.Products.Add(product);

                db.Inventory.Add(new InventoryItem
                {
                    Sku = "E2E-001",
                    Quantity = 10
                });

                var cart = new Cart
                {
                    CustomerId = "e2e-customer"
                };
                db.Carts.Add(cart);

                db.CartLines.Add(new CartLine
                {
                    Cart = cart,
                    Sku = "E2E-001",
                    Name = "E2E Test Product",
                    UnitPrice = 19.99m,
                    Quantity = 2
                });

                db.SaveChanges();
            });
        });
    }

    [Fact]
    public async Task Checkout_FullFlow_UsingMonolithUI_CreatesOrder()
    {
        // Arrange: create client against the monolith web app
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        // Act: simulate posting to the Checkout page handler
        var request = new
        {
            CustomerId = "e2e-customer",
            PaymentToken = "tok_e2e_test"
        };

        var response = await client.PostAsJsonAsync("/Checkout", request);

        // Assert: we expect a redirect to Orders/Details (or equivalent)
        Assert.True(response.StatusCode is HttpStatusCode.Redirect or HttpStatusCode.SeeOther);
        var location = response.Headers.Location?.ToString() ?? string.Empty;
        Assert.Contains("/Orders/Details", location);

        // Optionally, follow the redirect and ensure the page loads
        var followResponse = await client.GetAsync(location);
        Assert.Equal(HttpStatusCode.OK, followResponse.StatusCode);
    }
}
