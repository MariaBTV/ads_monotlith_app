using Microsoft.EntityFrameworkCore;
using RetailMonolith.Data;
using RetailMonolith.Services;

namespace RetailMonolith;

public partial class Program
{
    // Marker class for WebApplicationFactory.
}

public partial class ProgramEntry
{
    public static WebApplication BuildWebApp(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

// DB � localdb for hack; swap to SQL in appsettings for Azure
builder.Services.AddDbContext<AppDbContext>(o =>
    o.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection") ??
                   "Server=(localdb)\\MSSQLLocalDB;Database=RetailMonolith;Trusted_Connection=True;MultipleActiveResultSets=true"));


// Add services to the container.
builder.Services.AddRazorPages();
builder.Services.AddScoped<IPaymentGateway, MockPaymentGateway>();

// Register HttpClient for CheckoutService proxy (Phase 3: Strangler Fig pattern)
builder.Services.AddHttpClient<ICheckoutService, CheckoutService>(client =>
{
    client.BaseAddress = new Uri(builder.Configuration["CheckoutApi:BaseUrl"] ?? "http://localhost:5100");
    client.Timeout = TimeSpan.FromSeconds(30);
});

        builder.Services.AddScoped<ICartService, CartService>();
        builder.Services.AddHealthChecks();

        var app = builder.Build();

        // auto-migrate & seed (hack convenience)
        using (var scope = app.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.Database.Migrate();
            AppDbContext.SeedAsync(db).GetAwaiter().GetResult();
        }

        // Configure the HTTP request pipeline.
        if (!app.Environment.IsDevelopment())
        {
            app.UseExceptionHandler("/Error");
            app.UseHsts();
        }

        app.UseHttpsRedirection();
        app.UseStaticFiles();

        app.UseRouting();

        app.UseAuthorization();

        app.MapRazorPages();

        // minimal APIs for the decomp path
        app.MapPost("/api/checkout", async (ICheckoutService svc) =>
        {
            var order = await svc.CheckoutAsync("guest", "tok_test");
            return Results.Ok(new { order.Id, order.Status, order.Total });
        });

        app.MapGet("/api/orders/{id:int}", async (int id, AppDbContext db) =>
        {
            var order = await db.Orders.Include(o => o.Lines)
                .SingleOrDefaultAsync(o => o.Id == id);

            return order is null ? Results.NotFound() : Results.Ok(order);
        });

        return app;
    }

    public static async Task Main(string[] args)
    {
        var app = BuildWebApp(args);
        await app.RunAsync();
    }
}

