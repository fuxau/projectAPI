using Newtonsoft.Json;
using RestSharp;
using NolanReymondMapApi.Models;

namespace NolanReymondMapApi.Controllers
{
    public class RouteController
    {
        private const string GoogleMapsApiKey = "AIzaSyAFadmjQSC5N_GkjgtphV0rswV35OYhoII";

        public async Task<List<Route>> GetRoutesAsync(string origin, string destination)
        {
            var client = new RestClient("https://routes.googleapis.com");
            var request = new RestRequest("/directions/v2:computeRoutes", Method.Post);

            // Ajout des en-têtes
            request.AddHeader("Content-Type", "application/json");
            request.AddHeader("X-Goog-FieldMask", "routes.distanceMeters,routes.duration,routes.polyline.encodedPolyline");

            // Paramètres de la requête
            request.AddQueryParameter("key", GoogleMapsApiKey);

            var body = new
            {
                origin = new { address = origin },
                destination = new { address = destination },
                travelMode = "DRIVE",
                routingPreference = "TRAFFIC_AWARE",
                computeAlternativeRoutes = true, // Autorise plusieurs itinéraires
                languageCode = "fr"
            };
            request.AddJsonBody(body);

            // Envoi de la requête
            var response = await client.ExecuteAsync(request);

            // Vérification de la réponse
            if (!response.IsSuccessful)
            {
                Console.WriteLine($"Erreur API : {response.StatusCode} - {response.Content}");
                throw new Exception("Erreur lors de l'appel à l'API Google Maps.");
            }

            // Désérialisation du JSON
            var jsonResponse = JsonConvert.DeserializeObject<RoutesResponse>(response.Content);
            if (jsonResponse?.Routes == null)
            {
                Console.WriteLine("Erreur : La réponse JSON ne contient pas de routes.");
                return new List<Route>();
            }

            return jsonResponse.Routes;
        }
    }
}
