/*! 
@file ParallelClientInterface.cs
@author Woong Gyu La a.k.a Chris. <juhgiyo@gmail.com>
		<http://github.com/juhgiyo/epparallelclient.cs>
@date October 13, 2015
@brief Parallel Client Interface
@version 2.0

@section LICENSE

The MIT License (MIT)

Copyright (c) 2015 Woong Gyu La <juhgiyo@gmail.com>

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in
all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
THE SOFTWARE.

@section DESCRIPTION

A ParallelClientInterface Class.

*/
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using EpServerEngine.cs;

namespace EpParallelSocket.cs
{
    /// <summary>
    /// Parallel Client Option class
    /// </summary>
    public sealed class ParallelClientOps
    {
        /// <summary>
        /// callback object
        /// </summary>
        public IParallelClientCallback CallBackObj
        {
            get;
            set;
        }
        /// <summary>
        /// hostname
        /// </summary>
        public String HostName
        {
            get;
            set;
        }
        /// <summary>
        /// port
        /// </summary>
        public String Port
        {
            get;
            set;
        }

        /// <summary>
        /// receive type
        /// </summary>
        public ReceiveType ReceiveType
        {
            get;
            set;
        }

        /// <summary>
        /// maximum number of sockets to use
        /// </summary>
        public int MaxSocketCount
        {
            get;
            set;
        }

        /// <summary>
        /// connection time out in millisecond
        /// </summary>
        public int ConnectionTimeOut
        {
            get;
            set;
        }

        /// <summary>
        /// Default constructor
        /// </summary>
        public ParallelClientOps()
        {
            CallBackObj = null;
            HostName = ParallelSocketConf.DEFAULT_HOSTNAME;
            Port = ParallelSocketConf.DEFAULT_PORT;
            ReceiveType = ReceiveType.SEQUENTIAL;
            MaxSocketCount = ParallelSocketConf.DEFAULT_MAX_SOCKET_NUM;
            ConnectionTimeOut = Timeout.Infinite;
        }

        /// <summary>
        /// Default constructor
        /// </summary>
        /// <param name="callBackObj">callback object</param>
        /// <param name="hostName">hostname</param>
        /// <param name="port">port</param>
        /// <param name="receiveType">receive type</param>
        /// <param name="maxSocketCount">maximum number of sockets to use</param>
        /// <param name="noDelay">flag for no delay</param>
        /// <param name="connectionTimeOut">connection wait time in millisecond</param>
        public ParallelClientOps(IParallelClientCallback callBackObj, String hostName, String port, ReceiveType receiveType = ReceiveType.SEQUENTIAL, int maxSocketCount = ParallelSocketConf.DEFAULT_MAX_SOCKET_NUM, int connectionTimeOut = Timeout.Infinite)
        {
            this.CallBackObj = callBackObj;
            this.HostName = hostName;
            this.Port = port;
            this.ReceiveType = receiveType;
            this.MaxSocketCount = maxSocketCount;
            this.ConnectionTimeOut = connectionTimeOut;
        }
        /// <summary>
        /// default client option
        /// </summary>
        public static ParallelClientOps defaultClientOps = new ParallelClientOps();
    };

    /// <summary>
    /// Client interface
    /// </summary>
    public interface IParallelClient
    {
        /// <summary>
        /// callback object
        /// </summary>
        IParallelClientCallback CallBackObj
        {
            get;
        }
        /// <summary>
        /// hostname
        /// </summary>
        String HostName
        {
            get;
        }
        /// <summary>
        /// port
        /// </summary>
        String Port
        {
            get;
        }

        /// <summary>
        /// receive type
        /// </summary>
        ReceiveType ReceiveType
        {
            get;
        }

        /// <summary>
        /// maximum number of sockets to use
        /// </summary>
        int MaxSocketCount
        {
            get;
        }

        /// <summary>
        /// current number of sockets property
        /// </summary>
        int CurSocketCount
        {
            get;
        }

        /// <summary>
        /// connection time out in millisecond
        /// </summary>
        int ConnectionTimeOut
        {
            get;
        }
        /// <summary>
        /// GUID of Parallel Client
        /// </summary>
        Guid Guid
        {
            get;
        }

        /// <summary>
        /// Connect to server with given option
        /// </summary>
        /// <param name="ops">option for client</param>
        void Connect(ParallelClientOps ops);

        /// <summary>
        /// Disconnect from the server
        /// </summary>
        void Disconnect();

        /// <summary>
        /// Check if the connection is alive
        /// </summary>
        /// <returns></returns>
        bool IsConnectionAlive
        {
            get;
        }

        /// <summary>
        /// Send given packet to the server
        /// </summary>
        /// <param name="data">bytes of data</param>
        /// <param name="offset">offset from start idx</param>
        /// <param name="dataSize">data size</param>
        void Send(byte[] data, int offset, int dataSize);

        /// <summary>
        /// Send given packet to the server
        /// </summary>
        /// <param name="data">bytes of data</param>
        void Send(byte[] data);
    }

    public interface IParallelClientCallback
    {
        /// <summary>
        /// Connection callback
        /// </summary>
        /// <param name="client">client</param>
        /// <param name="status">connection status</param>
        void OnConnected(IParallelClient client, ConnectStatus status);

        /// <summary>
        /// Receive callback
        /// </summary>
        /// <param name="client">client</param>
        /// <param name="receivedPacket">received packet</param>
        void OnReceived(IParallelClient client, ParallelPacket receivedPacket);

        /// <summary>
        /// Send callback
        /// </summary>
        /// <param name="client">client</param>
        /// <param name="status">send status</param>
        /// <param name="sentPacket">sent packet</param>
        void OnSent(IParallelClient client, SendStatus status, ParallelPacket sentPacket);

        /// <summary>
        /// Disconnect callback
        /// </summary>
        /// <param name="client">client</param>
        void OnDisconnect(IParallelClient client);
    };
}
