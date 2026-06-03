namespace GameServer.Protocol;

/// <summary>와이어 프레임의 메시지 종류 식별자. 프레임 헤더에 uint16(LE)로 기록된다.</summary>
public enum Opcode : ushort
{
    None = 0,
    LoginReq = 1,
    LoginResp = 2,
    MoveReq = 3,
    EntityMoved = 4,
    Error = 255,
}
