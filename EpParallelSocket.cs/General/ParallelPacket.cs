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
        private byte[] m_packet;

        private long m_packetId;
        private ParallelPacketType m_packetType;
        private const int HEADER_SIZE = 12;

        /// <summary>
        /// Default constructor
        /// </summary>
        /// <param name="sequence">sequence of packet Id</param>
        /// <param name="packetType">packet Type</param>
        /// <param name="packet">packet</param>
        /// <param name="offset">offset in byte</param>
        /// <param name="byteSize">packet size in byte</param>
        public ParallelPacket(Packet packet)
        {
            m_packetId = BitConverter.ToInt64(packet.GetPacket(),0);
            m_packetType = (ParallelPacketType)BitConverter.ToInt32(packet.GetPacket(), 8);
            m_packet = packet.GetPacket();                        
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
