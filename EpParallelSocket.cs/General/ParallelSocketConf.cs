using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EpParallelSocket.cs
{
    /// <summary>
    /// Receive type
    /// </summary>
    public enum ReceiveType
    {
        /// <summary>
        /// Receive data as it comes
        /// </summary>
        BURST=0,
        /// <summary>
        /// Receive data in sequence
        /// </summary>
        SEQUENTIAL
    }

    /// <summary>
    /// Server configuration class
    /// </summary>
    public class ParallelSocketConf
    {
        /// <summary>
        /// Default hostname (localhost)
        /// </summary>
        public const String DEFAULT_HOSTNAME = "localhost";
        /// <summary>
        /// Default port (8080)
        /// </summary>
        public const String DEFAULT_PORT = "8080";
        /// <summary>
        /// Default number of sockets (20)
        /// </summary>
        public const int DEFAULT_MAX_SOCKET_NUM = 20;
    }
}
