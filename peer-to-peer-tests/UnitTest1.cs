using System;
using System.IO;
using System.Net.Http;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace peer_to_peer_tests
{
    [TestClass]
    public class PeerTests
    {
        [TestMethod]
        public async Task ProcessIncomingMessages_AcceptsConnectionAndProcessesMessages()
        {
            // Arrange
            var listenerMock = new Mock<ITcpListener>();
            var clientMock = new Mock<ITcpClient>();
            var consoleMock = new Mock<IConsole>();

            listenerMock.Setup(l => l.AcceptTcpClientAsync()).ReturnsAsync(clientMock.Object);
            clientMock.Setup(c => c.GetAddress()).Returns("127.0.0.1");

            var peer = new Peer(clientMock.Object, listenerMock.Object, consoleMock.Object);

            // Act
            await peer.ProcessIncomingMessages();

            // Assert
            listenerMock.Verify(l => l.AcceptTcpClientAsync(), Times.Once());
            clientMock.Verify(c => c.GetAddress(), Times.Once());
            consoleMock.Verify(c => c.WriteLine("Received connection from 127.0.0.1"), Times.Once());
        }

        [TestMethod]
        public async Task ProcessOutgoingMessages_SendsMessagesAndReceivesResponses()
        {
            // Arrange
            var tcpClientMock = new Mock<ITcpClient>();
            var tcpListenerMock = new Mock<ITcpListener>();
            var readStream = new MemoryStream();
            var writeStream = new MemoryStream();
            var writer = new StreamWriter(readStream);
            writer.WriteLine("Response 1");
            writer.Flush();
            readStream.Position = 0;

            var consoleMock = new Mock<IConsole>();
            consoleMock.SetupSequence(c => c.ReadLine()).Returns("Hello").Returns("");
            consoleMock.Setup(c => c.WriteLine("Received response: Response 1")).Verifiable();

            tcpClientMock.SetupSequence(c => c.GetStream()).Returns(writeStream).Returns(readStream);
            tcpClientMock.Setup(c => c.Close());

            var peer = new Peer(tcpClientMock.Object, tcpListenerMock.Object, consoleMock.Object);

            // Act
            await peer.ProcessOutgoingMessages();

            // Assert
            consoleMock.Verify();
            tcpClientMock.Verify(c => c.GetStream(), Times.Exactly(2));
        }
    }
}