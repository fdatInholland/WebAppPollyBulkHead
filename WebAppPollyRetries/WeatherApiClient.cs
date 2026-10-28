using WebAppPollyBulkHead.DTO;

namespace WebAppPollyBulkHead
{
    public class WeatherApiClient
    {
        private readonly HttpClient _httpClient;

        // HttpClient is automatically injected by DI because of AddHttpClient<WeatherApiClient>()
        public WeatherApiClient(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<WeatherResponse?> GetWeatherAsync(string city, CancellationToken cancellationToken = default)
        {
            // Any transient error or timeout during this call will automatically 
            // trigger the retry pipeline configured in Program.cs!
            var response = await _httpClient.GetFromJsonAsync<WeatherResponse>(
                $"forecast?city={city}",
                cancellationToken);

            return response;
        }
    }
}
