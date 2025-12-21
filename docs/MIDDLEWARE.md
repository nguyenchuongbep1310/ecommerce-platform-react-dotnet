# Middleware Implementation Guide

This document describes the middleware implementation across all microservices in the e-commerce platform.

## Overview

Middleware components are software components that sit in the HTTP request pipeline and process requests before they reach the application logic and responses before they are sent back to the client. They provide cross-cutting concerns like logging, error handling, security, and more.

## Implemented Middleware

### 1. Exception Handling Middleware

**Purpose**: Catches all unhandled exceptions and returns consistent error responses.

**Location**: `{ServiceName}/Middleware/ExceptionHandlingMiddleware.cs`

**Features**:

- Global exception handling
- Consistent error response format
- Different error codes for different exception types
- Environment-aware error details (detailed in development, generic in production)
- Structured error responses with trace IDs for debugging

**Error Response Format**:

```json
{
  "statusCode": 500,
  "message": "An internal server error occurred",
  "error": "Detailed error message",
  "stackTrace": "Stack trace (development only)",
  "traceId": "unique-trace-id",
  "timestamp": "2025-12-21T14:26:54Z"
}
```

**Exception Mappings**:

- `ArgumentNullException`, `ArgumentException` → 400 Bad Request
- `KeyNotFoundException` → 404 Not Found
- `UnauthorizedAccessException` → 401 Unauthorized
- `InvalidOperationException` → 409 Conflict
- All other exceptions → 500 Internal Server Error

### 2. Request Logging Middleware

**Purpose**: Logs all HTTP requests and responses with timing information.

**Location**: `{ServiceName}/Middleware/RequestLoggingMiddleware.cs`

**Features**:

- Logs request method, path, and client IP
- Tracks request duration
- Adds unique request ID to response headers
- Logs response status code and elapsed time

**Headers Added**:

- `X-Request-Id`: Unique identifier for the request

**Log Format**:

```
HTTP GET /api/products started. Request ID: abc123, Client IP: 192.168.1.1
HTTP GET /api/products completed with 200 in 45ms. Request ID: abc123
```

### 3. Security Headers Middleware

**Purpose**: Adds security headers to all HTTP responses.

**Location**: `{ServiceName}/Middleware/SecurityHeadersMiddleware.cs`

**Implemented in**: UserService, ProductCatalogService

**Headers Added**:

- `X-Content-Type-Options: nosniff` - Prevents MIME type sniffing
- `X-Frame-Options: DENY` - Prevents clickjacking attacks
- `X-XSS-Protection: 1; mode=block` - Enables XSS filter
- `Referrer-Policy: strict-origin-when-cross-origin` - Controls referrer information
- `Content-Security-Policy` - Helps prevent XSS and injection attacks
- `Strict-Transport-Security` - Enforces HTTPS (when using HTTPS)
- `Permissions-Policy` - Controls browser features

### 4. Correlation ID Middleware

**Purpose**: Manages correlation IDs across distributed requests for tracing.

**Location**: `ApiGateway/Middleware/CorrelationIdMiddleware.cs`, `ProductCatalogService/Middleware/CorrelationIdMiddleware.cs`

**Implemented in**: ApiGateway, ProductCatalogService

**Features**:

- Generates or accepts correlation ID from request header
- Adds correlation ID to response headers
- Stores correlation ID in HttpContext for downstream use

**Headers**:

- `X-Correlation-Id`: Unique identifier that follows a request through all services

### 5. Rate Limiting Middleware

**Purpose**: Prevents API abuse by limiting request rates.

**Location**: `ProductCatalogService/Middleware/RateLimitingMiddleware.cs`

**Implemented in**: ProductCatalogService

**Features**:

- Configurable request limits and time windows
- Per-client rate limiting (by IP address)
- Automatic cleanup of old entries
- Skips health checks and Hangfire dashboard
- Returns 429 Too Many Requests when limit exceeded

**Configuration**:

```json
{
  "RateLimiting": {
    "RequestLimit": 100,
    "TimeWindowMinutes": 1
  }
}
```

**Headers Added**:

- `X-RateLimit-Limit`: Maximum requests allowed
- `X-RateLimit-Remaining`: Remaining requests in current window
- `X-RateLimit-Reset`: When the rate limit resets
- `Retry-After`: Seconds to wait before retrying (when rate limited)

### 6. Performance Monitoring Middleware

**Purpose**: Tracks and logs performance metrics for requests.

**Location**: `ProductCatalogService/Middleware/PerformanceMonitoringMiddleware.cs`

**Implemented in**: ProductCatalogService

**Features**:

- Tracks request duration
- Logs slow requests (configurable threshold)
- Provides performance insights

### 7. API Version Middleware

**Purpose**: Adds API version information to response headers.

**Location**: `ProductCatalogService/Middleware/ApiVersionMiddleware.cs`

**Implemented in**: ProductCatalogService

**Features**:

- Adds API version to response headers
- Supports version detection from multiple sources

### 8. Request/Response Logging Middleware

**Purpose**: Detailed logging of request and response bodies (for debugging).

**Location**: `ProductCatalogService/Middleware/RequestResponseLoggingMiddleware.cs`

**Implemented in**: ProductCatalogService (optional, disabled by default)

**Features**:

