using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using UnionGenerator.FluentValidation.Extensions;
using UnionGenerator.FluentValidation.Filters;

namespace UnionGenerator.FluentValidation.Tests;

/// <summary>
/// Tests for CancellationToken support in FluentValidation integration.
/// </summary>
/// <remarks>
/// These tests verify that CancellationToken is properly propagated through:
/// - ValidationResultExtensions methods
/// - FluentValidationFilter action filter
/// - Async validation pipelines
/// </remarks>
public class CancellationTokenTests
{
    /// <summary>
    /// Test DTO for validation.
    /// </summary>
    private sealed record TestDto(string Email, int Age, string Username);

    /// <summary>
    /// Test validator that supports async validation with CancellationToken.
    /// </summary>
    private sealed class TestDtoValidator : AbstractValidator<TestDto>
    {
        public TestDtoValidator()
        {
            RuleFor(x => x.Email)
                .NotEmpty().WithMessage("Email is required.")
                .EmailAddress().WithMessage("Email must be valid.");

            RuleFor(x => x.Age)
                .GreaterThanOrEqualTo(18).WithMessage("Age must be at least 18.");

            RuleFor(x => x.Username)
                .NotEmpty().WithMessage("Username is required.")
                .Length(3, 20).WithMessage("Username must be between 3 and 20 characters.");
        }
    }

    /// <summary>
    /// Async validator that explicitly checks CancellationToken.
    /// </summary>
    private sealed class AsyncTestDtoValidator : AbstractValidator<TestDto>
    {
        public bool CancellationChecked { get; private set; }

        public AsyncTestDtoValidator()
        {
            RuleFor(x => x.Email)
                .MustAsync(async (email, ct) =>
                {
                    // Simulate async work
                    await Task.Delay(10, ct);
                    CancellationChecked = true;
                    ct.ThrowIfCancellationRequested();
                    return !string.IsNullOrEmpty(email);
                })
                .WithMessage("Email is required.");
        }
    }

    #region ToProblemDetailsError with CancellationToken (Sync Overload)

    [Fact]
    public void ToProblemDetailsError_WithCancellationToken_WhenNotCancelled_ReturnsError()
    {
        // Arrange
        var validator = new TestDtoValidator();
        var dto = new TestDto(Email: "", Age: 15, Username: "ab");
        var validationResult = validator.Validate(dto);
        var instance = "/api/test";
        var cts = new CancellationTokenSource();

        // Act
        var error = validationResult.ToProblemDetailsError(instance, cts.Token);

        // Assert
        Assert.NotNull(error);
        Assert.Equal(400, error.Status);
        Assert.Equal(instance, error.Instance);
        Assert.NotNull(error.Errors);
        Assert.Equal(3, error.Errors.Count);
    }

    [Fact]
    public void ToProblemDetailsError_WithCancelledToken_ThrowsOperationCanceledException()
    {
        // Arrange
        var validator = new TestDtoValidator();
        var dto = new TestDto(Email: "", Age: 15, Username: "ab");
        var validationResult = validator.Validate(dto);
        var instance = "/api/test";
        var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert
        Assert.Throws<OperationCanceledException>(() =>
            validationResult.ToProblemDetailsError(instance, cts.Token));
    }

    [Fact]
    public void ToProblemDetailsError_WithDefaultToken_WorksCorrectly()
    {
        // Arrange
        var validator = new TestDtoValidator();
        var dto = new TestDto(Email: "", Age: 15, Username: "ab");
        var validationResult = validator.Validate(dto);
        var instance = "/api/test";

        // Act
        var error = validationResult.ToProblemDetailsError(instance, CancellationToken.None);

        // Assert
        Assert.NotNull(error);
        Assert.Equal(400, error.Status);
    }

    #endregion

    #region ToProblemDetailsError with CancellationToken and Custom Detail

