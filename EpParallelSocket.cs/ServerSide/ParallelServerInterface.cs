/*! 
@file ParallelServerInterface.cs
@author Woong Gyu La a.k.a Chris. <juhgiyo@gmail.com>
		<http://github.com/juhgiyo/epparallelclient.cs>
@date October 13, 2015
@brief Parallel Server Interface
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

A ParallelServerInterface Class.

*/
using EpServerEngine.cs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EpParallelSocket.cs
{
    /// <summary>
    /// ParallelServer option class
    /// </summary>
    public sealed class ParallelServerOps
    {
        /// <summary>
        /// callback object
        /// </summary>
        public IParallelServerCallback CallBackObj
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


        public int MaxSocketCount
        {
            get;
            set;
        } 

        /// <summary>
        /// Default constructor
        /// </summary>
        public ParallelServerOps()
        {
            CallBackObj = null;
            Port = ParallelSocketConf.DEFAULT_PORT;
            ReceiveType = ReceiveType.SEQUENTIAL;
            MaxSocketCount = SocketCount.Infinite;
        }
        /// <summary>
        /// Default constructor
        /// </summary>
        /// <param name="callBackObj">callback object</param>
        /// <param name="port">port</param>
        /// <param name="receiveType">receive type</param>
        /// <param name="noDelay">noDelay falg</param>
        public ParallelServerOps(IParallelServerCallback callBackObj, String port, ReceiveType receiveType=ReceiveType.SEQUENTIAL, int socketCount = SocketCount.Infinite)
        {
            this.Port = port;
            this.CallBackObj = callBackObj;
            this.ReceiveType = receiveType;
            MaxSocketCount = socketCount;
        }

        /// <summary>
        /// Default server option
        /// </summary>
        public static ParallelServerOps defaultServerOps = new ParallelServerOps();
    };


    /// <summary>
    /// Parallel Server interface
    /// </summary>
    public interface IParallelServer
    {
        /// <summary>
        /// Return the port
        /// </summary>
        /// <returns>port</returns>
        String Port { get; }

        /// <summary>
        /// callback object
        /// </summary>
        IParallelServerCallback CallBackObj
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
        /// Start the server with given option
        /// </summary>
        /// <param name="ops">option for the server</param>
        void StartServer(ParallelServerOps ops);

        /// <summary>
        /// Stop the server
        /// </summary>
        void StopServer();

        /// <summary>
        /// Check whether server is started or not
        /// </summary>
        /// <returns>true if server is started, otherwise false</returns>
        bool IsServerStarted { get; }
        /// <summary>
        /// Shutdown all the client, connected
        /// </summary>
        void ShutdownAllClient();

        /// <summary>
        /// Broadcast the given packet to all the client, connected
        /// </summary>
        /// <param name="data">data in byte array</param>
        /// <param name="offset">offset in bytes</param>
        /// <param name="dataSize">data size in bytes</param>
        void Broadcast(byte[] data, int offset, int dataSize);

        /// <summary>
        /// Broadcast the given packet to all the client, connected
        /// </summary>
        /// <param name="data">data in byte array</param>
        void Broadcast(byte[] data);

        /// <summary>
        /// Return the connected client list
        /// </summary>
        /// <returns>the connected client list</returns>
        List<ParallelSocket> GetClientSocketList();

        /// <summary>
        /// Detach the given client from the server management
        /// </summary>
        /// <param name="clientSocket">the client to detach</param>
        /// <returns>true if successful, otherwise false</returns>
        bool DetachClient(ParallelSocket clientSocket);

    }


    /// <summary>
    /// Parallel Server callback interface
    /// </summary>
    public interface IParallelServerCallback
    {
        /// <summary>
        /// Server started callback
        /// </summary>
        /// <param name="server">server</param>
        /// <param name="status">start status</param>
        void OnServerStarted(IParallelServer server, StartStatus status);
        /// <summary>
        /// Accept callback
        /// </summary>
        /// <param name="server">server</param>
        /// <param name="ipInfo">connection info</param>
        /// <param name="streamCount">stream count for the parallel socket</param>
        /// <returns>the socket callback interface</returns>
        IParallelSocketCallback OnAccept(IParallelServer server, IPInfo ipInfo, int streamCount);
        /// <summary>
        /// Server stopped callback
        /// </summary>
        /// <param name="server">server</param>
        void OnServerStopped(IParallelServer server);
    };


    /// <summary>
    /// Parallel Socket interface
    /// </summary>
    public interface IParallelSocket
    {
        /// <summary>
        /// Disconnect the client
        /// </summary>
        void Disconnect();

        /// <summary>
        /// Check if the connection is alive
        /// </summary>
        /// <returns>true if the connection is alive, otherwise false</returns>
        bool IsConnectionAlive { get; }

        /// <summary>
        /// Send given data to the client
        /// </summary>
        /// <param name="data">data in byte array</param>
        /// <param name="offset">offset in bytes</param>
        /// <param name="dataSize">data size in bytes</param>
        void Send(byte[] data, int offset, int dataSize);

        /// <summary>
        /// Send given data to the client
        /// </summary>
        /// <param name="data">data in byte array</param>
        void Send(byte[] data);

        /// <summary>
        /// Broadcast given data to all client other than this
        /// </summary>
        /// <param name="data">data in byte array</param>
        /// <param name="offset">offset in bytes</param>
        /// <param name="dataSize">data size in bytes</param>
        void Broadcast(byte[] data, int offset, int dataSize);

        /// <summary>
        /// Broadcast given data to all client other than this
        /// </summary>
        /// <param name="data">data in byte array</param>
        void Broadcast(byte[] data);

        /// <summary>
        /// Return the IP information of the client
        /// </summary>
        /// <returns>the IP information of the client</returns>
        IPInfo IPInfo { get; }

        /// <summary>
        /// Return the server managing this socket
        /// </summary>
        /// <returns>the server managing this socket</returns>
        IParallelServer Server { get; }

        /// <summary>
        /// callback object property
        /// </summary>
        IParallelSocketCallback CallBackObj { get; }


        /// <summary>
        /// guid property
        /// </summary>
        Guid Guid { get; }

        /// <summary>
        /// receive type
        /// </summary>
        ReceiveType ReceiveType { get; }


    }


    /// <summary>
    /// Parallel Socket callback interface
    /// </summary>
    public interface IParallelSocketCallback
    {
        /// <summary>
        /// NewConnection callback
        /// </summary>
        /// <param name="socket">client socket</param>
        void OnNewConnection(IParallelSocket socket);

        /// <summary>
        /// Receive callback
        /// </summary>
        /// <param name="socket">client socket</param>
        /// <param name="receivedPacket">received packet</param>
        void OnReceived(IParallelSocket socket, ParallelPacket receivedPacket);

        /// <summary>
        /// Send callback
        /// </summary>
        /// <param name="socket">client socket</param>
        /// <param name="status">stend status</param>
        /// <param name="sentPacket">sent packet</param>
        void OnSent(IParallelSocket socket, SendStatus status, ParallelPacket sentPacket);

        /// <summary>
        /// Disconnect callback
        /// </summary>
        /// <param name="socket">client socket</param>
        void OnDisconnect(IParallelSocket socket);
    };
}
