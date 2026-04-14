using SharedContracts;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;

namespace PlayerClientDuplex
{
    [ServiceBehavior(InstanceContextMode = InstanceContextMode.Single, ConcurrencyMode = ConcurrencyMode.Multiple)]
    public class GamingLobbyService : IGamingLobbyDuplex
    {
        // Active clients (username -> callback)
        private readonly ConcurrentDictionary<string, IGamingLobbyCallback> _clients
            = new ConcurrentDictionary<string, IGamingLobbyCallback>();

        // Room memberships (room -> set of usernames)
        private readonly ConcurrentDictionary<string, HashSet<string>> _rooms
            = new ConcurrentDictionary<string, HashSet<string>>();

        private readonly object _roomLock = new object();

        // File storage (fileId -> metadata + raw bytes)
        private readonly ConcurrentDictionary<string, (FileMeta meta, byte[] data)> _files
            = new ConcurrentDictionary<string, (FileMeta, byte[])>();

        // -------- User Management --------
        public bool Register(string username)
        {
            // Capture the duplex callback channel for this user
            var callback = OperationContext.Current.GetCallbackChannel<IGamingLobbyCallback>();

            // Attempt to add user to the active clients dictionary
            // If already exists, update the callback reference instead of failing
            _clients.AddOrUpdate(username, callback, (_, __) => callback);

            return true; // Always true now; allows multiple logins without freezing
        }

        public void Unregister(string username)
        {
            _clients.TryRemove(username, out _);

            // Remove user from all rooms
            foreach (var room in _rooms.Values)
            {
                lock (room)
                {
                    room.Remove(username);
                }
            }
        }

        // -------- Room Management --------
        public bool JoinRoomDuplex(string username, string roomName)
        {
            // Ensure the duplex room exists
            var room = _rooms.GetOrAdd(roomName, _ =>
            {
                // Initialize with pre-existing users if any
                var preExistingUsers = new HashSet<string>();
                if (ServerState.Rooms.TryGetValue(roomName, out var serverRoom))
                {
                    preExistingUsers = new HashSet<string>(serverRoom.PlayerList.Keys);
                }
                return preExistingUsers;
            });

            // Add the joining user
            lock (room)
            {
                room.Add(username);
            }

            BroadcastUserList(roomName);
            return true;
        }

        public void LeaveRoomDuplex(string username, string roomName)
        {
            if (_rooms.TryGetValue(roomName, out var room))
            {
                lock (room)
                {
                    room.Remove(username);
                }
                BroadcastUserList(roomName);
            }
        }

        public RoomSnapshot GetRoomSnapshot(string roomName)
        {
            var snapshot = new RoomSnapshot
            {
                Users = _rooms.TryGetValue(roomName, out var users) ? users.ToList() : new List<string>(),
                Messages = ServerState.Rooms.TryGetValue(roomName, out var room) ? room.MessageHistory.ToList() : new List<ChatMessage>(),
                Files = ServerState.Rooms.TryGetValue(roomName, out var room2) ? room2.Files.ToList() : new List<FileMeta>()
            };
            return snapshot;
        }

        public bool CreateRoomDuplex(string roomName)
        {
            // Add to duplex management dictionary
            var added = _rooms.TryAdd(roomName, new HashSet<string>());

            if (added)
            {
                // Also create a ServerState.Room so ListRooms() sees it
                ServerState.Rooms.TryAdd(roomName, new Room { RoomName = roomName });
            }

            return added;
        }

        public List<RoomInfo> ListRooms()
        {
            // Return all rooms visible in ServerState
            return ServerState.Rooms.Values
                .Select(r => new RoomInfo { RoomName = r.RoomName })
                .OrderBy(r => r.RoomName)
                .ToList();
        }

        // -------- Messaging --------
        public void SendMessageDuplex(ChatMessage msg)
        {
            if (!string.IsNullOrEmpty(msg.To))
            {
                // Private message: send only to recipient and sender
                foreach (var user in new[] { msg.From, msg.To })
                {
                    if (_clients.TryGetValue(user, out var cb))
                    {
                        try { cb.OnNewMessage(msg); } catch { }
                    }
                }
                return;
            }

            // Public message: broadcast to all users in room
            if (string.IsNullOrEmpty(msg.Room)) return;
            if (_rooms.TryGetValue(msg.Room, out var users))
            {
                foreach (var user in users)
                {
                    if (_clients.TryGetValue(user, out var cb))
                    {
                        try { cb.OnNewMessage(msg); }
                        catch { }
                    }
                }
            }

            // Save message to room history
            if (!string.IsNullOrEmpty(msg.Room) && ServerState.Rooms.TryGetValue(msg.Room, out var room))
            {
                lock (room.MessageHistory)
                {
                    room.MessageHistory.Add(msg);
                }
            }
        }

        // -------- File Sharing --------
        public string UploadFile(FileMeta meta, byte[] fileData)
        {
            meta.FileId = Guid.NewGuid().ToString("N");
            meta.UploadedUtc = DateTime.UtcNow;

            _files[meta.FileId] = (meta, fileData);

            // Add to ServerState for history
            if (ServerState.Rooms.TryGetValue(meta.Room, out var room))
            {
                room.Files.Add(meta);

                var msg = new ChatMessage
                {
                    From = meta.Uploader,
                    Room = meta.Room,
                    TimestampUtc = DateTime.UtcNow,
                    IsFileLink = true,
                    FileId = meta.FileId,
                    FileName = meta.FileName,
                    Text = $"{meta.Uploader} shared {meta.FileName}"
                };
                room.MessageHistory.Add(msg);
            }

            // Notify all users in the room
            if (_rooms.TryGetValue(meta.Room, out var users))
            {
                var list = users.ToList();
                foreach (var user in list)
                {
                    if (_clients.TryGetValue(user, out var cb))
                    {
                        try { cb.OnFileShared(meta); } catch { }
                    }
                }
            }

            return meta.FileId;
        }

        public byte[] DownloadFile(string fileId)
        {
            return _files.TryGetValue(fileId, out var entry) ? entry.data : null;
        }

        public List<FileMeta> ListFiles(string roomName)
        {
            return _files.Values
                         .Where(f => f.meta.Room == roomName)
                         .Select(f => f.meta)
                         .ToList();
        }

        // -------- Helper: Broadcast user list --------
        private void BroadcastUserList(string roomName)
        {
            if (!_rooms.TryGetValue(roomName, out var users)) return;
            var list = users.ToList();

            foreach (var user in list)
            {
                if (_clients.TryGetValue(user, out var cb))
                {
                    try { cb.OnUserListChanged(roomName, list); } catch { }
                }
            }
        }
    }
}