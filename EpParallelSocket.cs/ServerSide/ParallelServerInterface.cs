/*! 
@file ParallelServerInterface.cs
@author Woong Gyu La a.k.a Chris. <juhgiyo@gmail.com>
		<http://github.com/juhgiyo/epparallelsocket.cs>
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
        /// acceptor object
        /// </summary>
        public IParallelServerAcceptor Acceptor
        {
            get;
            set;
        }

        /// <summary>
        /// callback object
        /// </summary>
        public IParallelServerCallback CallBackObj
        {
            get;
            set;
        }


        /// <summary>
        /// room callback object
        /// </summary>
        public IParallelRoomCallback RoomCallBackObj
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
        /// Maximum number of total stream count for the server
        /// </summary>
        public int MaxSocketCount
        {
            get;
            set;
        }


        /// <summary>
        /// Max stream count per parallel socket
        /// (This is independent from MaxSocketCount)
        /// </summary>
        public int MaxStreamCountPerSocket
        {
            get;
            set;
        } 

        /// <summary>
        /// Default constructor
        /// </summary>
        public ParallelServerOps()
        {
            Acceptor = null;
            CallBackObj = null;
            RoomCallBackObj = null;
            Port = ParallelSocketConf.DEFAULT_PORT;
            ReceiveType = ReceiveType.SEQUENTIAL;
            MaxSocketCount = SocketCount.Infinite;
            MaxStreamCountPerSocket = SocketCount.Infinite;
        }
        /// <summary>
        /// Default constructor
        /// </summary>
        /// <param name="acceptor">acceptor object</param>
        /// <param name="callBackObj">callback object</param>
        /// <param name="port">port</param>
        /// <param name="receiveType">receive type</param>
        /// <param name="noDelay">noDelay falg</param>
        public ParallelServerOps(IParallelServerAcceptor acceptor, String port, IParallelServerCallback callBackObj = null, IParallelRoomCallback roomCallBackObj = null, ReceiveType receiveType = ReceiveType.SEQUENTIAL, int socketCount = SocketCount.Infinite, int streamCountPerSocket = SocketCount.Infinite)
        {
            this.Port = port;
            this.Acceptor = acceptor;
            this.CallBackObj = callBackObj;
            this.RoomCallBackObj = roomCallBackObj;
            this.ReceiveType = receiveType;
            MaxSocketCount = socketCount;
            MaxStreamCountPerSocket = streamCountPerSocket;
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
        /// acceptor object
        /// </summary>
        IParallelServerAcceptor Acceptor
        {
            get;
            set;
        }

        /// <summary>
        /// callback object
        /// </summary>
        IParallelServerCallback CallBackObj
        {
            get;
            set;
        }

        /// <summary>
        /// room callback object
        /// </summary>
        IParallelRoomCallback RoomCallBackObj
        {
            get;
            set;
        }

        /// <summary>
        /// receive type
        /// </summary>
        ReceiveType ReceiveType
        {
            get;
        }
        /// <summary>
        /// maximum socket count property
        /// </summary>
        int MaxSocketCount
        {
            get;
        }

        /// <summary>
        /// maximum number of stream per parallel socket
        /// </summary>
        int MaxStreamCountPerSocket
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

        /// <summary>
        /// Return the room instance of given room name
        /// </summary>
        /// <param name="roomName">room name</param>
        /// <returns>the room instance</returns>
        IParallelRoom GetRoom(string roomName);

        /// <summary>
        /// Return the list of names of the room
        /// </summary>
        List<string> RoomNames { get; }

        /// <summary>
        /// Return the list of rooms
        /// </summary>
        List<IParallelRoom> Rooms { get; }

        /// <summary>
        /// Broadcast the given packet to all the client, connected
        /// </summary>
        /// <param name="data">data in byte array</param>
        /// <param name="offset">offset in bytes</param>
        /// <param name="dataSize">data size in bytes</param>
        void Broadcast(string roomName, byte[] data, int offset, int dataSize);


        /// <summary>
        /// Broadcast the given packet to all the client, connected
        /// </summary>
        /// <param name="data">data in byte array</param>
        void Broadcast(string roomName, byte[] data);


        /// <summary>
        /// OnServerStarted event
        /// </summary>
        OnParallelServerStartedDelegate OnParallelServerStarted
        {
            get;
            set;
        }
        /// <summary>
        ///  OnAccept event
        /// </summary>
        OnParallelServerAcceptedDelegate OnParallelServerAccepted
        {
            get;
            set;
        }
        /// <summary>
        /// OnserverStopped event
        /// </summary>
        OnParallelServerStoppedDelegate OnParallelServerStopped
        {
            get;
            set;
        }
    }

    public delegate void OnParallelServerStartedDelegate(IParallelServer server, StartStatus status);
    public delegate void OnParallelServerAcceptedDelegate(IParallelServer server, IParallelSocket socket);
    public delegate void OnParallelServerStoppedDelegate(IParallelServer server);

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
        void OnParallelServerStarted(IParallelServer server, StartStatus status);
        /// <summary>
        /// Accept callback
        /// </summary>
        /// <param name="server">server</param>
        /// <param name="socket">socket accepted</param>
        void OnParallelServerAccepted(IParallelServer server, IParallelSocket socket);
        /// <summary>
        /// Server stopped callback
        /// </summary>
        /// <param name="server">server</param>
        void OnParallelServerStopped(IParallelServer server);
    };

    public interface IParallelServerAcceptor
    {
        /// <summary>
        /// Accept callback
        /// </summary>
        /// <param name="server">server</param>
        /// <param name="ipInfo">connection info</param>
        /// <param name="streamCount">stream count for the parallel socket</param>
        /// <returns>true to accept otherwise false</returns>
        bool OnAccept(IParallelServer server, IPInfo ipInfo, int streamCount);
        IParallelSocketCallback GetSocketCallback();
    }


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
        IParallelSocketCallback CallBackObj { get; set; }


        /// <summary>
        /// guid property
        /// </summary>
        Guid Guid { get; }

        /// <summary>
        /// receive type
        /// </summary>
        ReceiveType ReceiveType { get; }

        /// <summary>
        /// maximum number of stream per parallel socket
        /// </summary>
        int MaxStreamCountPerSocket { get; }


        /// <summary>
        /// OnNewConnected event
        /// </summary>
        OnParallelSocketNewConnectionDelegate OnParallelSocketNewConnection
        {
            get;
            set;
        }
        /// <summary>
        /// OnRecevied event
        /// </summary>
        OnParallelSocketReceivedDelegate OnParallelSocketReceived
        {
            get;
            set;
        }
        /// <summary>
        /// OnSent event
        /// </summary>
        OnParallelSocketSentDelegate OnParallelSocketSent
        {
            get;
            set;
        }
        /// <summary>
        /// OnDisconnect event
        /// </summary>
        OnParallelSocketDisconnectDelegate OnParallelSocketDisconnect
        {
            get;
            set;
        }

        /// <summary>
        /// Return the room instance of given room name
        /// </summary>
        /// <param name="roomName">room name</param>
        /// <returns>the room instance</returns>
        IParallelRoom GetRoom(string roomName);

        /// <summary>
        /// Return the list of names of the room
        /// </summary>
        List<string> RoomNames { get; }

        /// <summary>
        /// Return the list of rooms
        /// </summary>
        List<IParallelRoom> Rooms { get; }

        /// <summary>
        /// Join the room
        /// </summary>
        /// <param name="roomName">room name</param>
        /// <returns>the instance of the room</returns>
        void Join(string roomName);

        /// <summary>
        /// Detach given socket from the given room
        /// </summary>
        /// <param name="roomName">room name</param>
        /// <returns>number of sockets left in the room</returns>
        void Leave(string roomName);

        /// <summary>
        /// Broadcast the given packet to all the client, connected
        /// </summary>
        /// <param name="data">data in byte array</param>
        /// <param name="offset">offset in bytes</param>
        /// <param name="dataSize">data size in bytes</param>
        void Broadcast(string roomName, byte[] data, int offset, int dataSize);


        /// <summary>
        /// Broadcast the given packet to all the client, connected
        /// </summary>
        /// <param name="data">data in byte array</param>
        void Broadcast(string roomName, byte[] data);
    }


    public delegate void OnParallelSocketNewConnectionDelegate(IParallelSocket socket);
    public delegate void OnParallelSocketReceivedDelegate(IParallelSocket socket, ParallelPacket receivedPacket);
    public delegate void OnParallelSocketSentDelegate(IParallelSocket socket, SendStatus status, ParallelPacket sentPacket);
    public delegate void OnParallelSocketDisconnectDelegate(IParallelSocket socket);

    /// <summary>
    /// Parallel Socket callback interface
    /// </summary>
    public interface IParallelSocketCallback
    {
        /// <summary>
        /// NewConnection callback
        /// </summary>
        /// <param name="socket">client socket</param>
        void OnParallelSocketNewConnection(IParallelSocket socket);

        /// <summary>
        /// Receive callback
        /// </summary>
        /// <param name="socket">client socket</param>
        /// <param name="receivedPacket">received packet</param>
        void OnParallelSocketReceived(IParallelSocket socket, ParallelPacket receivedPacket);

        /// <summary>
        /// Send callback
        /// </summary>
        /// <param name="socket">client socket</param>
        /// <param name="status">stend status</param>
        /// <param name="sentPacket">sent packet</param>
        void OnParallelSocketSent(IParallelSocket socket, SendStatus status, ParallelPacket sentPacket);

        /// <summary>
        /// Disconnect callback
        /// </summary>
        /// <param name="socket">client socket</param>
        void OnParallelSocketDisconnect(IParallelSocket socket);
    };
}
