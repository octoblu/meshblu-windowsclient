using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Octoblu
{
    public interface IOctobluClient
    {
        /// <summary>
        /// Initializes the plugin and sets it configuration store and message interface.
        /// Returns 
        ///    'false': if the plugin has not been configured yet, 
        ///    'true' : if it has been configured and initialization is successful.
        /// </summary>
        /// <param name="config">configuration store for the plugin's device (its uuid and token)</param>
        /// <param name="plugin">regsitered callbacks for the plugin</param>
        bool InitializePlugin(IOctobluConfig config, IOctobluPlugin plugin);
        
        /// <summary>
        /// Regsiter a new device with Octoblu on behalf of the plugin.
        /// </summary>
        /// <param name="name">Name to be given to the device</param>
        /// <param name="dev">other custom properties for the Octoblu device, in JSON string</param>
        /// <param name="owneruuid">UUID of the Octoblu owner account for the device</param>
        /// <param name="type">type of device (free string, could be anything)</param>
        void RegisterDevice(string name, string devJson, string owneruuid, string type);

        /// <summary>
        /// Connect to Octoblu and listen for messages, on behalf of our device
        /// Blocking call....
        /// </summary>
        void Connect(
            string messageSchemaJson = null,
            string optionsSchemaJson = null
            );
        
        /// <summary>
        /// Disconnect from Octoblu
        /// </summary>
        void Disconnect();
        
        /// <summary>
        /// Send a JSON message to an Octoblu device (* for broadcasting to all interested parties)
        /// </summary>
        /// <param name="devicesToSendTo">An array of device UUIDs to send the message to ('*' for broadcast)</param>
        /// <param name="data">JSON object that contains the payload to send to the device(s)</param>
        /// <param name="callback">(optional) callback function to receive the send response</param>
        void Message(string devicesToSendToJson, string dataJson, Action<string> callback);

        /// <summary>
        /// Stores sensor data for a particular device UUID. 
        /// Data needs to be of the form
        ///     uuid: "uuid of the device"
        ///     "key1": "data1"
        ///     "key2": "data2"
        ///     ...
        /// </summary>
        /// <param name="dataJson"></param>
        /// <param name="callback"></param>
        void Data(string dataJson, Action<string> callback);
    }
}
