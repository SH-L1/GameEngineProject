# Void Eater — 구현 계획서 (Plan.md)

> Unity 기반 3D 구멍 성장형 아케이드 게임 *"Void Eater"* 의 개발 계획서.
> 본 문서는 게임 개발 보고서(기획서)를 토대로 한 **기술 구현 전략 · 일정 · 역할 분담 · 위험 관리**를 정리한다.

- **팀** : 임성훈(2020635040) · 문승민(2022243064) · 김채은(2023733021)
- **현재 저장소 상태** : 신규 프로젝트(README.md 만 존재) — Unity 프로젝트 신규 구축 필요
- **작업 브랜치** : `claude/void-eater-game-dev-VTXkK`

---

## 1. 기술 스택 / 개발 환경

| 항목 | 선택 | 비고 |
|------|------|------|
| 엔진 | Unity **2022.3 LTS** | 안정성·LTS 수명 |
| 렌더 파이프라인 | **URP** | 로우폴리 도시 에셋 호환·Shader Graph 활용 |
| 언어 | C# | |
| 입력 | Unity **Input System** 패키지 | 키보드 + 게임패드 확장 용이 |
| 카메라 | **Cinemachine** | 탑다운 follow + 줌아웃 이펙트 |
| UI | **UI Toolkit** 또는 **uGUI + TextMeshPro** | 본 프로젝트는 uGUI + TMP 채택(난이도↓) |
| 오디오 | **Unity Audio Mixer** | Master/BGM/SFX 그룹 분리 |
| 데이터 저장 | `JsonUtility` + `Application.persistentDataPath` | 로컬 리더보드 |
| 빌드 타깃 | Windows / macOS Standalone | |
| 외부 에셋 | Unity Asset Store 무료 도시 팩 + 무료 SFX 팩 | 로우폴리 / 스타일라이즈드 |

---

## 2. 프로젝트 폴더 구조 (생성 예정)

```
Assets/
├─ Scripts/
│  ├─ Core/         GameManager, Timer, ScoreManager, GameState
│  ├─ Player/       PlayerHole, HoleController, InputReader
│  ├─ AI/           AIHole, AIBehaviour(FSM), AISpawner
│  ├─ Hole/         HoleBase, SwallowZone, HoleSizeFx
│  ├─ Objects/      Swallowable, ObjectRegistry, ObjectSpawner
│  ├─ UI/           HUD, PauseMenu, SettingsPanel,
│  │                GameOverPanel, LeaderboardPanel
│  ├─ Audio/        AudioManager, SfxPlayer
│  ├─ Data/         LeaderboardEntry, LeaderboardStore
│  └─ Utils/        Pool<T>, MathExt
├─ Prefabs/         Hole_Player, Hole_AI, Swallowable_*, UI_*
├─ ScriptableObjects/  ObjectSizeTable, AISettings, GameSettings
├─ Scenes/          Bootstrap, Game
├─ Audio/           BGM/, SFX/
├─ Materials/Shaders/  HoleMask(Stencil), Glow(Shader Graph)
└─ Art/             (도시 에셋 임포트)
```

---

## 3. 핵심 시스템 구현 전략

### 3.1 Hole(구멍) 표현 — 가장 위험한 핵심 모듈
**1차 채택 방식**: *평면 Ground + 자식 트리거 콜라이더 + 검은 디스크 시각*
- **시각** : Quad/Disk 메쉬 + 어두운 그라데이션 텍스처 + 경계 글로우(URP Shader Graph)
- **물리** : `SphereCollider`(`isTrigger=true`), `radius = currentHoleRadius`
- **이동** : `Rigidbody`(kinematic) + `MovePosition`, 맵 경계는 투명 BoxCollider 벽으로 제한
- **2차 업그레이드(여유 시)** : Stencil 셰이더로 ground를 "실제로 뚫린 것처럼" 마스킹

