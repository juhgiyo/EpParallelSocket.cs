/*! 
@file ParallelClient.cs
@author Woong Gyu La a.k.a Chris. <juhgiyo@gmail.com>
		<http://github.com/juhgiyo/epparallelsocket.cs>
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

A ParallelClient Class.

*/
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using EpServerEngine.cs;
using EpLibrary.cs;
using System.Diagnostics;
using System.Collections.Concurrent;

namespace EpParallelSocket.cs
{
    /// <summary>
    /// A Parallel Client class.
    /// </summary>
    public sealed class ParallelClient : ThreadEx, IParallelClient, INetworkClientCallback, IDisposable
    {
        /// <summary>
        /// client options
        /// </summary>
        private ParallelClientOps m_clientOps = null;

        /// <summary>
        /// GUID
        /// </summary>
        private Guid m_guid;

        /// <summary>
        /// sub client sets
        /// </summary>
        private HashSet<INetworkClient> m_clientSet = new HashSet<INetworkClient>();
        /// <summary>
        /// pending client sets
        /// </summary>
        private HashSet<INetworkClient> m_pendingClientSet = new HashSet<INetworkClient>();
        /// <summary>
        /// general lock
        /// </summary>
        private Object m_generalLock = new Object();

        /// <summary>
        /// receive lock
        /// </summary>
        private Object m_receiveLock = new Object();

        /// <summary>
        /// send lock
        /// </summary>
        private Object m_sendLock = new Object();

        /// <summary>
        /// Packet Sequence
        /// </summary>
        private long m_curPacketSequence = 0;

        /// <summary>
        /// callback object
        /// </summary>
        private IParallelClientCallback m_callBackObj = null;
        /// <summary>
        /// hostname
        /// </summary>
        private String m_hostName;
        /// <summary>
        /// port
        /// </summary>
        private String m_port;

        /// <summary>
        /// receive type
        /// </summary>
        private ReceiveType m_receiveType;

        /// <summary>
        /// number of sockets using
        /// </summary>
        private int m_maxSocketCount;

        /// <summary>
        /// current number of sockets
        /// </summary>
        private int m_curSocketCount;

        /// <summary>
        /// Last received packet ID
        /// </summary>
        private long m_curReceivedPacketId = -1;

        /// <summary>
        /// wait time in millisecond
        /// </summary>
        private int m_connectionTimeOut;

        /// <summary>
        /// flag for connection check
        /// </summary>
        private volatile bool m_isConnected = false;

        /// <summary>
        /// OnConnected event
        /// </summary>
        OnParallelClientConnectedDelegate m_onConnected = delegate { };
        /// <summary>
        /// OnRecevied event
        /// </summary>
        OnParallelClientReceivedDelegate m_onReceived = delegate { };
        /// <summary>
        /// OnSent event
        /// </summary>
        OnParallelClientSentDelegate m_onSent = delegate { };
        /// <summary>
        /// OnDisconnect event
        /// </summary>
        OnParallelClientDisconnectDelegate m_onDisconnect = delegate { };


        /// <summary>
        /// number of connection tried
        /// </summary>
        private int m_curConnectionTry = 0;

        /// <summary>
        /// send ready event
        /// </summary>
        private EventEx m_sendReadyEvent = new EventEx(false, EventResetMode.ManualReset);

        /// <summary>
        /// packet queue
        /// </summary>
        private Queue<ParallelPacket> m_packetQueue = new Queue<ParallelPacket>();
        /// <summary>
        /// pending packet set
        /// </summary>
        private HashSet<ParallelPacket> m_pendingPacketSet = new HashSet<ParallelPacket>();
        /// <summary>
        /// error packet set
        /// </summary>
        private HashSet<ParallelPacket> m_errorPacketSet = new HashSet<ParallelPacket>();

        /// <summary>
        /// received packet queue
        /// </summary>
        private PQueue<ParallelPacket> m_receivedQueue = new PQueue<ParallelPacket>();



