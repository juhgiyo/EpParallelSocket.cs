using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EpParallelSocket.cs
{
    public interface IParallelRoom
    {
        string RoomName
        {
            get;
        }

        /// <summary>
        /// Return the client socket list
        /// </summary>
        /// <returns>the client socket list</returns>
        List<IParallelSocket> GetSocketList();


        /// <summary>
        /// Broadcast the given packet to all the client, connected
        /// </summary>
        /// <param name="data">data in byte array</param>
        /// <param name="offset">offset in bytes</param>
        /// <param name="dataSize">data size in bytes</param>
        void Broadcast(byte[] data, int offset, int dataSize);


        /// <summary>
        /// Broadcast the given packet to all the client, connected
        /// </summary>
        /// <param name="data">data in byte array</param>
        void Broadcast(byte[] data);

    }
}
