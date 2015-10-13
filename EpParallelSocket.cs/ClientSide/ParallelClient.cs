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
using EpServerEngine.cs;

namespace EpParallelSocket.cs
{
    public sealed class ParallelClient : IParallelClient
    {
        /// <summary>
        /// client options
        /// </summary>
        private ClientOps m_clientOps = null;

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
        /// number of sockets using
        /// </summary>
        private int m_socketCount;

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


        private Queue<Packet> m_packetQueue;
        private HashSet<Packet> m_pendingPacketQueue;
        private HashSet<Packet> m_errorPacketQueue;

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
        /// Connect to server with given option
        /// </summary>
        /// <param name="ops">option for client</param>
        public void Connect(ParallelClientOps ops)
        {

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
            byte[] packet = new byte[dataSize+sizeof(long)];
            MemoryStream mStream = new MemoryStream(packet);
            mStream.Write(BitConverter.GetBytes(getCurPacketSequence()), 0, 8);
            mStream.Write(data,offset,dataSize);
            Packet sendPacket = new Packet(packet, packet.Count(), false);
            lock(m_generalLock){
                m_packetQueue.Enqueue(sendPacket);
            }
        }
    }
}
