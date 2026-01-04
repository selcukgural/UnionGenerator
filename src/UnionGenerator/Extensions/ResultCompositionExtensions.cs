using System;
using System.Threading.Tasks;

namespace UnionGenerator.Extensions;

/// <summary>
/// Provides monadic composition extensions for Result types.
/// Enables functional chaining of operations that return Result values.
/// </summary>
/// <remarks>
/// <para>
/// These extensions implement the Bind (also called FlatMap or SelectMany) pattern,
/// which is essential for composing operations that can fail.
/// </para>
/// <para>
/// The Bind method has the signature:
/// <code>
/// public static Result<T2, E> Bind<T1, T2, E>(
///     this Result<T1, E> result,
///     Func<T1, Result<T2, E>> binder)
/// </code>
/// </para>
/// <para>
/// Monadic Laws Satisfied:
/// - Left Identity: `Bind(Return(x), f) == f(x)`
/// - Right Identity: `Bind(m, Return) == m`
/// - Associativity: `Bind(Bind(m, f), g) == Bind(m, x => Bind(f(x), g))`
/// </para>
/// <para>
/// Thread-safety: These extensions are thread-safe as they do not modify shared state.
/// </para>
/// </remarks>
public static class ResultCompositionExtensions
{
    /// <summary>
    /// Binds (flat-maps) a Result to a function that returns another Result.
    /// </summary>
    /// <typeparam name="TSuccess">The type of the success value in the source result.</typeparam>
    /// <typeparam name="TSuccess2">The type of the success value in the bound result.</typeparam>
    /// <typeparam name="TError">The type of the error value (unchanged).</typeparam>
    /// <param name="result">The source result.</param>
    /// <param name="binder">
    /// Function that takes the success value and returns a new Result.
    /// If the source result is an error, the binder is not called.
    /// </param>
    /// <returns>
    /// The result of calling binder if the source is success, or the source error otherwise.
    /// </returns>
    /// <exception cref="ArgumentNullException">Thrown when result or binder is null.</exception>
    /// <remarks>
    /// <para>
    /// This method implements the monadic bind operation. It's useful for chaining operations
    /// where each step can fail and you want to stop processing on the first failure.
    /// </para>
    /// <para>
    /// Performance: O(1) for success case (no allocation), O(1) for error case (returns immediately).
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Chain multiple operations
    /// Result<User, ValidationError> result = GetUser(userId)
    ///     .Bind(user => ValidateUser(user))
    ///     .Bind(user => SaveUser(user));
    ///
    /// // Or use as query expression (if Result implements SelectMany)
    /// var result = from user in GetUser(userId)
    ///              from validated in ValidateUser(user)
    ///              from saved in SaveUser(validated)
    ///              select saved;
    /// </code>
    /// </example>
    public static dynamic Bind<TSuccess, TSuccess2, TError>(
        this dynamic result,
        Func<TSuccess, dynamic> binder)
    {
        ArgumentNullException.ThrowIfNull(result);
        ArgumentNullException.ThrowIfNull(binder);

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

        if (!isSuccess)
        {
            // Error case: return the error result directly
            return result;
        }

        // Success case: extract value and bind
        var valueProperty = resultType.GetProperty("Value")
                         ?? resultType.GetProperty("Data")
                         ?? resultType.GetProperty("Ok");

        if (valueProperty == null)
        {
            throw new InvalidOperationException(
                $"Result type '{resultType.Name}' does not have a recognizable value property.");
        }

        var value = (TSuccess?)valueProperty.GetValue(result);

        if (value == null)
        {
            throw new InvalidOperationException(
                $"Result type '{resultType.Name}' has null success value.");
        }

        // Call the binder and return its result
        var boundResult = binder(value);

        return boundResult;
    }

