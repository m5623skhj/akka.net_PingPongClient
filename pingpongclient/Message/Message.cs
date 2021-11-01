using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;

using Akka.Actor;
using Akka.IO;

using pingpongclient;

namespace MyMessage
{
    using PacketID = Message.PacketEnum;

    public class PacketGenerator : MyUtils.Singleton<PacketGenerator>
    {
        private HashSet<Message.PacketEnum> PacketFinder = new HashSet<Message.PacketEnum>();

        public PacketGenerator()
        {
            PacketFinder.Clear();

            for (var idx = PacketID.Start; idx != PacketID.End; ++idx)
            {
                PacketFinder.Add(idx);
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

            for (int idx = 0; idx < PacketIDField.Length; ++idx)
            {
                PacketIDField[idx] = RecvArray[idx];
            }

            System.UInt32 PacketID = BitConverter.ToUInt32(PacketIDField);
            string PacketName = FindPacketName((Message.PacketEnum)PacketID);

            if (PacketName == null)
            {
                return null;
            }

            Type type = Type.GetType("MyMessage.Message+" + PacketName);
            if (type != null)
            {
                return ToStr(RecvArray, type) as Message.MessageBase;
            }

            type = Type.GetType("MyMessage.Chatting2User." + PacketName);
            if (type != null)
            {
                return ToStr(RecvArray, type) as Message.MessageBase;
            }

            return null;
        }

        private string FindPacketName(Message.PacketEnum PacketID)
        {
            if (PacketFinder.Contains(PacketID) == false)
            {
                return null;
            }

            return PacketID.ToString();
        }

        public byte[] ClassToBytes(object obj)
        {
            int Size = Marshal.SizeOf(obj);

            byte[] arr = new byte[Size];

            IntPtr ptr = Marshal.AllocHGlobal(Size);
            Marshal.StructureToPtr(obj, ptr, false);
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
            Ping,
            Pong,
            // User2Chatting
            EnterChatRoomReq,
            LeaveChatRoom,
            SendChatting,
            // Chatting2User
            EnterChatRoomRes,
            RecvChatting,
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
            public abstract void PacketHandle(PingPongClient UserConnection);
        }

        #region ping
        /// <summary>
        /// 핑 메시지 송신
        /// </summary>
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public class Ping : MessageBase
        {
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 64)]
            public string Message;

            protected override void Init()
            {
                PacketID = (System.UInt32)PacketEnum.Ping;
            }

            public override void PacketHandle(PingPongClient UserConnection)
            {
                Pong PongMessage = new Pong();
                PongMessage.Message = Message;

                UserConnection.Connection.Tell(Tcp.Write.Create(ByteString.FromBytes(PacketGenerator.GetInst.ClassToBytes(PongMessage))));
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
            public string Message;

            protected override void Init()
            {
                PacketID = (System.UInt32)PacketEnum.Pong;
            }

            public override void PacketHandle(PingPongClient UserConnection)
            {
                System.Console.WriteLine(Message);
            }
        }
        #endregion
    }

    namespace User2Chatting
    {
        #region EnterChatRoomReq
        /// <summary>
        /// ChannelName에 해당하는 채팅 채널로 진입 요청
        /// </summary>
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public class EnterChatRoomReq : Message.MessageBase
        {
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 16)]
            public string ChannelName;

            protected override void Init()
            {
                PacketID = (System.UInt32)Message.PacketEnum.EnterChatRoomReq;
            }

            public override void PacketHandle(PingPongClient UserConnection)
            {
            }
        }
        #endregion

        #region LeaveChatRoom
        /// <summary>
        /// ChannelName에 해당하는 채팅 채널로 진입 요청
        /// </summary>
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public class LeaveChatRoom : Message.MessageBase
        {
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 16)]
            public string ChannelName;

            protected override void Init()
            {
                PacketID = (System.UInt32)Message.PacketEnum.LeaveChatRoom;
            }

            public override void PacketHandle(PingPongClient UserConnection)
            {
                
            }
        }
        #endregion

        #region SendChatting
        /// <summary>
        /// 지정한 채널에 채팅 메시지 송신
        /// </summary>
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public class SendChatting : Message.MessageBase
        {
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 16)]
            public string ChannelName;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 64)]
            public string ChatMessage;

            protected override void Init()
            {
                PacketID = (System.UInt32)Message.PacketEnum.SendChatting;
            }

            public override void PacketHandle(PingPongClient UserConnection)
            {
            }
        }
        #endregion
    }

    namespace Chatting2User
    {
        #region EnterChatRoomRes
        /// <summary>
        /// ChannelName에 해당하는 채팅 채널로 진입 응답
        /// </summary>
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public class EnterChatRoomRes : Message.MessageBase
        {
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 16)]
            public string ChannelName;
            bool IsEnterChannel = false;

            protected override void Init()
            {
                PacketID = (System.UInt32)Message.PacketEnum.EnterChatRoomRes;
            }

            public override void PacketHandle(PingPongClient UserConnection)
            {
                if(IsEnterChannel == false || UserConnection.ChannelSet.Contains(ChannelName) == true)
                {
                    return;
                }

                UserConnection.ChannelSet.Add(ChannelName);
            }
        }
        #endregion

        #region RecvChatting
        /// <summary>
        /// 지정한 채널에서 채팅 메시지 수신
        /// </summary>
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public class RecvChatting : Message.MessageBase
        {
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 16)]
            public string ChannelName;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 64)]
            public string ChatMessage;

            protected override void Init()
            {
                PacketID = (System.UInt32)Message.PacketEnum.RecvChatting;
            }

            public override void PacketHandle(PingPongClient UserConnection)
            {
                Console.WriteLine("{0} : {1}", ChannelName, ChatMessage);
            }
        }
        #endregion
    }
}
