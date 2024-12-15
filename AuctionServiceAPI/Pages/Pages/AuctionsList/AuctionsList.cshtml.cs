using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Http;
using Models;
using System.Net.Http;
using System.Net.Http.Json;


namespace MyApp.Namespace
{
    public class CatalogListModel : PageModel
    {
        private readonly IHttpClientFactory? _clientFactory = null;
        public List<Product>? Products { get; set; }
        public CatalogListModel(IHttpClientFactory clientFactory)
        => _clientFactory = clientFactory;
        public void OnGet()
        {
            using HttpClient? client = _clientFactory?.CreateClient("gateway");
            try
            {
                Products = client?.GetFromJsonAsync<List<Product>>(
                "catalog/GetProductsByCategory?category=1").Result;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }
    }
}