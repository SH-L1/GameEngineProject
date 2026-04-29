<div align="center">

# 🕳️ Void Eater

### *도시를 통째로 삼켜라 — Unity 기반 3D 구멍 성장형 아케이드 게임*

![Unity](https://img.shields.io/badge/Unity-2022.3_LTS-000000?style=flat-square&logo=unity)
![Render](https://img.shields.io/badge/Render-URP-3776AB?style=flat-square)
![Language](https://img.shields.io/badge/Language-C%23-239120?style=flat-square&logo=csharp)
![Platform](https://img.shields.io/badge/Platform-Windows_%7C_macOS-blue?style=flat-square)
![Status](https://img.shields.io/badge/Status-In_Development-orange?style=flat-square)
![License](https://img.shields.io/badge/Course-게임엔진I-purple?style=flat-square)

</div>

---

## 🎮 게임 소개

> **"도시 지하에서 깨어난 작은 균열, 모든 것을 삼키며 거대한 공허로 자라난다."**

**Void Eater** 는 도시 맵 위의 **구멍(Hole)** 을 조종하여 건물·차량·가로등 등 다양한 오브젝트를 삼켜
구멍의 크기를 키워나가는 **3D 아케이드 게임** 입니다.

맵에는 동일한 메커니즘으로 성장하는 **AI 적 구멍** 들이 존재하며,
**자신보다 큰 구멍에 흡수되면 즉시 게임 오버** 가 됩니다.
**10분의 제한 시간** 안에 더 빠르게, 더 크게 성장하여 로컬 리더보드 정상에 이름을 올리세요.

---

## ✨ 주요 특징

| 🌀 | 동적 성장 메커니즘 | 흡수할수록 커지는 구멍, 더 큰 오브젝트를 삼킬 수 있음 |
|---|---|---|
| 🤖 | **공격적인 AI 경쟁자** | 3~5기의 AI 구멍이 같은 룰로 성장하며 위협이 됨 |
| ⏱️ | **10분 한정 아케이드** | 짧고 강렬한 한 판, 무한 리트라이 |
| 🏆 | **로컬 리더보드 Top 10** | JSON 기반 점수 저장, 친구와 점수 경쟁 |
| 🎨 | **로우폴리 도시 비주얼** | 스타일라이즈드 아트 + 글로우 이펙트 |
| 🔊 | **다층 오디오 믹서** | 마스터 / BGM / SFX 개별 볼륨 조절 |
| 🖥️ | **다중 해상도 지원** | 720p / 1080p / 1440p 옵션 제공 |

---

## 🎯 게임 룰

```text
✔ 자신의 반경보다 작은 오브젝트만 삼킬 수 있다
✔ AI 적도 동일 규칙 — 더 작은 hole 까지 흡수 가능
✘ 자신보다 큰 적 구멍에 닿으면 즉시 게임 오버
⏰ 제한 시간 10분 경과 시 게임 자동 종료
🏅 게임 종료 시 로컬 리더보드(Top 10) 에 점수 등록
```

---

## 🕹️ 조작

| 입력 | 동작 |
|------|------|
| `W` `A` `S` `D` / 방향키 | 구멍 이동 |
| `Esc` | 일시정지 / 설정 메뉴 |
| `Enter` | 결과 화면에서 이름 등록 |

---

## 🧩 게임 시스템 한눈에 보기

```
┌─────────────────────────────────────────────────────────┐
│                     GAME LOOP                           │
├─────────────────────────────────────────────────────────┤
│  Boot ──▶ Playing ◀──▶ Paused                           │
│              │                                          │
│              └──▶ GameOver ──▶ Leaderboard ──▶ Restart  │
└─────────────────────────────────────────────────────────┘

       Player Hole                AI Hole (FSM)
       ───────────                ─────────────
        [이동]                    Wander → Eat
        [흡수] ◀── Swallowable ──▶ Hunt   → Flee
        [성장]                       ▲       ▼
        [생존]                       └───────┘
```

---

## 🛠️ 기술 스택

<table>
<tr><th>분류</th><th>기술 / 도구</th><th>용도</th></tr>
<tr><td>엔진</td><td>Unity 2022.3 LTS</td><td>코어 런타임</td></tr>
<tr><td>렌더</td><td>URP (Universal Render Pipeline)</td><td>로우폴리 / 셰이더 그래프</td></tr>
<tr><td>언어</td><td>C# 9.0+</td><td>게임 로직</td></tr>
<tr><td>입력</td><td>Unity Input System</td><td>키보드 / 게임패드</td></tr>
<tr><td>카메라</td><td>Cinemachine</td><td>탑다운 follow + 줌 이펙트</td></tr>
<tr><td>UI</td><td>uGUI + TextMeshPro</td><td>HUD · 메뉴 · 결과창</td></tr>
<tr><td>오디오</td><td>Unity Audio Mixer</td><td>Master / BGM / SFX 그룹</td></tr>
<tr><td>저장</td><td>JsonUtility + persistentDataPath</td><td>로컬 리더보드</td></tr>
<tr><td>버전관리</td><td>Git / GitHub</td><td>협업 · 브랜치 전략</td></tr>
</table>

---

## 📂 프로젝트 구조

```
Assets/
├─ Scripts/
│  ├─ Core/        ── GameManager, Timer, ScoreManager
│  ├─ Player/      ── PlayerHole, HoleController, InputReader
│  ├─ AI/          ── AIHole, AIBehaviour(FSM), AISpawner
│  ├─ Hole/        ── HoleBase, SwallowZone, HoleSizeFx
│  ├─ Objects/     ── Swallowable, ObjectRegistry, ObjectSpawner
│  ├─ UI/          ── HUD, PauseMenu, SettingsPanel, GameOverPanel
│  ├─ Audio/       ── AudioManager, SfxPlayer
│  ├─ Data/        ── LeaderboardEntry, LeaderboardStore
│  └─ Utils/       ── Pool<T>, MathExt
├─ Prefabs/        ── Hole_Player, Hole_AI, Swallowable_*
├─ ScriptableObjects/
├─ Scenes/         ── Bootstrap, Game
├─ Audio/          ── BGM/, SFX/
├─ Materials/Shaders/
└─ Art/            ── 도시 에셋 임포트
```

---

## 📅 개발 일정 (8주)

| 주차 | 마일스톤 | 핵심 산출물 |
|:---:|:---|:---|
| **W1** | 프로젝트 셋업 | URP 프로젝트, 도시 에셋, `.gitignore` |
| **W2** | 🌀 Player Hole MVP | 이동·흡수·성장·카메라 follow |
| **W3** | 🍽️ Swallow 시스템 | 오브젝트 일괄 적용, 성장 공식 |
| **W4** | 🤖 AI Hole | FSM, 스폰, 사망 처리 *(α 빌드)* |
| **W5** | 🖼️ UI 시스템 | HUD, Pause, Settings, AudioMixer |
| **W6** | 🏆 게임 루프 완성 | GameOver, 리더보드, 재시작 *(β 빌드)* |
| **W7** | ✨ 폴리싱 | VFX, SFX, BGM, 카메라 이펙트 |
| **W8** | 🚀 릴리즈 | 테스트, 밸런싱, Standalone 빌드 |

> 자세한 일정 및 위험 관리는 [`Plan.md`](./Plan.md) 참조.

---

## 👥 팀 구성

| 학번 | 이름 | 담당 영역 |
|:---:|:---:|:---|
| `2020635040` | **임성훈** | 🎮 코어 게임플레이 — `HoleBase`, `PlayerHole`, 성장 공식, 카메라 |
| `2022243064` | **문승민** | 🧠 AI / 게임 흐름 — `AIHole` FSM, Spawner, `GameManager`, Timer |
| `2023733021` | **김채은** | 🎨 UI / 오디오 / 데이터 — HUD·Settings, `AudioManager`, 리더보드, VFX |

---

## 🚀 빌드 & 실행 (예정)

```bash
# 1. 저장소 클론
git clone https://github.com/SH-L1/GameEngineProject.git
cd GameEngineProject

# 2. Unity Hub 에서 2022.3 LTS 로 프로젝트 열기
#    Add project → 본 폴더 선택

# 3. Scenes/Bootstrap.unity 를 열고 ▶ Play
```

> ⚠️ 현재는 기획·셋업 단계이며, Unity 프로젝트 파일은 W1 마일스톤에서 생성됩니다.

---

## 📑 문서

- 📋 [`Plan.md`](./Plan.md) — 구현 전략 · 일정 · 역할 분담 · 위험 관리
- 📖 게임 개발 보고서 — 게임 디자인 / 시스템 / 시나리오 / 레벨 / 사운드 / 테스트 계획

---

## 🎓 About

> **게임엔진 I** 수업 학기 프로젝트
> Unity 엔진을 활용한 3D 게임 개발 실습

---

<div align="center">

### *"Eat the world. Become the void."*

⭐ *Made with Unity & a lot of holes* ⭐

</div>
