/*! 
@file ParallelServer.cs
@author Woong Gyu La a.k.a Chris. <juhgiyo@gmail.com>
		<http://github.com/juhgiyo/epparallelsocket.cs>
@date October 13, 2015
@brief Parallel Server class
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

A ParallelServer Class.

*/
using EpLibrary.cs;
using EpServerEngine.cs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace EpParallelSocket.cs
{
    /// <summary>
    /// Parallel Server
    /// </summary>
    public sealed class ParallelServer : ThreadEx, IParallelServer,INetworkServerAcceptor, INetworkServerCallback, INetworkSocketCallback
    {
        /// <summary>
        /// port
        /// </summary>
        private String m_port = ParallelSocketConf.DEFAULT_PORT;

        /// <summary>
        /// maximum socket count
        /// </summary>
        private int m_maxSocketCount = SocketCount.Infinite;

        /// <summary>
        /// maximum number of stream per socket
        /// </summary>
        private int m_maxStreamCountPerSocket = SocketCount.Infinite;

        /// <summary>
        /// receive type
        /// </summary>
        private ReceiveType m_receiveType = ReceiveType.SEQUENTIAL;
        /// <summary>
        /// listner
        /// </summary>
        private IocpTcpServer m_listener;
        /// <summary>
        /// server option
        /// </summary>
        private ParallelServerOps m_serverOps = null;

        /// <summary>
        /// callback object
        /// </summary>
        private IParallelServerCallback m_callBackObj = null;

        /// <summary>
        /// acceptor object
        /// </summary>
        private IParallelServerAcceptor m_acceptor = null;

        /// <summary>
        /// room callback object
        /// </summary>
        private IParallelRoomCallback m_roomCallBackObj = null;

        /// <summary>
        /// general lock
        /// </summary>
        private Object m_generalLock = new Object();

        /// <summary>
        /// client socket list lock
        /// </summary>
        private Object m_listLock = new Object();

        /// <summary>
        /// client socket room lock
        /// </summary>
        private Object m_roomLock = new Object();

        /// <summary>
        /// client socket list
        /// </summary>
        private Dictionary<Guid, ParallelSocket> m_socketMap = new Dictionary<Guid, ParallelSocket>();


        /// <summary>
        /// room list
        /// </summary>
        private Dictionary<string, ParallelRoom> m_roomMap = new Dictionary<string, ParallelRoom>();

        /// <summary>
        /// OnServerStarted event
        /// </summary>
        OnParallelServerStartedDelegate m_onServerStarted = delegate { };
        /// <summary>
        ///  OnAccept event
        /// </summary>
        OnParallelServerAcceptedDelegate m_onAccepted = delegate { };
        /// <summary>
        /// OnserverStopped event
        /// </summary>
        OnParallelServerStoppedDelegate m_onServerStopped = delegate { };

        /// <summary>
        /// OnServerStarted event
        /// </summary>
        public OnParallelServerStartedDelegate OnParallelServerStarted
        {
            get
            {
                return m_onServerStarted;
            }
            set
            {
                if (value == null)
                {
                    m_onServerStarted = delegate { };
                    if (CallBackObj != null)
                        m_onServerStarted += CallBackObj.OnParallelServerStarted;
                }
                else
                {
                    m_onServerStarted = CallBackObj != null && CallBackObj.OnParallelServerStarted != value ? CallBackObj.OnParallelServerStarted + (value - CallBackObj.OnParallelServerStarted) : value;
                }
            }
        }
        /// <summary>
        ///  OnAccept event
        /// </summary>
        public OnParallelServerAcceptedDelegate OnParallelServerAccepted
        {
            get
            {
                return m_onAccepted;
            }
            set
            {
                if (value == null)
                {
                    m_onAccepted = delegate { };
                    if (CallBackObj != null)
                        m_onAccepted += CallBackObj.OnParallelServerAccepted;
                }
                else
                {
                    m_onAccepted = CallBackObj != null && CallBackObj.OnParallelServerAccepted != value ? CallBackObj.OnParallelServerAccepted + (value - CallBackObj.OnParallelServerAccepted) : value;
                }
            }
        }
        /// <summary>
        /// OnserverStopped event
        /// </summary>
        public OnParallelServerStoppedDelegate OnParallelServerStopped
        {
            get
            {
                return m_onServerStopped;
            }
            set
            {
                if (value == null)
                {
                    m_onServerStopped = delegate { };
                    if (CallBackObj != null)
                        m_onServerStarted += CallBackObj.OnParallelServerStarted;
                }
                else
                {
                    m_onServerStopped = CallBackObj != null && CallBackObj.OnParallelServerStopped != value ? CallBackObj.OnParallelServerStopped + (value - CallBackObj.OnParallelServerStopped) : value;
                }
            }
        }

        /// <summary>
        /// Default constructor
        /// </summary>
        public ParallelServer()
            : base()
        {
        }

        /// <summary>
        /// Default copy constructor
        /// </summary>
        /// <param name="b">the object to copy from</param>
        public ParallelServer(ParallelServer b)
            : base(b)
        {
            m_port = b.m_port;
            m_serverOps = b.m_serverOps;
            m_receiveType = b.m_receiveType;
        }

        ~ParallelServer()
        {
            if (IsServerStarted)
                StopServer();
        }

        /// <summary>
        /// Return port
        /// </summary>
        /// <returns>port</returns>
        public String Port
        {
            get
            {
                lock (m_generalLock)
                {
                    return m_port;
                }

            }
            private set
            {
                lock (m_generalLock)
                {
                    m_port = value;
                }
            }
        }

        /// <summary>
        /// Return port
        /// </summary>
        /// <returns>port</returns>
        public ReceiveType ReceiveType
        {
            get
            {
                lock (m_generalLock)
                {
                    return m_receiveType;
                }

            }
            private set
            {
                lock (m_generalLock)
                {
                    m_receiveType = value;
                }
            }
        }
        /// <summary>
        /// acceptor object
        /// </summary>
        public IParallelServerAcceptor Acceptor
        {
            get
            {
                lock (m_generalLock)
                {
                    return m_acceptor;
                }
            }
            set
            {
                lock (m_generalLock)
                {
                    if (value == null)
                        throw new NullReferenceException("Acceptor cannot be null!");
                    m_acceptor = value;
                }

            }
        }

        /// <summary>
        /// callback object
        /// </summary>
        public IParallelServerCallback CallBackObj
        {
            get
            {
                lock (m_generalLock)
                {
                    return m_callBackObj;
                }
            }
            set
            {
                lock (m_generalLock)
                {
                    if (m_callBackObj != null)
                    {
                        m_onServerStarted -= m_callBackObj.OnParallelServerStarted;
                        m_onServerStopped -= m_callBackObj.OnParallelServerStopped;
                    }
                    m_callBackObj = value;
                    if (m_callBackObj != null)
                    {
                        m_onServerStarted += m_callBackObj.OnParallelServerStarted;
                        m_onServerStopped += m_callBackObj.OnParallelServerStopped;
                    }
                }
            }
        }


        /// <summary>
        /// room callback object
        /// </summary>
        public IParallelRoomCallback RoomCallBackObj
        {
            get
            {
                lock (m_generalLock)
                {
                    return m_roomCallBackObj;
                }
            }
            set
            {
                lock (m_generalLock)
                {
                    m_roomCallBackObj = value;
                }
            }
        }

        /// <summary>
        /// maximum socket count property
        /// </summary>
        public int MaxSocketCount
        {
            get
            {
                lock (m_generalLock)
                {
                    return m_maxSocketCount;
                }
            }
            private set
            {
                lock (m_generalLock)
                {
                    m_maxSocketCount = value;
                }
            }
        }

        /// <summary>
        /// maximum number of stream per parallel socket
        /// </summary>
        public int MaxStreamCountPerSocket
        {
            get
            {
                lock (m_generalLock)
                {
                    return m_maxStreamCountPerSocket;
                }
            }
            private set
            {
                lock (m_generalLock)
                {
                    m_maxStreamCountPerSocket = value;
                }
            }
        }
        
        /// <summary>
        /// Callback Exception class
        /// </summary>
        [Serializable]
        private class CallbackException : Exception
        {
            /// <summary>
            /// Default constructor
            /// </summary>
            public CallbackException()
                : base()
            {

            }

            /// <summary>
            /// Default constructor
            /// </summary>
            /// <param name="message">message for exception</param>
            public CallbackException(String message)
                : base(message)
            {

            }
        }

        /// <summary>
        /// Start the server and start accepting the client
        /// </summary>
        protected override void execute()
        {
            StartStatus status = StartStatus.FAIL_SOCKET_ERROR;
            try
            {
                lock (m_generalLock)
                {
                    if (IsServerStarted)
                    {
                        status = StartStatus.FAIL_ALREADY_STARTED;
                        throw new CallbackException();
                    }
                    Acceptor = m_serverOps.Acceptor;
                    CallBackObj = m_serverOps.CallBackObj;
                    RoomCallBackObj = m_serverOps.RoomCallBackObj;
                    Port = m_serverOps.Port;
                    ReceiveType = m_serverOps.ReceiveType;
                    MaxSocketCount = m_serverOps.MaxSocketCount;
                    MaxStreamCountPerSocket = m_serverOps.MaxStreamCountPerSocket;

                    if (Port == null || Port.Length == 0)
                    {
                        Port = ServerConf.DEFAULT_PORT;
                    }
                    lock (m_listLock)
                    {
                        m_socketMap.Clear();
                    }
                    lock (m_roomLock)
                    {
                        m_roomMap.Clear();
                    }          

                    m_listener = new IocpTcpServer();
                    ServerOps listenerOps = new ServerOps(this, m_serverOps.Port, this,null, true, MaxSocketCount);
                    m_listener.StartServer(listenerOps);
                }

            }
            catch (CallbackException)
            {
                OnParallelServerStarted(this, status);
                return;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message + " >" + ex.StackTrace);
                if (m_listener != null)
                    m_listener.StopServer();
                m_listener = null;
                OnParallelServerStarted(this, StartStatus.FAIL_SOCKET_ERROR);
                return;
            }
        }

        /// <summary>
        /// Start the server with given option
        /// </summary>
        /// <param name="ops">options</param>
        public void StartServer(ParallelServerOps ops)
        {
            if (ops == null)
                ops = ParallelServerOps.defaultServerOps;
            if (ops.Acceptor == null)
                throw new NullReferenceException("acceptor cannot be null!");
            lock (m_generalLock)
            {
                m_serverOps = ops;
            }
            Start();
        }
        /// <summary>
        /// Stop the server
        /// </summary>
        public void StopServer()
        {
            lock (m_generalLock)
            {
                if (!IsServerStarted)
                    return;

                m_listener.StopServer();
                m_listener = null;
            }
        }

        /// <summary>
        /// Check if the server is started
        /// </summary>
        /// <returns>true if the server is started, otherwise false</returns>
        public bool IsServerStarted
        {
            get
            {
                lock (m_generalLock)
                {
                    if(m_listener!=null)
                        return m_listener.IsServerStarted;
                    else
                        return false;
                }
            }
        }
        /// <summary>
        /// Shut down all the client, connected
        /// </summary>
        public void ShutdownAllClient()
        {
            lock (m_listLock)
            {
                List<ParallelSocket> socketList = GetClientSocketList();
                foreach (ParallelSocket socket in socketList)
                {
                    socket.Disconnect();
                }
            }
        }

        /// <summary>
        /// Broadcast given data to the server
        /// </summary>
        /// <param name="data">data in byte array</param>
        /// <param name="offset">offset in bytes</param>
        /// <param name="dataSize">data size in bytes</param>
        public void Broadcast(IParallelSocket sender, byte[] data, int offset, int dataSize)
        {
            List<ParallelSocket> socketList = GetClientSocketList();

            foreach (ParallelSocket socket in socketList)
            {
                if(socket!=sender)
                    socket.Send(data, offset, dataSize);
            }
        }

        /// <summary>
        /// Broadcast given data to the server
        /// </summary>
        /// <param name="data">data in byte array</param>
        public void Broadcast(IParallelSocket sender, byte[] data)
        {
            List<ParallelSocket> socketList = GetClientSocketList();

            foreach (ParallelSocket socket in socketList)
            {
                if (socket != sender)
                    socket.Send(data);
            }
        }
        /// <summary>
        /// Broadcast given data to the server
        /// </summary>
        /// <param name="data">data in byte array</param>
        /// <param name="offset">offset in bytes</param>
        /// <param name="dataSize">data size in bytes</param>
        public void Broadcast(byte[] data, int offset, int dataSize)
        {
            Broadcast(null, data, offset, dataSize);
        }

        /// <summary>
        /// Broadcast given data to the server
        /// </summary>
        /// <param name="data">data in byte array</param>
        public void Broadcast(byte[] data)
        {
            Broadcast(null, data);
        }

        /// <summary>
        /// Return the client socket list
        /// </summary>
        /// <returns>the client socket list</returns>
        public List<ParallelSocket> GetClientSocketList()
        {
            lock (m_listLock)
            {
                return new List<ParallelSocket>(m_socketMap.Values);
            }
        }

        /// <summary>
        /// Detach the given client from the server management
        /// </summary>
        /// <param name="clientSocket">the client to detach</param>
        /// <returns></returns>
        public bool DetachClient(ParallelSocket clientSocket)
        {
            lock (m_listLock)
            {
                return m_socketMap.Remove(clientSocket.Guid);
            }
        }

        /// <summary>
        /// Accept callback
        /// </summary>
        /// <param name="server">server</param>
        /// <param name="ipInfo">connection info</param>
        /// <returns>the socket callback interface</returns>
        public bool OnAccept(INetworkServer server, IPInfo ipInfo)
        {
            return true;
        }
        /// <summary>
        /// Should return the socket callback object
        /// </summary>
        /// <returns>the socket callback object</returns>
        public INetworkSocketCallback GetSocketCallback()
        {
            return this;
        }


        /// <summary>
        /// Server started callback
        /// </summary>
        /// <param name="server">server</param>
        /// <param name="status">start status</param>
        public void OnServerStarted(INetworkServer server, StartStatus status)
        {
            OnParallelServerStarted(this, status);
        }

  
        /// <summary>
        /// Accept callback
        /// </summary>
        /// <param name="server">server</param>
        /// <param name="ipInfo">connection info</param>
        /// <returns>the socket callback interface</returns>
        public void OnServerAccepted(INetworkServer server, INetworkSocket socket)
        {
        }
        /// <summary>
        /// Server stopped callback
        /// </summary>
        /// <param name="server">server</param>
        public void OnServerStopped(INetworkServer server)
        {
            OnParallelServerStopped(this);
        }


        /// <summary>
        /// NewConnection callback
        /// </summary>
        /// <param name="socket">client socket</param>
        public void OnNewConnection(INetworkSocket socket)
        {
            // Request Identity of new connected socket
            ParallelPacket sendPacket = new ParallelPacket(-1, ParallelPacketType.IDENTITY_REQUEST,null);
            socket.Send(sendPacket.PacketRaw);
        }

        /// <summary>
        /// Receive callback
        /// </summary>
        /// <param name="socket">client socket</param>
        /// <param name="receivedPacket">received packet</param>
        public void OnReceived(INetworkSocket socket, Packet receivedPacket)
        {
            ParallelPacket receivedParallelPacket = new ParallelPacket(receivedPacket);
            switch (receivedParallelPacket.PacketType)
            {
                case ParallelPacketType.IDENTITY_RESPONSE:
                    PacketSerializer<IdentityResponse> serializer = new PacketSerializer<IdentityResponse>(receivedParallelPacket.PacketRaw,receivedParallelPacket.HeaderSize,receivedParallelPacket.DataByteSize);
                    IdentityResponse response = serializer.ClonePacketObj();
                    Guid guid = response.m_guid;
                    int streamCount = response.m_streamCount;
                    lock (m_listLock)
                    {
                        if (m_socketMap.ContainsKey(guid))
                        {
                            m_socketMap[guid].AddSocket(socket);
                        }
                        else
                        {
                            if (CallBackObj == null)
                            {
                                socket.Disconnect();
                                return;
                            }
                            
                            if (Acceptor.OnAccept(this, socket.IPInfo, streamCount))
                            {
                                // Create new Parallel Socket
                                IParallelSocketCallback socketCallback = Acceptor.GetSocketCallback();
                                ParallelSocket parallelSocket = new ParallelSocket(guid,socket, this);
                                parallelSocket.CallBackObj = socketCallback;
                                parallelSocket.Start();
                                m_socketMap[guid] = parallelSocket;
                                OnParallelServerAccepted(this, parallelSocket);
                            }
                            else
                            {
                                // Rejected by server
                                socket.Disconnect();
                            }
                        }
                    }
                    break;
                default:
                    // Invalid protocol
                    socket.Disconnect();
                    break;
            }
        }

        /// <summary>
        /// Send callback
        /// </summary>
        /// <param name="socket">client socket</param>
        /// <param name="status">stend status</param>
        /// <param name="sentPacket">sent packet</param>
        public void OnSent(INetworkSocket socket, SendStatus status, Packet sentPacket)
        {
        }

        /// <summary>
        /// Disconnect callback
        /// </summary>
        /// <param name="socket">client socket</param>
        public void OnDisconnect(INetworkSocket socket)
        {
        }


        /// <summary>
        /// Return the room instance of given room name
        /// </summary>
        /// <param name="roomName">room name</param>
        /// <returns>the room instance</returns>
        public IParallelRoom GetRoom(string roomName)
        {
            lock (m_roomLock)
            {
                if (m_roomMap.ContainsKey(roomName))
                    return m_roomMap[roomName];
                return null;
            }
        }

        /// <summary>
        /// Return the list of names of the room
        /// </summary>
        public List<string> RoomNames
        {
            get
            {
                lock (m_roomLock)
                {
                    return new List<string>(m_roomMap.Keys);
                }
            }
        }

        /// <summary>
        /// Return the list of rooms
        /// </summary>
        public List<IParallelRoom> Rooms
        {
            get
            {
                lock (m_roomLock)
                {
                    return new List<IParallelRoom>(m_roomMap.Values);
                }
            }
        }

        /// <summary>
        /// Join the room
        /// </summary>
        /// <param name="socket">socket</param>
        /// <param name="roomName">room name</param>
        /// <returns>the instance of the room</returns>
        public ParallelRoom Join(IParallelSocket socket, string roomName)
        {
            lock (m_roomLock)
            {
                ParallelRoom curRoom = null;
                if (m_roomMap.ContainsKey(roomName))
                {
                    curRoom = m_roomMap[roomName];
                }
                else
                {
                    curRoom = new ParallelRoom(roomName,RoomCallBackObj);
                    m_roomMap[roomName] = curRoom;
                }
                curRoom.AddSocket(socket);
                return curRoom;
            }
        }

        /// <summary>
        /// Detach given socket from the given room
        /// </summary>
        /// <param name="socket">socket to detach</param>
        /// <param name="roomName">room name</param>
        /// <returns>number of sockets left in the room</returns>
        public int Leave(IParallelSocket socket, string roomName)
        {
            lock (m_roomLock)
            {
                if (m_roomMap.ContainsKey(roomName))
                {
                    int numSocketLeft = m_roomMap[roomName].DetachClient(socket);
                    if (numSocketLeft == 0)
                    {
                        m_roomMap.Remove(roomName);
                    }
                    return numSocketLeft;
                }
                return 0;
            }
        }

        /// <summary>
        /// Broadcast the given packet to all the client, connected
        /// </summary>
        /// <param name="data">data in byte array</param>
        /// <param name="offset">offset in bytes</param>
        /// <param name="dataSize">data size in bytes</param>
        public void BroadcastToRoom(string roomName, byte[] data, int offset, int dataSize)
        {
            lock (m_roomLock)
            {
                if (m_roomMap.ContainsKey(roomName))
                {
                    m_roomMap[roomName].Broadcast(data, offset, dataSize);
                }
            }
        }


        /// <summary>
        /// Broadcast the given packet to all the client, connected
        /// </summary>
        /// <param name="data">data in byte array</param>
        public void BroadcastToRoom(string roomName, byte[] data)
        {
            lock (m_roomLock)
            {
                if (m_roomMap.ContainsKey(roomName))
                {
                    m_roomMap[roomName].Broadcast(data);
                }
            }
        }

    }
}
