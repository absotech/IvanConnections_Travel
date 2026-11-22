using IvanConnections_Travel.Models.Enums;
using IvanConnections_Travel.Utils;
using System.Text.Json.Serialization;

namespace IvanConnections_Travel.Models
{
    public class StopArrival
    {
        /// <summary>Id-ul vehiculului; corespunde cu <see cref="Vehicle.Id"/>.</summary>
        [JsonPropertyName("vehicleId")]
        public int? VehicleId { get; set; }

        /// <summary>Atenție: API-ul trimite aici numărul liniei (ex. „41”), nu eticheta vehiculului.</summary>
        [JsonPropertyName("vehicleLabel")]
        public string VehicleLabel { get; set; }

        [JsonPropertyName("arrivalMinutes")]
        public double ArrivalMinutes { get; set; }

        [JsonPropertyName("vehicleType")]
        public VehicleType VehicleType { get; set; }

        // Proprietăți doar pentru afișare (popup-ul de stație).

        [JsonIgnore]
        public string TypeName
        {
            get
            {
                var name = Translations.GetVehicleTypeNameInRomanian(VehicleType);
                return string.IsNullOrEmpty(name) ? "Vehicul" : char.ToUpperInvariant(name[0]) + name[1..];
            }
        }

        [JsonIgnore]
        public string VehicleGlyph => VehicleVisuals.GetGlyph(VehicleType);

        [JsonIgnore]
        public Color AccentColor => VehicleVisuals.GetAccentColor(VehicleType);

        /// <summary>Vehiculul este pe capăt (nu a plecat încă în traseu).</summary>
        [JsonIgnore]
        public bool IsAtTerminus => ArrivalMinutes < 0;

        /// <summary>Sosirea este iminentă (în cel mult două minute).</summary>
        [JsonIgnore]
        public bool IsImminent => !IsAtTerminus && ArrivalMinutes <= 2;

        /// <summary>Valoarea mare afișată în dreapta cardului de sosire.</summary>
        [JsonIgnore]
        public string ArrivalValue => IsAtTerminus
            ? "Pe capăt"
            : ArrivalMinutes < 1 ? "Acum" : Math.Round(ArrivalMinutes).ToString("0");

        /// <summary>Unitatea afișată sub valoare (doar când aceasta este un număr).</summary>
        [JsonIgnore]
        public bool HasArrivalUnit => !IsAtTerminus && ArrivalMinutes >= 1;

        /// <summary>Text explicativ afișat lângă numele traseului.</summary>
        [JsonIgnore]
        public string ArrivalHint => IsAtTerminus
            ? "așteaptă în capăt de linie"
            : ArrivalMinutes < 1 ? "sosește în stație" : "sosire estimată";
    }
}
