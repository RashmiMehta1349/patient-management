using System.Security.Cryptography;
using System.Text;

namespace PatientMgmt.BusinessLogic.Auth
{
    public class Sha256TokenHasher : ITokenHasher
    {
        public string Hash(string rawToken)
        {
            var bytes = Encoding.UTF8.GetBytes(rawToken);
            var hash = SHA256.HashData(bytes);
            return Convert.ToBase64String(hash);
        }
    }
}
