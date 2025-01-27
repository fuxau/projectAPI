using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Globalization;
using System.Net.Http;
using System.Net.Http.Headers;
using Microsoft.Maui.Controls;
using NolanReymondMapApi.Controllers;

namespace NolanReymondMapApi;

public partial class MainPage : ContentPage
{
    private readonly RouteController _routeController;
    private readonly TrainController _trainController;

    private string gareDepUIC = string.Empty; // Code UIC pour la gare de départ (SNCF)
    private string gareArvUIC = string.Empty; // Code UIC pour la gare d'arrivée (SNCF)

    public MainPage()
    {
        InitializeComponent();
        _routeController = new RouteController();
        _trainController = new TrainController();

        // Initialiser les DatePicker et TimePicker avec des valeurs par défaut
        DepartureDatePicker.Date = DateTime.Today;
        DepartureTimePicker.Time = DateTime.Now.TimeOfDay;
    }

    // Recherche des itinéraires routiers (Google Maps)
    private async void OnSearchRoutesClicked(object sender, EventArgs e)
    {
        try
        {
            string startCity = StartCityEntry?.Text;
            string endCity = EndCityEntry?.Text;

            if (string.IsNullOrEmpty(startCity) || string.IsNullOrEmpty(endCity))
            {
                await DisplayAlert("Erreur", "Veuillez entrer une ville de départ et une ville d'arrivée.", "OK");
                return;
            }

            var routes = await _routeController.GetRoutesAsync(startCity, endCity);

            if (routes == null || !routes.Any())
            {
                await DisplayAlert("Erreur", "Aucun itinéraire trouvé.", "OK");
                return;
            }

            // Affichage des itinéraires
            RoutesCollectionView.ItemsSource = routes.Select((route, index) => new
            {
                Title = $"Itinéraire {index + 1}",
                Distance = $"{route.DistanceMeters / 1000.0:F1} km",
                Duration = route.Duration.TotalHours >= 1
                    ? $"{(int)route.Duration.TotalHours}h {route.Duration.Minutes}m"
                    : $"{route.Duration.Minutes}m",
            }).ToList();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Erreur détectée : {ex.Message}");
            await DisplayAlert("Erreur", $"Une erreur est survenue : {ex.Message}", "OK");
        }
    }

    // Recherche des gares (SNCF)
    private async void OnSearchVilleButtonClicked(object sender, EventArgs e)
    {
        if (sender is Button button && button.CommandParameter is string direction)
        {
            string query = direction == "Dep" ? cityEntryDep.Text : cityEntryArv.Text;
            StackLayout dynamicContainer = direction == "Dep" ? DynamicButtonsContainerDep : DynamicButtonsContainerArv;
            Frame dynamicFrame = direction == "Dep" ? DynamicButtonsContainerDepFrame : DynamicButtonsContainerArvFrame;

            if (string.IsNullOrEmpty(query))
            {
                await DisplayAlert("Erreur", "Veuillez entrer une ville ou un nom de gare.", "OK");
                return;
            }

            var results = await _trainController.SearchStationsAsync(query);
            dynamicContainer.Children.Clear();

            if (results != null && results.Any())
            {
                foreach (var result in results)
                {
                    Button gareButton = new Button
                    {
                        Text = $"{result.libelle}",
                        CommandParameter = result.code_uic,
                        BackgroundColor = Colors.DarkGray,
                        TextColor = Colors.White,
                        CornerRadius = 10,
                        Padding = 10,
                        Margin = new Thickness(0, 5),
                        FontSize = 16
                    };

                    gareButton.Clicked += OnGareButtonClicked;
                    dynamicContainer.Children.Add(gareButton);
                }

                dynamicFrame.IsVisible = true;

                // Animation d'ouverture
                await dynamicFrame.TranslateTo(0, 0, 250, Easing.CubicIn);
            }
            else
            {
                await DisplayAlert("Résultat", "Aucune gare trouvée pour cette recherche.", "OK");
            }
        }
    }

