namespace AuctionService.Configuration
{
    public class MongoDBSettings
    {
        // MongoDB connection string 
        public string? ConnectionString { get; set; }

        // Name of the database 
        public string? DatabaseName { get; set; }

        // Name of the collection 
        public string? CollectionName { get; set; }
    }
}
