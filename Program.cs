using System.IdentityModel.Tokens.Jwt;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Models;

using var loggerFactory = LoggerFactory.Create(b => b.SetMinimumLevel(LogLevel.Trace).AddConsole());

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));


// Add services to the container.
builder.Services.AddControllers(); // Register the controllers
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddCors(options => {
    // Configure CORS 
    options.AddPolicy("AllowReactApp",
        policy => {
            policy.WithOrigins("http://localhost:5173") // Port where your React app is running                            
                  .AllowAnyHeader()
                  .AllowAnyMethod()
                  .AllowCredentials(); // If you're using cookies or other credentials
        });
});

builder.Services.AddAuthentication(options => {
    // Configure Authentication and Authorization
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
}).AddJwtBearer(options =>
    {
        options.SaveToken = true;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"] ?? throw new InvalidOperationException("Secret not configured"))),
            ClockSkew = new TimeSpan(0,0,5),
            ValidateIssuerSigningKey = true,
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
        };
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = ctx =>
            {
                // Extract the token from the cookie
                var token = ctx.Request.Cookies["access_token"];
                if (!string.IsNullOrEmpty(token))
                {
                    ctx.Token = token;
                }
                LogAttempt(ctx.Request.Headers, "OnMessageReceived");
                return Task.CompletedTask;
            },
            OnChallenge = ctx => LogAttempt(ctx.Request.Headers, "OnChallenge"),
            OnTokenValidated = ctx => LogAttempt(ctx.Request.Headers, "OnTokenValidated")
        };
    });

// Add Authorization
builder.Services.AddAuthorization();

// Register PasswordHasher
builder.Services.AddScoped<IPasswordHasher<AccountModel>, PasswordHasher<AccountModel>>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage(); // Add this to show detailed errors in development
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// Apply CORS policy
app.UseCors("AllowReactApp");

// Ensure routing is set up before mapping controllers
app.UseRouting();

// Use Authentication and Authorization
app.UseAuthentication();
app.UseAuthorization();


// Map controllers to routes
app.MapControllers(); // Ensure this is included to map the controllers

app.Run();


Task LogAttempt(IHeaderDictionary headers, string eventType)
{
    var logger = loggerFactory.CreateLogger<Program>();

    var authorizationHeader = headers["Authorization"].FirstOrDefault();

    if (authorizationHeader is null)
        logger.LogInformation($"{eventType}. JWT not present");
    else
    {
        string jwtString = authorizationHeader.Substring("Bearer ".Length);

        var jwt = new JwtSecurityToken(jwtString);

        logger.LogInformation($"{eventType}. Expiration: {jwt.ValidTo.ToLongTimeString()}. System time: {DateTime.UtcNow.ToLongTimeString()}");
    }

    return Task.CompletedTask;
}

record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}

