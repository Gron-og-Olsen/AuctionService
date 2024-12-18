using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using Models;
using RabbitMQ.Client;
using System.Text.Json;
using System.Text;
using Microsoft.AspNetCore.Authorization; // Add this for Encoding
using System.IO;


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
        private readonly string _rabbitHost;
        private readonly string _queueName = "bidsQueue"; // Køen, der modtager budbeskeder

        public AuctionController(
            IMongoCollection<Auction> auctionCollection,
            IMongoCollection<Bid> bidCollection,
            IMongoCollection<User> userCollection,
            IMongoCollection<Product> vareCollection,
            ILogger<AuctionController> logger,
            IConfiguration configuration,
            string rabbitMqHost) // Inject RabbitMQ host

        {
            _auctionCollection = auctionCollection;
            _bidCollection = bidCollection;
            _userCollection = userCollection;
            _vareCollection = vareCollection;
            _logger = logger;
            // Get RabbitMQ hostname from environment variable, throw exception if not found
            _rabbitHost = rabbitMqHost ?? throw new ArgumentNullException("RabbitMQ host not found in the environment variables.");
        }
        
        [HttpPost("Create", Name = "CreateAuction")]
        [Authorize]
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

        // Endpoint to get all auctions
        [HttpGet("GetAll", Name = "GetAllAuctions")]
        public async Task<ActionResult<List<Auction>>> GetAllAuctions()
        {
            _logger.LogInformation("Method GetAllAuctions called at {DT}", DateTime.UtcNow.ToLongTimeString());

            var auctions = await _auctionCollection.Find(_ => true).ToListAsync();

            return Ok(auctions);
        }
        
        [HttpGet("{auctionId}", Name = "GetAuctionById")]
        public async Task<ActionResult<Auction>> GetAuctionById(Guid auctionId)
        {
            var auction = await _auctionCollection.Find(a => a.Id == auctionId).FirstOrDefaultAsync();
            if (auction == null)
            {
                return NotFound(new { message = $"Auction with ID {auctionId} not found." });
            }
            return Ok(auction);
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

        [HttpPost("bid", Name = "PlaceBid")]
        [Authorize]
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

            // Optionally, update the auction to reflect the new bid
            auction.CurrentBid = newBid; // Update the auction with the new bid
            await _auctionCollection.ReplaceOneAsync(a => a.Id == auction.Id, auction);

            // Send the bid to RabbitMQ
            SendBidToQueue(newBid);

            _logger.LogInformation("New bid placed for auction {AuctionId} by user {UserId} at {DT}", newBid.AuctionId, newBid.UserId, DateTime.UtcNow.ToLongTimeString());

            return CreatedAtAction("GetAuctionById", new { AuctionId = auction.Id }, newBid);
        }

        // Method to send the bid to RabbitMQ
        private void SendBidToQueue(Bid bid)
        {
            var factory = new ConnectionFactory() { HostName = _rabbitHost }; // Use the rabbit host from the environment variable
            using (var connection = factory.CreateConnection())  // This should work with RabbitMQ.Client
            using (var channel = connection.CreateModel())
            {
                // Declare the queue (in case it's not already declared)
                channel.QueueDeclare(queue: "bidsQueue", durable: false, exclusive: false, autoDelete: false, arguments: null);

                // Serialize the bid object to JSON
                var message = JsonSerializer.Serialize(bid);
                var body = Encoding.UTF8.GetBytes(message);

                // Publish the message to the queue
                channel.BasicPublish(exchange: "", routingKey: "bidsQueue", basicProperties: null, body: body);
                _logger.LogInformation($"Bid for auction {bid.AuctionId} sent to RabbitMQ.");
            }
        }
    }
}