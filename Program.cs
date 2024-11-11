using System.IdentityModel.Tokens.Jwt;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using biddingServer.Models;
using SignalR.Hubs;
using biddingServer.services.product;
using System.Collections.Concurrent;
using biddingServer.dto.Hubs;
using biddingServer.services.account;

using var loggerFactory = LoggerFactory.Create(b => b.SetMinimumLevel(LogLevel.Information).AddConsole());

var builder = WebApplication.CreateBuilder(args);

string dbHost = builder.Configuration["DB_HOST"] ?? "localhost";
string dbName = builder.Configuration["DB_NAME"] ?? "bidding";
string dbPort = builder.Configuration["DB_PORT"] ?? "1433";
string dbUserId = builder.Configuration["DB_USERID"] ?? "SA";
string dbPassword = builder.Configuration["DB_PASSWORD"] ?? "";
string uiPort = builder.Configuration["UI_PORT"] ?? "3000";
string uiHost = builder.Configuration["UI_HOST"] ?? "localhost";

builder.Services.AddSignalR();

builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    string connString = $"Server={dbHost},{dbPort};Database={dbName};User Id={dbUserId};Password={dbPassword};TrustServerCertificate=True;";

    options.UseSqlServer(connString);
});

// Add services to the container.
builder.Services.AddControllers(); // Register the controllers
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddCors(options =>
{
    // Configure CORS
    options.AddPolicy("AllowReactApp",
        policy =>
        {
            // NOTE : protocol needs to be resolve
            // string protocol = uiPort == "80" ? "http" : "https";
            string protocol = uiPort == "443" ? "https" : "http";
            string uiString = uiPort == "80" || uiPort == "443" ?
                $"{protocol}://{uiHost}" :
                $"{protocol}://{uiHost}:{uiPort}";

            Console.WriteLine($"CORS configured for: {uiString}");

            policy.WithOrigins(uiString) // need to resolve the domain for the cors here
                  .AllowAnyHeader()
                  .AllowAnyMethod()
                  .AllowCredentials(); // If you're using cookies or other credentials
        });
});

builder.Services.AddAuthentication(options =>
{
    // Configure Authentication and Authorization
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
}).AddJwtBearer(options =>
    {
        options.SaveToken = true;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidIssuer = builder.Configuration["Jwt:Issuer"] ?? throw new InvalidOperationException("Jwt Issuer not configured"),
            ValidAudience = builder.Configuration["Jwt:Audience"] ?? throw new InvalidOperationException("Jwt Audience not configured"),
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"] ?? throw new InvalidOperationException("Jwt key not configured"))),
            ClockSkew = new TimeSpan(0, 0, 5),
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
                return Task.CompletedTask;
            },
            OnChallenge = ctx => { return Task.CompletedTask; },
            OnTokenValidated = ctx => { return Task.CompletedTask; }
        };
    });

// Add Authorization
builder.Services.AddAuthorization();

// Register Services
builder.Services.AddScoped<IPasswordHasher<AccountModel>, PasswordHasher<AccountModel>>();
builder.Services.AddScoped<IProductService, ProductService>();
builder.Services.AddScoped<IProductCategoryService, ProductCategoryService>();
builder.Services.AddScoped<IProductImagesService, ProductImagesService>();
builder.Services.AddScoped<IAccountService, AccountService>();

builder.Services.AddSingleton<ConcurrentDictionary<string, ConnectedUsersDTO>>();


var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage(); // Add this to show detailed errors in development
    app.UseSwagger();
    app.UseSwaggerUI();
}

// app.UseHttpsRedirection();

// Apply CORS policy
app.UseCors("AllowReactApp");

// Ensure routing is set up before mapping controllers
app.UseRouting();

// Use Authentication and Authorization
app.UseAuthentication();
app.UseAuthorization();

app.UseEndpoints(endpoints =>
{
    // Map controllers to routes
    _ = endpoints.MapControllers(); // Ensure this is included to map the controllers

    // Map hub routes
    _ = endpoints.MapHub<Bidding>("/subastaHub");
});

using (var scope = app.Services.CreateScope())
{
    string connString = $"Server={dbHost},{dbPort};Database={dbName};User Id={dbUserId};Password={dbPassword};TrustServerCertificate=True;";
    var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();
    optionsBuilder.UseSqlServer(connString);
    using (var dbContext = new ApplicationDbContext(optionsBuilder.Options))
    {
        try
        {
            dbContext.Database.Migrate();
        }
        catch (Exception err)
        {
            Console.WriteLine(connString);
            Console.WriteLine("Migration failed error: \n" + err.Message);
            // Console.WriteLine("========================\nFailure Stacktrace: \n" + err.StackTrace);

        }
    }
}

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