### 3.2 Swallowable 오브젝트
- 모든 도시 prefab에 `Swallowable` 컴포넌트 부착 (에디터 스크립트로 일괄 자동화)
- 필드 : `size` (메쉬 bounds 자동 계산), `score`, `swallowVfx`, `swallowSfx`
- 흡수 흐름
  1. Hole 트리거 진입(`OnTriggerStay`)
  2. `hole.radius > obj.size` 일 때만 흡수 가능
  3. 오브젝트가 hole 중심으로 lerp + scale 0 으로 축소
  4. 점수·반경 가산 → Pool 반환

### 3.3 성장 / 점수 공식
```
radiusGain = k * sqrt(objectVolume / currentVolume)   // 다이미시싱 리턴
score     += round(objectScore * comboMultiplier)
```
- 후반 폭주 방지 + 초반 빠른 성장감 동시 충족
- `k`, `comboMultiplier` 는 ScriptableObject(`GameSettings`)로 노출 → 비개발자 튜닝 가능

### 3.4 AI 구멍 (FSM)
```
Wander  ── 가까운 작은 오브젝트 감지 ──▶  Eat
Eat     ── 더 큰 hole 감지 ────────────▶  Flee
Eat     ── 플레이어 < 자기 ────────────▶  Hunt
Hunt    ── 플레이어 > 자기 ────────────▶  Flee
```
- `HoleBase` 상속 → 동일한 흡수 메커니즘 재사용
- NavMesh 미사용 (구멍은 평면 자유이동) → Vector steering + Raycast 장애물 회피
- 색상 경계(빨강/주황/초록) 로 식별

### 3.5 카메라
- `CinemachineVirtualCamera` 1대, 50~60도 탑다운 follow
- `holeRadius` 비례하여 `OrthographicSize`/거리를 보간 → 성장감 시각 표현
- 흡수 순간 짧은 줌아웃 펄스 (Cinemachine Impulse)

### 3.6 게임 매니저 / 게임 흐름
상태 머신 :
```
Boot → Playing → (Paused) → GameOver → (Restart)
```
- 10분 타이머 누적 → 0 시 GameOver
- 시작 시 `ObjectSpawner` 가 시드 기반 랜덤 배치, `AISpawner` 가 3~5기 배치
- 회차 간 모든 상태 리셋 체크리스트 적용

### 3.7 UI / UX
| 화면 | 구성 |
|------|------|
| HUD | 좌상단 점수 / 우상단 타이머(mm:ss) / 우하단 미니맵(선택) |
| Pause | 해상도(1280×720·1920×1080·2560×1440) 드롭다운 + 마스터/BGM/SFX 슬라이더 |
| GameOver | 최종 점수 · 이름 입력 · 리더보드 Top10 · 재시작 |

- 해상도 적용 : `Screen.SetResolution(w, h, FullScreenMode)`
- 모든 텍스트 TextMeshPro

### 3.8 오디오
- `AudioMixer` : Master / BGM / SFX 3 그룹
- 슬라이더 ↔ dB 변환 : `mixer.SetFloat("BGMVol", Mathf.Log10(value) * 20f)`
- `AudioManager` 싱글톤 + `SfxPlayer.PlayOneShot(clip, position, pitch)`
- 흡수 SFX 3 종 (소/중/대) — 오브젝트 size 에 따라 선택

### 3.9 로컬 리더보드 (Top 10)
- 저장 위치 : `Application.persistentDataPath/leaderboard.json`
- API : `LeaderboardStore.Add(name, score)` → 내림차순 정렬 → 상위 10 유지 → 저장
- 직렬화 : `JsonUtility` + 래퍼 클래스 (List 직렬화 한계 대응)
- 손상 시 try/catch → 백업 후 빈 리스트 초기화

### 3.10 풀링·VFX
- 흡수 파티클 / 흡수 SFX 인스턴스 / 도시 오브젝트 리스폰 대비 `Pool<T>` 유틸
- VFX : 흡수 파티클, 경계 글로우 펄스, 게임오버 시 화면 페이드아웃 + 붕괴 파티클

---