    [Fact]
    public void ToProblemDetailsError_WithDetailAndCancellationToken_WhenNotCancelled_ReturnsError()
    {
        // Arrange
        var validator = new TestDtoValidator();
        var dto = new TestDto(Email: "", Age: 15, Username: "ab");
        var validationResult = validator.Validate(dto);
        var instance = "/api/test";
        var detail = "Custom validation error.";
        var cts = new CancellationTokenSource();

        // Act
        var error = validationResult.ToProblemDetailsError(instance, detail, cts.Token);

        // Assert
        Assert.NotNull(error);
        Assert.Equal(400, error.Status);
        Assert.Equal(detail, error.Detail);
        Assert.Equal(instance, error.Instance);
    }

    [Fact]
    public void ToProblemDetailsError_WithDetailAndCancelledToken_ThrowsOperationCanceledException()
    {
        // Arrange
        var validator = new TestDtoValidator();
        var dto = new TestDto(Email: "", Age: 15, Username: "ab");
        var validationResult = validator.Validate(dto);
        var instance = "/api/test";
        var detail = "Custom validation error.";
        var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert
        Assert.Throws<OperationCanceledException>(() =>
            validationResult.ToProblemDetailsError(instance, detail, cts.Token));
    }

    #endregion

    #region ToProblemDetailsErrorIfInvalidAsync with CancellationToken

    [Fact]
    public async Task ToProblemDetailsErrorIfInvalidAsync_WithValidResult_ReturnNullWithoutCancelling()
    {
        // Arrange
        var validator = new TestDtoValidator();
        var dto = new TestDto(Email: "test@example.com", Age: 25, Username: "testuser");
        var instance = "/api/test";
        var cts = new CancellationTokenSource();

        // Act
        var error = await validator.ValidateAsync(dto, cts.Token)
            .ToProblemDetailsErrorIfInvalidAsync(instance, cts.Token);

        // Assert
        Assert.Null(error);
        Assert.False(cts.Token.IsCancellationRequested);
    }

    [Fact]
    public async Task ToProblemDetailsErrorIfInvalidAsync_WithInvalidResult_ReturnsErrorWithoutCancelling()
    {
        // Arrange
        var validator = new TestDtoValidator();
        var dto = new TestDto(Email: "", Age: 15, Username: "ab");
        var instance = "/api/test";
        var cts = new CancellationTokenSource();

        // Act
        var error = await validator.ValidateAsync(dto, cts.Token)
            .ToProblemDetailsErrorIfInvalidAsync(instance, cts.Token);

        // Assert
        Assert.NotNull(error);
        Assert.Equal(400, error.Status);
        Assert.False(cts.Token.IsCancellationRequested);
    }

    [Fact]
    public async Task ToProblemDetailsErrorIfInvalidAsync_WithCancelledBeforeValidation_ThrowsOperationCanceledException()
    {
        // Arrange
        var validator = new AsyncTestDtoValidator();
        var dto = new TestDto(Email: "test@example.com", Age: 25, Username: "testuser");
        var instance = "/api/test";
        var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert
        await Assert.ThrowsAsync<OperationCanceledException>(async () =>
            await validator.ValidateAsync(dto, cts.Token)
                .ToProblemDetailsErrorIfInvalidAsync(instance, cts.Token));
    }

