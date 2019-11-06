using System.Net;
using System.Text;

namespace SaltedCaramel
{
    public class HTTP
    {
        private static WebClient client = new WebClient();
        public static SCCrypto crypto = new SCCrypto();

        public static string Get(string endpoint)
        {
            return crypto.Decrypt(client.DownloadString(endpoint));
        }

        public static string Post(string endpoint, string message)
        {
            byte[] reqPayload = Encoding.UTF8.GetBytes(crypto.Encrypt(message));

            return crypto.Decrypt(Encoding.UTF8.GetString(client.UploadData(endpoint, reqPayload)));
        }
    }
}
