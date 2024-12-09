using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using Models;

namespace AuctionService.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class AuctionController : ControllerBase
    {
        private readonly IMongoCollection<Auction> _auctionCollection;
        private readonly IMongoCollection<Bid> _bidCollection;
        private readonly IMongoCollection<User> _userCollection;
        private readonly IMongoCollection<Product> _vareCollection;
        private readonly ILogger<AuctionController> _logger;

        public AuctionController(
            IMongoCollection<Auction> auctionCollection,
            IMongoCollection<Bid> bidCollection,
            IMongoCollection<User> userCollection,
            IMongoCollection<Product> vareCollection,
            ILogger<AuctionController> logger)
        {
            _auctionCollection = auctionCollection;
            _bidCollection = bidCollection;
            _userCollection = userCollection;
            _vareCollection = vareCollection;
            _logger = logger;
        }

        [HttpPost(Name = "CreateAuction")]
        public async Task<ActionResult<Auction>> CreateAuction([FromBody] AuctionRequest newAuctionRequest)
        {
            _logger.LogInformation("Method CreateAuction called at {DT}", DateTime.UtcNow.ToLongTimeString());

            // Assuming newAuctionRequest.ProductId is a string and needs to be converted to Guid
            Guid productId = Guid.Parse(newAuctionRequest.ProductId);

            // Query to find the product based on ProductId
            var product = await _vareCollection.Find(v => v.Id == productId).FirstOrDefaultAsync();
            if (product == null)
            {
                return NotFound(new { message = $"Product with ID {productId} not found." });
            }

            // Create the auction using the start and end time from the Product entity
            var newAuction = new Auction
            {
                Id = Guid.NewGuid(),
                ProductId = newAuctionRequest.ProductId,
                AuctionStartTime = product.ReleaseDate,  // Set AuctionStartTime from Product's ReleaseDate
                AuctionEndTime = product.ExpiryDate,    // Set AuctionEndTime from Product's ExpiryDate
                Status = "active",                      // Set initial status to active
                CurrentBid = null,                      // No bids at the start
                Bids = new List<Bid>()                  // Empty bid history at the start
            };

            // Insert the new auction into the database
            await _auctionCollection.InsertOneAsync(newAuction);
            _logger.LogInformation("New auction created with ID {ID} at {DT}", newAuction.Id, DateTime.UtcNow.ToLongTimeString());

            return CreatedAtRoute("GetAuctionById", new { AuctionId = newAuction.Id }, newAuction);
        }


        [HttpGet("id/{AuctionId}", Name = "GetAuctionById")]
        public async Task<ActionResult<Auction>> GetAuctionById(Guid AuctionId)
        {
            _logger.LogInformation("Method GetAuctionById called at {DT}", DateTime.UtcNow.ToLongTimeString());

            var auction = await _auctionCollection.Find(a => a.Id == AuctionId).FirstOrDefaultAsync();
            if (auction == null)
            {
                return NotFound(new { message = $"Auction with ID {AuctionId} not found." });
            }

            // Fetch the latest bid from Bid collection
            var latestBid = await _bidCollection.Find(b => b.BidId == AuctionId)
                                                 .SortByDescending(b => b.DateTime)
                                                 .FirstOrDefaultAsync();

            auction.CurrentBid = latestBid;
            return Ok(auction);
        }

        // Endpoint to get all auctions
        [HttpGet(Name = "GetAllAuctions")]
        public async Task<ActionResult<List<Auction>>> GetAllAuctions()
        {
            _logger.LogInformation("Method GetAllAuctions called at {DT}", DateTime.UtcNow.ToLongTimeString());

            var auctions = await _auctionCollection.Find(_ => true).ToListAsync();

            return Ok(auctions);
        }
        // Endpoint to get all bids made by a specific user (bidder)
        [HttpGet("bidder/{userId}", Name = "GetBidsByUserId")]
        public async Task<ActionResult<List<Bid>>> GetBidsByUserId(Guid userId)
        {
            _logger.LogInformation("Method GetBidsByUserId called at {DT}", DateTime.UtcNow.ToLongTimeString());

            // Find all bids made by the user
            var bids = await _bidCollection.Find(b => b.UserId == userId).ToListAsync();

            if (bids == null || bids.Count == 0)
            {
                return NotFound(new { message = $"No bids found for user with ID {userId}." });
            }

            return Ok(bids);
        }


        // Endpoint to mark the winner of an auction
        [HttpPost("winner", Name = "MarkWinner")]
        public async Task<ActionResult<Auction>> MarkWinner([FromBody] WinnerRequest winnerRequest)
        {
            _logger.LogInformation("Method MarkWinner called at {DT}", DateTime.UtcNow.ToLongTimeString());

            var auction = await _auctionCollection.Find(a => a.Id == winnerRequest.AuctionId).FirstOrDefaultAsync();
            if (auction == null)
            {
                return NotFound(new { message = $"Auction with ID {winnerRequest.AuctionId} not found." });
            }

            // Mark the winner (the last user who placed the highest bid)
            auction.Status = "completed";
            auction.CurrentBid = auction.Bids.OrderByDescending(b => b.DateTime).FirstOrDefault(); // Get the last bid as the current bid

            // Update the auction in the database
            await _auctionCollection.ReplaceOneAsync(a => a.Id == auction.Id, auction);

            // Log information about the winner being marked
            _logger.LogInformation("Auction with ID {ID} marked as completed with winner {WinnerId} at {DT}", auction.Id, auction.CurrentBid.UserId, DateTime.UtcNow.ToLongTimeString());

            return Ok(auction);
        }
        // Endpoint to get all bids placed in all auctions
        [HttpGet("bids", Name = "GetAllBids")]
        public async Task<ActionResult<List<Bid>>> GetAllBids()
        {
            _logger.LogInformation("Method GetAllBids called at {DT}", DateTime.UtcNow.ToLongTimeString());

            // Find all bids
            var bids = await _bidCollection.Find(_ => true).ToListAsync();

            return Ok(bids);
        }
        //TODO
        // gennem rabbit til worker
        [HttpPost("bid", Name = "PlaceBid")]
        public async Task<ActionResult<Bid>> PlaceBid([FromBody] Bid newBid)
        {
            _logger.LogInformation("Method PlaceBid called at {DT}", DateTime.UtcNow.ToLongTimeString());

            // Find the auction
            var auction = await _auctionCollection.Find(a => a.Id == newBid.AuctionId).FirstOrDefaultAsync();
            if (auction == null)
            {
                return NotFound(new { message = $"Auction with ID {newBid.AuctionId} not found." });
            }

            // Ensure the bid amount is valid (e.g., greater than the current bid)
            if (newBid.Value <= (auction.CurrentBid?.Value ?? 0))
            {
                return BadRequest(new { message = "Bid amount must be higher than the current bid." });
            }

            // Assign a new BidId if not already set
            newBid.BidId = Guid.NewGuid();
            newBid.DateTime = DateTime.UtcNow; // Set the bid timestamp

            // Insert the bid into the database
            await _bidCollection.InsertOneAsync(newBid);

            // Optionally, update the auction to reflect the new bid
            auction.CurrentBid = newBid; // Update the auction with the new bid
            await _auctionCollection.ReplaceOneAsync(a => a.Id == auction.Id, auction);

            _logger.LogInformation("New bid placed for auction {AuctionId} by user {UserId} at {DT}", newBid.AuctionId, newBid.UserId, DateTime.UtcNow.ToLongTimeString());

            return CreatedAtAction("GetAuctionById", new { AuctionId = auction.Id }, newBid);
        }



    }
}
