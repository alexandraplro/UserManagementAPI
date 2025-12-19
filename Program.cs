using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Secret key for signing tokens (store securely in real apps)
// Load JWT settings from appsettings.json
var jwtSettings = builder.Configuration.GetSection("Jwt");
var keyString = jwtSettings["Key"] 
    ?? throw new InvalidOperationException("JWT Key is missing in configuration.");
var key = Encoding.ASCII.GetBytes(keyString);
var issuer = jwtSettings["Issuer"] 
    ?? throw new InvalidOperationException("JWT Issuer is missing in configuration.");
var audience = jwtSettings["Audience"] 
    ?? throw new InvalidOperationException("JWT Audience is missing in configuration.");

// Services

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.RequireHttpsMetadata = false; // dev only
    options.SaveToken = true;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(key),
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidIssuer = jwtSettings["Issuer"],
        ValidAudience = jwtSettings["Audience"]
    };

    // 👇 Custom response for unauthorized requests
    options.Events = new JwtBearerEvents
    {
        OnChallenge = context =>
        {
            // Skip the default response
            context.HandleResponse();

            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            context.Response.ContentType = "application/json";
            return context.Response.WriteAsync("{\"error\":\"Unauthorized: Token missing or invalid\"}");
        }
    };
});


builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "UserManagementAPI",
        Version = "v1",
        Description = "An ASP.NET Core Web API demonstrating custom LoggingMiddleware and ErrorHandlingMiddleware with JWT authentication.",
        Contact = new Microsoft.OpenApi.Models.OpenApiContact
        {
            Name = "AlexandraPlRo",
            Url = new Uri("https://github.com/alexandraplro/UserManagementAPI")
        },
        License = new Microsoft.OpenApi.Models.OpenApiLicense
        {
            Name = "MIT License",
            Url = new Uri("https://opensource.org/licenses/MIT")
        }
    });

    // 👇 Apply security requirement globally
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            new string[] {}
        }
    });
});


// Build
var app = builder.Build();

// Error handling first
app.UseErrorHandlingMiddleware();

// Authentication + Authorization next
app.UseAuthentication();
app.UseAuthorization();

// Swagger (optional, usually before MapControllers)
if (app.Environment.IsDevelopment() || app.Environment.IsProduction())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "UserManagementAPI v1");
        c.RoutePrefix = string.Empty; // makes Swagger UI load at root URL
    });
}

// Logging last
app.UseLoggingMiddleware();

app.MapControllers();
app.Run();

