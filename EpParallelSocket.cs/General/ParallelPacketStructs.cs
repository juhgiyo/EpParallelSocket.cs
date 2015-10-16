using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace EpParallelSocket.cs
{
    public enum ParallelPacketType
    {
        IDENTITY_REQUEST,
        IDENTITY_RESPONSE,
        READY,
        DATA
    }


    [Serializable]
    public class IdentityResponse : ISerializable
    {
        public Guid m_guid;
        
        public IdentityResponse()
        {
        }
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
