# Phase 3: Checkout Service Refactor - Legacy Code Archive

**Date:** 19 November 2025  
**Phase:** 3 - Refactor Monolith to Proxy  
**Purpose:** Archive of business logic removed from monolith during Strangler Fig migration

---

## Original CheckoutService.cs (Business Logic - DELETED)

This file contained the full checkout business logic that has been migrated to the `RetailMonolith.Checkout.Api` microservice in Phase 2.

As of Phase 3, this logic has been **completely removed** and replaced with an HTTP proxy client.

```csharp
using Microsoft.EntityFrameworkCore;
using RetailMonolith.Data;
using RetailMonolith.Models;

namespace RetailMonolith.Services
{
    public class CheckoutService : ICheckoutService
    {
        private readonly AppDbContext _db;
        private readonly IPaymentGateway _payments;

        public CheckoutService(AppDbContext db, IPaymentGateway payments)
        {
            _db = db; _payments = payments;
        }
        public async Task<Order> CheckoutAsync(string customerId, string paymentToken, CancellationToken ct = default)
        {
            // 1) pull cart
            var cart = await _db.Carts
                .Include(c => c.Lines)
                .FirstOrDefaultAsync(c => c.CustomerId == customerId, ct)
                ?? throw new InvalidOperationException("Cart not found");

            var total = cart.Lines.Sum(l => l.UnitPrice * l.Quantity);

            // 2) reserve/decrement stock (optimistic)
            foreach (var line in cart.Lines)
            {
                var inv = await _db.Inventory.SingleAsync(i => i.Sku == line.Sku, ct);
                if (inv.Quantity < line.Quantity) throw new InvalidOperationException($"Out of stock: {line.Sku}");
                inv.Quantity -= line.Quantity;
            }

            // 3) charge
            var pay = await _payments.ChargeAsync(new(total, "GBP", paymentToken), ct);
            var status = pay.Succeeded ? "Paid" : "Failed";

            // 4) create order
            var order = new Order { CustomerId = customerId, Status = status, Total = total };
            order.Lines = cart.Lines.Select(l => new OrderLine
            {
                Sku = l.Sku,
                Name = l.Name,
                UnitPrice = l.UnitPrice,
                Quantity = l.Quantity
            }).ToList();

            _db.Orders.Add(order);

            // 5) clear cart
            _db.CartLines.RemoveRange(cart.Lines);
            await _db.SaveChangesAsync(ct);

            // (future) publish events here: OrderCreated / PaymentProcessed / InventoryReserved
            return order;
        }
    }
}
```

---

## Replacement: Proxy Implementation (Phase 3)

The new `CheckoutService` is now a thin HTTP client that forwards requests to the Checkout API microservice at `http://localhost:5100/api/checkout`.

**Key Changes:**
- Removed all database access (`AppDbContext`)
- Removed payment gateway dependency (`IPaymentGateway`)
- Removed business logic (cart retrieval, stock management, order creation)
- Added `HttpClient` for API communication
- Added response mapping from API DTOs to domain models
- Added error handling for HTTP failures (400, 500, 503, timeouts)

**Strangler Fig Progress:**
- ✅ Phase 1: API scaffolded
- ✅ Phase 2: Business logic extracted to API
- ✅ Phase 3: Monolith refactored to proxy
- ⏳ Phase 4: Verification & cleanup

---

## Verification

**Deleted Lines:** 58 lines of business logic removed  
**Added Lines:** 75 lines of HTTP proxy code  
**Net Change:** -58 business logic, +75 infrastructure  
**Business Logic in Monolith:** 0 (all in API)  

**Tests:**
- 7 proxy unit tests passing
- All tests mock `HttpMessageHandler` (no real API calls)
- Coverage: success paths, error scenarios, timeouts

---

*This file serves as a historical record only. The archived code should NOT be restored.*
