using AspNetCoreExample.Models;
using AspNetCoreExample.Services;
using Microsoft.AspNetCore.Mvc;
using UnionGenerator.AspNetCore.Extensions;

namespace AspNetCoreExample.Controllers;

/// <summary>
/// Controller for user management operations using Result union pattern.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class UsersController : ControllerBase
{
    private readonly IUserService _userService;

    /// <summary>
    /// Initializes a new instance of the <see cref="UsersController"/> class.
    /// </summary>
    /// <param name="userService">The user service.</param>
    public UsersController(IUserService userService)
    {
        _userService = userService;
    }

    /// <summary>
    /// Gets all users.
    /// </summary>
    /// <returns>A list of all users.</returns>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<User>), StatusCodes.Status200OK)]
    public IActionResult GetAllUsers()
    {
        var result = _userService.GetAllUsers();
        return result.ToActionResult();
    }

    /// <summary>
    /// Gets a user by ID.
    /// </summary>
    /// <param name="id">The user ID.</param>
    /// <returns>The user if found.</returns>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(User), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public IActionResult GetUser(int id)
    {
        var result = _userService.GetUser(id);
        return result.ToActionResult();
    }

    /// <summary>
    /// Creates a new user.
    /// </summary>
    /// <param name="dto">The user creation data.</param>
    /// <returns>The created user.</returns>
    [HttpPost]
    [ProducesResponseType(typeof(User), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public IActionResult CreateUser([FromBody] CreateUserDto dto)
    {
        if (ModelState.TryGetValidationError(HttpContext.Request.Path, out var validationError))
        {
            return validationError!.ToActionResult();
        }

        var result = _userService.CreateUser(dto, HttpContext.Request.Path);
        return result.ToActionResult(successStatusCode: StatusCodes.Status201Created);
    }

    /// <summary>
    /// Updates an existing user.
    /// </summary>
    /// <param name="id">The user ID.</param>
    /// <param name="dto">The user update data.</param>
    /// <returns>The updated user.</returns>
    [HttpPut("{id}")]
    [ProducesResponseType(typeof(User), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public IActionResult UpdateUser(int id, [FromBody] UpdateUserDto dto)
    {
        if (ModelState.TryGetValidationError(HttpContext.Request.Path, out var validationError))
        {
            return validationError!.ToActionResult();
        }

        var result = _userService.UpdateUser(id, dto, HttpContext.Request.Path);
        return result.ToActionResult();
    }

    /// <summary>
    /// Deletes a user.
    /// </summary>
    /// <param name="id">The user ID.</param>
    /// <returns>No content on success.</returns>
    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public IActionResult DeleteUser(int id)
    {
        var result = _userService.DeleteUser(id, HttpContext.Request.Path);
        return result.ToActionResult(successStatusCode: StatusCodes.Status204NoContent);
    }
}

