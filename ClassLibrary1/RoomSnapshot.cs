using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace SharedContracts
{
    [DataContract]
    public class RoomSnapshot
    {
        [DataMember] public List<string> Users { get; set; } = new List<string>();
        [DataMember] public List<ChatMessage> Messages { get; set; } = new List<ChatMessage> ();
        [DataMember] public List<FileMeta> Files { get; set; } = new List<FileMeta> ();
    }
}

