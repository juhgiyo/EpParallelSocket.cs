using EpServerEngine.cs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EpParallelSocket.cs
{
    /// <summary>
    /// Parallel P2P class
    /// </summary>
    public sealed class ParallelP2P: IParallelP2P, IParallelSocketCallback
    {
               /// <summary>
        /// first socket
        /// </summary>
        IParallelSocket m_socket1;
        /// <summary>
        /// second socket
        /// </summary>
        IParallelSocket m_socket2;

        /// <summary>
        /// flag whether p2p is paired
        /// </summary>
        bool m_isPaired = false;

        /// <summary>
        /// general lock
        /// </summary>
        Object m_generalLock = new Object();

        /// <summary>
        /// callback object
        /// </summary>
        IParallelP2PCallback m_callBackObj;

        /// <summary>
        /// flag whether P2P is paired
        /// </summary>
        public bool Paired
        {
            get
            {
                lock (m_generalLock)
                {
                    return m_isPaired;
                }
            }
            private set
            {
                lock (m_generalLock)
                {
                    m_isPaired = value;
                }
            }
        }

        /// <summary>
        /// callback object
        /// </summary>
        public IParallelP2PCallback CallBackObj
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
        /// Default constructor
        /// </summary>
        public ParallelP2P()
        {
        }

        /// <summary>
        /// Connect given two socket as p2p
        /// </summary>
        /// <param name="socket1">first socket</param>
        /// <param name="socket2">second socket</param>
        /// <param name="callback">callback object</param>
        /// <returns>true if paired otherwise false</returns>
        public bool ConnectPair(IParallelSocket socket1, IParallelSocket socket2, IParallelP2PCallback callBackObj)
        {
            if (!Paired)
            {
                if (socket1 != null && socket2 != null && socket1.IsConnectionAlive && socket2.IsConnectionAlive)
                {
                    lock (m_generalLock)
                    {
                        m_socket1 = socket1;
                        m_socket2 = socket2;
                        m_socket1.CallBackObj = this;
                        m_socket2.CallBackObj = this;
                        Paired = true;
                        CallBackObj = callBackObj;
                        return true;
                    }
                    
                }
            }
            return false;
        }

        /// <summary>
        /// Detach pair
        /// </summary>
        public void DetachPair()
        {
            if (Paired)
            {
                lock (m_generalLock)
                {
                    if (m_socket1 != null)
                        m_socket1.CallBackObj = null;
                    if (m_socket2 != null)
                        m_socket2.CallBackObj = null;
                    Paired = false;
                    if (CallBackObj != null)
                    {
                        if (CallBackObj != null)
                            {
                                Task t = new Task(delegate()
                                {
                                    CallBackObj.OnDetached(this, m_socket1, m_socket2);
                                });
                                t.Start();
                            }
                 
                    }
                    m_socket1 = null;
                    m_socket2 = null;
                }
            }
        }

        /// <summary>
        /// NewConnection callback
        /// </summary>
        /// <param name="socket">client socket</param>
        public void OnNewConnection(IParallelSocket socket)
        {
            // Will never get called
        }

        /// <summary>
        /// Receive callback
        /// </summary>
        /// <param name="socket">client socket</param>
        /// <param name="receivedPacket">received packet</param>
        public void OnReceived(IParallelSocket socket, ParallelPacket receivedPacket)
        {
            lock (m_generalLock)
            {
                if (socket == m_socket1)
                {
                    m_socket2.Send(receivedPacket.GetPacketRaw(), receivedPacket.GetHeaderSize(), receivedPacket.GetDataByteSize());
                }
                else
                {
                    m_socket1.Send(receivedPacket.GetPacketRaw(), receivedPacket.GetHeaderSize(), receivedPacket.GetDataByteSize());
                }
            }
        }

        /// <summary>
        /// Send callback
        /// </summary>
        /// <param name="socket">client socket</param>
        /// <param name="status">stend status</param>
        /// <param name="sentPacket">sent packet</param>
        public void OnSent(IParallelSocket socket, SendStatus status, ParallelPacket sentPacket)
        {
        }

        /// <summary>
        /// Disconnect callback
        /// </summary>
        /// <param name="socket">client socket</param>
        public void OnDisconnect(IParallelSocket socket)
        {
            DetachPair();
        }
    }
}
