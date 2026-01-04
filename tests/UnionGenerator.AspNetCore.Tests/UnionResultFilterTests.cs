using System.Collections.Generic;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using UnionGenerator.AspNetCore.Caching;
using UnionGenerator.AspNetCore.Filters;
using Xunit;

namespace UnionGenerator.AspNetCore.Tests;

/// <summary>
/// Tests for UnionResultFilter - MVC action filter for automatic union result conversion.
/// </summary>
public class UnionResultFilterTests
{
    /// <summary>
    /// Initializes a new instance of the <see cref="UnionResultFilterTests"/> class.
    /// Clears the cache before each test to ensure test isolation.
    /// </summary>
    public UnionResultFilterTests()
    {
        // Clear cache to ensure test isolation
        UnionPropertyCache.Default.Clear();
    }
    /// <summary>
    /// Tests that the filter converts successful union result to OkObjectResult.
    /// </summary>
    [Fact]
    public void OnActionExecuted_WithSuccessfulUnionResult_ConvertsToOkObjectResult()
    {
        // Arrange
        var testResult = new TestUnionResult { IsSuccess = true, Value = "Success!" };
        var objectResult = new ObjectResult(testResult);
        
        var httpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext();
        var actionContext = new ActionContext(httpContext, new RouteData(), new ActionDescriptor());
        var context = new ActionExecutedContext(
            actionContext,
            new List<IFilterMetadata>(),
            null
        )
        {
            Result = objectResult
        };

        var filter = new UnionResultFilter();

        // Act
        filter.OnActionExecuted(context);

        // Assert
        context.Result.Should().BeOfType<OkObjectResult>();
        var okResult = (OkObjectResult)context.Result;
        okResult.Value.Should().Be("Success!");
    }

    /// <summary>
    /// Tests that the filter converts error union result to ProblemObjectResult.
    /// </summary>
    [Fact]
    public void OnActionExecuted_WithErrorUnionResult_ConvertsToProblemResult()
    {
        // Arrange
        var errorDetails = ProblemDetailsErrorFactory.BadRequest(
            "/test",
            "Invalid input"
        );

        var testResult = new TestUnionResult { IsSuccess = false, ErrorValue = errorDetails };
        var objectResult = new ObjectResult(testResult);
        
        var httpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext();
        var actionContext = new ActionContext(httpContext, new RouteData(), new ActionDescriptor());
        var context = new ActionExecutedContext(
            actionContext,
            new List<IFilterMetadata>(),
            null
        )
        {
            Result = objectResult
        };

        var filter = new UnionResultFilter();

        // Act
        filter.OnActionExecuted(context);

        // Assert
        context.Result.Should().BeOfType<ObjectResult>();
        var result = (ObjectResult)context.Result;
        result.StatusCode.Should().Be(400);
    }

    /// <summary>
    /// Tests that the filter ignores non-union results.
    /// </summary>
    [Fact]
    public void OnActionExecuted_WithNonUnionResult_LeavesResultUnchanged()
    {
        // Arrange
        var objectResult = new ObjectResult(new { Message = "Hello" });
        var originalResult = objectResult;
        
        var httpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext();
        var actionContext = new ActionContext(httpContext, new RouteData(), new ActionDescriptor());
        var context = new ActionExecutedContext(
            actionContext,
            new List<IFilterMetadata>(),
            null
        )
        {
            Result = objectResult
        };

        var filter = new UnionResultFilter();

        // Act
        filter.OnActionExecuted(context);

        // Assert
        context.Result.Should().Be(originalResult);
    }

    /// <summary>
    /// Tests that the filter ignores null results.
    /// </summary>
    [Fact]
    public void OnActionExecuted_WithNullResult_LeavesResultUnchanged()
    {
        // Arrange
        var objectResult = new ObjectResult(null);
        var originalResult = objectResult;
        
        var httpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext();
        var actionContext = new ActionContext(httpContext, new RouteData(), new ActionDescriptor());
        var context = new ActionExecutedContext(
            actionContext,
            new List<IFilterMetadata>(),
            null
        )
        {
            Result = objectResult
        };

        var filter = new UnionResultFilter();

        // Act
        filter.OnActionExecuted(context);

        // Assert
        context.Result.Should().Be(originalResult);
    }

