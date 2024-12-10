using MongoDB.Driver;
using Microsoft.Extensions.Options;
using Models;
using AuctionService.Configuration;

var builder = WebApplication.CreateBuilder(args);

// Configure MongoDB settings
var mongoSettings = builder.Configuration.GetSection("MongoDB").Get<MongoDBSettings>();

// Register MongoDB services
builder.Services.AddSingleton<IMongoClient>(sp => new MongoClient(mongoSettings.ConnectionString));

// Register Auction collection
builder.Services.AddSingleton(sp =>
{
    var client = sp.GetRequiredService<IMongoClient>();
    var database = client.GetDatabase(mongoSettings.AuctionDatabaseName);
    return database.GetCollection<Auction>(mongoSettings.AuctionCollectionName);
});

// Register Bid collection
builder.Services.AddSingleton(sp =>
{
    var client = sp.GetRequiredService<IMongoClient>();
    var database = client.GetDatabase(mongoSettings.BidDatabaseName);
    return database.GetCollection<Bid>(mongoSettings.BidCollectionName);
});

// Register User collection
builder.Services.AddSingleton(sp =>
{
    var client = sp.GetRequiredService<IMongoClient>();
    var database = client.GetDatabase(mongoSettings.UserDatabaseName);
    return database.GetCollection<User>(mongoSettings.UserCollectionName);
});

// Register Vare collection
builder.Services.AddSingleton(sp =>
{
    var client = sp.GetRequiredService<IMongoClient>();
    var database = client.GetDatabase(mongoSettings.VareDatabaseName);
    return database.GetCollection<Product>(mongoSettings.VareCollectionName);
});

builder.Services.AddControllers();

var app = builder.Build();

app.UseAuthorization();

app.MapControllers();

app.Run();
