using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;

namespace Models
{
    public enum Category
    {
        Electronics = 1,
        Clothing = 2,
        HomeAppliances = 3,
        Books = 4,
        Paintings = 5,

        // tilføj eventuelt flere kategorier
    }

    public class Product
    {
        [BsonId]
        [BsonRepresentation(BsonType.String)] // Ensures the Guid is stored as a string in MongoDB
        public Guid Id { get; set; }
        public string? Name { get; set; }
        public string? Description { get; set; }
        public Category Category { get; set; } // Ændret fra string til enum
        public decimal FinalPrice { get; set; }
        public decimal CurrentBid { get; set; }
        public string? Brand { get; set; }
        public string? Model { get; set; }
        public string Condition { get; set; }
        public string[]? ImageUrls { get; set; } // Ændret til array af strenge, så vi kan have flere billeder
        public string Valuation { get; set; }
        public DateTime ReleaseDate { get; set; }
        public DateTime ExpiryDate { get; set; }
    }
}