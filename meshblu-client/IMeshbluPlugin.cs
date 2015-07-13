using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Octoblu
{
    /// <summary>
    /// Plugins are expected to implement this interface.
    /// </summary>
    public interface IMeshbluPlugin
    {
        /// <summary>
        /// Notification from Octoblu that the device is identified, connected and ready
        /// receive messages
        /// </summary>
        void OnReady();
        /// <summary>
        /// Main message event handler registered with Octoblu.
        /// Recieves json messages from Octoblu, meant for thie plugin's device.
        /// </summary>
        /// <param name="json"></param>
        void OnMessage(string json);

        /// <summary>
        /// Gets called when Octoblu needs to report an error
        /// </summary>
        /// <param name="error"></param>
        void OnError(string error);

        /// <summary>
        /// This is called by Octoblu to set the plugin with a new device Configuration,
        /// This also includes the additional options as set by the user, according to the
        /// optionsSchema
        /// </summary>
        /// <param name="newConfig">new configuration that Octoblu sends down in JSON string form</param>
        void OnConfig(string newConfig);
    }
}
