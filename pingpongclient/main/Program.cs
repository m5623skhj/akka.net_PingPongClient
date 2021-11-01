using System;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;

using Akka.Actor;
using Akka.IO;

using MyMessage;

/// <summary>
/// 참고 자료
/// https://getakka.net/articles/networking/io.html
/// https://blog.rajephon.dev/2018/12/08/akka-02/
/// </summary>

namespace pingpongclient
{
    public class PingPongClient : UntypedActor
    {
        public IActorRef Connection;
        public HashSet<string> ChannelSet;

        public PingPongClient(string host, int port)
        {
            var EndPoint = new DnsEndPoint(host, port);
            Context.System.Tcp().Tell(new Tcp.Connect(EndPoint));
        }

        public void SendPacekt(Message.MessageBase packet)
        {
            Connection.Tell(Tcp.Write.Create(ByteString.FromBytes(PacketGenerator.GetInst.ClassToBytes(packet))));
        }

        protected override void OnReceive(object message)
        {
            if (message is Tcp.Connected)
            {
                var connected = message as Tcp.Connected;
                Console.WriteLine("Connected to {0}", connected.RemoteAddress);

                Connection = Sender;

                Sender.Tell(new Tcp.Register(Self));
                ReadConsole();

                // Enter Channel Test
                //*
                EnterChattingChannel("TestChannel1");
                EnterChattingChannel("TestChannel2");
                //*/

                Become(Connected());
            }
            else if(message is Tcp.CommandFailed)
            {
                Console.WriteLine("Connection failed");
            }
            else
            {
                Unhandled(message);
            }
        }

        private void EnterChattingChannel(string channel)
        {
            MyMessage.User2Chatting.EnterChatRoomReq Packet = new MyMessage.User2Chatting.EnterChatRoomReq();
            Packet.ChannelName = channel;

            SendPacekt(Packet);
        }

        protected override void Unhandled(object message)
        {
            // logging?
            Console.WriteLine("UnHandled message received {0}", message.ToString());
        }

        private UntypedReceive Connected()
        {
            return message =>
            {
                string SendMessage = message as string;
                if (SendMessage != null)
                {
                    var SendChatting = new MyMessage.User2Chatting.SendChatting();
                    { SendChatting.ChannelName = "TestChannel2"; SendChatting.ChatMessage = SendMessage; };

                    Connection.Tell(Tcp.Write.Create(ByteString.FromBytes(PacketGenerator.GetInst.ClassToBytes(SendChatting))));

                    ReadConsole();
                }
                else if (message is Tcp.Received)
                {
                    var ReceivedMessage = message as Tcp.Received;
                    if (ReceivedMessage != null)
                    {
                        var RecvPacket = PacketGenerator.GetInst.MakePacket(ReceivedMessage.Data.ToString());
                        PacketHandle(RecvPacket);
                    }
                }
                else if (message is Tcp.PeerClosed)
                {
                    Console.WriteLine("Connection closed");
                }
                else
                {
                    Unhandled(message);
                }
            };
        }

        private void ReadConsole()
        {
            Task.Factory.StartNew(self => Console.In.ReadLineAsync().PipeTo((ICanTell)self), Self);
        }

        private void PacketHandle(Message.MessageBase RecvPacket)
        {
            if (RecvPacket != null)
            {
                RecvPacket.PacketHandle(this);
            }
        }
    }

    class Program
    {
        static void Main(string[] args)
        {
            ActorSystem MyActorSystem = ActorSystem.Create("MyActorSystem");
            IActorRef TCPManager = MyActorSystem.Tcp();

            IActorRef Client = MyActorSystem.ActorOf(Props.Create(() =>
                new PingPongClient("127.0.0.1", 63325)), "ClientActor");

            MyActorSystem.WhenTerminated.Wait();
        }
    }
}
