using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Win32;
using System.Diagnostics;

namespace Octoblu
{
    public interface IMeshbluConfig
    {
        // Public properties
        string Uuid { get; }
        string Token { get; }
        string MeshbluUrl { get; }
        int MeshbluPort { get; }
        string PluginName { get; }

        /// <summary>
        /// Retrieve Octoblu configuration
        /// </summary>
        /// <returns></returns>
        bool Read();

        /// <summary>
        /// Write Octoblu configuration
        /// </summary>
        /// <param name="uuid">Octoblu device uuid</param>
        /// <param name="token">Octoblu device token</param>
        void Write(string uuid, string token);
    }

    public enum MeshbluConfigLocation
    {
        Machine,
        User
    }
    /// <summary>
    ///  This class sets and retrieves identity information (uuid and token) 
    ///  necessary to setup a connection to Octoblu. This is a default implementation
    ///  that uses the registry to store the configuration, each plugin can create its own.
    /// </summary>
    public class MeshbluConfig : IMeshbluConfig
    {
        private RegistryKey _hive = Registry.CurrentUser;
        private const string _baseKeyPath = "Software\\Citrix\\OctobluAgent";
        private string _uuid = null;
        private string _token = null;
        private string _meshbluUrl = "wss://meshblu.octoblu.com";
        private int    _meshbluPort = 443;
        private string _pluginName = null;

        // Public properties
        public string Uuid  { get { return _uuid; } }
        public string Token { get { return _token; } }
        public string MeshbluUrl { get { return _meshbluUrl; } }
        public int    MeshbluPort { get { return _meshbluPort; } }
        public string PluginName { get { return _pluginName; } }

        public MeshbluConfig(string pluginName, MeshbluConfigLocation loc = MeshbluConfigLocation.User)
        {
            if (loc == MeshbluConfigLocation.Machine)
                _hive = Registry.LocalMachine;
            _pluginName = pluginName;
        }
        
        /// <summary>
        /// Retrieve Octoblu configuration
        /// </summary>
        /// <returns></returns>
        public bool Read()
        {
            bool deviceConfigured = false;
            var keypath = _baseKeyPath + "\\" + _pluginName;
            var ourKey = _hive.CreateSubKey(keypath);
            if (ourKey != null)
            {
                _uuid = (string)ourKey.GetValue("deviceuuid", null);
                _token = (string)ourKey.GetValue("devicetoken", null);
                _meshbluUrl = (string)ourKey.GetValue("meshbluUrl", "wss://meshblu.octoblu.com");
                _meshbluPort = (int)ourKey.GetValue("meshbluPort", 443);
                if (_uuid != null && _token != null)
                    deviceConfigured = true;
            }
            if (!deviceConfigured)
            {
                Trace.WriteLine("OctobluConfig: The Octoblu node has not been registered for this user.");
                return false;
            }
            return true;
        }
        /// <summary>
        /// Write Octoblu configuration
        /// </summary>
        /// <param name="uuid">Octoblu device uuid</param>
        /// <param name="token">Octoblu device token</param>
        public void Write(string uuid, string token)
        {
            var keypath = _baseKeyPath + "\\" + _pluginName;
            var key = _hive.CreateSubKey(keypath);
            key.SetValue("deviceuuid", uuid, RegistryValueKind.String);
            key.SetValue("devicetoken", token, RegistryValueKind.String);
            Trace.WriteLine("OctobluConfig: Wrote device configuration to regsitry..");
        }
    }
}