## 4. 일정 (8주, 3인 팀)

| 주차 | 마일스톤 | 산출물 |
|------|----------|--------|
| **W1** | Unity 프로젝트 생성·URP 세팅·도시 에셋 임포트·Git 워크플로 정착 | Bootstrap·Game 씬, `.gitignore`, 기본 prefab 골격 |
| **W2** | **Player Hole MVP** — 이동·트리거 흡수·반경 성장·카메라 추적 | 플레이어 구멍 동작 가능 |
| **W3** | Swallowable 일괄 적용·맵 오브젝트 랜덤 배치·성장 공식 튜닝 | 점수 누적·물리 검증 통과 |
| **W4** | **AI Hole** FSM · 회피/추적 로직 · 적 3~5기 스폰 · 플레이어 사망 처리 | AI 베이스라인 (α) |
| **W5** | UI(HUD·Pause·설정) · 해상도/볼륨 옵션 · AudioMixer 연동 | 인게임 UI 완성 |
| **W6** | GameOver 화면 · 로컬 리더보드(JSON) · 결과→재시작 루프 | **전체 게임 루프 완성 (β)** |
| **W7** | VFX(흡수·글로우 펄스·페이드아웃) · SFX 3종 피치 · BGM 루프 · 카메라 줌아웃 이펙트 | 폴리싱 빌드 |
| **W8** | 알파/베타/최종 테스트(기획서 §7.1) · 밸런싱 · Standalone 빌드 · 발표자료 | **릴리즈 빌드** |

---

## 5. 역할 분담 (제안)

| 담당 | 영역 | 주요 모듈 |
|------|------|-----------|
| **임성훈** (2020635040) | 코어 게임플레이 | `HoleBase`, `PlayerHole`, `Swallowable`, 성장 공식, 카메라 |
| **문승민** (2022243064) | AI / 게임 흐름 | `AIHole` FSM, `AISpawner`, `ObjectSpawner`, `GameManager`, `Timer` |
| **김채은** (2023733021) | UI / 오디오 / 데이터 / 그래픽 폴리싱 | HUD·Settings·GameOver UI, `AudioManager`, `LeaderboardStore`, VFX |

> 각 마일스톤 종료 시 코드 리뷰 + 통합 빌드 + 합동 플레이테스트 1회 진행.

### 5.1 임성훈 담당 개발 Phase

임성훈 담당 범위는 코어 게임플레이에 집중한다. 다른 팀원의 AI, UI, 게임 흐름 작업이 붙기 쉽도록 입력·점수·상태 관리와 강하게 결합하지 않고, `HoleBase` 중심의 재사용 가능한 흡수/성장 규칙을 먼저 완성한다.

| Phase | 목표 | 산출물 | 상태 |
|------|------|--------|------|
| **P1** | Player Hole MVP 골격 | `HoleBase`, `PlayerHole`, `Swallowable`, `GameSettings`, 기본 카메라 follow | 완료 |
| **P2** | 흡수 판정 안정화 | 트리거 중복 방지, 콜라이더/렌더 bounds 기반 size·volume 계산, 비활성화/풀링 호환 | 완료 |
| **P3** | 성장 공식 튜닝 | `growthCoefficient`, radius gain clamp, score multiplier, 작은/중간/큰 오브젝트 테스트 프리셋 | 완료 |
| **P4** | 카메라 성장 연출 | 반경 기반 줌 보간, 흡수 순간 줌아웃 펄스, 최소/최대 카메라 사이즈 제한 | 완료 |
| **P5** | 팀 통합 인터페이스 정리 | AI가 상속할 `HoleBase` API 확정, UI가 구독할 `RadiusChanged`/`ScoreChanged` 이벤트 검증 | 예정 |
| **P6** | Unity 씬 검증 | Player prefab 구성, Swallowable 샘플 배치, Play Mode 수동 테스트 체크리스트 완료 | 예정 |

