// using Microsoft.AspNetCore.Mvc.RazorPages;
// using Microsoft.Extensions.Http;
// using Models;
// using MongoDB.Driver;
// using System.Net.Http;
// using System.Net.Http.Json;

// namespace MyApp.CatalogList
// {
//     public class CatalogListModel : PageModel
//     {
//         private readonly IHttpClientFactory? _clientFactory = null;
//         private readonly IMongoClient _mongoClient;
//         public List<Product>? Products { get; set; }

//         public CatalogListModel(IHttpClientFactory clientFactory, IMongoClient mongoClient)
//         {
//             _clientFactory = clientFactory;
//             _mongoClient = mongoClient;
//         }

//         public void OnGet()
//         {
//             using HttpClient? client = _clientFactory?.CreateClient("gateway");
//             try
//             {
//                 Products = client?.GetFromJsonAsync<List<Product>>(
//                 "catalog/GetProductsByCategory?category=1").Result;
//             }
//             catch (Exception ex)
//             {
//                 Console.WriteLine(ex.Message);
//             }

//             // Ekstra kode for at hente data fra MongoDB
//             var database = _mongoClient.GetDatabase("AuctionDB");
//             var collection = database.GetCollection<Product>("AuctionCollection");
//             var productsFromDb = collection.Find(FilterDefinition<Product>.Empty).ToList();

//             // Eksempel på at kombinere data fra API og MongoDB
//             if (Products != null)
//             {
//                 Products.AddRange(productsFromDb);
//             }
//             else
//             {
//                 Products = productsFromDb;
//             }
//         }
//     }
// }
