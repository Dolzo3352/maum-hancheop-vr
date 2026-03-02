# Dear Brave — A팀 개발 현황

## 개발 목표

> VR 디오라마 위에서 펼쳐지는 9개 스테이지 내러티브를 타임라인 기반으로 순차 재생하고, The Line VR 스타일의 라이팅 크로스페이드로 자연스럽게 전환하는 시스템

---

## 담당 모듈

| 모듈 | 설명 |
|------|------|
| **디오라마 Stage 관리** | 9개 스테이지 활성화/비활성화, 스케일 제어 |
| **타임라인 내러티브 시스템** | 스테이지별 타임라인 재생 + 전환 연출 |

---

## 구현 완료 항목

### 1. 스테이지 관리 시스템

**DioramaStageManager** — 9개 스테이지의 활성화/비활성화 관리

- `ActivateStage(index)` : 해당 스테이지만 켜고 나머지 끔
- `ActivateStageAdditive(index)` : 크로스페이드용, 다른 스테이지 끄지 않고 추가 활성화
- `DeactivateStage(index)` : 특정 스테이지 하나만 끔
- `GetStage(index)` : 스테이지 GameObject 반환

### 2. 타임라인 재생

**TimelineController** — per-stage PlayableDirector 방식

- 각 스테이지 GameObject 안에 PlayableDirector가 있고, 타임라인/바인딩은 Inspector에서 직접 설정
- `SetDirector(director)` : 스테이지 전환 시 Director를 동적으로 교체
- 타임라인 종료 감지 → `OnTimelineFinished` 이벤트 발행

### 3. 스테이지 전환 (The Line VR 스타일)

**NarrativeSequencer** — 전체 전환 흐름 오케스트레이션

```
① 다음 스테이지 추가 활성화 (양쪽 동시에 켜짐)
② 라이팅 크로스페이드 (이전 어둡게 ↔ 다음 밝게) ← 시선 유도
③ 페이드 아웃 (짧은 암전)
④ 이전 스테이지 비활성화
⑤ 텔레포트 (사용자를 다음 시청 위치로 이동)
⑥ 타임라인 재생
⑦ 페이드 인
```

**StageTransitionHandler** — 페이드 인/아웃 처리 (머티리얼 알파)

### 4. 라이팅 크로스페이드

**StageLightingController** — 스테이지별 Light + Volume 동시 제어

- 글로벌 앰비언트 = 거의 검정 + 각 스테이지에 Spot Light 배치
- 크로스페이드 시 이전 스테이지 Light/Volume이 서서히 꺼지고, 다음이 켜짐
- SmoothStep 보간으로 부드러운 전환

### 5. 텔레포트

**WaypointTeleporter** — 웨이포인트 기반 VR 카메라 이동

- 각 스테이지별 시청 위치(웨이포인트)를 배치
- 암전 중 즉시 이동 (`TeleportImmediate`)
- 키보드 O/P로 디버그 이동 가능

### 6. 디오라마 스케일 트랙

**DioramaScaleTrack** — 커스텀 타임라인 트랙

- 타임라인에서 디오라마 스케일을 애니메이션
- 5가지 이징 타입: Linear, EaseInOut, EaseIn, EaseOut, ExaggeratedBounce

### 7. 인터랙션 시스템

**InteractableOutline** — XR 호버 시 아웃라인 표시 (QuickOutline 기반)

- XR Simple Interactable의 호버/그랩 이벤트에 연결
- 그랩 후 자동 비활성화 옵션

**억새 인터랙션** — SilverGrass / GrassGroupManager / GrassGrowTrigger

- 플레이어 접근 시 억새 성장 애니메이션
- 순차/중앙확산 성장 모드
- 타겟(손) 거리에 따른 실시간 굽힘 반응

---

## 스크립트 구조

```
Scripts_khy/
├── Stage/                      ← 핵심 시스템
│   ├── DioramaStageManager     (스테이지 활성화/비활성화)
│   ├── NarrativeSequencer      (전환 흐름 오케스트레이션)
│   ├── TimelineController      (per-stage Director 재생)
│   ├── StageTransitionHandler  (페이드 인/아웃)
│   ├── StageData               (ScriptableObject - 스테이지 정보)
│   └── TransitionType          (enum - Fade/Dissolve/Physical)
│
├── Lighting/
│   ├── StageLightingController (Light + Volume 크로스페이드)
│   └── LightingPreset          (ScriptableObject - 라이팅 프리셋)
│
├── Camera/
│   ├── WaypointTeleporter      (VR 텔레포트)
│   └── CameraEffectPreset      (ScriptableObject - 카메라 효과)
│
├── DiolamaScale/
│   ├── DioramaScaleTrack       (커스텀 타임라인 트랙)
│   ├── DioramaScaleClip        (타임라인 클립)
│   ├── DioramaScaleBehaviour   (재생 동작 + 5 이징)
│   ├── DioramaScaleMixerBehaviour (믹서)
│   ├── DioramaScaleOrigin      (기준점)
│   └── DioramaScaleClipEditor  (Inspector 커스텀 에디터)
│
└── Interaction/
    ├── InteractableOutline     (XR 아웃라인 피드백)
    ├── SilverGrass             (억새 개별 애니메이션)
    ├── GrassGroupManager       (억새 그룹 관리)
    └── GrassGrowTrigger        (억새 성장 트리거)
```

총 **20개 파일** (19 클래스 + 1 enum)

---

## 씬 구조

```
DioramaRoot (DioramaStageManager)
├── Stage_01 (#1 집)
│   ├── 오브젝트들
│   ├── PlayableDirector (타임라인 + 바인딩)
│   ├── Spot Light (로컬 조명)
│   └── Volume (포스트 프로세싱)
├── Stage_02 (#2 골목길)
│   ├── ...
├── ...
└── Stage_09 (#9 집도착)

NarrativeManager
├── NarrativeSequencer
├── TimelineController
├── StageTransitionHandler
├── StageLightingController
└── WaypointTeleporter (웨이포인트 참조)
```

---

## 스테이지별 타임라인 구성

각 스테이지 타임라인에 Track Group으로 정리:

| Track Group | 내용 |
|-------------|------|
| 캐릭터 | 아이 걷기/달리기 애니메이션 |
| NPC | NPC 행동 애니메이션 |
| 연출 | 카메라, 디오라마 스케일 |
| 오디오 | BGM, 환경음, SFX |
| 오브젝트 | 배경 오브젝트 애니메이션 |

---

## 남은 작업

| 항목 | 상태 | 비고 |
|------|------|------|
| 카메라 이펙트 적용 시스템 | 대기 | CameraEffectPreset SO 있음, 적용 스크립트 필요 |
| 오디오 시스템 | 대기 | 기획 확정 후 |
| Light Layers 설정 | 대기 | 아트 에셋 확정 후 |
| 인터랙션 대기 시스템 | 대기 | 타임라인 일시정지 + idle + 재개 |
| 프로토타입 전체 흐름 테스트 | 대기 | 9개 스테이지 통합 테스트 |

---

## 참고: The Line VR 스타일 전환 원리

```
기존 (하드컷):
  Stage_01 끝 → [검정] → Stage_02 시작
  → 뚝뚝 끊기는 느낌

현재 (크로스페이드):
  Stage_01 끝 → [양쪽 동시 ON + 라이팅 전환] → [짧은 암전 + 텔레포트] → Stage_02 시작
  → 시선이 자연스럽게 다음 스테이지로 유도됨
```
