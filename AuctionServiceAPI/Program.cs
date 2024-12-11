using MongoDB.Driver;
using Microsoft.Extensions.Options;
using Models;
using AuctionService.Configuration;
using NLog;
using NLog.Web;

var logger = NLog.LogManager.Setup().LoadConfigurationFromAppSettings()
.GetCurrentClassLogger();
logger.Debug("init main");

try 
{
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
builder.Logging.ClearProviders();
builder.Host.UseNLog();

var app = builder.Build();

app.UseAuthorization();

app.MapControllers();

app.Run();

}
catch (Exception ex)
{
    logger.Error(ex, "Stopped program because of exception");
    throw;
}
finally
{
    NLog.LogManager.Shutdown();
}