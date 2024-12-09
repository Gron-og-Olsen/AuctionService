using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using Models;
using Microsoft.Extensions.Logging;  // Logger namespace

namespace AuctionService.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class AuctionController : ControllerBase
    {
        private readonly IMongoCollection<Auction> _auctionCollection;
        private readonly ILogger<AuctionController> _logger;

        public AuctionController(IMongoCollection<Auction> auctionCollection, ILogger<AuctionController> logger)
        {
            _auctionCollection = auctionCollection;
            _logger = logger;
        }

        // Endpoint for creating a new auction via POST
        [HttpPost(Name = "CreateAuction")]
        public async Task<ActionResult<Auction>> CreateAuction([FromBody] AuctionRequest newAuctionRequest)
        {
            _logger.LogInformation("Method CreateAuction called at {DT}", DateTime.UtcNow.ToLongTimeString());

            // Assign a new ID if not provided
            var newAuction = new Auction
            {
                Id = Guid.NewGuid(),
                ProductId = newAuctionRequest.ProductId,
                AuctionStartTime = newAuctionRequest.AuctionStartTime,
                AuctionEndTime = newAuctionRequest.AuctionEndTime,
                Status = "active",  // Set initial status to active
                CurrentBid = null,  // No bids at the start
                Bids = new List<Bid>()  // Empty bid history at the start
            };

            // Insert the new auction into MongoDB
            await _auctionCollection.InsertOneAsync(newAuction);

            // Log information about the auction being created
            _logger.LogInformation("New auction with ID {ID} created at {DT}", newAuction.Id, DateTime.UtcNow.ToLongTimeString());

            // Return the created auction with a 201 Created status code
            return CreatedAtRoute("GetAuctionById", new { AuctionId = newAuction.Id }, newAuction);
        }

        // Endpoint to get an auction by ID
        [HttpGet("id/{AuctionId}", Name = "GetAuctionById")]
        public async Task<ActionResult<Auction>> GetAuctionById(Guid AuctionId)
        {
            _logger.LogInformation("Method GetAuctionById called at {DT}", DateTime.UtcNow.ToLongTimeString());

            var auction = await _auctionCollection.Find(a => a.Id == AuctionId).FirstOrDefaultAsync();
            if (auction == null)
            {
                return NotFound(new { message = $"Auction with ID {AuctionId} not found." });
            }

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
    }
}
