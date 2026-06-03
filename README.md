# GameServerOrleansAI

C# **Microsoft Orleans**(Actor Model) 기반 **MMO 월드** 게임서버.
에이전틱 코딩 + GitHub PR 워크플로우로 점진적으로 구축한다.

## 스택
- .NET 10 / C#
- Microsoft Orleans 9.x (Grain = actor)
- TCP + 커스텀 바이너리 프로토콜 (길이 프리픽스 프레이밍 + MemoryPack)
- xUnit + Orleans `TestingHost` (TDD)

## 아키텍처
```
TCP Client ──binary frame──> Gateway(Orleans Client) ──> Orleans Silo
                                                            ├─ PlayerGrain
                                                            ├─ ZoneGrain (스트림 브로드캐스트)
                                                            └─ WorldGrain
```

## 빌드 / 테스트 / 실행
```bash
dotnet build GameServer.slnx
dotnet test
dotnet run --project src/GameServer.Silo      # 터미널 A
dotnet run --project src/GameServer.Gateway   # 터미널 B
dotnet run --project tools/GameServer.TestClient  # 터미널 C, D
```

자세한 규약과 로드맵은 [.AGENTS.md](.AGENTS.md) 참고.
