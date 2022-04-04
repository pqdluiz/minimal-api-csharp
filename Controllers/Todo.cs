using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.IdentityModel.Tokens;
using MinimalApi.Database;
using MinimalApi.Models;
using MinimalApi.Services;

namespace minimal_api_csharp.Controllers
{
    public class TodoController
    {
        public TodoController(string[] args)
        {
            var builder = new Builder();
            var app = builder.Build(args).Build();
            app.MapPost("/token", async (IDbContextFactory<TodoDbContext> dbContextFactory, HttpContext http, User inputUser) =>
{
    using (var dbContext = dbContextFactory.CreateDbContext())
    {
        if (!string.IsNullOrEmpty(inputUser.Username) &&
            !string.IsNullOrEmpty(inputUser.Password))
        {
            var loggedInUser = await dbContext.Users
                .FirstOrDefaultAsync(user => user.Username == inputUser.Username
                && user.Password == inputUser.Password);
            if (loggedInUser == null)
            {
                http.Response.StatusCode = 401;
                return;
            }

            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, loggedInUser.Username),
                new Claim(JwtRegisteredClaimNames.Name, loggedInUser.Username),
                new Claim(JwtRegisteredClaimNames.Email, loggedInUser.Email)
            };

            var token = new JwtSecurityToken
            (
                issuer: builder.Build(args).Configuration["Issuer"],
                audience: builder.Build(args).Configuration["Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddDays(60),
                notBefore: DateTime.UtcNow,
                signingCredentials: new SigningCredentials(
                    new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Build(args).Configuration["SigningKey"])),
                    SecurityAlgorithms.HmacSha256)
            );

            await http.Response.WriteAsJsonAsync(new { token = new JwtSecurityTokenHandler().WriteToken(token) });
            return;
        }

        http.Response.StatusCode = 400;
    }
}).Produces(200).WithTags("Authentication").Produces(401);

            app.MapGet("/todoitems", [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)] async (IDbContextFactory<TodoDbContext> dbContextFactory, HttpContext http) =>
            {
                using (var dbContext = dbContextFactory.CreateDbContext())
                {
                    var user = http.User;
                    return Results.Ok(await dbContext.TodoItems.Where(t => t.User.Username == user.FindFirst(ClaimTypes.NameIdentifier).Value)
                        .Select(t => new TodoItemOutput(t.Title, t.IsCompleted, t.CreatedOn)).ToListAsync());
                }
            }).Produces(200, typeof(List<TodoItemOutput>)).ProducesProblem(401);

            app.MapGet("/todoitems/{id}", [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)] async (IDbContextFactory<TodoDbContext> dbContextFactory, HttpContext http, int id) =>
             {
                 using (var dbContext = dbContextFactory.CreateDbContext())
                 {
                     var user = http.User;
                     return await dbContext.TodoItems.FirstOrDefaultAsync(t => t.User.Username == user.FindFirst(ClaimTypes.NameIdentifier).Value && t.Id == id) is not TodoItem todo ? Results.NotFound() : Results.Ok(todo);
                 }
             }).Produces(200, typeof(TodoItem)).ProducesProblem(401);

            app.MapPost("/todoitems", [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)] async (IDbContextFactory<TodoDbContext> dbContextFactory, HttpContext http, TodoItemInput todoItemInput) =>
             {
                 using (var dbContext = dbContextFactory.CreateDbContext())
                 {
                     var todoItem = new TodoItem
                     {
                         Title = todoItemInput.Title,
                         IsCompleted = todoItemInput.IsCompleted,
                     };
                     var httpUser = http.User;
                     var user = await dbContext.Users.FirstOrDefaultAsync(t => t.Username == httpUser.FindFirst(ClaimTypes.NameIdentifier).Value);
                     todoItem.User = user;
                     todoItem.UserId = user.Id;
                     dbContext.TodoItems.Add(todoItem);
                     await dbContext.SaveChangesAsync();
                     return Results.Created($"/todoitems/{todoItem.Id}", todoItem);
                 }
             }).Accepts<TodoItemInput>("application/json").Produces(201, typeof(TodoItemOutput)).ProducesProblem(401);

            app.MapPut("/todoitems/{id}", [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)] async (IDbContextFactory<TodoDbContext> dbContextFactory, HttpContext http, int id, TodoItemInput todoItemInput) =>
             {
                 using (var dbContext = dbContextFactory.CreateDbContext())
                 {
                     var user = http.User;
                     if (await dbContext.TodoItems.FirstOrDefaultAsync(t => t.User.Username == user.FindFirst(ClaimTypes.NameIdentifier).Value && t.Id == id) is TodoItem todoItem)
                     {
                         todoItem.IsCompleted = todoItemInput.IsCompleted;
                         await dbContext.SaveChangesAsync();
                         return Results.NoContent();
                     }

                     return Results.NotFound();
                 }
             }).Accepts<TodoItemInput>("application/json").Produces(201, typeof(TodoItemOutput)).ProducesProblem(404).ProducesProblem(401);

            app.MapDelete("/todoitems/{id}", [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)] async (IDbContextFactory<TodoDbContext> dbContextFactory, HttpContext http, int id) =>
            {
                using (var dbContext = dbContextFactory.CreateDbContext())
                {
                    var user = http.User;
                    if (await dbContext.TodoItems.FirstOrDefaultAsync(t => t.User.Username == user.FindFirst(ClaimTypes.NameIdentifier).Value && t.Id == id) is TodoItem todoItem)
                    {
                        dbContext.TodoItems.Remove(todoItem);
                        await dbContext.SaveChangesAsync();
                        return Results.NoContent();
                    }

                    return Results.NotFound();
                }
            }).Accepts<TodoItemInput>("application/json").Produces(204).ProducesProblem(404).ProducesProblem(401);

            app.MapGet("/health", async (HealthCheckService healthCheckService) =>
            {
                var report = await healthCheckService.CheckHealthAsync();
                return report.Status == HealthStatus.Healthy ? Results.Ok(report) : Results.StatusCode(StatusCodes.Status503ServiceUnavailable);
            }).WithTags(new[] { "Health" }).Produces(200).ProducesProblem(503).ProducesProblem(401);

            app.MapGet("/todoitems/history", [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)] async (IDbContextFactory<TodoDbContext> dbContextFactory, HttpContext http) =>
             {
                 using (var dbContext = dbContextFactory.CreateDbContext())
                 {
                     var user = http.User;
                     return Results.Ok(await dbContext.TodoItems.TemporalAsOf(DateTime.UtcNow)
                        .Where(t => t.User.Username == user.FindFirst(ClaimTypes.NameIdentifier).Value)
                        .OrderBy(todoItem => EF.Property<DateTime>(todoItem, "PeriodStart"))
                        .Select(todoItem => new TodoItemAudit
                        {
                            Title = todoItem.Title,
                            IsCompleted = todoItem.IsCompleted,
                            PeriodStart = EF.Property<DateTime>(todoItem, "PeriodStart"),
                            PeriodEnd = EF.Property<DateTime>(todoItem, "PeriodEnd")
                        })
                        .ToListAsync());
                 }
             }).Produces<TodoItemAudit>(200).WithTags(new[] { "EF Core Feature" }).ProducesProblem(401);

        }
    }
}