P1 기준 배치 가이드:
- Player 오브젝트: `Rigidbody` + `SphereCollider(isTrigger)` + `PlayerHole`
- Player Hole 초기 반경은 `0.5`로 맞춘다. `PlayerHole.radius`와 `SphereCollider.radius`가 같은 값이어야 한다
- Hole 시각 오브젝트: 검은 disk/quad를 `visualRoot`에 연결
- 먹을 오브젝트: `Collider` + `Renderer` + `Swallowable`
- Main Camera: `HoleCameraFollow`를 붙이고 `target`에 Player Hole 연결

P1 컴파일 확인:
- `dotnet restore "My project/Assembly-CSharp.csproj"` 완료
- `dotnet build "My project/Assembly-CSharp.csproj" --no-restore` 완료
- 결과: 경고 0개, 오류 0개

P1 Unity 에디터 작업 체크리스트:
- `Assets/Create/Void Eater/Game Settings`로 `GameSettings` 에셋 생성
- Player Hole 오브젝트 생성 후 `Rigidbody`, `SphereCollider`, `PlayerHole` 부착
- `SphereCollider`는 Trigger로 사용하며, `PlayerHole`의 `settings`에 `GameSettings` 에셋 연결
- Hole 시각용 disk/quad 자식 오브젝트를 만들고 `visualRoot`에 연결
- 먹을 샘플 오브젝트에 `Collider`, `Renderer`, `Swallowable` 부착
- Main Camera에 `HoleCameraFollow` 부착 후 `target`에 Player Hole 연결
- Main Camera를 Orthographic으로 설정하면 반경 기반 줌이 즉시 동작함

P2 구현 내용:
- `HoleBase.CanSwallow`에 크기 기반 흡수 가능 여부와 선택형 full containment 검사 추가
- Scale 값 단순 비교를 제거하고, `SphereCollider`의 월드 반경과 오브젝트가 실제로 hole 안쪽으로 들어온 정도를 기준으로 흡수/통과 여부를 판단
- 최종 통과 가능 여부는 오브젝트 XZ footprint의 대각 반경(`RequiredPassThroughRadius`)이 hole 월드 반경 안에 들어갈 수 있는지로 제한
- `GameSettings`에 `requireFullContainment`, `swallowTolerance` 튜닝값 추가
- `Swallowable`이 코루틴 위치 보간 대신 Rigidbody 힘/토크로 hole 가장자리에서 중심을 잃고 굴러 떨어지는 Hole.io식 흡수 방식으로 변경
- 작은 오브젝트는 hole 안쪽으로 충분히 들어오면 collider를 trigger로 전환해 ground를 통과하고, 큰 오브젝트는 일부만 기울거나 밀리되 최종 흡수되지 않음
- `Swallowable`의 collider 캐싱, size/volume 재계산, 재활성화 reset 흐름 정리
- 흡수 중복 방지 및 흡수 도중 hole/object가 비활성화되는 경우 복구 처리

P2 컴파일 확인:
- `dotnet restore "My project/Assembly-CSharp.csproj"` 완료
- `dotnet build "My project/Assembly-CSharp.csproj" --no-restore` 완료
- 결과: 경고 0개, 오류 0개

