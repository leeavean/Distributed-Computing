using SharedContracts;
using System;
using System.ServiceModel;
using System.Collections.Generic;

namespace PlayerClientDuplex
{
    public class ServiceClient
    {
        private static ServiceClient _instance;

        public IGamingLobbyDuplex Proxy { get; private set; }

        public static ServiceClient Instance
        {
            get
            {
                if (_instance == null)
                    throw new InvalidOperationException("ServiceClient not initialized. Call Initialize first.");
                return _instance;
            }
        }

        // Must call this first, once
        public static void Initialize(IGamingLobbyCallback callbackHandler)
        {
            if (_instance == null)
                _instance = new ServiceClient(callbackHandler);
        }

        // Correct Logout: call Proxy's Unregister directly
        public void Logout(string username)
        {
            try
            {
                Proxy?.Unregister(username);
            }
            catch
            {
                // Ignore any network exceptions
            }
        }

        private ServiceClient(IGamingLobbyCallback callbackHandler)
        {
            // Setup WCF duplex binding
            var binding = new NetTcpBinding
            {
                Security = { Mode = SecurityMode.None },
                MaxReceivedMessageSize = int.MaxValue,
                MaxBufferSize = int.MaxValue,
                MaxBufferPoolSize = int.MaxValue,
                TransferMode = TransferMode.Buffered,
                ReliableSession = { Enabled = true, InactivityTimeout = TimeSpan.FromMinutes(10) }
            };

            binding.ReaderQuotas.MaxArrayLength = int.MaxValue;
            binding.ReaderQuotas.MaxBytesPerRead = int.MaxValue;
            binding.ReaderQuotas.MaxStringContentLength = int.MaxValue;
            binding.ReaderQuotas.MaxDepth = 32;
            binding.ReaderQuotas.MaxNameTableCharCount = int.MaxValue;

            var ctx = new InstanceContext(callbackHandler);
            var factory = new DuplexChannelFactory<IGamingLobbyDuplex>(
                ctx,
                binding,
                new EndpointAddress("net.tcp://localhost:9001/GamingLobbyServiceDuplex")
            );

            Proxy = factory.CreateChannel();
        }

        // Helper to list rooms via Proxy
        public List<RoomInfo> ListRooms()
        {
            try
            {
                return Proxy.ListRooms();
            }
            catch
            {
                return new List<RoomInfo>();
            }
        }
    }
}