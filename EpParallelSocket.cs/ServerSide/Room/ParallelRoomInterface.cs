/*! 
@file ParallelRoomInterface.cs
@author Woong Gyu La a.k.a Chris. <juhgiyo@gmail.com>
		<http://github.com/juhgiyo/epparallelsocket.cs>
@date October 13, 2015
@brief Parallel Room interface
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

A ParallelRoom Interface.

*/
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EpParallelSocket.cs
{
    public interface IParallelRoom
    {
        /// <summary>
        /// Room name property
        /// </summary>
        string RoomName
        {
            get;
        }

        /// <summary>
        /// Callback Object property
        /// </summary>
        IParallelRoomCallback CallBackObj
        {
            get;
            set;
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


        /// <summary>
        /// OnCreated event
        /// </summary>
        OnParallelRoomCreatedDelegate OnParallelRoomCreated
        {
            get;
            set;
        }
        /// <summary>
        /// OnJoin event
        /// </summary>
        OnParallelRoomJoinDelegate OnParallelRoomJoin
        {
            get;
            set;
        }
        /// <summary>
        /// OnLeave event
        /// </summary>
        OnParallelRoomLeaveDelegate OnParallelRoomLeave
        {
            get;
            set;
        }
        /// <summary>
        /// OnBroadcast event
        /// </summary>
        OnParallelRoomBroadcastDelegate OnParallelRoomBroadcast
        {
            get;
            set;
        }
        /// <summary>
        /// OnDestroy event
        /// </summary>
        OnParallelRoomDestroyDelegate OnParallelRoomDestroy
        {
            get;
            set;
        }
    }


    public delegate void OnParallelRoomCreatedDelegate(IParallelRoom room);
    public delegate void OnParallelRoomJoinDelegate(IParallelRoom room, IParallelSocket socket);
    public delegate void OnParallelRoomLeaveDelegate(IParallelRoom room, IParallelSocket socket);
    public delegate void OnParallelRoomBroadcastDelegate(IParallelRoom room, IParallelSocket sender, byte[] data, int offset, int dataSize);
    public delegate void OnParallelRoomDestroyDelegate(IParallelRoom room);

    public interface IParallelRoomCallback
    {
        /// <summary>
        /// Room created callback
        /// </summary>
        /// <param name="room">room</param>
        void OnParallelRoomCreated(IParallelRoom room);

        /// <summary>
        /// Join callback
        /// </summary>
        /// <param name="room">room</param>
        /// <param name="socket">socket</param>
        void OnParallelRoomJoin(IParallelRoom room, IParallelSocket socket);

        /// <summary>
        /// Leave callback
        /// </summary>
        /// <param name="room">room</param>
        /// <param name="socket">socket</param>
        void OnParallelRoomLeave(IParallelRoom room, IParallelSocket socket);

        /// <summary>
        /// Broadcast callback
        /// </summary>
        /// <param name="room">room</param>
        /// <param name="data">data</param>
        /// <param name="offset">offset of data to start</param>
        /// <param name="dataSize">datasize</param>
        void OnParallelRoomBroadcast(IParallelRoom room, IParallelSocket sender, byte[] data, int offset, int dataSize);


        /// <summary>
        /// Room destroyed callback
        /// </summary>
        /// <param name="room"></param>
        void OnParallelRoomDestroy(IParallelRoom room);



    }
}
