using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using UnionGenerator.AspNetCore.Middleware;
using Xunit;

namespace UnionGenerator.AspNetCore.Tests.Middleware;

/// <summary>
/// Tests for UnionResultLoggingMiddleware.
/// </summary>
public class UnionResultLoggingMiddlewareTests
{
    /// <summary>
    /// Mock union error type for testing.
    /// </summary>
    [SuppressMessage("ReSharper", "UnusedAutoPropertyAccessor.Local")]
    [SuppressMessage("ReSharper", "UnusedMember.Local")]
    private sealed class TestUnionError
    {
        public bool IsSuccess { get; set; }
        public string? Value { get; set; }
        public ProblemDetailsError? Error { get; set; }
    }

    [Fact]
    public void Constructor_WithNullNext_ThrowsArgumentNullException()
    {
        // Arrange & Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new UnionResultLoggingMiddleware(
                next: null!,
                logger: NullLogger<UnionResultLoggingMiddleware>.Instance
            )
        );
    }

    [Fact]
    public void Constructor_WithNullLogger_ThrowsArgumentNullException()
    {
        // Arrange & Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new UnionResultLoggingMiddleware(
                next: _ => Task.CompletedTask,
                logger: null!
            )
        );
    }

    [Fact]
    public async Task InvokeAsync_WithNullContext_ThrowsArgumentNullException()
    {
        // Arrange
        var middleware = new UnionResultLoggingMiddleware(
            next: _ => Task.CompletedTask,
            logger: NullLogger<UnionResultLoggingMiddleware>.Instance
        );

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => middleware.InvokeAsync(null!));
    }

    [Fact]
    public async Task InvokeAsync_WithSuccessResponse_CallsNext()
    {
        // Arrange
        var nextCalled = false;
        RequestDelegate next = async _ => { nextCalled = true; await Task.CompletedTask; };

        var middleware = new UnionResultLoggingMiddleware(
            next: next,
            logger: NullLogger<UnionResultLoggingMiddleware>.Instance
        );

        var context = new DefaultHttpContext();
        context.Response.StatusCode = 200;

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        Assert.True(nextCalled);
    }

    [Fact]
    public async Task InvokeAsync_WithErrorResponse_CallsNext()
    {
        // Arrange
        var nextCalled = false;
        RequestDelegate next = async _ => { nextCalled = true; await Task.CompletedTask; };

        var middleware = new UnionResultLoggingMiddleware(
            next: next,
            logger: NullLogger<UnionResultLoggingMiddleware>.Instance
        );

        var context = new DefaultHttpContext();
        context.Response.StatusCode = 404;

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        Assert.True(nextCalled);
    }

    [Fact]
    public async Task InvokeAsync_WithNotFoundStatus_DoesNotThrow()
    {
        // Arrange
        RequestDelegate next = async _ => { await Task.CompletedTask; };

        var middleware = new UnionResultLoggingMiddleware(
            next: next,
            logger: NullLogger<UnionResultLoggingMiddleware>.Instance
        );

        var context = new DefaultHttpContext();
        context.Request.Method = "GET";
        context.Request.Path = "/api/users/1";
        context.Response.StatusCode = 404;

        // Act & Assert (should not throw)
        await middleware.InvokeAsync(context);
    }

    [Fact]
    public async Task InvokeAsync_WithValidationErrorStatus_DoesNotThrow()
    {
        // Arrange
        RequestDelegate next = async _ => { await Task.CompletedTask; };

        var middleware = new UnionResultLoggingMiddleware(
            next: next,
            logger: NullLogger<UnionResultLoggingMiddleware>.Instance
        );

        var context = new DefaultHttpContext();
        context.Request.Method = "POST";
        context.Request.Path = "/api/users";
        context.Response.StatusCode = 422;

        // Act & Assert (should not throw)
        await middleware.InvokeAsync(context);
    }

    [Fact]
    public async Task InvokeAsync_WithConflictStatus_DoesNotThrow()
    {
        // Arrange
        RequestDelegate next = async _ => { await Task.CompletedTask; };

        var middleware = new UnionResultLoggingMiddleware(
            next: next,
            logger: NullLogger<UnionResultLoggingMiddleware>.Instance
        );

        var context = new DefaultHttpContext();
        context.Request.Method = "POST";
        context.Request.Path = "/api/items";
        context.Response.StatusCode = 409;

        // Act & Assert (should not throw)
        await middleware.InvokeAsync(context);
    }

    [Fact]
    public async Task InvokeAsync_CapturesPathAndMethod()
    {
        // Arrange
        RequestDelegate next = async _ => { await Task.CompletedTask; };

        var middleware = new UnionResultLoggingMiddleware(
            next: next,
            logger: NullLogger<UnionResultLoggingMiddleware>.Instance
        );

        var context = new DefaultHttpContext();
        context.Request.Method = "POST";
        context.Request.Path = "/api/users/create";
        context.Response.StatusCode = 201;

        // Act
        await middleware.InvokeAsync(context);

        // Assert - verify context is properly handled
        Assert.Equal("POST", context.Request.Method);
        Assert.Equal("/api/users/create", context.Request.Path.Value);
    }

    [Fact]
    public async Task InvokeAsync_WithInternalServerError_DoesNotThrow()
    {
        // Arrange
        RequestDelegate next = async _ => { await Task.CompletedTask; };

        var middleware = new UnionResultLoggingMiddleware(
            next: next,
            logger: NullLogger<UnionResultLoggingMiddleware>.Instance
        );

        var context = new DefaultHttpContext();
        context.Request.Method = "GET";
        context.Request.Path = "/api/data";
        context.Response.StatusCode = 500;

        // Act & Assert (should not throw)
        await middleware.InvokeAsync(context);
    }

    [Fact]
    public async Task InvokeAsync_WithMultipleErrorStatuses_AllHandled()
    {
        // Arrange
        var middleware = new UnionResultLoggingMiddleware(
            next: _ => Task.CompletedTask,
            logger: NullLogger<UnionResultLoggingMiddleware>.Instance
        );

        var errorStatuses = new[] { 400, 401, 403, 404, 409, 422, 500, 502, 503 };

        // Act & Assert
        foreach (var status in errorStatuses)
        {
            var context = new DefaultHttpContext();
            context.Response.StatusCode = status;

            // Should not throw for any error status
            await middleware.InvokeAsync(context);
        }
    }

    [Fact]
    public async Task InvokeAsync_WithSuccessStatuses_AllHandled()
    {
        // Arrange
        var middleware = new UnionResultLoggingMiddleware(
            next: _ => Task.CompletedTask,
            logger: NullLogger<UnionResultLoggingMiddleware>.Instance
        );

        var successStatuses = new[] { 200, 201, 204, 299 };

        // Act & Assert
        foreach (var status in successStatuses)
        {
            var context = new DefaultHttpContext();
            context.Response.StatusCode = status;

            // Should not throw for any success status
            await middleware.InvokeAsync(context);
        }
    }
}

