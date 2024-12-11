using MongoDB.Driver;
using Microsoft.Extensions.Options;
using Models;
using AuctionService.Configuration;

<<<<<<< HEAD
var logger = NLog.LogManager.Setup().LoadConfigurationFromAppSettings()
    .GetCurrentClassLogger();
logger.Debug("init main");

try
{
    var builder = WebApplication.CreateBuilder(args);
=======
var builder = WebApplication.CreateBuilder(args);
>>>>>>> parent of 8a6c13c (nlog med loki)

    // Configure MongoDB settings
    var mongoSettings = builder.Configuration.GetSection("MongoDB").Get<MongoDBSettings>();

    // Register MongoDB services
    builder.Services.AddSingleton<IMongoClient>(sp => new MongoClient(mongoSettings.ConnectionString));

    // Register Mongo collections
    builder.Services.AddSingleton(sp =>
    {
        var client = sp.GetRequiredService<IMongoClient>();
        var database = client.GetDatabase(mongoSettings.AuctionDatabaseName);
        return database.GetCollection<Auction>(mongoSettings.AuctionCollectionName);
    });

    builder.Services.AddSingleton(sp =>
    {
        var client = sp.GetRequiredService<IMongoClient>();
        var database = client.GetDatabase(mongoSettings.BidDatabaseName);
        return database.GetCollection<Bid>(mongoSettings.BidCollectionName);
    });

    builder.Services.AddSingleton(sp =>
    {
        var client = sp.GetRequiredService<IMongoClient>();
        var database = client.GetDatabase(mongoSettings.UserDatabaseName);
        return database.GetCollection<User>(mongoSettings.UserCollectionName);
    });

    builder.Services.AddSingleton(sp =>
    {
        var client = sp.GetRequiredService<IMongoClient>();
        var database = client.GetDatabase(mongoSettings.VareDatabaseName);
        return database.GetCollection<Product>(mongoSettings.VareCollectionName);
    });

<<<<<<< HEAD
    // Add MongoDBSettings to DI
    builder.Services.Configure<MongoDBSettings>(builder.Configuration.GetSection("MongoDB"));
=======
builder.Services.AddControllers();
>>>>>>> parent of 8a6c13c (nlog med loki)

    builder.Services.AddControllers();
    builder.Logging.ClearProviders();
    builder.Host.UseNLog();

    var app = builder.Build();

    // Use Authentication (if needed)
    // app.UseAuthentication();
    app.UseAuthorization();

<<<<<<< HEAD
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
=======
app.Run();
>>>>>>> parent of 8a6c13c (nlog med loki)