P2 Unity 에디터 작업 체크리스트:
- 상단 메뉴 `Void Eater > Setup Phase 2 Test Scene` 실행
- 생성된 `Player Hole`이 `Player Hole > visualRoot > HoleVisual` 계층인지 확인
- Main Camera가 Orthographic이고 `HoleCameraFollow.target`이 `Player Hole`인지 확인
- Play 후 `WASD` 또는 방향키로 Player Hole이 움직이는지 확인
- 기존 `GameSettings` 에셋을 열고 새 필드가 보이는지 확인
- `Require Full Containment`는 우선 끈다. 켜면 오브젝트가 hole 안쪽에 충분히 들어와야 흡수된다
- `Swallow Tolerance`는 `0.1`부터 시작하고, 흡수가 너무 빡빡하면 `0.2~0.3`으로 조정
- 작은 Cube, 중간 Cube, Player보다 큰 Cube를 각각 배치해 크기 기반 흡수 가능/불가능 경계를 확인
- 작은 오브젝트는 hole 가장자리에서 중심을 잃고 굴러 떨어진 뒤 비활성화되는지 확인
- 큰 오브젝트는 가장자리에서 일부 기울거나 밀리지만 ground를 완전히 통과하거나 비활성화되지 않는지 확인
- Hole보다 XZ footprint 대각 반경이 큰 오브젝트는 물리 영향만 받고 최종 흡수되지 않는지 확인
- Hole과 Cube의 X/Z scale이 같을 때 Cube가 가장자리에서 물리 힘을 받고, 충분히 안쪽으로 들어오면 통과하는지 확인
- 흡수된 오브젝트가 Hierarchy에서 비활성화되는지 확인
- 같은 오브젝트가 한 번에 점수를 여러 번 주지 않는지 Console/Inspector로 확인

P3 구현 내용:
- `GameSettings`에 `baseGrowthRequired`, `growthRequirementMultiplier`, `radiusGainPerLevel`, `scoreMultiplier` 추가
- `GameSettings.CalculateGrowthProgress`, `CalculateGrowthRequired`, `CalculateScoreGain`으로 성장 게이지/점수 계산을 한 곳에서 튜닝
- `HoleBase.AddGrowth`가 즉시 반경을 키우지 않고 성장 게이지를 채우며, 게이지가 다 찼을 때만 단계적으로 반경 증가
- `HoleProgressRing` 추가: Hole 가장자리 테두리와 외곽 성장 게이지 원 표시
- `HoleDebugHUD` 추가: Play 중 현재 `Radius`, `Score`를 화면 좌상단에 표시
- `Void Eater > Setup Phase 2 Test Scene` 메뉴가 Tiny/Small/Medium/Tall/Large 테스트 오브젝트를 생성하도록 확장

P3 컴파일 확인:
- `dotnet restore "My project/Assembly-CSharp-Editor.csproj"` 완료
- `dotnet build "My project/Assembly-CSharp-Editor.csproj" --no-restore` 완료
- 결과: 경고 0개, 오류 0개

P3 Unity 에디터 작업 체크리스트:
- 상단 메뉴 `Void Eater > Setup Core Test Scene` 다시 실행
- Main Camera에 `HoleDebugHUD`가 붙고 `target`이 Player Hole인지 확인
- `GameSettings`에서 `Base Growth Required`, `Growth Requirement Multiplier`, `Radius Gain Per Level`, `Maximum Radius`, `Score Multiplier` 값 확인
- Tiny/Small Cube를 먼저 먹고 점수는 즉시 오르되, Radius는 성장 게이지가 다 찬 뒤에만 증가하는지 확인
- Hole 가장자리의 외곽 게이지 링이 오브젝트를 먹을 때마다 차오르는지 확인
- Medium/Tall/Large 오브젝트가 성장 정도에 따라 먹히는 시점이 달라지는지 확인
- 성장이 너무 빠르면 `objectVolumeProgressWeight`, `objectScoreProgressWeight`, `radiusGainPerLevel`을 낮추거나 `baseGrowthRequired`를 올린다
- 성장 체감이 너무 약하면 `objectVolumeProgressWeight`, `objectScoreProgressWeight`, `radiusGainPerLevel`을 조금 올린다

P4 구현 내용:
- `HoleCameraFollow`가 `HoleBase.Swallowed` 이벤트를 구독해 흡수 순간 짧은 줌아웃 펄스를 적용
- 반경 기반 `OrthographicSize` 보간에 `minOrthographicSize`, `maxOrthographicSize` 제한 추가
- `swallowPulseSize`, `swallowPulseDuration`으로 흡수 카메라 펄스 강도와 시간을 조절 가능
- `Void Eater > Setup Core Test Scene` 메뉴가 P4 카메라 기본값을 자동 세팅

