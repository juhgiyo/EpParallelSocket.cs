/*! 
@file ParallelClient.cs
@author Woong Gyu La a.k.a Chris. <juhgiyo@gmail.com>
		<http://github.com/juhgiyo/epparallelclient.cs>
@date October 13, 2015
@brief ParallelClient Interface
@version 2.0

@section LICENSE

The MIT License (MIT)

Copyright (c) 2014 Woong Gyu La <juhgiyo@gmail.com>

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
    public sealed class ParallelClient : ThreadEx, IParallelClient, INetworkClientCallback
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
        private static long m_curPacketSequence = 0;

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
        /// flag for nodelay
        /// </summary>
        private bool m_noDelay;
        /// <summary>
        /// wait time in millisecond
        /// </summary>
        private int m_connectionTimeOut;

        /// <summary>
        /// flag for connection check
        /// </summary>
        private volatile bool m_isConnected = false;

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
            if (IsConnectionAlive)
                Disconnect();
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
            private set
            {
                lock (m_generalLock)
                {
                    m_callBackObj = value;
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
        /// flag for no delay
        /// </summary>
        public bool NoDelay
        {
            get
            {
                lock (m_generalLock)
                {
                    return m_noDelay;
                }
            }
            private set
            {
                lock (m_generalLock)
                {
                    m_noDelay = value;
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
                    NoDelay = m_clientOps.NoDelay;
                    ConnectionTimeOut = m_clientOps.ConnectionTimeOut;

                    m_curPacketSequence = 0;
                    m_clientSet.Clear();

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
                    ClientOps clientOps = new ClientOps(this, HostName, Port, NoDelay, ConnectionTimeOut);
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
                if (CallBackObj != null)
                {
                    Task t = new Task(delegate()
                    {
                        CallBackObj.OnConnected(this, status);
                    });
                    t.Start();

                }
                return;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message + " >" + ex.StackTrace);
                if (CallBackObj != null)
                {
                    Task t = new Task(delegate()
                    {
                        CallBackObj.OnConnected(this, ConnectStatus.FAIL_SOCKET_ERROR);
                    });
                    t.Start();
                }
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
                            client.Send(sendPacket.GetPacketRaw());
                            continue;
                        }
                        else if (m_packetQueue.Count > 0)
                        {
                            ParallelPacket sendPacket = m_packetQueue.Dequeue();
                            m_pendingPacketSet.Add(sendPacket);
                            INetworkClient client = m_pendingClientSet.First();
                            m_pendingClientSet.Remove(client);
                            client.Send(sendPacket.GetPacketRaw());
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
            if (IsConnectionAlive)
            {
                return;
            }

            if (ops == null)
                ops = ParallelClientOps.defaultClientOps;
            if (ops.CallBackObj == null)
                throw new NullReferenceException("callBackObj is null!");
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
                try
                {
                    m_curPacketSequence++;
                }
                catch (Exception)
                {
                    m_curPacketSequence = 0;
                }
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
            if (status == ConnectStatus.SUCCESS)
            {
                lock (m_generalLock)
                {
                    m_clientSet.Add(client);
                }
                CurSocketCount++;
                if (CurSocketCount == 1)
                {
                    if (CallBackObj != null)
                    {
                        Task t = new Task(delegate()
                        {
                            CallBackObj.OnConnected(this, ConnectStatus.FAIL_SOCKET_ERROR);
                        });
                        t.Start();
                        //CallBackObj.OnConnected(this, ConnectStatus.FAIL_SOCKET_ERROR);
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
            switch (receivedParallelPacket.GetPacketType())
            {
                case ParallelPacketType.DATA:
                    if (m_receiveType == ReceiveType.BURST)
                    {
                        if (CallBackObj != null)
                        {
                            Task t = new Task(delegate()
                            {
                                CallBackObj.OnReceived(this, receivedParallelPacket);
                            });
                            t.Start();
                            //CallBackObj.OnReceived(this, receivedParallelPacket);
                        }
                    }
                    else if (m_receiveType == ReceiveType.SEQUENTIAL)
                    {
                        lock (m_receiveLock)
                        {
                            m_receivedQueue.Enqueue(receivedParallelPacket);
                            while (m_curReceivedPacketId == -1 || m_curReceivedPacketId + 1 == m_receivedQueue.Peek().GetPacketID())
                            {
                                ParallelPacket curPacket = m_receivedQueue.Dequeue();
                                m_curReceivedPacketId = curPacket.GetPacketID();
                                if (CallBackObj != null)
                                {
                                    Task t = new Task(delegate()
                                    {
                                        CallBackObj.OnReceived(this, receivedParallelPacket);
                                    });
                                    t.Start();
                                    //m_callBackObj.OnReceived(this, receivedParallelPacket);
                                }
                            }
                        }
                    }
                    break;
                case ParallelPacketType.IDENTITY_REQUEST:
                    ParallelPacket sendPacket=new ParallelPacket(getCurPacketSequence(),ParallelPacketType.IDENTITY_RESPONSE,Guid.ToByteArray());
                    client.Send(sendPacket.GetPacketRaw());
                    break;
                case ParallelPacketType.READY:
                    lock (m_sendLock)
                    {
                        m_pendingClientSet.Add(client);
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
            if (sentParallelPacket.GetPacketType() == ParallelPacketType.DATA)
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
                if (CallBackObj != null)
                {
                    Task t = new Task(delegate()
                    {
                        CallBackObj.OnSent(this, status, sentParallelPacket);
                    });
                    t.Start();
                    //CallBackObj.OnSent(this, status, sentParallelPacket);
                }           
            
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
                    if (CallBackObj != null)
                    {
                        Task t = new Task(delegate()
                        {
                            CallBackObj.OnDisconnect(this);
                        });
                        t.Start();
                    }
                }
            }
        }
    }
}
