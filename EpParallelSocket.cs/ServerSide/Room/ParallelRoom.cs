/*! 
@file ParallelRoom.cs
@author Woong Gyu La a.k.a Chris. <juhgiyo@gmail.com>
		<http://github.com/juhgiyo/epparallelsocket.cs>
@date October 13, 2015
@brief Parallel Room class
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

A ParallelRoom Class.

*/
using System;
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
        /// callback object
        /// </summary>
        private IParallelRoomCallback m_callBackObj;

        /// <summary>
        /// general lock
        /// </summary>
        private Object m_generalLock = new Object();

        /// <summary>
        /// list lock
        /// </summary>
        private Object m_listLock = new Object();

        /// <summary>
        /// Room name property
        /// </summary>
        public string RoomName
        {
            get
            {
                lock (m_generalLock)
                {
                    return m_roomName;
                }
            }
            private set
            {
                lock (m_generalLock)
                {
                    m_roomName = value;
                }
            }

        }


        /// <summary>
        /// OnCreated event
        /// </summary>
        OnParallelRoomCreatedDelegate m_onCreated = delegate { };
        /// <summary>
        /// OnJoin event
        /// </summary>
        OnParallelRoomJoinDelegate m_onJoin = delegate { };
        /// <summary>
        /// OnLeave event
        /// </summary>
        OnParallelRoomLeaveDelegate m_onLeave = delegate { };
        /// <summary>
        /// OnBroadcast event
        /// </summary>
        OnParallelRoomBroadcastDelegate m_onBroadcast = delegate { };
        /// <summary>
        /// OnDestroy event
        /// </summary>
        OnParallelRoomDestroyDelegate m_onDestroy = delegate { };

        /// <summary>
        /// OnCreated event
        /// </summary>
        public OnParallelRoomCreatedDelegate OnParallelRoomCreated
        {
            get
            {
                return m_onCreated;
            }
            set
            {
                if (value == null)
                {
                    m_onCreated = delegate { };
                    if (CallBackObj != null)
                        m_onCreated += CallBackObj.OnParallelRoomCreated;
                }
                else
                {
                    m_onCreated = CallBackObj != null && CallBackObj.OnParallelRoomCreated != value ? CallBackObj.OnParallelRoomCreated + (value - CallBackObj.OnParallelRoomCreated) : value;
                }
            }
        }
        /// <summary>
        /// OnJoin event
        /// </summary>
        public OnParallelRoomJoinDelegate OnParallelRoomJoin
        {
            get
            {
                return m_onJoin;
            }
            set
            {
                if (value == null)
                {
                    m_onJoin = delegate { };
                    if (CallBackObj != null)
                        m_onJoin += CallBackObj.OnParallelRoomJoin;
                }
                else
                {
                    m_onJoin = CallBackObj != null && CallBackObj.OnParallelRoomJoin != value ? CallBackObj.OnParallelRoomJoin + (value - CallBackObj.OnParallelRoomJoin) : value;
                }
            }
        }
        /// <summary>
        /// OnLeave event
        /// </summary>
        public OnParallelRoomLeaveDelegate OnParallelRoomLeave
        {
            get
            {
                return m_onLeave;
            }
            set
            {
                if (value == null)
                {
                    m_onLeave = delegate { };
                    if (CallBackObj != null)
                        m_onLeave += CallBackObj.OnParallelRoomLeave;
                }
                else
                {
                    m_onLeave = CallBackObj != null && CallBackObj.OnParallelRoomLeave != value ? CallBackObj.OnParallelRoomLeave + (value - CallBackObj.OnParallelRoomLeave) : value;
                }
            }
        }
        /// <summary>
        /// OnBroadcast event
        /// </summary>
        public OnParallelRoomBroadcastDelegate OnParallelRoomBroadcast
        {
            get
            {
                return m_onBroadcast;
            }
            set
            {
                if (value == null)
                {
                    m_onBroadcast = delegate { };
                    if (CallBackObj != null)
                        m_onBroadcast += CallBackObj.OnParallelRoomBroadcast;
                }
                else
                {
                    m_onBroadcast = CallBackObj != null && CallBackObj.OnParallelRoomBroadcast != value ? CallBackObj.OnParallelRoomBroadcast + (value - CallBackObj.OnParallelRoomBroadcast) : value;
                }
            }
        }
        /// <summary>
        /// OnDestroy event
        /// </summary>
        public OnParallelRoomDestroyDelegate OnParallelRoomDestroy
        {
            get
            {
                return m_onDestroy;
            }
            set
            {
                if (value == null)
                {
                    m_onDestroy = delegate { };
                    if (CallBackObj != null)
                        m_onDestroy += CallBackObj.OnParallelRoomDestroy;
                }
                else
                {
                    m_onDestroy = CallBackObj != null && CallBackObj.OnParallelRoomDestroy != value ? CallBackObj.OnParallelRoomDestroy + (value - CallBackObj.OnParallelRoomDestroy) : value;
                }
            }
        }


        /// <summary>
        /// Callback Object property
        /// </summary>
        public IParallelRoomCallback CallBackObj
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
                    if (m_callBackObj != null)
                    {
                        m_onCreated -= m_callBackObj.OnParallelRoomCreated;
                        m_onJoin -= m_callBackObj.OnParallelRoomJoin;
                        m_onLeave -= m_callBackObj.OnParallelRoomLeave;
                        m_onBroadcast -= m_callBackObj.OnParallelRoomBroadcast;
                        m_onDestroy -= m_callBackObj.OnParallelRoomDestroy;
                    }
                    m_callBackObj = value;
                    if (m_callBackObj != null)
                    {
                        m_onCreated += m_callBackObj.OnParallelRoomCreated;
                        m_onJoin += m_callBackObj.OnParallelRoomJoin;
                        m_onLeave += m_callBackObj.OnParallelRoomLeave;
                        m_onBroadcast += m_callBackObj.OnParallelRoomBroadcast;
                        m_onDestroy += m_callBackObj.OnParallelRoomDestroy;
                    }
                }
            }
        }

        /// <summary>
        /// Default constructor
        /// </summary>
        /// <param name="roomName">name of the room</param>
        public ParallelRoom(string roomName,IParallelRoomCallback callbackObj=null)
        {
            RoomName = roomName;
            CallBackObj = callbackObj;
            Task t = new Task(delegate()
            {
                OnParallelRoomCreated(this);
            });
            t.Start();

        }

        public void AddSocket(IParallelSocket socket)
        {
            lock (m_listLock)
            {
                m_socketList.Add(socket);
            }
            
            Task t = new Task(delegate()
            {
                OnParallelRoomJoin(this, socket);
            });
            t.Start();
            
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
        /// <param name="socket">the client to detach</param>
        /// <returns>the number of socket in the room</returns>
        public int DetachClient(IParallelSocket socket)
        {
            lock (m_listLock)
            {
                m_socketList.Remove(socket);
                
                Task t = new Task(delegate()
                {
                    OnParallelRoomLeave(this, socket);
                });
                t.Start();
                
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
            
            Task t = new Task(delegate()
            {
                OnParallelRoomBroadcast(this, data, offset, dataSize);
            });
            t.Start();
            
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
