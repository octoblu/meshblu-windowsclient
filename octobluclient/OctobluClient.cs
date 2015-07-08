using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Win32;
using Quobject.SocketIoClientDotNet.Client;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Reflection;
using System.Diagnostics;

namespace Octoblu
{
    public class OctobluClientFactory
    {
        //Singleton of this octoblu connection
        private static OctobluClient _octobluDevInstance = new OctobluClient();
        public static IOctobluClient GetInstance()
        {
            return _octobluDevInstance;
        }
    }

    /// <summary>
    /// This class encapsulates all functionality necessary to setup a connection with
    /// Octoblu, send and receive messages from it.
    /// </summary>
    public class OctobluClient : IOctobluClient
    {
        private ManualResetEvent _endEvent = new ManualResetEvent(false);
        private Quobject.SocketIoClientDotNet.Client.Socket _socket = null;
        private IOctobluPlugin _plugin = null;
        private IOctobluConfig _config = null;
        
        public OctobluClient()
        {
        }

        private void SetupMeshbluConnection()
        {
            var opts = new IO.Options();

            opts.Port = _config.MeshbluPort;
            opts.ForceNew = true;
            opts.Secure = true;
            opts.IgnoreServerCertificateValidation = true;

            _socket = Quobject.SocketIoClientDotNet.Client.IO.Socket(_config.MeshbluUrl, opts);
        }

        /// <summary>
        /// Error handler for when Octoblu reports an error
        /// </summary>
        /// <param name="data"></param>
        void OnError(object data)
        {
            _plugin.OnError(data.ToString());
        }
        /// <summary>
        /// Event Handler for when Octoblu reports a new device configuration.
        /// Call plugin to report new configuration
        /// This happens when user updates properties of device in the flow designer
        /// </summary>
        /// <param name="newConfig"></param>
        void OnConfig(object newConfig)
        {
            dynamic settings = JObject.Parse(newConfig.ToString());
            _plugin.OnConfig(settings);
        }
        /// <summary>
        /// The device is ready to receive messages from Octoblu
        /// </summary>
        /// <param name="jsondata">Response from Octoblu</param>
        void OnReady(object jsondata)
        {
            var data = JsonConvert.DeserializeObject<ReadyResponse>(jsondata.ToString());
            Trace.WriteLine("OctobluConnection: Device ready: " + data.status);
            _plugin.OnReady();
        }
        /// <summary>
        /// This function is the handler for all messages received from Octoblu
        /// </summary>
        /// <param name="data">Plugin specific Message data</param>
        void OnMessage(object data)
        {
            Trace.WriteLine("OctobluConnection: Message received: " + data.ToString());
            dynamic settings = JObject.Parse(data.ToString());
            // webhook sends in payload in 'params', trigger sends it in 'payload'
            if (settings["params"] != null || settings["payload"] != null)
            {
                if(settings["params"] != null)
                    _plugin.OnMessage(settings["params"].ToString());
                else 
                    _plugin.OnMessage(settings["payload"].ToString());
            }
        }
        /// <summary>
        /// Dump out the device configuration
        /// </summary>
        void UpdateDevice(JObject messageSchema, JObject optionsSchema)
        {
            // Dump out the Octoblu configuration information about this device
            var whoamireq = new JObject();
            whoamireq["uuid"] = _config.Uuid;
            _socket.Emit(
                "whoami",
                new AckImpl((deviceData) =>
                    {
                        // Update the device with the caller supplied options and message schema, 
                        // so it can be configured from the flow designer
                        if (messageSchema != null || optionsSchema != null)
                        {
                            var devObj = JObject.Parse(deviceData.ToString());
                            var options = (devObj["options"] == null ? new JObject() : devObj["options"]);

                            var updDev = new JObject();
                            updDev["uuid"] = _config.Uuid;
                            updDev["token"] = _config.Token;
                            
                            // if a message schema or options schema has been specified for the device, 
                            // use it here
                            if(messageSchema != null)
                                updDev["messageSchema"] = messageSchema;
                            if(optionsSchema != null)
                                updDev["optionsSchema"] = optionsSchema;
                            updDev["options"] = options;
                                
                            _socket.Emit("update", updDev);
                        }
                    }),
                whoamireq);
        }

        void RemoveAllListeners()
        {
            _socket.Off();
        }

        ////////////////////////////////////////////////////
        // IOctoblu interface implementation
        ////////////////////////////////////////////////////
        /// <summary>
        /// Initializes a plugin
        /// </summary>
        /// <param name="config">Configuration for the plugin's deviec (device uuid and token)</param>
        /// <param name="plugin">Plugin interface to recieve events on</param>
        /// <returns></returns>
        public bool InitializePlugin(IOctobluConfig config, IOctobluPlugin plugin){
            _config = config;
            _plugin = plugin;
            return _config.Read();
        }

