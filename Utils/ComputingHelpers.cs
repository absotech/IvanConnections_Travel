using IvanConnections_Travel.Models;

namespace IvanConnections_Travel.Utils
{
    public static class ComputingHelpers
    {
        public static string ComputeMd5Hash(string input)
        {
            using var md5 = System.Security.Cryptography.MD5.Create();
            var hashBytes = md5.ComputeHash(System.Text.Encoding.UTF8.GetBytes(input));
            return Convert.ToBase64String(hashBytes);
        }
        public static string ComputeVehicleHash(List<Vehicle> vehicles)
        {
            var sb = new System.Text.StringBuilder();
            foreach (var v in vehicles)
            {
                sb.Append($"{v.Id}-{v.Latitude}-{v.Longitude};");
            }
            using var sha256 = System.Security.Cryptography.SHA256.Create();
            var hashBytes = sha256.ComputeHash(System.Text.Encoding.UTF8.GetBytes(sb.ToString()));
            return Convert.ToBase64String(hashBytes);
        }

    }
}
