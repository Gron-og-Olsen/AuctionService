using System;
using System.Collections.Generic;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Models
{
    public class Auction
    {
        [BsonId]
        [BsonRepresentation(BsonType.String)]
        public Guid Id { get; set; }                  // Auction identifier
        public string ProductId { get; set; }          // ID on product from CatalogService
        public Bid CurrentBid { get; set; }            // Current highest bid
        public List<Bid> Bids { get; set; }            // Bid history
        public DateTime AuctionStartTime { get; set; } // Start time for the auction
        public DateTime AuctionEndTime { get; set; }   // End time for the auction
        public string Status { get; set; }             // Status of the auction: active, completed, cancelled
    }

    public class Bid
    {
        [BsonId]
        [BsonRepresentation(BsonType.String)] 
        public Guid BidId { get; set; }
        
        [BsonRepresentation(BsonType.String)]             // Unique bid ID
        public Guid AuctionId { get; set; }
        
        [BsonRepresentation(BsonType.String)]           // ID of the auction this bid is for
        public Guid UserId { get; set; }               // User ID of the person who placed the bid
        public decimal Value { get; set; }             // Bid amount
        public DateTime DateTime { get; set; }         // Time when the bid was placed
        public string Status { get; set; }             // Status of the bid: accepted, rejected
    }

    public class AuctionRequest
    {
        public string ProductId { get; set; }          // Product ID from CatalogService
        public DateTime AuctionStartTime { get; set; } // Start time for the auction
        public DateTime AuctionEndTime { get; set; }   // End time for the auction
    }

    public class WinnerRequest
    {
        public Guid AuctionId { get; set; }            // ID of the auction
        public Guid UserId { get; set; }               // ID of the user who is the winner
    }
}
