# IvanConnections Travel 🚍🗺️

**IvanConnections Travel** is a mobile app built with .NET MAUI that shows real-time positions of buses and trams on a Google Map. It's designed for flexibility and extensibility — you can even plug in your own backend API!

![Platform: Android](https://img.shields.io/badge/platform-Android-green)
![License: MIT](https://img.shields.io/badge/license-MIT-blue)
![.NET](https://img.shields.io/badge/.NET-9.0-blueviolet)
![MAUI](https://img.shields.io/badge/built%20with-.NET%20MAUI-512BD4)

## ✨ Features

- 🗺️ **Google Maps integration** – Vehicles shown live on the map
- 🚍 **Real-time data** – Pulls positions of buses and trams from a backend API
- 🔌 **Custom API support** – Default API is internal, but anyone can host their own
- ♿ **Accessibility details** – See if vehicles support bikes or wheelchairs
- ⚡ **Extra info** – Speed, direction, electric status, route color and more
- 📱 **Currently Android-only** – Ports to other platforms are welcome!

## 📦 Tech Stack

- [.NET MAUI](https://learn.microsoft.com/en-us/dotnet/maui/) – for cross-platform UI
- [Google Maps SDK](https://developers.google.com/maps/documentation)
- C# / XAML
- JSON-based REST API

## 🛠️ Build Requirements

- [.NET 9 SDK](https://dotnet.microsoft.com/en-us/download/dotnet/9.0)
- **Visual Studio 2022** with MAUI workload installed
- Android SDK and emulator or physical device

## 🚀 Getting Started

1. Clone the repo:
   ```bash
   git clone https://github.com/absotech/IvanConnections_Travel.git
   cd IvanConnections_Travel
2. Open in Visual Studio 2022.

3. Set the Android project as startup.

4. Deploy to emulator or connected Android device.
## 🧩 API Format

The app consumes data in this JSON format from a REST API:
```bash
[
  {
    "id": 111,
    "label": "1051",
    "latitude": 47.1052899,
    "longitude": 27.5597816,
    "timestamp": "2025-04-10T13:53:15",
    "vehicleType": 3,
    "bikeAccessible": "BIKE_INACCESSIBLE",
    "wheelchairAccessible": "WHEELCHAIR_ACCESSIBLE",
    "speed": 21,
    "direction": 1.75,
    "localTimestamp": "2025-04-10T16:53:15",
    "routeShortName": "27",
    "tripHeadsign": "Tătărași Nord",
    "routeColor": "#a2238e",
    "isElectricBus": false,
    "isNewTram": false
  }
]
```
Vehicle Model

In the app, this data is mapped to the following C# class:
```bash
public class Vehicle
{
    public int Id { get; set; }
    public string? Label { get; set; }
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public DateTime? Timestamp { get; set; }
    public VehicleType? VehicleType { get; set; }
    public string? BikeAccessible { get; set; }
    public string? WheelchairAccessible { get; set; }
    public int? Speed { get; set; }
    public double? Direction { get; set; }
    public DateTime? LocalTimestamp { get; set; }
    public string? RouteShortName { get; set; }
    public string? TripHeadsign { get; set; }
    public string? RouteColor { get; set; }
    public bool IsElectricBus { get; set; }
    public bool IsNewTram { get; set; }
}
```
ℹ️ If you're building your own API, just return a JSON array of vehicles like above.

🤝 Contributing

Contributions are very welcome!

    🐛 Bug reports

    🌍 Platform ports (iOS, Windows, etc.)

    ✨ New features

    📄 Documentation improvements

Feel free to fork the repo, open issues, and submit pull requests.
📄 License

This project is licensed under the [MIT License](https://opensource.org/license/mit).

Made with ❤️ by [Absotech](https://github.com/absotech) — Powered by public transport and .NET.
