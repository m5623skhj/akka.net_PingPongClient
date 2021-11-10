using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;
using System.Reflection;

using Akka.Actor;
using Akka.IO;
using Akka.Serialization;

using pingpongclient;

// TODO
// Akka.Serialization으로 구현 변경
namespace MyMessage
{
    public class PacketGenerator : MyUtils.Singleton<PacketGenerator>
    {
        private Dictionary<string, Type> packetFinder = new Dictionary<string, Type>();

        public PacketGenerator()
        {
            packetFinder.Clear();
            LoadMessageItems();
        }

        private void LoadMessageItems()
        {
            Type[] typeInThisAssembly = Assembly.GetExecutingAssembly().GetTypes();
            foreach (Type type in typeInThisAssembly)
            {
                if (type.GetInterface(typeof(IMessage).ToString()) != null)
                {
                    packetFinder.Add(type.Name.ToLower(), type);
                }
            }
        }

        public IMessage CreateMessage(string MessageName)
        {
            Type type = FindMessageType(MessageName);
            if (type == null)
            {
                return null;
            }

            return Activator.CreateInstance(type) as IMessage;
        }

        private Type FindMessageType(string MessageName)
        {
            Type messageType;

            if (packetFinder.TryGetValue(MessageName, out messageType) == true)
            {
                return messageType;
            }

            return null;
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
    }

    // TODO
    // 리플렉션 혹은 IL 혹은 ExpressionTree 를 이용해서 패킷 처리 자동화 할 수 있게 고민해보기 
    // MessageFactory 느낌으로 만들어보기 

    public interface IMessage
    {
        string MessageName
        {
            get;
        }
        public void PacketHandle(PingPongClient UserConnection);
    }

    #region ping
    /// <summary>
    /// 핑 메시지 송신
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public class Ping : IMessage
    {
        public string MessageName
        {
            get;
        }

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 64)]
        public string Message;

        public void PacketHandle(PingPongClient UserConnection)
        {

        }
    }
    #endregion

    #region pong
    /// <summary>
    /// 핑 메시지 수신시 퐁 메시지 송신 
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public class Pong : IMessage
    {
        public string MessageName
        {
            get;
        }

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 64)]
        public string Message;

        public void PacketHandle(PingPongClient UserConnection)
        {
            System.Console.WriteLine(Message);
        }
    }
    #endregion

    namespace User2Chatting
    {
        #region EnterChatRoomReq
        /// <summary>
        /// ChannelName에 해당하는 채팅 채널로 진입 요청
        /// </summary>
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public class EnterChatRoomReq : IMessage
        {
            public string MessageName
            {
                get;
            }

            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 16)]
            public string ChannelName;

            public void PacketHandle(PingPongClient UserConnection)
            {

            }
        }
        #endregion

        #region LeaveChatRoom
        /// <summary>
        /// ChannelName에 해당하는 채팅 채널로 진입 요청
        /// </summary>
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public class LeaveChatRoom : IMessage
        {
            public string MessageName
            {
                get;
            }

            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 16)]
            public string ChannelName;

            public void PacketHandle(PingPongClient UserConnection)
            {

            }
        }
        #endregion

        #region SendChatting
        /// <summary>
        /// 지정한 채널에 채팅 메시지 송신
        /// </summary>
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public class SendChatting : IMessage
        {
            public string MessageName
            {
                get;
            }

            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 16)]
            public string ChannelName;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 64)]
            public string ChatMessage;

            public void PacketHandle(PingPongClient UserConnection)
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
        public class EnterChatRoomRes : IMessage
        {
            public string MessageName
            {
                get;
            }

            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 16)]
            public string ChannelName;
            bool IsEnterChannel = false;

            public void PacketHandle(PingPongClient UserConnection)
            {
                if (IsEnterChannel == false || UserConnection.ChannelSet.Contains(ChannelName) == true)
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
        public class RecvChatting : IMessage
        {
            public string MessageName
            {
                get;
            }

            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 16)]
            public string ChannelName;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 64)]
            public string ChatMessage;

            public void PacketHandle(PingPongClient UserConnection)
            {
                Console.WriteLine("{0} : {1}", ChannelName, ChatMessage);
            }
        }
        #endregion
    }
}
