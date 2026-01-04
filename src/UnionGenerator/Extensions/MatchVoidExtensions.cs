using System;
using UnionGenerator.Attributes;

namespace UnionGenerator.Extensions;

/// <summary>
/// Provides helper extension methods for unit/void result matching patterns.
/// Useful for Result<Unit, E> or other void-like union types.
/// </summary>
/// <remarks>
/// <para>
/// These extension methods simplify the common pattern of matching against
/// Result types where the success case carries no data (Unit/void-like).
/// Instead of using Match with lambda expressions, MatchVoid accepts simple Action delegates.
/// </para>
/// <para>
/// Example:
/// <code>
/// // Instead of:
/// result.Match(
///     ok: _ => { DoSomething(); return Unit.Value; },
///     error: err => { LogError(err); return Unit.Value; }
/// );
/// 
/// // Use:
/// result.MatchVoid(
///     ok: () => { DoSomething(); },
///     error: err => { LogError(err); }
/// );
/// </code>
/// </para>
/// </remarks>
public static class MatchVoidExtensions
{
    /// <summary>
    /// Matches a unit-like result against two action delegates.
    /// </summary>
    /// <typeparam name="TError">The type of the error value.</typeparam>
    /// <param name="result">
    /// The result to match. Expected to have IsSuccess/IsOk property and Error/ErrorValue property.
    /// </param>
    /// <param name="ok">Action to execute if the result is successful.</param>
    /// <param name="error">Action to execute if the result contains an error, receiving the error value.</param>
    /// <exception cref="ArgumentNullException">Thrown when result, ok, or error is null.</exception>
    /// <remarks>
    /// <para>
    /// This method is equivalent to calling Match() with unit-returning lambdas, but more concise
    /// and explicit about the intent (side effects, not transformations).
    /// </para>
    /// <para>
    /// Thread-safety: This method is thread-safe as it does not modify shared state.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// Result&lt;Unit, ValidationError&gt; validationResult = ValidateUser(user);
    /// 
    /// validationResult.MatchVoid(
    ///     ok: () => Console.WriteLine("User is valid"),
    ///     error: err => Console.WriteLine($"Validation failed: {err.Message}")
    /// );
    /// </code>
    /// </example>
    public static void MatchVoid<TError>(
        this dynamic result,
        Action ok,
        Action<TError> error)
    {
        ArgumentNullException.ThrowIfNull(result);
        ArgumentNullException.ThrowIfNull(ok);
        ArgumentNullException.ThrowIfNull(error);

        var resultType = result.GetType();

        // Find success indicator property
        var successProperty = resultType.GetProperty("IsSuccess")
                           ?? resultType.GetProperty("IsOk")
                           ?? resultType.GetProperty("IsSome");

        if (successProperty == null)
        {
            throw new InvalidOperationException(
                $"Result type '{resultType.Name}' does not have a recognizable success case property.");
        }

        var isSuccess = (bool)(successProperty.GetValue(result) ?? false);

        if (isSuccess)
        {
            ok();
        }
        else
        {
            // Find error value property
            var errorProperty = resultType.GetProperty("Error")
                             ?? resultType.GetProperty("ErrorValue")
                             ?? resultType.GetProperty("Failure")
                             ?? resultType.GetProperty("FailureValue");

            if (errorProperty == null)
            {
                throw new InvalidOperationException(
                    $"Result type '{resultType.Name}' does not have a recognizable error property.");
            }

            var errorValue = (TError?)errorProperty.GetValue(result);

            if (errorValue == null)
            {
                throw new InvalidOperationException(
                    $"Result type '{resultType.Name}' has null error value.");
            }

            error(errorValue);
        }
    }

    /// <summary>
    /// Matches a unit-like result against one or both action delegates, with flexible matching.
    /// </summary>
    /// <typeparam name="TError">The type of the error value.</typeparam>
    /// <param name="result">The result to match.</param>
    /// <param name="ok">Optional action to execute if the result is successful.</param>
    /// <param name="error">Optional action to execute if the result contains an error.</param>
    /// <remarks>
    /// <para>
    /// This overload allows you to handle only the case(s) you care about.
    /// If either ok or error is null, it is skipped (no exception thrown).
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Handle only error case
    /// result.MatchVoid<ValidationError>(
    ///     ok: null,
    ///     error: err => Console.WriteLine($"Error: {err.Message}")
    /// );
    /// </code>
    /// </example>
    public static void MatchVoid<TError>(
        this dynamic result,
        Action? ok,
        Action<TError>? error)
    {
        ArgumentNullException.ThrowIfNull(result);

        if (ok == null && error == null)
        {
            return; // Nothing to do
        }

        var resultType = result.GetType();

        // Find success indicator property
        var successProperty = resultType.GetProperty("IsSuccess")
                           ?? resultType.GetProperty("IsOk")
                           ?? resultType.GetProperty("IsSome");

        if (successProperty == null)
        {
            throw new InvalidOperationException(
                $"Result type '{resultType.Name}' does not have a recognizable success case property.");
        }

        var isSuccess = (bool)(successProperty.GetValue(result) ?? false);

        if (isSuccess)
        {
            ok?.Invoke();
        }
        else if (error != null)
        {
            // Find error value property
            var errorProperty = resultType.GetProperty("Error")
                             ?? resultType.GetProperty("ErrorValue")
                             ?? resultType.GetProperty("Failure")
                             ?? resultType.GetProperty("FailureValue");

            if (errorProperty == null)
            {
                throw new InvalidOperationException(
                    $"Result type '{resultType.Name}' does not have a recognizable error property.");
            }

            var errorValue = (TError?)errorProperty.GetValue(result);

            if (errorValue == null)
            {
                throw new InvalidOperationException(
                    $"Result type '{resultType.Name}' has null error value.");
            }

            error(errorValue);
        }
    }
}

