using EpLibrary.cs;
using EpServerEngine.cs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Threading;

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
        private HashSet<INetworkClient> m_pendingClientSet = new HashSet<INetworkClient>();

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
        private static long m_curPacketSequence = 0;
        
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
        public ParallelSocket(INetworkSocket client, IParallelServer server)
            : base()
        {
            m_server = server;
            IPInfo = client.IPInfo;
            AddSocket(client);
        }

        ~ParallelSocket()
        {
            if (IsConnectionAlive)
                Disconnect();
        }

        /// <summary>
        /// Get IP information
        /// </summary>
        /// <returns>IP information</returns>
        public IPInfo IPInfo
        {
            get
            {
                return m_ipInfo;
            }
            private set
            {
                m_ipInfo = value;
            }
        }

        /// <summary>
        /// Get managing server
        /// </summary>
        /// <returns>managing server</returns>
        public IParallelServer Server
        {
            get
            {
                return m_server;
            }
        }

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
        /// Start the new connection, and inform the callback object, that the new connection is made
        /// </summary>
        protected override void execute()
        {
            IsConnectionAlive = true;
            startReceive();
            if (CallBackObj != null)
                CallBackObj.OnNewConnection(this);
        }

        /// <summary>
        /// Disconnect the client socket
        /// </summary>
        public void Disconnect()
        {
            lock (m_generalLock)
            {
                if (!IsConnectionAlive)
                    return;
                try
                {
                    m_client.Client.Shutdown(SocketShutdown.Both);
                    //m_client.Client.Disconnect(true);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message + " >" + ex.StackTrace);
                }
                m_client.Close();
                IsConnectionAlive = false;
            }
            m_server.DetachClient(this);

            lock (m_sendQueueLock)
            {
                m_sendQueue.Clear();
            }
            if (CallBackObj != null)
            {
                Task t = new Task(delegate()
                {
                    CallBackObj.OnDisconnect(this);
                });
                t.Start();
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
                    // 	        try
                    // 	        {
                    // 	            return m_client.Connected;
                    // 	        }
                    // 	        catch (Exception ex)
                    // 	        {
                    // 	            Console.WriteLine(ex.Message + " >" + ex.StackTrace);
                    // 	            return false;
                    // 	        }
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
        /// Send given data to the client
        /// </summary>
        /// <param name="data">data in byte array</param>
        /// <param name="offset">offset in bytes</param>
        /// <param name="dataSize">data size in bytes</param>
        public void Send(byte[] data, int offset, int dataSize)
        {
            Packet sendPacket = new Packet(data, offset, dataSize, false);
            //             byte[] packet = new byte[dataSize];
            //             MemoryStream stream = new MemoryStream(packet);
            //             stream.Write(data, offset, dataSize);
            //             Packet sendPacket = new Packet(packet,0, packet.Count(), false);
            Send(sendPacket);
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
            m_clientSet.Add(socket);
            ((IocpTcpSocket)socket).CallBackObj = this;
            ParallelPacket sendPacket = new ParallelPacket(-1, ParallelPacketType.READY, null);
            socket.Send(sendPacket.GetPacketRaw());
        }

        public Guid Guid
        {
            get
            {
                return m_guid;
            }
        }


        /// <summary>
        /// NewConnection callback
        /// </summary>
        /// <param name="socket">client socket</param>
        public void OnNewConnection(INetworkSocket socket)
        {

        }

        /// <summary>
        /// Receive callback
        /// </summary>
        /// <param name="socket">client socket</param>
        /// <param name="receivedPacket">received packet</param>
        public void OnReceived(INetworkSocket socket, Packet receivedPacket)
        {

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

    }

}
