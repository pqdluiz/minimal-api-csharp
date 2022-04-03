using System.Collections.Immutable;
using System;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using MinimalApi.Database;
using MinimalApi.Graphql;
using Microsoft.OpenApi.Models;

namespace MinimalApi.Services
{
    public class Builder
    {
        public WebApplicationBuilder Build(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            builder.Services.AddAuthorization();
            builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters()
                {
                    ValidateActor = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = builder.Configuration["Issuer"],
                    ValidAudience = builder.Configuration["Audience"],
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["SigningKey"]))
                };
            });
            builder.Services.AddDbContextFactory<TodoDbContext>(options => options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

            builder.Services.AddGraphQLServer()
                .AddQueryType<Query>()
                .AddMutationType<Mutation>()
                .AddSubscriptionType<Subscription>()
                .AddInMemorySubscriptions();

            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen(setup =>
            {
                setup.SwaggerDoc("v1", new OpenApiInfo()
                {
                    Description = "Todo web api implementation using Minimal Api in Asp.Net Core",
                    Title = "Todo Api",
                    Version = "v1",
                    Contact = new OpenApiContact()
                    {
                        Name = "anuraj",
                        Url = new Uri("https://dotnetthoughts.net")
                    }
                });

                setup.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
                {
                    Description = "JWT Authorization header using the Bearer scheme. Example: \"Authorization: Bearer {token}\"",
                    Name = "Authorization",
                    In = ParameterLocation.Header,
                    Type = SecuritySchemeType.Http,
                    Scheme = "Bearer"
                });

                setup.OperationFilter<AddAuthorizationHeaderOperationHeader>();
            });

            builder.Services.AddDatabaseDeveloperPageExceptionFilter();
            builder.Services.AddHealthChecks().AddDbContextCheck<TodoDbContext>();

            return builder;
        }
    }
}