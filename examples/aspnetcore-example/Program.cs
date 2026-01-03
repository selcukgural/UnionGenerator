using AspNetCoreExample.Models;
using AspNetCoreExample.Services;
using Microsoft.AspNetCore.Mvc;
using UnionGenerator.AspNetCore;
using UnionGenerator.AspNetCore.Filters;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "UnionGenerator ASP.NET Core Example API",
        Version = "v1",
        Description = "Example API demonstrating UnionGenerator integration with ASP.NET Core for Result pattern and ProblemDetails"
    });

    // Include XML comments if generated
    var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
    {
        options.IncludeXmlComments(xmlPath);
    }
});

// Register application services
builder.Services.AddSingleton<IUserService, UserService>();

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "UnionGenerator Example API v1");
        options.RoutePrefix = string.Empty; // Serve Swagger UI at root
    });
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

// Minimal API endpoints with UnionEndpointFilter
var minimalApiGroup = app.MapGroup("/api/minimal");

minimalApiGroup.MapGet("/users", GetAllUsersMinimal)
    .WithName("GetAllUsersMinimal")
    .WithOpenApi()
    .AddEndpointFilter<UnionEndpointFilter>();

minimalApiGroup.MapGet("/users/{id}", GetUserMinimal)
    .WithName("GetUserMinimal")
    .WithOpenApi()
    .AddEndpointFilter<UnionEndpointFilter>();

minimalApiGroup.MapPost("/users", CreateUserMinimal)
    .WithName("CreateUserMinimal")
    .WithOpenApi()
    .AddEndpointFilter<UnionEndpointFilter>();

minimalApiGroup.MapPut("/users/{id}", UpdateUserMinimal)
    .WithName("UpdateUserMinimal")
    .WithOpenApi()
    .AddEndpointFilter<UnionEndpointFilter>();

minimalApiGroup.MapDelete("/users/{id}", DeleteUserMinimal)
    .WithName("DeleteUserMinimal")
    .WithOpenApi()
    .AddEndpointFilter<UnionEndpointFilter>();

app.Run();

// Minimal API endpoint handlers
static Result<IReadOnlyList<User>, ProblemDetailsError> GetAllUsersMinimal(IUserService userService)
{
    return userService.GetAllUsers();
}

static Result<User, ProblemDetailsError> GetUserMinimal(int id, IUserService userService)
{
    return userService.GetUser(id);
}

static Result<User, ProblemDetailsError> CreateUserMinimal(
    [FromBody] CreateUserDto dto,
    IUserService userService,
    HttpContext httpContext)
{
    return userService.CreateUser(dto, httpContext.Request.Path);
}

static Result<User, ProblemDetailsError> UpdateUserMinimal(
    int id,
    [FromBody] UpdateUserDto dto,
    IUserService userService,
    HttpContext httpContext)
{
    return userService.UpdateUser(id, dto, httpContext.Request.Path);
}

static Result<bool, ProblemDetailsError> DeleteUserMinimal(
    int id,
    IUserService userService,
    HttpContext httpContext)
{
    return userService.DeleteUser(id, httpContext.Request.Path);
}

