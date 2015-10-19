using EpLibrary.cs;
using EpServerEngine.cs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Diagnostics;

namespace EpParallelSocket.cs
{

    /// <summary>
    /// IOCP TCP Socket class
    /// </summary>
    public sealed class ParallelSocket : ThreadEx, IParallelSocket, INetworkSocketCallback
    {

        /// <summary>
        /// GUID
        /// </summary>
        private Guid m_guid;

        /// <summary>
        /// sub client sets
        /// </summary>
        private HashSet<INetworkSocket> m_clientSet = new HashSet<INetworkSocket>();

        /// <summary>
        /// pending client sets
        /// </summary>
        private HashSet<INetworkSocket> m_pendingClientSet = new HashSet<INetworkSocket>();

        /// <summary>
        /// managing server
        /// </summary>
        private IParallelServer m_server = null;

        /// <summary>
        /// IP information
        /// </summary>
        private IPInfo m_ipInfo;

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
        private IParallelSocketCallback m_callBackObj = null;

        /// <summary>
        /// receive type
        /// </summary>
        private ReceiveType m_receiveType;

        /// <summary>
        /// current number of sockets
        /// </summary>
        private int m_curSocketCount;

        /// <summary>
        /// maximum number of stream per parallel socket
        /// </summary>
        private int m_maxStreamCountPerSocket;

        /// <summary>
        /// Last received packet ID
        /// </summary>
        private long m_curReceivedPacketId = -1;

        /// <summary>
        /// flag for connection check
        /// </summary>
        private bool m_isConnected = false;


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
        /// <param name="client">client</param>
        /// <param name="server">managing server</param>
        public ParallelSocket(Guid guid,INetworkSocket client, IParallelServer server)
            : base()
        {
            m_guid = guid;
            m_server = server;
            IPInfo = client.IPInfo;
            ReceiveType = server.ReceiveType;
            MaxStreamCountPerSocket = server.MaxStreamCountPerSocket;
            AddSocket(client);
        }

        ~ParallelSocket()
        {
            if (IsConnectionAlive)
                Disconnect();
        }

        /// <summary>
        /// Get managing server
        /// </summary>
        /// <returns>managing server</returns>
        public IParallelServer Server
        {
            get
            {
                lock (m_generalLock)
                {
                    return m_server;
                }
            }
        }

        /// <summary>
        /// Get IP information
        /// </summary>
        /// <returns>IP information</returns>
        public IPInfo IPInfo
        {
            get
            {
                lock (m_generalLock)
                {
                    return m_ipInfo;
                }
            }
            private set
            {
                lock (m_generalLock)
                {
                    m_ipInfo = value;
                }
            }
        }


