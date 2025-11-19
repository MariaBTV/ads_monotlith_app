# Guiding Star: Checkout Microservice Extraction

## Vision
Our end goal is to decouple the critical "Checkout" capability from the existing `RetailMonolith` application into a standalone, independently deployable microservice: `RetailMonolith.Checkout.Api`.

This transition follows the **Strangler Fig Pattern** (often referred to as the Decomposition Fig pattern), allowing us to incrementally migrate functionality without a "big bang" rewrite.

## Target Architecture

### 1. The Checkout Microservice
- **Responsibility:** Handling the complete checkout process, including cart retrieval, inventory reservation, payment processing, and order creation.
- **Interface:** A RESTful API exposing endpoints for checkout operations (e.g., `POST /api/checkout`).
- **Data Ownership:** Eventually, this service should own its own data (Orders/Payments), but initially, it may share the database or access it directly to minimise friction during the transition.

### 2. The Monolith (Legacy)
- **Role:** Remains the host for the UI (Razor Pages) and other domains (Product Catalogue, Cart).
- **Integration:** Instead of processing checkouts internally, the Monolith's `CheckoutService` will act as a **proxy client**, forwarding requests to the new Checkout Microservice.

## Key Benefits
1.  **Scalability:** The checkout process can be scaled independently during high-traffic events (e.g., Black Friday) without scaling the entire monolith.
2.  **Resilience:** Issues in the product catalogue or other areas won't directly impact the ability to process payments for items already in the cart.
3.  **Velocity:** The checkout team can deploy changes faster without waiting for the monolith's release cycle.

## Success Definition
The migration is considered successful when:
- The Monolith no longer contains business logic for checkout.
- The `RetailMonolith.Checkout.Api` is running and handling 100% of checkout traffic.
- The end-to-end user experience (Add to Cart -> Checkout -> Order Confirmation) remains unchanged.

## Strategic Alignment & Future Roadmap
While our immediate focus is decomposition, this work lays the foundation for several future initiatives. We must ensure our design decisions today do not hinder these upcoming goals:

### 1. Containerisation (Immediate Next Step)
The extraction of the Checkout API is a prerequisite for moving towards a microservices architecture hosted in containers (Azure Container Apps or AKS).
- **Design Implication:** The new API must be stateless and configurable via environment variables.
- **Design Implication:** Logging should be directed to `stdout`/`stderr` to be easily scraped by container orchestrators.

### 2. Observability
By splitting the services, we introduce network boundaries that require better monitoring.
- **Design Implication:** Ensure the new API is ready for OpenTelemetry (e.g., using standard `ILogger` and `ActivitySource` patterns) so we can easily add Application Insights later.

### 3. Event-Driven Integration
Future requirements involve publishing order events to Service Bus.
- **Design Implication:** When refactoring the checkout logic, identify where the "Order Created" event occurs. While we won't implement the bus now, the code structure should make it easy to inject an `IEventPublisher` later.

### 4. Modern Authentication
We will eventually move to Microsoft Entra ID.
- **Design Implication:** Avoid hardcoding "guest" logic too deeply. Where possible, abstract user identification behind an interface (e.g., `ICurrentUserService`) so we can swap the implementation from a hardcoded string to a ClaimsPrincipal later.
