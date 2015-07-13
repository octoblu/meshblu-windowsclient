using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Octoblu
{
    // Serialization classes that represent JSON messages received from Octoblu
    public class IdentifyResponse
    {
        public string socketid;
    }
    public class NotReadyResponse
    {
        public string status;
    }
    public class ReadyResponse
    {
        public string status;
    }
    public class RegisterResponse
    {
        public string uuid;
        public string token;
    }
}