        /// <summary>
        /// OnConnected event
        /// </summary>
        public OnParallelClientConnectedDelegate OnParallelClientConnected
        {
            get
            {
                return m_onConnected;
            }
            set
            {
                if (value == null)
                {
                    m_onConnected = delegate { };
                    if (CallBackObj != null)
                        m_onConnected += CallBackObj.OnParallelClientConnected;
                }
                else
                {
                    m_onConnected = CallBackObj != null && CallBackObj.OnParallelClientConnected != value ? CallBackObj.OnParallelClientConnected + (value - CallBackObj.OnParallelClientConnected) : value;
                }
            }
        }
        /// <summary>
        /// OnRecevied event
        /// </summary>
        public OnParallelClientReceivedDelegate OnParallelClientReceived
        {
            get
            {
                return m_onReceived;
            }
            set
            {
                if (value == null)
                {
                    m_onReceived = delegate { };
                    if (CallBackObj != null)
                        m_onReceived += CallBackObj.OnParallelClientReceived;
                }
                else
                {
                    m_onReceived = CallBackObj != null && CallBackObj.OnParallelClientReceived != value ? CallBackObj.OnParallelClientReceived + (value - CallBackObj.OnParallelClientReceived) : value;
                }
            }
        }
        /// <summary>
        /// OnSent event
        /// </summary>
        public OnParallelClientSentDelegate OnParallelClientSent
        {
            get
            {
                return m_onSent;
            }
            set
            {
                if (value == null)
                {
                    m_onSent = delegate { };
                    if (CallBackObj != null)
                        m_onSent += CallBackObj.OnParallelClientSent;
                }
                else
                {
                    m_onSent = CallBackObj != null && CallBackObj.OnParallelClientSent != value ? CallBackObj.OnParallelClientSent + (value - CallBackObj.OnParallelClientSent) : value;
                }
            }
        }
        /// <summary>
        /// OnDisconnect event
        /// </summary>
        public OnParallelClientDisconnectDelegate OnParallelClientDisconnect
        {
            get
            {
                return m_onDisconnect;
            }
            set
            {
                if (value == null)
                {
                    m_onDisconnect = delegate { };
                    if (CallBackObj != null)
                        m_onDisconnect += CallBackObj.OnParallelClientDisconnect;
                }
                else
                {
                    m_onDisconnect = CallBackObj != null && CallBackObj.OnParallelClientDisconnect != value ? CallBackObj.OnParallelClientDisconnect + (value - CallBackObj.OnParallelClientDisconnect) : value;
                }
            }
        }

        /// <summary>
        /// Default constructor
        /// </summary>
        public ParallelClient()
        {

        }

        /// <summary>
        /// Default copy constructor
        /// </summary>
        /// <param name="b">the object to copy from</param>
        public ParallelClient(ParallelClient b)
        {
            m_clientOps = b.m_clientOps;
        }

