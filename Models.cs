using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace SharedContracts
{
    [DataContract]
    public class PlayerInfo
    {
        [DataMember] public string Username { get; set; }
        [DataMember] public DateTime LastSeenUtc { get; set; }
    }

    [DataContract]
    public class RoomInfo
    {
        [DataMember] public string RoomName { get; set; }
        [DataMember] public List<PlayerInfo> PlayerList { get; set; } = new List<PlayerInfo>();
    }

    [DataContract]
    public class ChatMessage
    {
        [DataMember] public string From { get; set; }
        [DataMember] public string To { get; set; } // null/empty for public in-room
        [DataMember] public string Room { get; set; }
        [DataMember] public string Text { get; set; }
        [DataMember] public DateTime TimestampUtc { get; set; }
        [DataMember] public bool IsFileLink { get; set; } = false;
        [DataMember] public string FileId { get; set; }    // if IsFileLink is true
        [DataMember] public string FileName { get; set; }  // if IsFileLink is true
    }

    [DataContract]
    public class FileMeta
    {
        [DataMember] public string FileId { get; set; }
        [DataMember] public string FileName { get; set; }
        [DataMember] public string Uploader { get; set; }
        [DataMember] public DateTime UploadedUtc { get; set; }
        [DataMember] public string Room { get; set; }
        [DataMember] public long FileSize { get; set; }
        [DataMember] public string ContentType { get; set; }
    }

    [DataContract]
    public class LobbyUpdateBundle
    {
        [DataMember] public List<ChatMessage> Messages { get; set; } = new List<ChatMessage>();
        [DataMember] public List<PlayerInfo> PlayerList { get; set; } = new List<PlayerInfo>();
        [DataMember] public List<FileMeta> Files { get; set; } = new List<FileMeta>();
        [DataMember] public DateTime ServerTimeUtc { get; set; }
        [DataMember] public List<RoomInfo> RoomList { get; set; } = new List<RoomInfo>();
    }
}
