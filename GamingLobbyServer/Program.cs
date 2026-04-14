using System;
using System.ServiceModel;
using SharedContracts;

namespace GamingLobbyServer
{
    class Program
    {
        static void Main()
        {
            // ----------------- Polling service -----------------
            var pollingBase = new Uri("net.tcp://localhost:9000/GamingLobbyService");
            var pollingHost = new ServiceHost(typeof(GamingLobbyServer.GamingLobbyService), pollingBase);

            var pollingBinding = new NetTcpBinding
            {
                Security = { Mode = SecurityMode.None },
                MaxReceivedMessageSize = int.MaxValue,
                MaxBufferSize = int.MaxValue,
                MaxBufferPoolSize = int.MaxValue,
                TransferMode = TransferMode.Streamed
            };

            pollingBinding.ReaderQuotas.MaxArrayLength = int.MaxValue;
            pollingBinding.ReaderQuotas.MaxStringContentLength = int.MaxValue;
            pollingBinding.ReaderQuotas.MaxBytesPerRead = int.MaxValue;
            pollingBinding.ReaderQuotas.MaxDepth = 32;
            pollingBinding.ReaderQuotas.MaxNameTableCharCount = int.MaxValue;


            pollingHost.AddServiceEndpoint(typeof(IGamingLobbyService), pollingBinding, "");
            pollingHost.Open();
            Console.WriteLine("Polling service running at " + pollingBase);

            var duplexBase = new Uri("net.tcp://localhost:9001/GamingLobbyServiceDuplex");
            var duplexHost = new ServiceHost(typeof(PlayerClientDuplex.GamingLobbyService), duplexBase);

            var duplexBinding = new NetTcpBinding
            {
                Security = { Mode = SecurityMode.None },
                MaxReceivedMessageSize = int.MaxValue,
                MaxBufferSize = int.MaxValue,
                MaxBufferPoolSize = int.MaxValue,
                TransferMode = TransferMode.Buffered, // Duplex requires buffered
                ReliableSession = { Enabled = true, InactivityTimeout = TimeSpan.FromMinutes(10) }
            };

            duplexBinding.ReaderQuotas.MaxArrayLength = int.MaxValue;
            duplexBinding.ReaderQuotas.MaxBytesPerRead = int.MaxValue;
            duplexBinding.ReaderQuotas.MaxStringContentLength = int.MaxValue;
            duplexBinding.ReaderQuotas.MaxDepth = 32;
            duplexBinding.ReaderQuotas.MaxNameTableCharCount = int.MaxValue;

            duplexHost.AddServiceEndpoint(typeof(IGamingLobbyDuplex), duplexBinding, "");
            duplexHost.Open();
            Console.WriteLine("Duplex service running at " + duplexBase);

            Console.WriteLine("Press Enter to exit...");
            Console.ReadLine();

            // Graceful shutdown
            try
            {
                pollingHost.Close();
                duplexHost.Close();
            }
            catch
            {
                pollingHost.Abort();
                duplexHost.Abort();
            }
        }
    }
}