    // Sélection d'une gare (SNCF)
    private async void OnGareButtonClicked(object sender, EventArgs e)
    {
        if (sender is Button button && button.CommandParameter is string codeUIC)
        {
            string direction = button.Parent == DynamicButtonsContainerDep ? "Dep" : "Arv";
            Frame dynamicFrame = direction == "Dep" ? DynamicButtonsContainerDepFrame : DynamicButtonsContainerArvFrame;

            // Vérifie si c'est pour la gare de départ ou d'arrivée
            if (direction == "Dep")
            {
                gareDepUIC = codeUIC;
                cityEntryDep.Text = button.Text; // Met à jour le champ avec le nom de la gare
            }
            else
            {
                gareArvUIC = codeUIC;
                cityEntryArv.Text = button.Text; // Met à jour le champ avec le nom de la gare
            }

            // Animation de fermeture
            await dynamicFrame.TranslateTo(0, -10, 250, Easing.CubicOut);
            dynamicFrame.IsVisible = false;
        }
    }

    // Recherche d'un trajet ferroviaire (SNCF)
    private async void OnSearchTrajetButtonClicked(object sender, EventArgs e)
    {
        try
        {
            if (string.IsNullOrEmpty(gareDepUIC) || string.IsNullOrEmpty(gareArvUIC))
            {
                await DisplayAlert("Erreur", "Veuillez sélectionner les gares de départ et d'arrivée.", "OK");
                return;
            }

            // Récupérer la date et l'heure sélectionnées
            DateTime selectedDate = DepartureDatePicker?.Date ?? DateTime.Today;
            TimeSpan selectedTime = DepartureTimePicker?.Time ?? DateTime.Now.TimeOfDay;

            // Combiner la date et l'heure en un seul DateTime
            DateTime departureDateTime = selectedDate.Date + selectedTime;

            // Formater la DateTime selon le format requis par l'API (yyyyMMdd'T'HHmmss)
            string formattedDateTime = departureDateTime.ToString("yyyyMMdd'T'HHmmss");

            HttpClient client = new HttpClient();
            var authValue = Convert.ToBase64String(Encoding.ASCII.GetBytes($"272c0ef1-9ecb-4acc-a168-22e96ef262c8:")); // Remplacez par vos propres identifiants si nécessaire
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", authValue);

            // Construire l'URL avec les paramètres datetime et count=5
            string url = $"https://api.sncf.com/v1/coverage/sncf/journeys?from=stop_area:SNCF:{gareDepUIC}&to=stop_area:SNCF:{gareArvUIC}&datetime={formattedDateTime}&count=5";

            var response = await client.GetAsync(url);

            if (!response.IsSuccessStatusCode)
            {
                await DisplayAlert("Erreur", $"Erreur API : {response.StatusCode}", "OK");
                return;
            }

            var jsonResponse = await response.Content.ReadAsStringAsync();
            Console.WriteLine("Réponse de l'API : " + jsonResponse);

            var jsonDoc = JsonDocument.Parse(jsonResponse);

            if (jsonDoc.RootElement.TryGetProperty("journeys", out var journeys) && journeys.GetArrayLength() > 0)
            {
                var formattedJourneys = new List<object>();

                foreach (var journey in journeys.EnumerateArray())
                {
                    var duration = journey.GetProperty("duration").GetInt32();
                    var carbon = journey.GetProperty("co2_emission").GetProperty("value").GetDouble();

                    string departureDateTimeStr = journey.GetProperty("departure_date_time").GetString();
                    string arrivalDateTimeStr = journey.GetProperty("arrival_date_time").GetString();

                    string format = "yyyyMMdd'T'HHmmss";
                    DateTime departureDate = DateTime.ParseExact(departureDateTimeStr, format, CultureInfo.InvariantCulture);
                    DateTime arrivalDate = DateTime.ParseExact(arrivalDateTimeStr, format, CultureInfo.InvariantCulture);

                    formattedJourneys.Add(new
                    {
                        DepartureTime = $"Départ : {departureDate:dd/MM/yyyy HH:mm}",
                        ArrivalTime = $"Arrivée : {arrivalDate:dd/MM/yyyy HH:mm}",
                        Duration = $"Durée : {(duration > 0 ? TimeSpan.FromSeconds(duration).ToString(@"hh\:mm\:ss") : "Non disponible")}",
                        CarbonEmission = $"Empreinte carbone : {(carbon >= 0 ? carbon + " gCO2" : "Non disponible")}"
                    });
                }

                // Assignation des données formatées à la CollectionView
                SncfRoutesCollectionView.ItemsSource = formattedJourneys;
            }
            else
            {
                await DisplayAlert("Erreur", "Aucun trajet trouvé pour ce parcours.", "OK");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Erreur lors du traitement de la réponse JSON : {ex.Message}");
            await DisplayAlert("Erreur", "Une erreur est survenue lors du traitement des données.", "OK");
        }
    }
}