    [Fact]
    public async Task ToProblemDetailsErrorIfInvalidAsync_WithCancelledDuringValidation_ThrowsOperationCanceledException()
    {
        // Arrange
        var validator = new AsyncTestDtoValidator();
        var dto = new TestDto(Email: "test@example.com", Age: 25, Username: "testuser");
        var instance = "/api/test";
        var cts = new CancellationTokenSource(millisecondsDelay: 5); // Cancel after 5ms

        // Act & Assert
        // TaskCanceledException inherits from OperationCanceledException, so we use ThrowsAnyAsync
        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
            await validator.ValidateAsync(dto, cts.Token)
                .ToProblemDetailsErrorIfInvalidAsync(instance, cts.Token));
    }

    [Fact]
    public async Task ToProblemDetailsErrorIfInvalidAsync_WithCancelledAfterValidation_ThrowsOperationCanceledException()
    {
        // Arrange
        var validator = new TestDtoValidator();
        var dto = new TestDto(Email: "", Age: 15, Username: "ab");
        var instance = "/api/test";
        var cts = new CancellationTokenSource();

        // Perform validation first (no cancellation)
        var validationResult = await validator.ValidateAsync(dto, CancellationToken.None);

        // Cancel after validation completes
        cts.Cancel();

        // Act & Assert
        await Assert.ThrowsAsync<OperationCanceledException>(async () =>
            await Task.Run(() => validationResult.ToProblemDetailsError(instance, cts.Token)));
    }

    #endregion

    #region FluentValidationFilter with CancellationToken

    [Fact]
    public async Task FluentValidationFilter_PropagatesCancellationToken_WhenValidationFails()
    {
        // Arrange
        var validator = new TestDtoValidator();
        var dto = new TestDto(Email: "", Age: 15, Username: "ab");

        var serviceProvider = new ServiceCollection()
            .AddSingleton<IValidator<TestDto>>(validator)
            .BuildServiceProvider();

        var filter = new FluentValidationFilter(serviceProvider);

        var httpContext = new DefaultHttpContext();
        httpContext.Request.Path = "/api/test";
        var cts = new CancellationTokenSource();
        cts.Cancel(); // Cancel before validation
        httpContext.RequestAborted = cts.Token;

        var actionDescriptor = new ActionDescriptor();
        actionDescriptor.Parameters = new List<ParameterDescriptor>
        {
            new ParameterDescriptor
            {
                Name = "dto",
                ParameterType = typeof(TestDto)
            }
        };

        var actionContext = new ActionContext(
            httpContext,
            new RouteData(),
            actionDescriptor);

        var actionExecutingContext = new ActionExecutingContext(
            actionContext,
            new List<IFilterMetadata>(),
            new Dictionary<string, object?> { { "dto", dto } },
            controller: null!);


        // Act & Assert
        await Assert.ThrowsAsync<OperationCanceledException>(async () =>
            await filter.OnActionExecutionAsync(actionExecutingContext, () =>
                Task.FromResult(new ActionExecutedContext(actionContext, new List<IFilterMetadata>(), null!))));
    }

    [Fact]
    public async Task FluentValidationFilter_WorksCorrectly_WhenTokenNotCancelled()
    {
        // Arrange
        var validator = new TestDtoValidator();
        var dto = new TestDto(Email: "", Age: 15, Username: "ab");

        var serviceProvider = new ServiceCollection()
            .AddSingleton<IValidator<TestDto>>(validator)
            .BuildServiceProvider();

        var filter = new FluentValidationFilter(serviceProvider);

        var httpContext = new DefaultHttpContext();
        httpContext.Request.Path = "/api/test";
        httpContext.RequestAborted = CancellationToken.None;

        var actionDescriptor = new ActionDescriptor();
        actionDescriptor.Parameters = new List<ParameterDescriptor>
        {
            new ParameterDescriptor
            {
                Name = "dto",
                ParameterType = typeof(TestDto)
            }
        };

        var actionContext = new ActionContext(
            httpContext,
            new RouteData(),
            actionDescriptor);

        var actionExecutingContext = new ActionExecutingContext(
            actionContext,
            new List<IFilterMetadata>(),
            new Dictionary<string, object?> { { "dto", dto } },
            controller: null!);


        // Act
        await filter.OnActionExecutionAsync(actionExecutingContext, () =>
            Task.FromResult(new ActionExecutedContext(actionContext, new List<IFilterMetadata>(), null!)));

        // Assert
        Assert.NotNull(actionExecutingContext.Result);
        var objectResult = Assert.IsType<ObjectResult>(actionExecutingContext.Result);
        Assert.Equal(400, objectResult.StatusCode);
    }

    [Fact]
    public async Task FluentValidationFilter_PassesThrough_WhenValidationSucceeds()
    {
        // Arrange
        var validator = new TestDtoValidator();
        var dto = new TestDto(Email: "test@example.com", Age: 25, Username: "testuser");

        var serviceProvider = new ServiceCollection()
            .AddSingleton<IValidator<TestDto>>(validator)
            .BuildServiceProvider();

        var filter = new FluentValidationFilter(serviceProvider);

        var httpContext = new DefaultHttpContext();
        httpContext.Request.Path = "/api/test";
        var cts = new CancellationTokenSource();
        httpContext.RequestAborted = cts.Token;

        var actionDescriptor = new ActionDescriptor();
        actionDescriptor.Parameters = new List<ParameterDescriptor>
        {
            new ParameterDescriptor
            {
                Name = "dto",
                ParameterType = typeof(TestDto)
            }
        };

        var actionContext = new ActionContext(
            httpContext,
            new RouteData(),
            actionDescriptor);

        var actionExecutingContext = new ActionExecutingContext(
            actionContext,
            new List<IFilterMetadata>(),
            new Dictionary<string, object?> { { "dto", dto } },
            controller: null!);


        var nextCalled = false;

        // Act
        await filter.OnActionExecutionAsync(actionExecutingContext, () =>
        {
            nextCalled = true;
            return Task.FromResult(new ActionExecutedContext(actionContext, new List<IFilterMetadata>(), null!));
        });

        // Assert
        Assert.Null(actionExecutingContext.Result);
        Assert.True(nextCalled);
    }

    #endregion

    #region Edge Cases

    [Fact]
    public void ToProblemDetailsError_WithCancelledToken_BeforeProcessing_ThrowsImmediately()
    {
        // Arrange
        var validator = new TestDtoValidator();
        var dto = new TestDto(Email: "", Age: 15, Username: "ab");
        var validationResult = validator.Validate(dto);
        var instance = "/api/test";
        var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert
        var exception = Assert.Throws<OperationCanceledException>(() =>
            validationResult.ToProblemDetailsError(instance, cts.Token));

        Assert.Equal(cts.Token, exception.CancellationToken);
    }

    [Fact]
    public async Task AsyncValidator_RespectsCancellationToken_DuringValidation()
    {
        // Arrange
        var validator = new AsyncTestDtoValidator();
        var dto = new TestDto(Email: "test@example.com", Age: 25, Username: "testuser");
        var cts = new CancellationTokenSource();

        // Act
        var validationResult = await validator.ValidateAsync(dto, cts.Token);

        // Assert
        Assert.True(validator.CancellationChecked);
        Assert.True(validationResult.IsValid);
        Assert.False(cts.Token.IsCancellationRequested);
    }

    [Fact]
    public async Task ToProblemDetailsErrorIfInvalidAsync_PropagatesInnerException_WhenNotCancellation()
    {
        // Arrange
        var tcs = new TaskCompletionSource<ValidationResult>();
        tcs.SetException(new InvalidOperationException("Test exception"));
        var instance = "/api/test";

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await tcs.Task.ToProblemDetailsErrorIfInvalidAsync(instance, CancellationToken.None));

        Assert.Equal("Test exception", exception.Message);
    }

    #endregion

    #region Performance & Thread Safety

    [Fact]
    public void ToProblemDetailsError_WithCancellationToken_IsThreadSafe()
    {
        // Arrange
        var validator = new TestDtoValidator();
        var dto = new TestDto(Email: "", Age: 15, Username: "ab");
        var validationResult = validator.Validate(dto);
        var instance = "/api/test";
        var exceptions = new List<Exception>();

        // Act
        Parallel.For(0, 100, i =>
        {
            try
            {
                var cts = new CancellationTokenSource();
                if (i % 2 == 0)
                {
                    cts.Cancel();
                }

                var error = validationResult.ToProblemDetailsError(instance, cts.Token);
                Assert.NotNull(error);
            }
            catch (OperationCanceledException)
            {
                // Expected for cancelled tokens
            }
            catch (Exception ex)
            {
                lock (exceptions)
                {
                    exceptions.Add(ex);
                }
            }
        });

        // Assert
        Assert.Empty(exceptions);
    }

    #endregion
}

