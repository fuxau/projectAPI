using Newtonsoft.Json;
using System.Net.Http.Headers;
using System.Text;
using ville;
using System.Globalization; // Pour CultureInfo
using System.Text.Json;     // Pour JsonDocument

namespace NolanReymondMapApi.Controllers
{
    public class TrainController
    {
        private const string SncfApiKey = "272c0ef1-9ecb-4acc-a168-22e96ef262c8";

        public async Task<List<Result>> SearchStationsAsync(string query)
        {
            using var client = new HttpClient();
            string url = $"https://ressources.data.sncf.com/api/explore/v2.1/catalog/datasets/liste-des-gares/records?refine=voyageurs%3A%22O%22&limit=50&where=libelle like \"%{query}%\" or commune like \"%{query}%\"";

            var response = await client.GetAsync(url);

            if (!response.IsSuccessStatusCode)
            {
                Console.WriteLine($"Erreur lors de la recherche des gares : {response.StatusCode}");
                return new List<Result>();
            }

            var jsonResponse = await response.Content.ReadAsStringAsync();
            var root = JsonConvert.DeserializeObject<Root>(jsonResponse);
            return root?.results ?? new List<Result>();
        }

        public async Task<JourneyInfo?> GetJourneyAsync(string gareDepUIC, string gareArvUIC)
        {
            using var client = new HttpClient();
            var authValue = Convert.ToBase64String(Encoding.ASCII.GetBytes($"{SncfApiKey}:"));
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", authValue);

            string url = $"https://api.sncf.com/v1/coverage/sncf/journeys?from=stop_area:SNCF:{gareDepUIC}&to=stop_area:SNCF:{gareArvUIC}";
            var response = await client.GetAsync(url);

            if (!response.IsSuccessStatusCode)
            {
                Console.WriteLine($"Erreur lors de la recherche du trajet : {response.StatusCode}");
                return null;
            }

            var jsonResponse = await response.Content.ReadAsStringAsync();
            var jsonDoc = JsonDocument.Parse(jsonResponse);

            if (jsonDoc.RootElement.TryGetProperty("journeys", out var journeys) && journeys.GetArrayLength() > 0)
            {
                var journey = journeys[0];
                var duration = journey.GetProperty("duration").GetInt32();
                var carbon = journey.GetProperty("co2_emission").GetProperty("value").GetDouble();

                string departureDateTime = journey.GetProperty("departure_date_time").GetString();
                string arrivalDateTime = journey.GetProperty("arrival_date_time").GetString();

                string format = "yyyyMMdd'T'HHmmss";
                DateTime departureDate = DateTime.ParseExact(departureDateTime, format, CultureInfo.InvariantCulture);
                DateTime arrivalDate = DateTime.ParseExact(arrivalDateTime, format, CultureInfo.InvariantCulture);

                return new JourneyInfo
                {
                    Duration = TimeSpan.FromSeconds(duration),
                    CarbonEmission = carbon,
                    DepartureTime = departureDate,
                    ArrivalTime = arrivalDate
                };
            }

            return null;
        }
    }
}