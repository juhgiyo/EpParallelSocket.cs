using EpLibrary.cs;
using EpServerEngine.cs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace EpParallelSocket.cs
{

    /// <summary>
    /// IOCP TCP Socket class
    /// </summary>
    public sealed class ParallelSocket : ThreadEx, IParallelSocket
    {
        /// <summary>
        /// actual client
        /// </summary>
        private HashSet<IocpTcpSocket> m_clientSet = null;
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
        /// send lock
        /// </summary>
        private Object m_sendLock = new Object();
        /// <summary>
        /// send queue lock
        /// </summary>
        private Object m_sendQueueLock = new Object();
        /// <summary>
        /// send queue
        /// </summary>
        private Queue<ParallelPacket> m_sendQueue = new Queue<ParallelPacket>();
        /// <summary>
        /// callback object
        /// </summary>
        private IParallelSocketCallback m_callBackObj = null;

        /// <summary>
        /// send event
        /// </summary>
        private EventEx m_sendEvent = new EventEx();

        /// <summary>
        /// flag for connection check
        /// </summary>
        private bool m_isConnected = false;



        /// <summary>
        /// Default constructor
        /// </summary>
        /// <param name="client">client</param>
        /// <param name="server">managing server</param>
        public ParallelSocket(TcpClient client, INetworkServer server)
            : base()
        {
            m_client = client;
            m_server = server;
            IPEndPoint remoteIpEndPoint = m_client.Client.RemoteEndPoint as IPEndPoint;
            IPEndPoint localIpEndPoint = m_client.Client.LocalEndPoint as IPEndPoint;
            if (remoteIpEndPoint != null)
            {
                String socketHostName = remoteIpEndPoint.Address.ToString();
                m_ipInfo = new IPInfo(socketHostName, remoteIpEndPoint, IPEndPointType.REMOTE);
            }
            else if (localIpEndPoint != null)
            {
                String socketHostName = localIpEndPoint.Address.ToString();
                m_ipInfo = new IPInfo(socketHostName, localIpEndPoint, IPEndPointType.LOCAL);
            }

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

        /// <summary>
        /// Send callback function
        /// </summary>
        /// <param name="result">result</param>
        private static void onSent(IAsyncResult result)
        {
          
        }

    }

}
