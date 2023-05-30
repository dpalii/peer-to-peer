# Peer-to-peer

In this example, the program creates a P2P network where each peer can both send and receive messages. The StartPeer method runs on a separate task and listens for incoming connections using a TcpListener. When a connection is established, the HandleClientCommunication method is invoked on a new task to handle the communication with the connected peer.

The ProcessIncomingMessages method reads incoming messages from the connected peer, processes them, and sends a response back to the sender.

The ProcessIncomingMessages method runs on the main thread and prompts the user to enter messages. It establishes a connection to the localhost using a `TcpClient