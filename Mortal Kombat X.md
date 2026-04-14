Assignment 1: Gaming Lobby System
=============================================

**Unit:** Distributed Computing

**Group Members:** Mohamed Leevan Ahmed, Binuk Perera, Zayan Saeed

1. Project Overview

-------------------

This project implements a multi-user distributed system consisting of a central server and two distinct client implementations. The system facilitates real-time communication, room management, and file sharing through two different network communication paradigms: **Polling (Pull-based)** and **Duplex (Push-based)**.

### Core Features

* **User Authentication:** Unique username registration and session management.

* **Lobby/Room Management:** Dynamic creation and joining of chat rooms.

* **Multi-Channel Messaging:** Support for public room chat and targeted private messaging.

* **Distributed File System:** Binary file upload/download via WCF streaming.

* **State Synchronization:** Full state snapshots for new clients joining active rooms.

* * *

2. System Architecture

----------------------

The solution is built using **WCF (Windows Communication Foundation)** and structured into five main components to ensure modularity and separation of concerns:

1. **SharedContracts:** A class library containing the service interfaces (`IGamingLobbyService`, `IGamingLobbyDuplex`) and data models (`ChatMessage`, `FileMeta`).

2. **GamingLobbyServer:** The central host managing thread-safe state using `ConcurrentDictionary` and hosting both service endpoints.

3. **WpfApp1 (Polling Client):** A WPF client using a `DispatcherTimer` to periodically pull updates from the server.

4. **PlayerClientDuplex:** A real-time client utilizing WCF Duplex callbacks for instantaneous server-to-client notifications.

5. **SharedFiles:** A dedicated directory for persistent storage of shared assets.

* * *

3. Technical Implementation Details

-----------------------------------

* **Concurrency:** The server uses `InstanceContextMode.Single` and `ConcurrencyMode.Multiple` to allow high-throughput, multi-threaded access to the lobby state.

* **Networking:** Communication is handled via `NetTcpBinding` for high performance.

* **Security:** As per assignment scope, security is set to `None`, focusing on the distributed logic.

* **Threading:** Clients utilize the `Dispatcher` to safely marshal background network events onto the UI thread, preventing deadlocks or UI freezing.

* **Large File Handling:** Configured with maximized `ReaderQuotas` and `Streamed` transfer modes to allow for binary file exchange.

* * *

4. Setup and Installation

-------------------------

### Prerequisites

* Visual Studio 2022

* .NET Framework 4.8 

### Running the Application

1. **Open the Solution:** Load the `.sln` file in Visual Studio.

2. **Start the Server:** Right-click the **GamingLobbyServer** project -> Debug -> Start New Instance.
   
   * _Ensure the server is running before starting clients._

3. **Start the Clients:** * Right-click **WpfApp1** or **PlayerClientDuplex** -> Debug -> Start New Instance.
   
   * Multiple instances of the clients were run to test the distributed interaction.

* * *

5. Known Limitations / Design Choices

-------------------------------------

* **Local Storage:** Files are currently stored in the server's local `BaseDirectory`.

* **Fault Tolerance:** The Duplex client includes `ReliableSession` configuration to handle minor network instability.

* **Polling Frequency:** The polling client is set to 1-second intervals to balance UI responsiveness with server load.

* * *

6. Reflection

---------------------------------------------------------

This project demonstrated the trade-offs between Polling and Duplex architectures. While Polling was easier to implement and more firewall-friendly, the Duplex architecture provided a significantly better user experience for real-time messaging by eliminating the latency inherent in pull-based systems.