    /// <summary>
    /// Tests that the filter ignores non-ObjectResult results.
    /// </summary>
    [Fact]
    public void OnActionExecuted_WithNonObjectResult_LeavesResultUnchanged()
    {
        // Arrange
        IActionResult originalResult = new OkResult();
        
        var httpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext();
        var actionContext = new ActionContext(httpContext, new RouteData(), new ActionDescriptor());
        var context = new ActionExecutedContext(
            actionContext,
            new List<IFilterMetadata>(),
            null
        )
        {
            Result = originalResult
        };

        var filter = new UnionResultFilter();

        // Act
        filter.OnActionExecuted(context);

        // Assert
        context.Result.Should().Be(originalResult);
    }

    /// <summary>
    /// Tests that OnActionExecuting does nothing.
    /// </summary>
    [Fact]
    public void OnActionExecuting_DoesNothing()
    {
        // Arrange
        var httpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext();
        var actionContext = new ActionContext(httpContext, new RouteData(), new ActionDescriptor());
        var context = new ActionExecutingContext(
            actionContext,
            new List<IFilterMetadata>(),
            new Dictionary<string, object?>(),
            null
        );

        var filter = new UnionResultFilter();

        // Act & Assert (should not throw)
        filter.OnActionExecuting(context);
    }

    /// <summary>
    /// Tests that the filter handles union types with IsOk property.
    /// </summary>
    [Fact]
    public void OnActionExecuted_WithIsOkProperty_ConvertsCorrectly()
    {
        // Arrange
        var testResult = new TestUnionResultWithIsOk { IsOk = true, Value = "Data" };
        var objectResult = new ObjectResult(testResult);
        
        var httpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext();
        var actionContext = new ActionContext(httpContext, new RouteData(), new ActionDescriptor());
        var context = new ActionExecutedContext(
            actionContext,
            new List<IFilterMetadata>(),
            null
        )
        {
            Result = objectResult
        };

        var filter = new UnionResultFilter();

        // Act
        filter.OnActionExecuted(context);

        // Assert
        context.Result.Should().BeOfType<OkObjectResult>();
    }

    /// <summary>
    /// Tests that the filter handles union types with IsSome property.
    /// </summary>
    [Fact]
    public void OnActionExecuted_WithIsSomeProperty_ConvertsCorrectly()
    {
        // Arrange
        var testResult = new TestUnionResultWithIsSome { IsSome = true, Value = "Data" };
        var objectResult = new ObjectResult(testResult);
        
        var httpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext();
        var actionContext = new ActionContext(httpContext, new RouteData(), new ActionDescriptor());
        var context = new ActionExecutedContext(
            actionContext,
            new List<IFilterMetadata>(),
            null
        )
        {
            Result = objectResult
        };

        var filter = new UnionResultFilter();

        // Act
        filter.OnActionExecuted(context);

        // Assert
        context.Result.Should().BeOfType<OkObjectResult>();
    }

    /// <summary>
    /// Tests that the filter gracefully handles conversion errors.
    /// </summary>
    [Fact]
    public void OnActionExecuted_WithConversionError_LeavesResultUnchanged()
    {
        // Arrange - create a malformed union-like type that will cause conversion issues
        var testResult = new TestUnionResultMalformed { IsSuccess = true };
        var objectResult = new ObjectResult(testResult);
        var originalResult = objectResult;
        
        var httpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext();
        var actionContext = new ActionContext(httpContext, new RouteData(), new ActionDescriptor());
        var context = new ActionExecutedContext(
            actionContext,
            new List<IFilterMetadata>(),
            null
        )
        {
            Result = objectResult
        };

        var filter = new UnionResultFilter();

        // Act
        filter.OnActionExecuted(context);

        // Assert - should return original result when conversion fails
        context.Result.Should().Be(originalResult);
    }

    #region Test Helpers

    /// <summary>
    /// Test helper class simulating a union result type.
    /// </summary>
    private class TestUnionResult
    {
        public bool IsSuccess { get; set; }
        public string? Value { get; set; }
        public ProblemDetailsError? ErrorValue { get; set; }
    }

    /// <summary>
    /// Test helper class with IsOk property.
    /// </summary>
    private class TestUnionResultWithIsOk
    {
        public bool IsOk { get; set; }
        public string? Value { get; set; }
        public ProblemDetailsError? Error { get; set; }
    }

    /// <summary>
    /// Test helper class with IsSome property.
    /// </summary>
    private class TestUnionResultWithIsSome
    {
        public bool IsSome { get; set; }
        public string? Value { get; set; }
        public ProblemDetailsError? NoneValue { get; set; }
    }

    /// <summary>
    /// Malformed test helper class for error scenarios.
    /// </summary>
    private class TestUnionResultMalformed
    {
        public bool IsSuccess { get; set; }
        // Missing Value property - intentionally malformed
    }

    #endregion
}

