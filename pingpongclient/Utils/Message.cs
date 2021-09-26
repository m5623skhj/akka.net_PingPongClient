using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;

using Akka.Actor;
using Akka.IO;

using MySystem;

namespace MyMessage
{
    using PacketID = Message.PacketEnum;

    public class PacketGenerator : MyUtils.Singleton<PacketGenerator>
    {
        private Dictionary<uint, string> PacketFinder = new Dictionary<System.UInt32, string>();

        public PacketGenerator()
        {
            for (var idx = PacketID.Start; idx != PacketID.End; ++idx)
            {
                PacketFinder.Add((uint)idx, idx.ToString());
            }
        }

        public Message.MessageBase MakePacket(string ReceiveString)
        {
            if (ReceiveString.Length < sizeof(System.UInt32))
            {
                return null;
            }

            byte[] RecvArray = Encoding.UTF8.GetBytes(ReceiveString);
            byte[] PacketIDField = new byte[sizeof(System.UInt32)];

            for(int idx=0; idx<PacketIDField.Length; ++idx)
            {
                PacketIDField[idx] = RecvArray[idx];
            }

            System.UInt32 PacketID = BitConverter.ToUInt32(PacketIDField);
            string PacketName = FindPacketName(PacketID);

            if (PacketName == null)
            {
                return null;
            }

            Type type = Type.GetType("MyMessage.Message+" + PacketName);
            return ToStr(RecvArray, type) as Message.MessageBase;
        }

        private string FindPacketName(System.UInt32 PacketID)
        {
            if(PacketFinder.Count < PacketID)
            {
                return null;
            }

            return PacketFinder[PacketID];
        }

        public byte[] ClassToBytes(object obj)
        {
            int Size = Marshal.SizeOf(obj);

            byte[] arr = new byte[Size];

            IntPtr ptr = Marshal.AllocHGlobal(Size);
            Marshal.StructureToPtr(obj, ptr, false);
            // 가상 함수 테이블 포인터 때문에 이러는건지는 모르겠음
            Marshal.Copy(ptr, arr, 0, Size);
            Marshal.FreeHGlobal(ptr);

            return arr;
        }

        // https://jacking.tistory.com/112
        private object ToStr(byte[] byteData, Type type)
        {
            GCHandle gch = GCHandle.Alloc(byteData, GCHandleType.Pinned);
            object result = Marshal.PtrToStructure(gch.AddrOfPinnedObject(), type);
            gch.Free();
            return result;
        }
    }

    public class Message
    {
        internal enum PacketEnum : System.UInt32
        {
            Start = 0,
            Ping = 1,
            Pong,
            End
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public abstract class MessageBase
        {
            protected System.UInt32 PacketID;

            protected MessageBase()
            {
                Init();
            }

            protected abstract void Init();
            public abstract void PacketHandle(IActorRef Connection);
        }

        #region ping
        /// <summary>
        /// 핑 메시지 송신
        /// </summary>
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public class Ping : MessageBase
        {
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 64)]
            public string message;

            protected override void Init()
            {
                PacketID = (System.UInt32)PacketEnum.Ping;
            }

            public override void PacketHandle(IActorRef Connection)
            {
                Pong PongMessage = new Pong();
                PongMessage.message = message;

                Connection.Tell(Tcp.Write.Create(ByteString.FromBytes(PacketGenerator.GetInst.ClassToBytes(PongMessage))));
            }
        }
        #endregion

        #region pong
        /// <summary>
        /// 핑 메시지 수신시 퐁 메시지 송신 
        /// </summary>
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public class Pong : MessageBase
        {
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 64)]
            public string message;

            protected override void Init()
            {
                PacketID = (System.UInt32)PacketEnum.Pong;
            }

            public override void PacketHandle(IActorRef Connection)
            {
                System.Console.WriteLine(message);
            }
        }
        #endregion
    }
}