        ~ParallelClient()
        {
            Dispose(false);
        }
        /// <summary>
        /// Return the hostname
        /// </summary>
        /// <returns>hostname</returns>
        public String HostName
        {
            get
            {
                lock (m_generalLock)
                {
                    return m_hostName;
                }
            }
            private set
            {
                lock (m_generalLock)
                {
                    m_hostName = value;
                }
            }
        }
        /// <summary>
        /// Return the port
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
        /// callback object
        /// </summary>
        public IParallelClientCallback CallBackObj
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
                        m_onConnected -= m_callBackObj.OnParallelClientConnected;
                        m_onSent -= m_callBackObj.OnParallelClientSent;
                        m_onReceived -= m_callBackObj.OnParallelClientReceived;
                        m_onDisconnect -= m_callBackObj.OnParallelClientDisconnect;
                    }
                    m_callBackObj = value;
                    if (m_callBackObj != null)
                    {
                        m_onConnected += m_callBackObj.OnParallelClientConnected;
                        m_onSent += m_callBackObj.OnParallelClientSent;
                        m_onReceived += m_callBackObj.OnParallelClientReceived;
                        m_onDisconnect += m_callBackObj.OnParallelClientDisconnect;
                    }
                }
            }
        }
        /// <summary>
        /// receive type
        /// </summary>
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
        /// Return the number of sockets using
        /// </summary>
        /// <returns>number of sockets using</returns>
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
        /// current number of sockets property
        /// </summary>
        public int CurSocketCount
        {
            get
            {
                lock (m_generalLock)
                {
                    return m_curSocketCount;
                }
            }
            private set
            {
                lock (m_generalLock)
                {
                    m_curSocketCount = value;
                }
            }
        }
       
        /// <summary>
        /// connection time out in millisecond
        /// </summary>
        public int ConnectionTimeOut
        {
            get
            {
                lock (m_generalLock)
                {
                    return m_connectionTimeOut;
                }
            }
            private set
            {
                lock (m_generalLock)
                {
                    m_connectionTimeOut = value;
                }
            }
        }
        /// <summary>
        /// GUID of Parallel Client
        /// </summary>
        public Guid Guid
        {
            get
            {
                lock (m_generalLock)
                {
                    return m_guid;
                }
            }
            private set
            {
                lock (m_generalLock)
                {
                    m_guid = value;
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
        /// Make the connection to the server and start receiving
        /// </summary>
        protected override void execute()
        {
            ConnectStatus status = ConnectStatus.SUCCESS;
            try
            {
                lock (m_generalLock)
                {
                    if (IsConnectionAlive)
                    {
                        status = ConnectStatus.FAIL_ALREADY_CONNECTED;
                        throw new CallbackException();
                    }

                    Guid = Guid.NewGuid();

                    CurSocketCount = 0;


                    CallBackObj = m_clientOps.CallBackObj;
                    HostName = m_clientOps.HostName;
                    Port = m_clientOps.Port;
                    ReceiveType = m_clientOps.ReceiveType;
                    MaxSocketCount = m_clientOps.MaxSocketCount;
                    ConnectionTimeOut = m_clientOps.ConnectionTimeOut;

                    m_curPacketSequence = 0;
                    m_clientSet.Clear();
                    m_curConnectionTry = 0;


                    lock (m_sendLock)
                    {
                        m_pendingClientSet.Clear();

                        m_packetQueue.Clear();
                        m_pendingPacketSet.Clear();
                        m_errorPacketSet.Clear();
                    }
                    lock (m_receiveLock)
                    {
                        m_curReceivedPacketId = -1;
                        m_receivedQueue.Clear();
                    }


                    m_sendReadyEvent.ResetEvent();

                    if (HostName == null || HostName.Length == 0)
                    {
                        HostName = ServerConf.DEFAULT_HOSTNAME;
                    }

                    if (Port == null || Port.Length == 0)
                    {
                        Port = ServerConf.DEFAULT_PORT;
                    }
                    ClientOps clientOps = new ClientOps(this, HostName, Port, true, ConnectionTimeOut);
                    for (int i = 0; i < MaxSocketCount; i++)
                    {
                        INetworkClient client = new IocpTcpClient();
                        client.Connect(clientOps);
                    }
                    IsConnectionAlive = true;
                }
            }
            catch (CallbackException)
            {
                
                Task t = new Task(delegate()
                {
                    OnParallelClientConnected(this, status);
                });
                t.Start();


                return;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message + " >" + ex.StackTrace);
                Task t = new Task(delegate()
                {
                    OnParallelClientConnected(this, ConnectStatus.FAIL_SOCKET_ERROR);
                });
                t.Start();

                return;
            }
            startSend();
        }

        /// <summary>
        /// Start to send packet to the server
        /// </summary>
        private void startSend()
        {
            while (IsConnectionAlive)
            {
                lock (m_sendLock)
                {
                    while (m_pendingClientSet.Count > 0 && (m_errorPacketSet.Count > 0 || m_packetQueue.Count > 0))
                    {
                        if (m_errorPacketSet.Count > 0)
                        {
                            ParallelPacket sendPacket = m_errorPacketSet.First();
                            m_errorPacketSet.Remove(sendPacket);
                            m_pendingPacketSet.Add(sendPacket);
                            INetworkClient client = m_pendingClientSet.First();
                            m_pendingClientSet.Remove(client);
                            client.Send(sendPacket.PacketRaw);
                            continue;
                        }
                        else if (m_packetQueue.Count > 0)
                        {
                            ParallelPacket sendPacket = m_packetQueue.Dequeue();
                            m_pendingPacketSet.Add(sendPacket);
                            INetworkClient client = m_pendingClientSet.First();
                            m_pendingClientSet.Remove(client);
                            client.Send(sendPacket.PacketRaw);
                            continue;
                        }
                    }
                }
                m_sendReadyEvent.WaitForEvent();
            }
        }
        /// <summary>
        /// Connect to server with given option
        /// </summary>
        /// <param name="ops">option for client</param>
        public void Connect(ParallelClientOps ops)
        {
            if (ops == null)
                ops = ParallelClientOps.defaultClientOps;
//             if (ops.CallBackObj == null)
//                 throw new NullReferenceException("callBackObj is null!");
            lock (m_generalLock)
            {
                m_clientOps = ops;
            }
            Start();
        }

        /// <summary>
        /// Disconnect from the server
        /// </summary>
        public void Disconnect()
        {
            lock (m_generalLock)
            {
                List<INetworkClient> clientList=new List<INetworkClient>(m_clientSet);
                foreach (INetworkClient client in clientList)
                {
                    client.Disconnect();
                }
            }
        }

        /// <summary>
        /// Check if the connection is alive
        /// </summary>
        /// <returns></returns>
        public bool IsConnectionAlive
        {
            get
            {
                lock (m_generalLock)
                {
                    return m_isConnected;
                }
            }
            private set
            {
                lock (m_generalLock)
                {
                    m_isConnected = value;
                }
            }
        }
        /// <summary>
        /// Get current packet sequence
        /// </summary>
        /// <returns>current packet sequence</returns>
        private long getCurPacketSequence()
        {
            lock (m_generalLock)
            {
                long retSequence = m_curPacketSequence;
                if (m_curPacketSequence == long.MaxValue)
                {
                    m_curPacketSequence = -1;
                }
                m_curPacketSequence++;
                return retSequence;
            }
        }

        /// <summary>
        /// Send given packet to the server
        /// </summary>
        /// <param name="data">bytes of data</param>
        /// <param name="offset">offset from start idx</param>
        /// <param name="dataSize">data size</param>
        public void Send(byte[] data, int offset, int dataSize)
        {
            lock (m_sendLock)
            {
                m_packetQueue.Enqueue(new ParallelPacket(getCurPacketSequence(), ParallelPacketType.DATA, data, offset, dataSize));
                if (m_pendingClientSet.Count > 0 && (m_errorPacketSet.Count > 0 || m_packetQueue.Count > 0))
                    m_sendReadyEvent.SetEvent();
            }
        }

        /// <summary>
        /// Send given packet to the server
        /// </summary>
        /// <param name="data">bytes of data</param>
        public void Send(byte[] data)
        {
            Send(data, 0, data.Count());
        }


        /// <summary>
        /// Connection callback
        /// </summary>
        /// <param name="client">client</param>
        /// <param name="status">connection status</param>
        public void OnConnected(INetworkClient client, ConnectStatus status)
        {
            lock (m_generalLock)
            {
                m_curConnectionTry++;
            }
            if (status == ConnectStatus.SUCCESS)
            {
                lock (m_generalLock)
                {
                    m_clientSet.Add(client);
                }
                CurSocketCount++;
                if (CurSocketCount == 1)
                {
                    Task t = new Task(delegate()
                    {
                        OnParallelClientConnected(this, status);
                    });
                    t.Start();
                    
                }

            }
            else
            {
                lock (m_generalLock)
                {
                    // if all sockets failed, trigger IsConnectionAlive to false
                    if (CurSocketCount==0 && m_curConnectionTry == MaxSocketCount)
                    {
                        m_sendReadyEvent.SetEvent();
                        IsConnectionAlive = false;
                        
                        Task t = new Task(delegate()
                        {
                            OnParallelClientConnected(this, status);
                        });
                        t.Start();
                        
                    }
                }
            }
            
        }

        /// <summary>
        /// Receive callback
        /// </summary>
        /// <param name="client">client</param>
        /// <param name="receivedPacket">received packet</param>
        public void OnReceived(INetworkClient client, Packet receivedPacket)
        {
            ParallelPacket receivedParallelPacket = new ParallelPacket(receivedPacket);
            switch (receivedParallelPacket.PacketType)
            {
                case ParallelPacketType.DATA:
                    if (ReceiveType == ReceiveType.BURST)
                    {
                        OnParallelClientReceived(this, receivedParallelPacket);
                    }
                    else if (m_receiveType == ReceiveType.SEQUENTIAL)
                    {
                        lock (m_receiveLock)
                        {
                            m_receivedQueue.Enqueue(receivedParallelPacket);
                            while (!m_receivedQueue.IsEmpty() && m_curReceivedPacketId + 1 == m_receivedQueue.Peek().PacketID)
                            {
                                ParallelPacket curPacket = m_receivedQueue.Dequeue();
                                m_curReceivedPacketId = curPacket.PacketID;
                                if (m_curReceivedPacketId == long.MaxValue)
                                {
                                    m_curReceivedPacketId = -1;
                                }
                                OnParallelClientReceived(this, curPacket);
                            }
                        }
                    }
                    break;
                case ParallelPacketType.IDENTITY_REQUEST:
                    PacketSerializer<IdentityResponse> serializer = new PacketSerializer<IdentityResponse>(new IdentityResponse(Guid,MaxSocketCount));
                    ParallelPacket sendPacket=new ParallelPacket(-1,ParallelPacketType.IDENTITY_RESPONSE,serializer.PacketRaw);
                    client.Send(sendPacket.PacketRaw);
                    break;
                case ParallelPacketType.READY:
                    lock (m_sendLock)
                    {
                        m_pendingClientSet.Add(client);
                        if (m_pendingClientSet.Count > 0 && (m_errorPacketSet.Count > 0 || m_packetQueue.Count > 0))
                            m_sendReadyEvent.SetEvent();
                    }
                    break;
            }
        }

        /// <summary>
        /// Send callback
        /// </summary>
        /// <param name="client">client</param>
        /// <param name="status">send status</param>
        public void OnSent(INetworkClient client, SendStatus status, Packet sentPacket)
        {
            ParallelPacket sentParallelPacket = ParallelPacket.FromPacket(sentPacket);
            if (sentParallelPacket.PacketType == ParallelPacketType.DATA)
            {
                lock (m_sendLock)
                {
                    m_pendingPacketSet.Remove(sentParallelPacket);
                    if (status == SendStatus.SUCCESS || status == SendStatus.FAIL_INVALID_PACKET)
                    {
                        m_pendingClientSet.Add(client);
                    }
                    if (status != SendStatus.SUCCESS)
                    {
                        m_errorPacketSet.Add(sentParallelPacket);
                    }
                    if (m_pendingClientSet.Count > 0 && (m_errorPacketSet.Count > 0 || m_packetQueue.Count > 0))
                        m_sendReadyEvent.SetEvent();
                }
                
                Task t = new Task(delegate()
                {
                    OnParallelClientSent(this, status, sentParallelPacket);
                });
                t.Start();
                
            
            }
        }

        /// <summary>
        /// Disconnect callback
        /// </summary>
        /// <param name="client">client</param>
        public void OnDisconnect(INetworkClient client)
        {
            lock (m_generalLock)
            {
                m_clientSet.Remove(client);

                lock (m_sendLock)
                {
                    m_pendingClientSet.Remove(client);
                }

                CurSocketCount--;
                if (CurSocketCount <= 0)
                {
                    lock (m_sendLock)
                    {
                        m_packetQueue.Clear();
                        m_pendingPacketSet.Clear();
                        m_errorPacketSet.Clear();
                    }
                    lock (m_receiveLock)
                    {
                        m_receivedQueue.Clear();
                    }
                    m_sendReadyEvent.SetEvent();
                    IsConnectionAlive = false;
                    Task t = new Task(delegate()
                    {
                        OnParallelClientDisconnect(this);
                    });
                    t.Start();

                }
            }
        }
        /// <summary>
        /// Gets or sets a value indicating whether this instance is disposed.
        /// </summary>
        /// <value>
        ///  <c>true</c> if this instance is disposed; otherwise, <c>false</c>.
        /// </value>
        /// <remarks>Default initialization for a bool is 'false'</remarks>
        private bool IsDisposed { get; set; }

        public void Dispose()
        {
            // Dispose of unmanaged resources.
            Dispose(true);
            // Suppress finalization.
            GC.SuppressFinalize(this);
        }

        // Protected implementation of Dispose pattern.
        private void Dispose(bool isDisposing)
        {
            try
            {
                if (!this.IsDisposed)
                {
                    if (IsConnectionAlive)
                        Disconnect();
                    if (isDisposing)
                    {
                        // Free any other managed objects here.
                        if (m_sendReadyEvent != null)
                        {
                            m_sendReadyEvent.Dispose();
                            m_sendReadyEvent = null;
                        }
                    }

                    // Free any unmanaged objects here.
                }
            }
            finally
            {
                this.IsDisposed = true;
            }
        }

    }
}