- Logs request and response bodies
- Useful for debugging
- Should only be enabled in development

## Middleware Pipeline Order

The order in which middleware is registered is critical. Here's the recommended order:

1. **Exception Handling** - Must be first to catch all errors
2. **Correlation ID** - Early in pipeline for request tracking
3. **Security Headers** - Add security headers early
4. **Performance Monitoring** - Track performance
5. **Rate Limiting** - Prevent abuse before processing
6. **Request Logging** - Log requests
7. **CORS** - Handle cross-origin requests
8. **Authentication** - Verify user identity
9. **Authorization** - Check user permissions
10. **Application Logic** - Controllers and endpoints

## Service-Specific Implementation

### ApiGateway

**Middleware**:

- Exception Handling
- Correlation ID
- Request Logging

**Pipeline**:

```csharp
app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseMiddleware<CorrelationIdMiddleware>();
app.UseMiddleware<RequestLoggingMiddleware>();
app.UseCors("CorsPolicy");
// ... Ocelot middleware
```

### UserService

**Middleware**:

- Exception Handling
- Security Headers
- Request Logging

**Additional Features**:

- Built-in ASP.NET Core Rate Limiting (using `AddRateLimiter`)
- Multiple rate limiting policies (auth, refresh, profile, validate, concurrent)

**Pipeline**:

```csharp
app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseMiddleware<SecurityHeadersMiddleware>();
app.UseMiddleware<RequestLoggingMiddleware>();
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();
```

### ProductCatalogService

**Middleware** (Most comprehensive):

- Correlation ID
- Exception Handling
- Security Headers
- Performance Monitoring
- Rate Limiting
- Request Logging
- API Versioning
- Request/Response Logging (optional)

**Pipeline**:

```csharp
app.UseCustomMiddleware(); // Extension method that applies all middleware in order
app.UseAuthorization();
```

### OrderService

**Middleware**:

- Exception Handling
- Request Logging

**Pipeline**:

```csharp
app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseMiddleware<RequestLoggingMiddleware>();
app.UseAuthentication();
app.UseAuthorization();
```

### ShoppingCartService

**Middleware**:

- Exception Handling
- Request Logging

**Pipeline**:

```csharp
app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseMiddleware<RequestLoggingMiddleware>();
app.UseAuthentication();
app.UseAuthorization();
```

### PaymentService

**Middleware**:

- Exception Handling
- Request Logging

**Pipeline**:

```csharp
app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseMiddleware<RequestLoggingMiddleware>();
app.UseAuthorization();
```

### NotificationService

**Middleware**:

- Exception Handling
- Request Logging

**Pipeline**:

```csharp
app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseMiddleware<RequestLoggingMiddleware>();
app.UseCors("CorsPolicy");
```

## Best Practices

### 1. Middleware Order

Always maintain the correct order of middleware. Exception handling should be first, and authentication/authorization should come after logging and security headers.

### 2. Performance

- Keep middleware lightweight
- Avoid heavy computations in middleware
- Use async/await properly
- Consider caching where appropriate

### 3. Error Handling

- Always catch exceptions in middleware
- Provide meaningful error messages
- Use appropriate HTTP status codes
- Include trace IDs for debugging

### 4. Logging

- Log at appropriate levels (Information, Warning, Error)
- Include context (request ID, user ID, etc.)
- Don't log sensitive information (passwords, tokens)
- Use structured logging

### 5. Security

- Always add security headers
- Validate input early in the pipeline
- Use HTTPS in production
- Implement rate limiting to prevent abuse

### 6. Testing

- Test middleware independently
- Test middleware order
- Test error scenarios
- Test performance impact

## Configuration

### Rate Limiting Configuration

Add to `appsettings.json`:

```json
{
  "RateLimiting": {
    "RequestLimit": 100,
    "TimeWindowMinutes": 1
  }
}
```

### Performance Monitoring Configuration

Add to `appsettings.json`:

```json
{
  "PerformanceMonitoring": {
    "SlowRequestThresholdMs": 1000
  }
}
```

## Extending Middleware

To add new middleware:

1. Create a new class in the `Middleware` folder
2. Implement the `InvokeAsync` method
3. Register in `Program.cs` in the appropriate order
4. Add configuration if needed
5. Document the middleware

Example:

```csharp
public class CustomMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<CustomMiddleware> _logger;

    public CustomMiddleware(RequestDelegate next, ILogger<CustomMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Before request processing

        await _next(context);

        // After request processing
    }
}
```

## Troubleshooting

### Middleware Not Executing

- Check the order of middleware registration
- Ensure middleware is registered before `app.Run()`
- Verify the middleware class is public

### Performance Issues

- Profile middleware execution time
- Check for blocking calls
- Ensure async/await is used correctly
- Consider caching

### Error Handling Not Working

- Ensure exception handling middleware is first
- Check that exceptions are not caught elsewhere
- Verify error response format

## References

- [ASP.NET Core Middleware](https://docs.microsoft.com/en-us/aspnet/core/fundamentals/middleware/)
- [Custom Middleware](https://docs.microsoft.com/en-us/aspnet/core/fundamentals/middleware/write)
- [Rate Limiting](https://docs.microsoft.com/en-us/aspnet/core/performance/rate-limit)
- [Security Headers](https://owasp.org/www-project-secure-headers/)
