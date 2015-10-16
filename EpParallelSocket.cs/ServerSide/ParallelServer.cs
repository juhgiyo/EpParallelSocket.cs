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
    public sealed class ParallelServer : ThreadEx, IParallelServer
    {
        /// <summary>
        /// port
        /// </summary>
        private String m_port = ParallelSocketConf.DEFAULT_PORT;

        /// <summary>
        /// receive type
        /// </summary>
        private ReceiveType m_receiveType = ReceiveType.SEQUENTIAL;
        /// <summary>
        /// listner
        /// </summary>
        private TcpListener m_listener = null;
        /// <summary>
        /// server option
        /// </summary>
        private ParallelServerOps m_serverOps = null;

        /// <summary>
        /// callback object
        /// </summary>
        private IParallelServerCallback m_callBackObj = null;

        /// <summary>
        /// general lock
        /// </summary>
        private Object m_generalLock = new Object();

        /// <summary>
        /// client socket list lock
        /// </summary>
        private Object m_listLock = new Object();
        /// <summary>
        /// client socket list
        /// </summary>
        private Dictionary<Guid, ParallelSocket> m_socketMap = new Dictionary<Guid, ParallelSocket>();

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
            private set
            {
                lock (m_generalLock)
                {
                    m_callBackObj = value;
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

                    CallBackObj = m_serverOps.CallBackObj;
                    Port = m_serverOps.Port;
                    ReceiveType = m_serverOps.ReceiveType;

                    if (Port == null || Port.Length == 0)
                    {
                        Port = ServerConf.DEFAULT_PORT;
                    }
                    m_socketMap.Clear();

                    m_listener = new TcpListener(IPAddress.Any, Convert.ToInt32(m_port));
                    m_listener.Server.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
                    m_listener.Start();
                    m_listener.BeginAcceptTcpClient(new AsyncCallback(ParallelServer.onAccept), this);
                }

            }
            catch (CallbackException)
            {
                CallBackObj.OnServerStarted(this, status);
                return;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message + " >" + ex.StackTrace);
                if (m_listener != null)
                    m_listener.Stop();
                m_listener = null;
                CallBackObj.OnServerStarted(this, StartStatus.FAIL_SOCKET_ERROR);
                return;
            }
            CallBackObj.OnServerStarted(this, StartStatus.SUCCESS);
        }

        /// <summary>
        /// Accept callback function
        /// </summary>
        /// <param name="result">result</param>
        private static void onAccept(IAsyncResult result)
        {
            ParallelServer server = result.AsyncState as ParallelServer;
            TcpClient client = null;
            try { client = server.m_listener.EndAcceptTcpClient(result); }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message + " >" + ex.StackTrace);
                if (client != null)
                {
                    try
                    {
                        client.Client.Shutdown(SocketShutdown.Both);
                        //client.Client.Disconnect(true);
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e.Message + " >" + e.StackTrace);
                    }
                    client.Close();
                    client = null;
                }
            }

            try { server.m_listener.BeginAcceptTcpClient(new AsyncCallback(ParallelServer.onAccept), server); }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message + " >" + ex.StackTrace);
                if (client != null)
                    client.Close();
                server.StopServer();
                return;
            }

            if (client != null)
            {
                // TODO: Need to map with GUID
                ParallelSocket socket = new ParallelSocket(client, server);
                IParallelSocketCallback socketCallbackObj = server.CallBackObj.OnAccept(server, socket.IPInfo);
                if (socketCallbackObj == null)
                {
                    socket.Disconnect();
                }
                else
                {
                    socket.CallBackObj = socketCallbackObj;
                    socket.Start();
                    lock (server.m_listLock)
                    {
                        server.m_socketMap.Add(socket);
                    }
                }
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
            if (ops.CallBackObj == null)
                throw new NullReferenceException("callBackObj is null!");
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

                m_listener.Stop();
                m_listener = null;
            }
            ShutdownAllClient();

            if (CallBackObj != null)
                CallBackObj.OnServerStopped(this);
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
                    if (m_listener != null)
                        return true;
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
        public void Broadcast(byte[] data, int offset, int dataSize)
        {
            List<ParallelSocket> socketList = GetClientSocketList();

            foreach (ParallelSocket socket in socketList)
            {
                socket.Send(data, offset, dataSize);
            }
        }

        /// <summary>
        /// Broadcast given data to the server
        /// </summary>
        /// <param name="data">data in byte array</param>
        public void Broadcast(byte[] data)
        {
            List<ParallelSocket> socketList = GetClientSocketList();

            foreach (ParallelSocket socket in socketList)
            {
                socket.Send(data);
            }
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
        public bool DetachClient(IocpTcpSocket clientSocket)
        {
            lock (m_listLock)
            {
                return m_socketMap.Remove(clientSocket.Guid);
            }
        }

    }
}
