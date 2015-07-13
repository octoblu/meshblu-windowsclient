using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace Octoblu
{
    class Program
    {
        static void Run()
        {
            var plugin = new TestPlugin();
            var config = new MeshbluConfig("TestDevice");
            var octoblu = OctobluClientFactory.GetInstance();
            if (!octoblu.InitializePlugin(config, plugin))
            {
                Console.WriteLine("Enter the UUID of the Octoblu account to create this device under:");
                string uuid = Console.ReadLine();
                if (uuid != null)
                {
                    // register a brand new device with Octoblu
                    string name = System.Environment.UserName + "_On_" + System.Environment.MachineName;
                    var dev = new JObject();

                    // custom properties we might want on the device
                    dev["username"] = System.Environment.UserName;
                    dev["computername"] = System.Environment.MachineName;

                    octoblu.RegisterDevice(name, dev.ToString(), uuid, "testdevice");
                }
            }
            // THIS IS A BLOCKING CALL
            octoblu.Connect();
        }

        static void Main(string[] args)
        {
            Run();
        }
    }
}
