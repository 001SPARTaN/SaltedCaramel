using System.IO;
using System.Net;
using System.Text;

namespace SaltedCaramel
{
    class HTTP
    {
        internal static string Get(string endpoint)
        {
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(endpoint);

            using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
            using (StreamReader reader = new StreamReader(response.GetResponseStream()))
            {
                return reader.ReadToEnd();
            }
        }
        internal static string Post(string endpoint, string message)
        {
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(endpoint);
            request.Method = "POST";
            byte[] reqPayload = Encoding.UTF8.GetBytes(message);
            request.ContentType = "text/plain";
            request.ContentLength = reqPayload.Length;

            // Send payload to endpoint
            Stream rqstream = request.GetRequestStream();
            rqstream.Write(reqPayload, 0, reqPayload.Length);
            rqstream.Close();

            // Read response from endpoint
            using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
            using (StreamReader reader = new StreamReader(response.GetResponseStream()))
            {
                return reader.ReadToEnd();
            }
        }
    }
}
