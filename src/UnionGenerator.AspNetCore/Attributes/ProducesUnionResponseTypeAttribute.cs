using Microsoft.AspNetCore.Mvc;

namespace UnionGenerator.AspNetCore.Attributes;

/// <summary>
/// Specifies that an action method returns a union type and documents the possible HTTP response types.
/// </summary>
/// <remarks>
/// <para>
/// This attribute combines the functionality of documenting union-based return types for OpenAPI/Swagger
/// with the standard <see cref="ProducesResponseTypeAttribute"/> behavior.
/// </para>
/// <para>
/// When applied to an action method, it indicates that the method returns a union type with specific
/// success and error cases, each potentially having different HTTP status codes and response types.
/// </para>
/// <para>
/// This attribute is primarily used for API documentation generation and does not affect runtime behavior.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// [HttpGet("{id}")]
/// [ProducesUnionResponseType(typeof(User), StatusCodes.Status200OK)]
/// [ProducesUnionResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
/// [ProducesUnionResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
/// public Result&lt;User, ProblemDetailsError&gt; GetUser(int id)
/// {
///     return _userService.GetUser(id);
/// }
/// </code>
/// </example>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
public sealed class ProducesUnionResponseTypeAttribute : ProducesResponseTypeAttribute
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ProducesUnionResponseTypeAttribute"/> class.
    /// </summary>
    /// <param name="statusCode">
    /// The HTTP status code of the response.
    /// </param>
    public ProducesUnionResponseTypeAttribute(int statusCode)
        : base(statusCode)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ProducesUnionResponseTypeAttribute"/> class.
    /// </summary>
    /// <param name="type">
    /// The type of the value returned in the response body for this status code.
    /// </param>
    /// <param name="statusCode">
    /// The HTTP status code of the response.
    /// </param>
    public ProducesUnionResponseTypeAttribute(Type type, int statusCode)
        : base(type, statusCode)
    {
    }
}