        /// <summary>
        /// callback obj property
        /// </summary>
        public IParallelSocketCallback CallBackObj
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
        /// guid property
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
        /// Start the new connection, and inform the callback object, that the new connection is made
        /// </summary>
        protected override void execute()
        {
            IsConnectionAlive = true;
            if (CallBackObj != null)
                CallBackObj.OnNewConnection(this);
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
                            INetworkSocket client = m_pendingClientSet.First();
                            m_pendingClientSet.Remove(client);
                            client.Send(sendPacket.GetPacketRaw());
                            continue;
                        }
                        else if (m_packetQueue.Count > 0)
                        {
                            ParallelPacket sendPacket = m_packetQueue.Dequeue();
                            m_pendingPacketSet.Add(sendPacket);
                            INetworkSocket client = m_pendingClientSet.First();
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
        /// Disconnect the client socket
        /// </summary>
        public void Disconnect()
        {
            lock (m_generalLock)
            {
                List<INetworkSocket> clientList = new List<INetworkSocket>(m_clientSet);
                foreach (INetworkSocket socket in clientList)
                {
                    socket.Disconnect();
                }
            }
        }

        /// <summary>
        /// Check if the connection is alive
        /// </summary>
        /// <returns>true if connection is alive, otherwise false</returns>
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
        /// Send given data to the client
        /// </summary>
        /// <param name="data">data in byte array</param>
        /// <param name="offset">offset in bytes</param>
        /// <param name="dataSize">data size in bytes</param>
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
        /// Send given data to the client
        /// </summary>
        /// <param name="data">data in byte array</param>
        public void Send(byte[] data)
        {
            Send(data, 0, data.Count());
        }

        /// <summary>
        /// Broadcast given data to all client other than this
        /// </summary>
        /// <param name="data">data in byte array</param>
        /// <param name="offset">offset in bytes</param>
        /// <param name="dataSize">data size in bytes</param>
        public void Broadcast(byte[] data, int offset, int dataSize)
        {
            List<ParallelSocket> socketList = Server.GetClientSocketList();

            foreach (ParallelSocket socket in socketList)
            {
                if (socket != this)
                    socket.Send(data, offset, dataSize);
            }
        }

        /// <summary>
        /// Broadcast given data to all client other than this
        /// </summary>
        /// <param name="data">data in byte array</param>
        public void Broadcast(byte[] data)
        {
            List<ParallelSocket> socketList = Server.GetClientSocketList();

            foreach (ParallelSocket socket in socketList)
            {
                if (socket != this)
                    socket.Send(data);
            }
        }

        public void AddSocket(INetworkSocket socket)
        {
            lock (m_generalLock)
            {
                if (MaxStreamCountPerSocket != SocketCount.Infinite && m_clientSet.Count > MaxStreamCountPerSocket)
                {
                    socket.Disconnect();
                    return;
                }
                m_clientSet.Add(socket);
            }
            CurSocketCount++;
            ((IocpTcpSocket)socket).CallBackObj = this;
            ParallelPacket sendPacket = new ParallelPacket(-1, ParallelPacketType.READY, null);
            socket.Send(sendPacket.GetPacketRaw());
            lock (m_sendLock)
            {
                m_pendingClientSet.Add(socket);
                if (m_pendingClientSet.Count > 0 && (m_errorPacketSet.Count > 0 || m_packetQueue.Count > 0))
                    m_sendReadyEvent.SetEvent();
            }
        }



        /// <summary>
        /// NewConnection callback
        /// </summary>
        /// <param name="socket">client socket</param>
        public void OnNewConnection(INetworkSocket socket)
        {
            // Will never get called
        }

        /// <summary>
        /// Receive callback
        /// </summary>
        /// <param name="socket">client socket</param>
        /// <param name="receivedPacket">received packet</param>
        public void OnReceived(INetworkSocket socket, Packet receivedPacket)
        {
            ParallelPacket receivedParallelPacket = new ParallelPacket(receivedPacket);
            switch (receivedParallelPacket.GetPacketType())
            {
                case ParallelPacketType.DATA:
                    if (ReceiveType == ReceiveType.BURST)
                    {
                        if (CallBackObj != null)
                        {
//                             Task t = new Task(delegate()
//                             {
//                                 CallBackObj.OnReceived(this, receivedParallelPacket);
//                             });
//                             t.Start();
                            CallBackObj.OnReceived(this, receivedParallelPacket);
                        }
                    }
                    else if (ReceiveType == ReceiveType.SEQUENTIAL)
                    {
                        lock (m_receiveLock)
                        {
                            m_receivedQueue.Enqueue(receivedParallelPacket);
                            while (!m_receivedQueue.IsEmpty() && m_curReceivedPacketId + 1 == m_receivedQueue.Peek().GetPacketID())
                            {
                                ParallelPacket curPacket = m_receivedQueue.Dequeue();
                                if (curPacket.GetPacketID() != -1)
                                    m_curReceivedPacketId = curPacket.GetPacketID();
                                if (CallBackObj != null)
                                {
//                                     Task t = new Task(delegate()
//                                     {
//                                         CallBackObj.OnReceived(this, curPacket);
//                                     });
//                                     t.Start();
                                    CallBackObj.OnReceived(this, curPacket);
                                }
                            }
                        }
                    }
                    break;
                default:
                    socket.Disconnect(); // Invalid protocol
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
            ParallelPacket sentParallelPacket = ParallelPacket.FromPacket(sentPacket);
            if (sentParallelPacket.GetPacketType() == ParallelPacketType.DATA)
            {
                lock (m_sendLock)
                {
                    m_pendingPacketSet.Remove(sentParallelPacket);
                    if (status == SendStatus.SUCCESS || status == SendStatus.FAIL_INVALID_PACKET)
                    {
                        m_pendingClientSet.Add(socket);
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
        /// <param name="socket">client socket</param>
        public void OnDisconnect(INetworkSocket socket)
        {
            lock (m_generalLock)
            {
                m_clientSet.Remove(socket);

                lock (m_sendLock)
                {
                    m_pendingClientSet.Remove(socket);
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
                    m_server.DetachClient(this);
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
