# PingPong Client

## 제작 기간
2021.09.26 ~ 2021.11.11

## 개요

`PingPong Client`는 PingPong Server와 통신하는 Akka.NET 기반 TCP 클라이언트입니다.

### 주요 기능
1. **TCP 소켓 기반 서버 연결 및 통신**
2. **콘솔 입력을 통한 실시간 채팅 메시지 전송**
3. **멀티 채널 동시 구독 및 메시지 수신**
4. **리플렉션 기반 패킷 자동 처리**

---

## 메시지 프로토콜

서버와 동일한 프로토콜 사용:

```csharp
// 채널 입장/퇴장
EnterChatRoomReq  { ChannelName: string(16) }
EnterChatRoomRes  { ChannelName: string(16), bool }
LeaveChatRoom     { ChannelName: string(16) }

// 메시지 송수신
SendChatting { ChannelName: string(16), ChatMessage: string(64) }
RecvChatting { ChannelName: string(16), ChatMessage: string(64) }
```

---

## TODO

- [ ] **Akka.Serialization으로 직렬화 방식 변경**
- [ ] **패킷 처리 자동화 개선** (리플렉션 최적화)
- [ ] **명령어 기반 채널 관리** (입장/퇴장/목록 조회)

---

## 참고 자료

- [Akka.NET I/O Documentation](https://getakka.net/articles/networking/io.html)
- [Akka.NET 한글 튜토리얼](https://blog.rajephon.dev/2018/12/08/akka-02/)