    /// <summary>
    /// Maps a Result's success value to a new value without changing the error case.
    /// </summary>
    /// <typeparam name="TSuccess">The type of the success value in the source result.</typeparam>
    /// <typeparam name="TSuccess2">The type of the mapped success value.</typeparam>
    /// <typeparam name="TError">The type of the error value (unchanged).</typeparam>
    /// <param name="result">The source result.</param>
    /// <param name="mapper">Function to transform the success value.</param>
    /// <returns>
    /// A result with the mapped success value if the source is success, or the source error otherwise.
    /// </returns>
    /// <exception cref="ArgumentNullException">Thrown when result or mapper is null.</exception>
    /// <remarks>
    /// <para>
    /// This is a pure functional map operation. It transforms the success value without changing
    /// the result's overall structure or error handling behavior.
    /// </para>
    /// <para>
    /// Performance: O(n) where n is the cost of the mapper function.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Transform the success value
    /// Result<int, string> result = GetNumber()
    ///     .Map(num => num * 2)
    ///     .Map(num => num + 10);
    /// </code>
    /// </example>
    public static dynamic Map<TSuccess, TSuccess2, TError>(
        this dynamic result,
        Func<TSuccess, TSuccess2> mapper)
    {
        ArgumentNullException.ThrowIfNull(result);
        ArgumentNullException.ThrowIfNull(mapper);

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

        if (!isSuccess)
        {
            // Error case: return unchanged
            return result;
        }

        // Success case: extract, map, and return new result of same type
        var valueProperty = resultType.GetProperty("Value")
                         ?? resultType.GetProperty("Data")
                         ?? resultType.GetProperty("Ok");

        if (valueProperty == null)
        {
            throw new InvalidOperationException(
                $"Result type '{resultType.Name}' does not have a recognizable value property.");
        }

        var value = (TSuccess?)valueProperty.GetValue(result);

        if (value == null)
        {
            throw new InvalidOperationException(
                $"Result type '{resultType.Name}' has null success value.");
        }

        var mappedValue = mapper(value);

        // Create a new result of the same type with the mapped value
        // This requires the result type to have a factory method or constructor.
        // For now, we'll use reflection to call the appropriate factory.
        var factoryMethods = resultType.GetMethods(
            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static
        );

        // Try to find a factory method that accepts the mapped value
        var factory = factoryMethods.FirstOrDefault(m =>
            m.Name.Equals("Ok", StringComparison.OrdinalIgnoreCase) ||
            m.Name.Equals("Success", StringComparison.OrdinalIgnoreCase)
        );

        if (factory != null && factory.GetParameters().Length == 1)
        {
            return factory.Invoke(null, new object?[] { mappedValue })!;
        }

        throw new InvalidOperationException(
            $"Result type '{resultType.Name}' does not have a recognizable factory method for creating success results.");
    }

    /// <summary>
    /// Maps the error case of a Result to a different error type.
    /// </summary>
    /// <typeparam name="TSuccess">The type of the success value (unchanged).</typeparam>
    /// <typeparam name="TError">The type of the original error.</typeparam>
    /// <typeparam name="TError2">The type of the mapped error.</typeparam>
    /// <param name="result">The source result.</param>
    /// <param name="mapper">Function to transform the error value.</param>
    /// <returns>
    /// A result with the success value if the source is success, or the mapped error otherwise.
    /// </returns>
    /// <exception cref="ArgumentNullException">Thrown when result or mapper is null.</exception>
    /// <remarks>
    /// <para>
    /// This is useful for converting domain errors to API errors, for example.
    /// </para>
    /// <para>
    /// Performance: O(n) where n is the cost of the mapper function.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Convert domain errors to API errors
    /// Result<User, DomainError> result = GetUser(id);
    /// 
    /// Result<User, ApiError> apiResult = result.MapError(err =>
    ///     new ApiError { Code = err.Code, Message = err.Message }
    /// );
    /// </code>
    /// </example>
    public static dynamic MapError<TSuccess, TError, TError2>(
        this dynamic result,
        Func<TError, TError2> mapper)
    {
        ArgumentNullException.ThrowIfNull(result);
        ArgumentNullException.ThrowIfNull(mapper);

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
            // Success case: return unchanged
            return result;
        }

        // Error case: extract, map, and return new result
        var errorProperty = resultType.GetProperty("Error")
                         ?? resultType.GetProperty("ErrorValue")
                         ?? resultType.GetProperty("Failure")
                         ?? resultType.GetProperty("FailureValue");

        if (errorProperty == null)
        {
            throw new InvalidOperationException(
                $"Result type '{resultType.Name}' does not have a recognizable error property.");
        }

        var error = (TError?)errorProperty.GetValue(result);

        if (error == null)
        {
            throw new InvalidOperationException(
                $"Result type '{resultType.Name}' has null error value.");
        }

        var mappedError = mapper(error);

        // Create a new result with the mapped error
        var factoryMethods = resultType.GetMethods(
            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static
        );

        var factory = factoryMethods.FirstOrDefault(m =>
            m.Name.Equals("Error", StringComparison.OrdinalIgnoreCase) ||
            m.Name.Equals("Failure", StringComparison.OrdinalIgnoreCase)
        );

        if (factory != null && factory.GetParameters().Length == 1)
        {
            return factory.Invoke(null, new object?[] { mappedError })!;
        }

        throw new InvalidOperationException(
            $"Result type '{resultType.Name}' does not have a recognizable factory method for creating error results.");
    }
}

