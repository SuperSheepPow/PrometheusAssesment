using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using System.Net.Http;
using System.Text.Json;
using api.Models;

namespace api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class StockController : ControllerBase
    {
       private readonly HttpClient _httpClient;

        public StockController(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        [HttpGet("{symbol}")]
        [ProducesResponseType(typeof(List<Stock>), 200)]

        public async Task<IActionResult> GetStockData(string symbol)
        {
            var url = $"https://query1.finance.yahoo.com/v8/finance/chart/{symbol}?interval=15m";
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36");

            var response = await _httpClient.GetStringAsync(url);
            var jsonData = JsonSerializer.Deserialize<JsonElement>(response);

            // TODO: process jsonData -> group by day, calculate averages
            return Ok(jsonData);
        } 
    }
}