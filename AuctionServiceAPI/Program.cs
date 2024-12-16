using MongoDB.Driver;
using Microsoft.Extensions.Options;
using Models;
using AuctionService.Configuration;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Microsoft.AspNetCore.Identity;


try
{
    var builder = WebApplication.CreateBuilder(args);

    // Retrieve MongoDB connection details from environment variables
    var connectionString = Environment.GetEnvironmentVariable("MONGO_CONNECTION_STRING")
                           ?? throw new InvalidOperationException("MongoDB connection string is not set in the environment variables.");
    var auctionDatabaseName = Environment.GetEnvironmentVariable("AuctionDatabaseName");
    var bidDatabaseName = Environment.GetEnvironmentVariable("BidDatabaseName");
    var userDatabaseName = Environment.GetEnvironmentVariable("UserDatabaseName");
    var vareDatabaseName = Environment.GetEnvironmentVariable("VareDatabaseName");

    var auctionCollectionName = Environment.GetEnvironmentVariable("AuctionCollectionName");
    var bidCollectionName = Environment.GetEnvironmentVariable("BidCollectionName");
    var userCollectionName = Environment.GetEnvironmentVariable("UserCollectionName");
    var vareCollectionName = Environment.GetEnvironmentVariable("VareCollectionName");

    if (string.IsNullOrEmpty(connectionString) || string.IsNullOrEmpty(auctionDatabaseName) || string.IsNullOrEmpty(bidDatabaseName)
        || string.IsNullOrEmpty(userDatabaseName) || string.IsNullOrEmpty(vareDatabaseName))
    {
        throw new InvalidOperationException("MongoDB connection details are not fully set in the environment variables.");
    }

    // Register MongoDB services using environment variables
    builder.Services.AddSingleton<IMongoClient>(sp => new MongoClient(connectionString));

    builder.Services.AddSingleton(sp =>
    {
        var client = sp.GetRequiredService<IMongoClient>();
        var database = client.GetDatabase(auctionDatabaseName);
        return database.GetCollection<Auction>(auctionCollectionName);
    });

    builder.Services.AddSingleton(sp =>
    {
        var client = sp.GetRequiredService<IMongoClient>();
        var database = client.GetDatabase(bidDatabaseName);
        return database.GetCollection<Bid>(bidCollectionName);
    });

    builder.Services.AddSingleton(sp =>
    {
        var client = sp.GetRequiredService<IMongoClient>();
        var database = client.GetDatabase(userDatabaseName);
        return database.GetCollection<User>(userCollectionName);
    });

    // Register Vare collection
    builder.Services.AddSingleton(sp =>
    {
        var client = sp.GetRequiredService<IMongoClient>();
        var database = client.GetDatabase(vareDatabaseName);
        return database.GetCollection<Product>(vareCollectionName);
    });

    builder.Services.AddControllers();

    // Retrieve AuthService URL from environment variables
    var authServiceUrl = Environment.GetEnvironmentVariable("AUTHSERVICE_URL")
                         ?? throw new InvalidOperationException("AuthService URL is not set in the environment variables.");

    // Use HttpClient to communicate with AuthService
    var httpClient = new HttpClient { BaseAddress = new Uri(authServiceUrl) };

    // Get validation keys from AuthService
    var authServiceResponse = httpClient.GetAsync("Auth/GetValidationKeys").Result;

    string issuer, secret;

    if (authServiceResponse.IsSuccessStatusCode)
    {
        var keys = authServiceResponse.Content.ReadFromJsonAsync<ValidationKeys>().Result;
        issuer = keys?.Issuer ?? throw new Exception("Issuer not found in AuthService response.");
        secret = keys?.Secret ?? throw new Exception("Secret not found in AuthService response.");
    }
    else
    {
        throw new Exception("Failed to retrieve validation keys from AuthService.");
    }

    builder.Services
        .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddJwtBearer(options =>
        {
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = false,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = issuer,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret))
            };
        });

    var app = builder.Build();

    app.UseAuthorization();

    app.MapControllers();

    app.Run();
}
catch (Exception ex)
{
    // Handle any exceptions here if needed
    Console.WriteLine($"An error occurred: {ex.Message}");
    throw;
}

// Model for validation keys
public class ValidationKeys
{
    public string Issuer { get; set; }
    public string Secret { get; set; }
}
