using System;
using System.Net;
using System.Text;
using System.Threading.Tasks;

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
        private IActorRef Connection;

        public PingPongClient(string host, int port)
        {
            var EndPoint = new DnsEndPoint(host, port);
            Context.System.Tcp().Tell(new Tcp.Connect(EndPoint));
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
                    var PingMessage = new Message.Ping
                    { Message = SendMessage };

                    Connection.Tell(Tcp.Write.Create(ByteString.FromBytes(PacketGenerator.GetInst.ClassToBytes(PingMessage))));

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
            RecvPacket.PacketHandle(Connection);
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
