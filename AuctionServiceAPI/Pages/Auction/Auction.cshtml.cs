using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Http;
using Models;
using MongoDB.Driver;
using System.Net.Http;
using System.Net.Http.Json;

namespace MyApp.Auktion
{
    [Authorize]
    public class AuktionModel : PageModel
    {
        private readonly IHttpClientFactory? _clientFactory = null;
        private readonly IMongoClient _mongoClient;
        public Auction? Auktion { get; set; }
        public List<Bid>? Bids { get; set; }
        public string[]? ImageUrls { get; set; }

        public AuktionModel(IHttpClientFactory clientFactory, IMongoClient mongoClient)
        {
            _clientFactory = clientFactory;
            _mongoClient = mongoClient;
        }




        public void OnGet(string id)
        {
            using HttpClient? client = _clientFactory?.CreateClient("gateway");
            try
            {
                // Hent data fra API baseret på ID
                Auktion = client?.GetFromJsonAsync<Auction>(
                $"catalog/GetProductById?id={id}").Result;
            }
            catch (Exception ex)
            {
                Console.WriteLine("DUI" + ex.Message);
            }




            // Hent data fra MongoDB
            var database1 = _mongoClient.GetDatabase(Environment.GetEnvironmentVariable("AuctionDB"));
            var auctionCollection = database1.GetCollection<Auction>(Environment.GetEnvironmentVariable("AuctionCollection"));

            var database2 = _mongoClient.GetDatabase(Environment.GetEnvironmentVariable("BidDB"));
            var bidCollection = database2.GetCollection<Bid>(Environment.GetEnvironmentVariable("BidCollection"));

            var database3 = _mongoClient.GetDatabase(Environment.GetEnvironmentVariable("ProductDB"));
            var imageCollection = database3.GetCollection<AuctionImages>("ImageCollectionName");



            // Få auktion fra MongoDB
            var auctionFromDb = auctionCollection.Find(a => a.Id.ToString() == id).FirstOrDefault();
            var bidsFromDb = bidCollection.Find(b => b.AuctionId.ToString() == id).ToList();



            if (auctionFromDb != null)
            {
                auctionFromDb.Bids = bidsFromDb;
                auctionFromDb.CurrentBid = bidsFromDb.OrderByDescending(b => b.Value).FirstOrDefault();

                // Hent billeder fra tredje MongoDB-database
                ImageUrls = imageCollection.Find(img => img.AuctionId == auctionFromDb.Id).FirstOrDefault()?.ImageUrls;
            }

            // Kombinér data fra API og MongoDB
            if (auctionFromDb != null)
            {
                Auktion = Auktion ?? auctionFromDb;
                if (Auktion == null)
                {
                    Auktion = auctionFromDb;
                }
            }
        }


        // Metode til at håndtere bud
        public IActionResult OnPostBid(Guid auctionId, decimal bidValue)
        {
            if (!User.Identity.IsAuthenticated)
            {
                return RedirectToPage("/Account/Login", new { returnUrl = $"/Auktion/{auctionId}" });
            }

            // Her kan du tilføje logik til at gemme buddet i databasen
            return Page();
        }


    }


    public class AuctionImages
    {
        public Guid AuctionId { get; set; }
        public string[] ImageUrls { get; set; }
    }
}
