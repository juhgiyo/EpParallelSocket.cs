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

namespace EpParallelSocket.cs
{
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

        private HashSet<INetworkClient> m_clientSet = new HashSet<INetworkClient>();
        private HashSet<INetworkClient> m_pendingClientSet = new HashSet<INetworkClient>();
        /// <summary>
        /// general lock
        /// </summary>
        private Object m_generalLock = new Object();

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
        private int m_socketCount;

        /// <summary>
        /// current number of sockets
        /// </summary>
        private int m_curSocketCount;

        /// <summary>
        /// current number of sockets property
        /// </summary>
        public int CurSocketCount
        {
            get
            {
                lock(m_generalLock)
                {
                    return m_curSocketCount;
                }
            }
        }

        /// <summary>
        /// flag for nodelay
        /// </summary>
        private bool m_noDelay;
        /// <summary>
        /// wait time in millisecond
        /// </summary>
        private int m_waitTimeInMilliSec;

        /// <summary>
        /// flag for connection check
        /// </summary>
        private bool m_isConnected = false;


        private Queue<Packet> m_packetQueue= new Queue<Packet>();
        private HashSet<Packet> m_pendingPacketSet = new HashSet<Packet>();
        private HashSet<Packet> m_errorPacketSet = new HashSet<Packet>();
        
        private PQueue<Packet> m_receiveQueue = new PQueue<Packet>();

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
            if (IsConnectionAlive())
                Disconnect();
        }
        /// <summary>
        /// Return the hostname
        /// </summary>
        /// <returns>hostname</returns>
        public String GetHostName()
        {
            lock (m_generalLock)
            {
                return m_hostName;
            }
        }

        /// <summary>
        /// Return the port
        /// </summary>
        /// <returns>port</returns>
        public String GetPort()
        {
            lock (m_generalLock)
            {
                return m_port;
            }
        }

        public ReceiveType GetReceiveType()
        {
            lock (m_generalLock)
            {
                return m_receiveType;
            }
        }

        /// <summary>
        /// Return the number of sockets using
        /// </summary>
        /// <returns>number of sockets using</returns>
        public int GetSocketCount()
        {
            lock (m_generalLock)
            {
                return m_socketCount;
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
                    if (IsConnectionAlive())
                    {
                        status = ConnectStatus.FAIL_ALREADY_CONNECTED;
                        throw new CallbackException();
                    }

                    m_guid = Guid.NewGuid();
                    m_callBackObj = m_clientOps.callBackObj;
                    m_hostName = m_clientOps.hostName;
                    m_port = m_clientOps.port;
                    m_receiveType = m_clientOps.receiveType;
                    m_socketCount = m_clientOps.socketCount;
                    m_noDelay = m_clientOps.noDelay;
                    m_waitTimeInMilliSec = m_clientOps.waitTimeInMilliSec;

                    m_curPacketSequence = 0;
                    m_curSocketCount = 0;
                    
                    m_clientSet.Clear();
                    m_pendingClientSet.Clear();

                    m_packetQueue.Clear();
                    m_pendingPacketSet.Clear();
                    m_errorPacketSet.Clear();

                    if (m_hostName == null || m_hostName.Length == 0)
                    {
                        m_hostName = ServerConf.DEFAULT_HOSTNAME;
                    }

                    if (m_port == null || m_port.Length == 0)
                    {
                        m_port = ServerConf.DEFAULT_PORT;
                    }
                    ClientOps clientOps = new ClientOps(this, m_hostName, m_port, m_noDelay, m_waitTimeInMilliSec);
                    for (int i = 0; i < m_socketCount; i++)
                    {
                        INetworkClient client = new IocpTcpClient();
                        client.Connect(clientOps);
                    }
                    m_isConnected = true;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message + " >" + ex.StackTrace);
                if (m_callBackObj != null)
                {
                    Thread t = new Thread(delegate()
                    {
                        m_callBackObj.OnConnected(this, ConnectStatus.FAIL_SOCKET_ERROR);
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
            while (m_isConnected)
            {
                lock (m_generalLock)
                {
                    while (m_pendingClientSet.Count > 0 && (m_errorPacketSet.Count > 0 || m_packetQueue.Count > 0))
                    {
                        if (m_errorPacketSet.Count > 0)
                        {
                            Packet sendPacket = m_errorPacketSet.First();
                            m_errorPacketSet.Remove(sendPacket);
                            m_pendingPacketSet.Add(sendPacket);
                            INetworkClient client = m_pendingClientSet.First();
                            m_pendingClientSet.Remove(client);
                            client.Send(sendPacket);
                            continue;
                        }
                        else if (m_packetQueue.Count > 0)
                        {
                            Packet sendPacket = m_packetQueue.Dequeue();
                            m_pendingPacketSet.Add(sendPacket);
                            INetworkClient client = m_pendingClientSet.First();
                            m_pendingClientSet.Remove(client);
                            client.Send(sendPacket);
                            continue;
                        }
                    }
                }
                Thread.Sleep(0);
            }
        }
        /// <summary>
        /// Connect to server with given option
        /// </summary>
        /// <param name="ops">option for client</param>
        public void Connect(ParallelClientOps ops)
        {
            
            lock (m_generalLock)
            {
                if (IsConnectionAlive())
                {
                    return;
                }
            }
            if (ops == null)
                ops = ParallelClientOps.defaultClientOps;
            if (ops.callBackObj == null)
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
        }

        /// <summary>
        /// Check if the connection is alive
        /// </summary>
        /// <returns></returns>
        public bool IsConnectionAlive()
        {
            return m_isConnected;
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
        /// <param name="dataSize">data size</param>
        public void Send(byte[] data, int offset, int dataSize)
        {
            byte[] packet = new byte[sizeof(long) + sizeof(int) + dataSize];
            MemoryStream mStream = new MemoryStream(packet);
            mStream.Write(BitConverter.GetBytes(getCurPacketSequence()), 0, 8);
            mStream.Write(BitConverter.GetBytes((int)ParallelPacketType.DATA), 0, 4);
            mStream.Write(data,offset,dataSize);
            Packet sendPacket = new Packet(packet, packet.Count(), false);
            lock(m_generalLock){
                m_packetQueue.Enqueue(sendPacket);
            }
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
                    m_curSocketCount++;
                    if (m_curSocketCount == 1)
                    {
                        // TODO: callback on connected
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
            switch (m_receiveType)
            {
                case ReceiveType.BURST:
                    // TODO: callback on receive
                    break;
                case ReceiveType.SEQUENTIAL:
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
            lock (m_generalLock)
            {
                m_pendingPacketSet.Remove(sentPacket);
                if (status == SendStatus.SUCCESS || status == SendStatus.FAIL_INVALID_PACKET)
                {
                    m_pendingClientSet.Add(client);
                }
                if (status != SendStatus.SUCCESS)
                {
                    m_errorPacketSet.Add(sentPacket);
                }
                // TODO: Sent callback
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
                m_pendingClientSet.Remove(client);
                m_curSocketCount--;
                if (m_curSocketCount <= 0)
                {
                    // TODO: Disconnect callback
                }
            }
        }
    }
}
