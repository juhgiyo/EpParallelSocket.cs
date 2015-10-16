/*! 
@file ParallelPacketStruct.cs
@author Woong Gyu La a.k.a Chris. <juhgiyo@gmail.com>
		<http://github.com/juhgiyo/epparallelclient.cs>
@date October 13, 2015
@brief Parallel Packet Struct Interface
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

A ParallelPacketStruct Class.

*/
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace EpParallelSocket.cs
{
    /// <summary>
    /// Parallel packet type
    /// </summary>
    public enum ParallelPacketType
    {
        /// <summary>
        /// Identity request type
        /// </summary>
        IDENTITY_REQUEST,
        /// <summary>
        /// Identity response type
        /// </summary>
        IDENTITY_RESPONSE,
        /// <summary>
        /// Communication ready type
        /// </summary>
        READY,
        /// <summary>
        /// Data type
        /// </summary>
        DATA
    }

    /// <summary>
    /// Identity Response Packet class
    /// </summary>
    [Serializable]
    public class IdentityResponse : ISerializable
    {
        /// <summary>
        /// global unique id
        /// </summary>
        public Guid m_guid;
        
        /// <summary>
        /// Default constructor
        /// </summary>
        public IdentityResponse()
        {
        }
        /// <summary>
        /// Default constructor
        /// </summary>
        /// <param name="guid">guid</param>
        public IdentityResponse(Guid guid)
        {
            m_guid=guid;
        }
        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            // Use the AddValue method to specify serialized values.
            info.AddValue("guid", m_guid, typeof(Guid));
        }

        // The special constructor is used to deserialize values. 
        public IdentityResponse(SerializationInfo info, StreamingContext context)
        {
            // Reset the property value using the GetValue method.
            m_guid = (Guid)info.GetValue("guid", typeof(Guid));
        }
    }
}
