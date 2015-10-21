/*! 
@file ParallelPacket.cs
@author Woong Gyu La a.k.a Chris. <juhgiyo@gmail.com>
		<http://github.com/juhgiyo/epparallelsocket.cs>
@date October 13, 2015
@brief Parallel Packet Interface
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

A ParallelPacket Class.

*/
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using EpServerEngine.cs;

namespace EpParallelSocket.cs
{
    /// <summary>
    /// Parallel Packet class
    /// </summary>
    public sealed class ParallelPacket :IComparable<ParallelPacket>, IEquatable<ParallelPacket>
    {
        /// <summary>
        /// packet
        /// </summary>
        private byte[] m_packet=null;

        /// <summary>
        /// packet Id
        /// </summary>
        private long m_packetId;
        /// <summary>
        /// packet type
        /// </summary>
        private ParallelPacketType m_packetType;
        /// <summary>
        /// parallel packet header size
        /// </summary>
        private const int HEADER_SIZE = 12;

        /// <summary>
        /// Default constructor
        /// </summary>
        /// <param name="packetId">packet Id</param>
        /// <param name="packetType">packet Type</param>
        /// <param name="data">data</param>
        public ParallelPacket(long packetId, ParallelPacketType packetType, byte[] data)
        {
            m_packetId = packetId;
            m_packetType = packetType;
            if(data==null)
                m_packet = new byte[sizeof(long) + sizeof(int)];
            else
                m_packet = new byte[sizeof(long) + sizeof(int) + data.Count()];
            MemoryStream mStream = new MemoryStream(m_packet);
            mStream.Write(BitConverter.GetBytes(m_packetId), 0, 8);
            mStream.Write(BitConverter.GetBytes((int)m_packetType), 0, 4);
            if(data!=null)  
                mStream.Write(data, 0, data.Count());

        }

        /// <summary>
        /// Default constructor
        /// </summary>
        /// <param name="packetId">packet Id</param>
        /// <param name="packetType">packet Type</param>
        /// <param name="data">data</param>
        /// <param name="offset">offset in byte</param>
        /// <param name="dataSize">data size in byte</param>
        public ParallelPacket(long packetId, ParallelPacketType packetType, byte[] data, int offset, int dataSize)
        {
            m_packetId = packetId;
            m_packetType = packetType;

            m_packet = new byte[sizeof(long) + sizeof(int) + dataSize];
            MemoryStream mStream = new MemoryStream(m_packet);
            mStream.Write(BitConverter.GetBytes(m_packetId), 0, 8);
            mStream.Write(BitConverter.GetBytes((int)m_packetType), 0, 4);
            if(data!=null)
                mStream.Write(data, offset, dataSize);

        }

        /// <summary>
        /// Default constructor
        /// </summary>
        /// <param name="packet">received Packet</param>
        public ParallelPacket(Packet packet)
        {
            m_packetId = BitConverter.ToInt64(packet.PacketRaw,0);
            m_packetType = (ParallelPacketType)BitConverter.ToInt32(packet.PacketRaw, 8);
            m_packet = packet.PacketRaw;
        }

        public static ParallelPacket FromPacket(Packet packet)
        {
            return new ParallelPacket(packet);
        }

        /// <summary>
        /// Default copy constructor
        /// </summary>
        /// <param name="b">the object to copy from</param>
        public ParallelPacket(ParallelPacket b)
        {
            m_packet = b.m_packet;
            m_packetId = b.m_packetId;
            m_packetType = b.m_packetType;
        }

        public bool Equals(ParallelPacket other)
        {
            return (m_packetId == other.m_packetId);
        }

        public int CompareTo(ParallelPacket obj)
        {
            if (m_packetId < obj.m_packetId)
                return -1;
            else if (m_packetId == obj.m_packetId)
                return 0;
            return 1;
        }

        /// <summary>
        /// Return the packet id
        /// </summary>
        /// <returns>the packet id</returns>
        public long PacketID
        {
            get
            {
                return m_packetId;
            }
        }
        /// <summary>
        /// Return the packet type
        /// </summary>
        /// <returns>the packet type</returns>
        public ParallelPacketType PacketType
        {
            get
            {
                return m_packetType;
            }
        }

        /// <summary>
        /// Return the packet header size
        /// </summary>
        /// <returns>the packet header size</returns>
        public int HeaderSize
        {
            get
            {
                return HEADER_SIZE;
            }
        }
        /// <summary>
        /// Return the size of packet in byte
        /// </summary>
        /// <returns>the size of packet in byte</returns>
        public int PacketByteSize
        {
            get
            {
                return m_packet.Count();
            }
        }

        /// <summary>
        /// Return the size of data in byte
        /// </summary>
        /// <returns>the size of data in byte</returns>
        public int DataByteSize
        {
            get
            {
                return m_packet.Count() - HEADER_SIZE;
            }
        }

        /// <summary>
        /// Return the actual packet
        /// </summary>
        /// <returns>the actual packet</returns>
        public byte[] PacketRaw
        {
            get
            {
                return m_packet;
            }
        }

        /// <summary>
        /// Return the actual packet
        /// </summary>
        /// <returns>the actual packet</returns>
        public IEnumerable<byte> Data
        {
            get
            {
                return m_packet.Skip(HEADER_SIZE);
            }
        }
        /// <summary>
        /// Return the cloned data from the packet
        /// </summary>
        /// <returns>the cloned data</returns>
        public byte[] CloneData()
        {
            return m_packet.Skip(HEADER_SIZE).ToArray();
        }


    }
}
