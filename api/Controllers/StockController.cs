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
            var url = $"https://query1.finance.yahoo.com/v8/finance/chart/{symbol}?interval=15m&range=1mo";
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36");
            //changing some code around to actually get the response status, so i go in with GetAsync before converting to stringAsync
            var response = await _httpClient.GetAsync(url);

            if (!response.IsSuccessStatusCode)
            {
                return NotFound(new { message = $"The stock symbol '{symbol}' was not found." });
            }
            var responseContent = await response.Content.ReadAsStringAsync();

            var jsonData = JsonSerializer.Deserialize<JsonElement>(responseContent);
            


            //After looking through the json data, I want to pull the important variables for the stock data.
            var result = jsonData.GetProperty("chart").GetProperty("result")[0];
            var timestampsJson = result.GetProperty("timestamp");
            List<long> timestamps = new List<long>();
            foreach (var t in timestampsJson.EnumerateArray())
            {
                timestamps.Add(t.GetInt64());
            }

            // quote seems to contain the open, high, low, volume arrays, so ill collect it to use as a clean jumping off point
            var quote = result.GetProperty("indicators").GetProperty("quote")[0];

            //for highs lows and volumes, i need to convert the json arrays into c# lists and collect them
            // highs
            var highsJson = quote.GetProperty("high");
            List<decimal> highs = new List<decimal>();
            foreach (var h in highsJson.EnumerateArray())
            {
                highs.Add(h.ValueKind == JsonValueKind.Number ? h.GetDecimal() : 0); //to be safe im putting in a check for nulls
            }

            // lows
            var lowsJson = quote.GetProperty("low");
            List<decimal> lows = new List<decimal>();
            foreach (var l in lowsJson.EnumerateArray())
            {
                lows.Add(l.ValueKind == JsonValueKind.Number ? l.GetDecimal() : 0);
            }

            // volumes
            var volumesJson = quote.GetProperty("volume");
            List<long> volumes = new List<long>();
            foreach (var v in volumesJson.EnumerateArray())
            {
                volumes.Add(v.ValueKind == JsonValueKind.Number ? v.GetInt64() : 0);
            }

            //Need to group data by day rather than minute intervals
            /*
            My idea here is to make a dictionary, with each key representing a date, and the value being a list of an intraday value.

            If we dont have the date, add to the dictionary, else append to the value list
            */
            Dictionary<string, List<(decimal low, decimal high, long volume)>> dataByDay = new Dictionary<string, List<(decimal low, decimal high, long volume)>>();

            for (int i = 0; i < timestamps.Count; i++)
            {
                DateTime date = DateTimeOffset.FromUnixTimeSeconds(timestamps[i]).DateTime.Date;
                string key = date.ToString("yyyy-MM-dd");

                //if the date hasnt been added to the dictionary yet, initialize a new date
                if (!dataByDay.ContainsKey(key))
                    dataByDay[key] = new List<(decimal, decimal, long)>();

                //add the intraday data to its corresponding day
                dataByDay[key].Add((lows[i], highs[i], volumes[i]));
            }

            List<Stock> finalData = new List<Stock>();
            //Now that we have each days data grouped, i just need to average the highs and lows, and then sum the volumes
            //From there, we can construct Stock objects, and add them to our final list to be displayed in swagger.
            foreach (var dayEntry in dataByDay)
            {
                decimal lowSum = 0;
                decimal highSum = 0;
                long volumeSum = 0;
                int count = dayEntry.Value.Count;

                foreach (var item in dayEntry.Value)
                {
                    lowSum += (decimal)item.low;
                    highSum += (decimal)item.high;
                    volumeSum += item.volume;
                }

                finalData.Add(new Stock
                {
                    Day = dayEntry.Key,
                    LowAverage = Math.Round(lowSum / count, 4),//the example was rounded to 4 decimal places, so im going with that.
                    HighAverage = Math.Round(highSum / count, 4),
                    Volume = volumeSum
                });
            }

            return Ok(finalData);
        } 
    }
}