        /// <summary>
        /// Register a new device with Octoblu. 
        /// </summary>
        /// <param name="dev">JSON object that represent custom properties you want on the device</param>
        /// <param name="owneruuid">Uuid of the Octoblu account owner to register device under</param>
        /// <param name="type">device type designation on Octoblu</param>
        public void RegisterPluginDevice(string name, string devJson, string owneruuid, string type)
        {
            ManualResetEvent syncEvent = new ManualResetEvent(false);
            try
            {
                // Read Octoblu configuration from the registry
                _config.Read();

                SetupMeshbluConnection();

                // Add the rest of the properties to Octoblu device
                if (name == null || devJson == null || owneruuid == null || type == null)
                    throw new ArgumentException("Null argument specified");

                var dev = JObject.Parse(devJson);
                dev["name"] = name;
                dev["type"] = "device:" + type;

                var owners = new JArray();
                owners.Add(owneruuid);
                dev["owner"] = owners;

                if(dev["configureWhitelist"] == null)
                    dev["configureWhitelist"] = "*";// anybody can configure this device
                if(dev["discoverWhitelist"] == null)
                    dev["discoverWhitelist"] = owners; // only owners are can discover this device
                if(dev["receiveWhitelist"] == null)
                    dev["receiveWhitelist"] = "*";     // anyone can receive messages from this device
                if(dev["sendWhitelist"] == null)
                    dev["sendWhitelist"] = "*";        // anyone can send messages to this device

                Trace.WriteLine("OctobluConnection: Registering device.... for owner:" + owneruuid);
                _socket.Emit(
                    "register",
                    new AckImpl((newdevicedata) =>
                    {
                        Trace.WriteLine(newdevicedata.ToString());

                        // write the new device uuid and token to user's registry
                        var newDevice = JsonConvert.DeserializeObject<RegisterResponse>(newdevicedata.ToString());
                        _config.Write(newDevice.uuid, newDevice.token);

                        syncEvent.Set();
                    }),
                    dev);
            }
            catch (Exception e)
            {
                syncEvent.Set();
                Trace.WriteLine("OctobluConnection Exception Registering device : " + e.ToString());
            }
            syncEvent.WaitOne();
        }
        /// <summary>
        /// Connect to Octoblu and listen for messages, on behalf of our device
        /// Blocking call....
        /// </summary>
        public void Connect(
            string messageSchemaJson = null,
            string optionsSchemaJson = null
            )
        {
            try
            {
                if (!_config.Read())
                {
                    {
                        Trace.WriteLine("OctobluConnection: Device is not configured yet !");
                        return;
                    }
                }

                SetupMeshbluConnection();
                _socket.On(Socket.EVENT_CONNECT, () =>
                {
                    // remove all existing listeners
                    this.RemoveAllListeners();

                    // Setup a series of listeners
                    _socket.On("identify", (jsondata) =>
                    {
                        var data = JsonConvert.DeserializeObject<IdentifyResponse>(jsondata.ToString());
                        Trace.WriteLine("OctobluConnection: Websocket connecting to Meshblu with socket id: " + data.socketid);
                        Trace.WriteLine("OctobluConnection: Sending device uuid: " + _config.Uuid);

                        // Identify this device to Octoblu using the 
                        // device's uuid and token from the registry
                        var id = new JObject();
                        id["uuid"] = _config.Uuid;
                        id["token"] = _config.Token;
                        id["socketid"] = data.socketid;
                        _socket.Emit("identity", id);
                    });
                    
                    _socket.On("notReady", (jsondata) =>
                    {
                        // Octoblu says this device is not ready and will not recieve messages
                        var data = JsonConvert.DeserializeObject<NotReadyResponse>(jsondata.ToString());
                        Trace.WriteLine("OctobluConnection: Device not ready: " + data.status); // could be 401 (not authenticated)
                    });
                    
                    _socket.On("ready", (jsondata) =>
                    {
                        // This device is now ready to receive messages
                        JObject messageSchema = null;
                        JObject optionsSchema = null;
                        if (messageSchemaJson != null)
                            messageSchema = JObject.Parse(messageSchemaJson);
                        if (optionsSchemaJson != null)
                            optionsSchema = JObject.Parse(optionsSchemaJson);
                        UpdateDevice(messageSchema, optionsSchema);
                        OnReady(jsondata);
                    });

                    _socket.On("config", (jsonData) => {
                        // device is getting updated, let plugin know
                        Trace.WriteLine("ONCONFIG BEING CALLED------");
                        OnConfig(jsonData);
                    });

                    _socket.On("error", (jsonData) =>
                    {
                        // error being reported by Octoblu, let plugin know
                        OnError(jsonData);
                    });

                    // General message handler
                    _socket.On("message", (jsonmessage) =>
                    {
                        // This device recevied a mesage, pass it on to the plugin
                        OnMessage(jsonmessage);
                    });
                });
            }
            catch (Exception e)
            {
                Trace.WriteLine("OctobluConnection Exception: Connecting to Octoblu : " + e.ToString());
                _endEvent.Set();
            }
            _endEvent.WaitOne();
        }
        /// <summary>
        /// Disconnect from Octoblu
        /// </summary>
        public void Disconnect()
        {
            // Disconnect from Octoblu and end this process
            _socket.Disconnect();
            _socket = null;
            _endEvent.Set();
        }
        /// <summary>
        /// Sends a JSON message to the devices specified
        /// </summary>
        /// <param name="devicesToSendTo">Array of device UUIDs</param>
        /// <param name="data">JSON data</param>
        public void SendMessage(string devicesToSendToJson, string dataJson)
        {
            try
            {
                var msg = new JObject();
                msg["devices"] = JObject.Parse(devicesToSendToJson);
                msg["payload"] = JObject.Parse(dataJson);
                _socket.Emit(
                    "message",
                    new AckImpl((sendResp) =>
                    {
                        Trace.WriteLine("SendMessage response received: " + sendResp.ToString());
                    }),
                    msg);
            }catch(Exception e)
            {
                Trace.WriteLine("OctobluConnection Exception: " + e.ToString());
            }
        }
    }
}
