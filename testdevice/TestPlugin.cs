using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace Octoblu
{
    class TestPlugin : IOctobluPlugin
    {
        public void OnReady()
        {
            Console.WriteLine("TestPlugin: Octoblu device is ready to recieve messages");
        }
        public void OnMessage(string json)
        {
            Console.WriteLine("TestPlugin: received Octoblu message: " + json);
        }
        public void OnError(string error)
        {
            Console.WriteLine("TestPlugin: Octoblu returned Error message: " + error);
        }
        public void OnConfig(JObject newConfig)
        {
            Console.WriteLine("TestPlugin: Octoblu set new device configuration " + newConfig.ToString());
        }
    }
}