P4 컴파일 확인:
- `dotnet build "My project/Assembly-CSharp-Editor.csproj" --no-restore` 완료
- 결과: 경고 0개, 오류 0개

P4 Unity 에디터 작업 체크리스트:
- 상단 메뉴 `Void Eater > Setup Core Test Scene` 다시 실행
- Main Camera의 `HoleCameraFollow`에 새 필드가 보이는지 확인
- Play 후 오브젝트를 먹을 때 카메라가 짧게 줌아웃했다가 돌아오는지 확인
- Hole이 커질수록 카메라 시야가 천천히 넓어지는지 확인
- 펄스가 과하면 `Swallow Pulse Size`를 낮추고, 약하면 조금 올린다
- 전체 시야가 너무 넓거나 좁으면 `Min/Max Orthographic Size`, `Size Per Radius`를 조정한다

---

## 6. 위험 요소 & 대응 전략

| 리스크 | 영향 | 대응 |
|--------|------|------|
| 구멍이 ground를 "실제 뚫린 것처럼" 보이게 하기 어려움 | 시각 품질 | 1차 검은 디스크 + 글로우 → 2차 Stencil 셰이더 (W7 여유 시) |
| AI 동작이 단조롭거나 추적이 너무 강함 | 게임 밸런스 | FSM 가중치 + 거리 기반 hysteresis 튜닝 (W4 말 베타테스트) |
| 도시 에셋 prefab 의 콜라이더·피벗 비표준 | 흡수 오작동 | 에디터 스크립트로 일괄 검사 + `Swallowable` 자동 부착 |
| GC 스파이크 (오브젝트/파티클) | 프레임 드랍 | `Pool<T>` 도입 + Profiler 정기 점검 (W3, W7) |
| 리더보드 JSON 파손 | 데이터 손실 | try/catch → 백업·초기화 로직 |
| 시간 부족 시 단일 스테이지 분량 부족 감 | 콘텐츠 빈약 | 회차별 시드 랜덤화로 반복 다양성 확보 (이미 기획에 반영) |

---

## 7. 검증 / 테스트 계획 (E2E)

- **알파 (W4 말)** : 핵심 메커니즘 검증 — 5분 세션 × 3회. 이동/흡수/성장/타이머/게임오버 모두 정상 동작.
- **베타 (W6 말)** : AI 밸런싱 — 적 3·4·5기 시나리오별 풀세션 5회. 평균 생존시간·최종 점수 기록 후 `GameSettings` 튜닝.
- **최종 (W8)** :
  - 해상도 1280×720 / 1920×1080 / 2560×1440 별 UI 깨짐 점검
  - 마스터·BGM·SFX 슬라이더 끝값(0% / 100%) 청취 검증
  - 리더보드 10건 초과 입력 → 정렬·잘림 확인
  - `persistentDataPath` 삭제 후 재실행 시 빈 리더보드 정상 동작
- **디버그 도구** : Unity Console + Physics Debug Visualizer + Profiler + AI 경로 Gizmos.

---

## 8. 진행 체크리스트 (요약)

- [ ] W1 — Unity 2022.3 LTS + URP 프로젝트 생성, `.gitignore`, 도시·SFX 에셋 임포트
- [ ] W2 — `HoleBase` / `PlayerHole` / 카메라 follow MVP
- [ ] W3 — `Swallowable` + `ObjectSpawner` + 성장 공식
- [ ] W4 — `AIHole` FSM + `AISpawner` + 사망 처리
- [ ] W5 — HUD / PauseMenu / SettingsPanel + AudioMixer
- [ ] W6 — GameOverPanel + `LeaderboardStore`(JSON) + 재시작 루프
- [ ] W7 — VFX / SFX 3종 / BGM / 카메라 줌아웃 이펙트
- [ ] W8 — 알파·베타·최종 테스트, 밸런싱, Standalone 빌드, 발표자료

---

*본 계획서는 개발 진행에 따라 갱신된다. 각 주차 종료 시점에 본 파일의 체크박스 및 변경사항을 함께 커밋한다.*
