using FluentAssertions;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using UnionGenerator.AspNetCore;
using UnionGenerator.FluentValidation.Filters;

namespace UnionGenerator.FluentValidation.Tests;

/// <summary>
/// Tests for FluentValidationFilter - automatic validation filter for action parameters.
/// </summary>
public class FluentValidationFilterTests
{
    /// <summary>
    /// Tests that the filter validates parameters with registered validators.
    /// </summary>
    [Fact]
    public async Task OnActionExecutionAsync_WithValidParameter_PassesThrough()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddScoped<IValidator<TestDto>, TestDtoValidator>();
        var provider = services.BuildServiceProvider();

        var filter = new FluentValidationFilter(provider);
        var testDto = new TestDto { Name = "Valid", Age = 25 };
        
        var context = CreateActionExecutingContext(testDto);

        var nextCalled = false;
        var next = new ActionExecutionDelegate(async () =>
        {
            nextCalled = true;
            return new ActionExecutedContext(context, [], null);
        });

        // Act
        await filter.OnActionExecutionAsync(context, next);

        // Assert
        nextCalled.Should().BeTrue();
        context.Result.Should().BeNull();
    }

    /// <summary>
    /// Tests that the filter blocks invalid parameters.
    /// </summary>
    [Fact]
    public async Task OnActionExecutionAsync_WithInvalidParameter_ReturnsProblemResult()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddScoped<IValidator<TestDto>, TestDtoValidator>();
        var provider = services.BuildServiceProvider();

        var filter = new FluentValidationFilter(provider);
        var invalidDto = new TestDto { Name = "", Age = -1 }; // Invalid
        
        var context = CreateActionExecutingContext(invalidDto);

        var nextCalled = false;
        var next = new ActionExecutionDelegate(async () =>
        {
            nextCalled = true;
            return new ActionExecutedContext(context, [], null);
        });

        // Act
        await filter.OnActionExecutionAsync(context, next);

        // Assert
        nextCalled.Should().BeFalse();
        context.Result.Should().NotBeNull();
        context.Result.Should().BeOfType<ObjectResult>();
    }

    /// <summary>
    /// Tests that the filter ignores parameters without validators.
    /// </summary>
    [Fact]
    public async Task OnActionExecutionAsync_WithoutValidator_PassesThrough()
    {
        // Arrange
        var services = new ServiceCollection();
        var provider = services.BuildServiceProvider(); // No validators registered

        var filter = new FluentValidationFilter(provider);
        var testDto = new TestDto { Name = "", Age = -1 }; // Would be invalid if validator existed
        
        var context = CreateActionExecutingContext(testDto);

        var nextCalled = false;
        var next = new ActionExecutionDelegate(async () =>
        {
            nextCalled = true;
            return new ActionExecutedContext(context, [], null);
        });

        // Act
        await filter.OnActionExecutionAsync(context, next);

        // Assert
        nextCalled.Should().BeTrue();
        context.Result.Should().BeNull();
    }

    /// <summary>
    /// Tests that the filter ignores null parameters.
    /// </summary>
    [Fact]
    public async Task OnActionExecutionAsync_WithNullParameter_PassesThrough()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddScoped<IValidator<TestDto>, TestDtoValidator>();
        var provider = services.BuildServiceProvider();

        var filter = new FluentValidationFilter(provider);
        
        var context = CreateActionExecutingContextWithNull();

        var nextCalled = false;
        var next = new ActionExecutionDelegate(async () =>
        {
            nextCalled = true;
            return new ActionExecutedContext(context, [], null);
        });

        // Act
        await filter.OnActionExecutionAsync(context, next);

        // Assert
        nextCalled.Should().BeTrue();
        context.Result.Should().BeNull();
    }

    /// <summary>
    /// Tests that the filter throws when service provider is null.
    /// </summary>
    [Fact]
    public void Constructor_WithNullServiceProvider_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(
            () => new FluentValidationFilter(null!)
        );
    }

    /// <summary>
    /// Tests that validation results are correctly formatted as ProblemDetails.
    /// </summary>
    [Fact]
    public async Task OnActionExecutionAsync_ValidationFailure_FormattsAsProblemDetails()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddScoped<IValidator<TestDto>, TestDtoValidator>();
        var provider = services.BuildServiceProvider();

        var filter = new FluentValidationFilter(provider);
        var invalidDto = new TestDto { Name = "", Age = -1 };
        
        var context = CreateActionExecutingContext(invalidDto);

        var next = new ActionExecutionDelegate(async () =>
        {
            return new ActionExecutedContext(context, [], null);
        });

        // Act
        await filter.OnActionExecutionAsync(context, next);

        // Assert
        context.Result.Should().NotBeNull();
        if (context.Result is ObjectResult objectResult)
        {
            objectResult.Value.Should().BeAssignableTo<ProblemDetailsError>();
        }
    }

    /// <summary>
    /// Tests that the filter handles multiple parameters.
    /// </summary>
    [Fact]
    public async Task OnActionExecutionAsync_WithMultipleParameters_ValidatesAll()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddScoped<IValidator<TestDto>, TestDtoValidator>();
        services.AddScoped<IValidator<SecondDto>, SecondDtoValidator>();
        var provider = services.BuildServiceProvider();

        var filter = new FluentValidationFilter(provider);
        
        var context = CreateActionExecutingContextWithMultiple(
            new TestDto { Name = "Valid", Age = 25 },
            new SecondDto { Email = "valid@test.com" }
        );

        var nextCalled = false;
        var next = new ActionExecutionDelegate(async () =>
        {
            nextCalled = true;
            return new ActionExecutedContext(context, [], null);
        });

        // Act
        await filter.OnActionExecutionAsync(context, next);

        // Assert
        nextCalled.Should().BeTrue();
        context.Result.Should().BeNull();
    }

    /// <summary>
    /// Tests that the filter stops at first validation failure with multiple parameters.
    /// </summary>
    [Fact]
    public async Task OnActionExecutionAsync_WithMultipleInvalidParameters_StopsAtFirst()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddScoped<IValidator<TestDto>, TestDtoValidator>();
        services.AddScoped<IValidator<SecondDto>, SecondDtoValidator>();
        var provider = services.BuildServiceProvider();

        var filter = new FluentValidationFilter(provider);
        
        var context = CreateActionExecutingContextWithMultiple(
            new TestDto { Name = "", Age = -1 }, // Invalid
            new SecondDto { Email = "invalid" } // Also invalid
        );

        var nextCalled = false;
        var next = new ActionExecutionDelegate(async () =>
        {
            nextCalled = true;
            return new ActionExecutedContext(context, [], null);
        });

        // Act
        await filter.OnActionExecutionAsync(context, next);

        // Assert
        nextCalled.Should().BeFalse();
        context.Result.Should().NotBeNull();
    }

    #region Test Helpers

    private class TestDto
    {
        public string Name { get; set; } = "";
        public int Age { get; set; }
    }

    private class TestDtoValidator : AbstractValidator<TestDto>
    {
        public TestDtoValidator()
        {
            RuleFor(x => x.Name).NotEmpty().WithMessage("Name is required.");
            RuleFor(x => x.Age).GreaterThan(0).WithMessage("Age must be greater than 0.");
        }
    }

    private class SecondDto
    {
        public string Email { get; set; } = "";
    }

    private class SecondDtoValidator : AbstractValidator<SecondDto>
    {
        public SecondDtoValidator()
        {
            RuleFor(x => x.Email).EmailAddress().WithMessage("Email must be valid.");
        }
    }

    private static ActionExecutingContext CreateActionExecutingContext(TestDto dto)
    {
        var httpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext();
        var actionContext = new ActionContext(
            httpContext,
            new RouteData(),
            new ActionDescriptor { Parameters = new List<ParameterDescriptor>() }
        );

        var context = new ActionExecutingContext(
            actionContext,
            new List<IFilterMetadata>(),
            new Dictionary<string, object?> { { "dto", dto } },
            null
        );

        var paramDescriptor = new ParameterDescriptor { Name = "dto" };
        ((List<ParameterDescriptor>)actionContext.ActionDescriptor.Parameters).Add(paramDescriptor);

        return context;
    }

    private static ActionExecutingContext CreateActionExecutingContextWithNull()
    {
        var httpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext();
        var actionContext = new ActionContext(
            httpContext,
            new RouteData(),
            new ActionDescriptor { Parameters = new List<ParameterDescriptor>() }
        );

        var context = new ActionExecutingContext(
            actionContext,
            new List<IFilterMetadata>(),
            new Dictionary<string, object?> { { "dto", null } },
            null
        );

        var paramDescriptor = new ParameterDescriptor { Name = "dto" };
        ((List<ParameterDescriptor>)actionContext.ActionDescriptor.Parameters).Add(paramDescriptor);

        return context;
    }

    private static ActionExecutingContext CreateActionExecutingContextWithMultiple(TestDto dto, SecondDto secondDto)
    {
        var httpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext();
        var actionContext = new ActionContext(
            httpContext,
            new RouteData(),
            new ActionDescriptor { Parameters = new List<ParameterDescriptor>() }
        );

        var context = new ActionExecutingContext(
            actionContext,
            new List<IFilterMetadata>(),
            new Dictionary<string, object?> 
            { 
                { "dto", dto },
                { "secondDto", secondDto }
            },
            null
        );

        var param1 = new ParameterDescriptor { Name = "dto" };
        var param2 = new ParameterDescriptor { Name = "secondDto" };
        var paramList = (List<ParameterDescriptor>)actionContext.ActionDescriptor.Parameters;
        paramList.Add(param1);
        paramList.Add(param2);

        return context;
    }

    #endregion
}

