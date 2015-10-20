﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EpParallelSocket.cs
{
    public sealed class ParallelRoom: IParallelRoom
    {
                /// <summary>
        /// socket list
        /// </summary>
        private HashSet<IParallelSocket> m_socketList = new HashSet<IParallelSocket>();
        
        /// <summary>
        /// name of the room
        /// </summary>
        private string m_roomName;

        /// <summary>
        /// general lock
        /// </summary>
        private Object m_generalLock = new Object();

        /// <summary>
        /// list lock
        /// </summary>
        private Object m_listLock = new Object();


        public string RoomName
        {
            get
            {
                lock (m_generalLock)
                {
                    return m_roomName;
                }
            }
        }
        /// <summary>
        /// Default constructor
        /// </summary>
        /// <param name="roomName">name of the room</param>
        public ParallelRoom(string roomName)
        {
            m_roomName = roomName;
        }

        public void AddSocket(IParallelSocket socket)
        {
            lock (m_listLock)
            {
                m_socketList.Add(socket);
            }
        }

        /// <summary>
        /// Return the client socket list
        /// </summary>
        /// <returns>the client socket list</returns>
        public List<IParallelSocket> GetSocketList()
        {
            lock (m_listLock)
            {
                return new List<IParallelSocket>(m_socketList);
            }
        }

        /// <summary>
        /// Detach the given client from the server management
        /// </summary>
        /// <param name="clientSocket">the client to detach</param>
        /// <returns>the number of socket in the room</returns>
        public int DetachClient(IParallelSocket clientSocket)
        {
            lock (m_listLock)
            {
                m_socketList.Remove(clientSocket);
                return m_socketList.Count;
            }
        }

        /// <summary>
        /// Broadcast the given packet to all the client, connected
        /// </summary>
        /// <param name="data">data in byte array</param>
        /// <param name="offset">offset in bytes</param>
        /// <param name="dataSize">data size in bytes</param>
        public void Broadcast(byte[] data, int offset, int dataSize)
        {
            List<IParallelSocket> list = GetSocketList();
            foreach (IParallelSocket socket in list)
            {
                socket.Send(data,offset,dataSize);
            }
        }

        /// <summary>
        /// Broadcast the given packet to all the client, connected
        /// </summary>
        /// <param name="data">data in byte array</param>
        public void Broadcast(byte[] data)
        {
            Broadcast(data,0,data.Count());
        }
    }
}
