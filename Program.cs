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
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "User Management API", Version = "v1" });

    // 👇 Add security definition
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Enter 'Bearer {your JWT token}'"
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
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Logging last
app.UseLoggingMiddleware();

app.MapControllers();
app.Run();

