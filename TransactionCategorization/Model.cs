using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace TransactionCategorization
{
    internal class Model
    {
        public DateTime Date { get; set; }
        public decimal Amount { get; set; }
        public string Description { get; set; } = string.Empty;
        public string Category { get; set; } = "Unset";
        public string? Hash { get; set; }

        public string CreateMD5()
        {
            using (var md5 = MD5.Create())
            {
                var input = Encoding.ASCII.GetBytes(
                    Date.ToString("o", CultureInfo.InvariantCulture) +
                    Amount.ToString() +
                    Description);
                var hash = md5.ComputeHash(input);
                return Convert.ToHexString(hash);
            }
        }
    }
}
