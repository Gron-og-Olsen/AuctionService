namespace AuctionService.Configuration
{
    public class MongoDBSettings
    {
        // MongoDB connection string 
        public string? ConnectionString { get; set; }

        // Name of the Auction database and collections
        public string? AuctionDatabaseName { get; set; }
        public string? AuctionCollectionName { get; set; }

        // Name of the Bid database and collections
        public string? BidDatabaseName { get; set; }
        public string? BidCollectionName { get; set; }

        // Name of the User database and collections
        public string? UserDatabaseName { get; set; }
        public string? UserCollectionName { get; set; }

        // Name of the Vare database and collections
        public string? VareDatabaseName { get; set; }
        public string? VareCollectionName { get; set; }
    }
}
