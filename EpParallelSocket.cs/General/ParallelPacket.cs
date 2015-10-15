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

    public class ParallelPacket : IComparable<ParallelPacket>, IEquatable<ParallelPacket>
    {
        private byte[] m_packet=null;

        private long m_packetId;
        private ParallelPacketType m_packetType;
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

            m_packet = new byte[sizeof(long) + sizeof(int) + data.Count()];
            MemoryStream mStream = new MemoryStream(m_packet);
            mStream.Write(BitConverter.GetBytes(m_packetId), 0, 8);
            mStream.Write(BitConverter.GetBytes((int)m_packetType), 0, 4);
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

        public long GetPacketID()
        {
            return m_packetId;
        }
        /// <summary>
        /// Return the size of packet in byte
        /// </summary>
        /// <returns>the size of packet in byte</returns>
        public int GetPacketByteSize()
        {
            return m_packet.Count();
        }

        /// <summary>
        /// Return the size of data in byte
        /// </summary>
        /// <returns>the size of data in byte</returns>
        public int GetDataByteSize()
        {
            return m_packet.Count() - HEADER_SIZE;
        }

        /// <summary>
        /// Return the actual packet
        /// </summary>
        /// <returns>the actual packet</returns>
        public byte[] GetPacketRaw()
        {
            return m_packet;
        }

        /// <summary>
        /// Return the actual packet
        /// </summary>
        /// <returns>the actual packet</returns>
        public IEnumerable<byte> GetData()
        {
            return m_packet.Skip(HEADER_SIZE);
        }


    }
}
