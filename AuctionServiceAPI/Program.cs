using MongoDB.Driver;
using Models;
using Microsoft.AspNetCore.Identity;

var builder = WebApplication.CreateBuilder(args);


// Configure MongoDB settings
var mongoSettings = builder.Configuration.GetSection("MongoDB");
var connectionString = mongoSettings["ConnectionString"];
var databaseName = mongoSettings["DatabaseName"];
var collectionName = mongoSettings["CollectionName"];

// Register MongoDB services
builder.Services.AddSingleton<IMongoClient, MongoClient>(sp => new MongoClient(connectionString));
builder.Services.AddSingleton(sp =>
{
    var client = sp.GetRequiredService<IMongoClient>();
    var database = client.GetDatabase(databaseName);
    return database.GetCollection<Auction>(collectionName);
});

builder.Services.AddControllers();

var app = builder.Build();

app.UseAuthorization();

app.MapControllers();

app.Run();
