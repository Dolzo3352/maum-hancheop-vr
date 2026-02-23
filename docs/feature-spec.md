# Dear Brave — 기능명세서

## 용어 정의

| 용어 | 의미 |
|------|------|
| Scene | Unity Scene 파일 (.scene). 본 프로젝트에서는 1개만 사용 |
| Stage | 하나의 Unity Scene 안에서 논리적으로 구분되는 콘텐츠 단위. GameObject 활성화/비활성화로 전환 |

```
[단일 Unity Scene: MainExperience.scene]
 └── DioramaRoot
      ├── Stage_01_집앞
      ├── Stage_02_골목길
      ├── Stage_03_큰길
      ├── Stage_04_시장입구
      ├── Stage_05_시장안
      ├── Stage_06_두부가게
      ├── Stage_07_귀갓길
      ├── Stage_08_골목귀환
      └── Stage_09_집도착
```

---

## 설계 원칙

### 타임라인이 마스터

모든 연출은 타임라인에서 제어합니다. 코드는 타임라인이 호출하는 실행기 역할만 합니다.

```
[각 Stage 타임라인 구성]
  ├── Animation Track ──────── 캐릭터 이동 + 애니메이션
  ├── Animation Track ──────── NPC 동작
  ├── DioramaScale Track ───── 스케일 전환 (유일한 Custom Track)
  ├── Audio Track ─────────── BGM, 환경음, 나레이션
  ├── Signal Track ────────── 카메라 효과 프리셋 호출
  └── Activation Track ────── 오브젝트 온/오프
```

| 구분 | 빌트인 사용 | 커스텀 개발 |
|------|-------------|-------------|
| 캐릭터 이동/애니메이션 | Animation Track | — |
| NPC 동작 | Animation Track + Activation Track | — |
| 스케일 전환 | — | DioramaScaleTrack (**구현 완료**) |
| BGM/환경음/나레이션 | Audio Track | — |
| 카메라 연출 | Signal Track | — (프리셋으로 대응) |
| 오브젝트 온/오프 | Activation Track | — |

---

## 팀 분배 개요

| 구분 | A — 무대 담당 | B — 배우/연출 담당 |
|------|---------------|-------------------|
| 영역 | Stage 공간, 전환, 스케일, 라이팅 | 카메라, 오디오, 타임라인 연출 |
| 핵심 | DioramaStageManager, NarrativeSequencer | PsychologicalCameraEffect, SpatialAudioManager |
| 공통 | VRComfortManager (후반 합류) | VRComfortManager (후반 합류) |

---

## 담당 스크립트 요약

| # | 스크립트 | 담당 | 유형 | 상태 |
|---|---------|------|------|------|
| 1-1 | DioramaStageManager | A | MonoBehaviour | 미구현 |
| 1-2 | DioramaScaleTrack | A | Custom Timeline Track | **구현 완료** |
| 1-2a | DioramaScaleClip | A | PlayableAsset | **구현 완료** |
| 1-2b | DioramaScaleBehaviour | A | PlayableBehaviour | **구현 완료** |
| 1-2c | DioramaScaleMixerBehaviour | A | PlayableBehaviour | **구현 완료** |
| 1-2d | DioramaScaleOrigin | A | MonoBehaviour | **구현 완료** |
| 1-2e | DioramaScaleClipEditor | A | Custom Editor | **구현 완료** |
| 2-1 | NarrativeSequencer | A | MonoBehaviour | 미구현 |
| 2-2 | StageData | A | ScriptableObject | 미구현 |
| 2-3 | TimelineController | A | MonoBehaviour | 미구현 |
| 2-4 | StageTransitionHandler | A | MonoBehaviour | 미구현 |
| 3-1 | PsychologicalCameraEffect | B | MonoBehaviour | 미구현 |
| 3-2 | CameraEffectPreset | B | ScriptableObject | 미구현 |
| 4-1 | SpatialAudioManager | B | MonoBehaviour | 미구현 |
| 5-1 | LightingPresetManager | A | MonoBehaviour | 미구현 |
| 5-2 | LightingPreset | A | ScriptableObject | 미구현 |
| 5-3 | DioramaVisualEffect | A | MonoBehaviour | 미구현 |
| 6-1 | VRComfortManager | 공통 | MonoBehaviour | 미구현 |

총 17개 (SO 3개, Custom Track 1세트 6개 포함)

---

## 접점: 타임라인 Signal 기반

A와 B의 접점은 Timeline Signal입니다. 코드 간 이벤트 구독이 아니라, 타임라인 에디터에서 Signal을 찍고 SignalReceiver에서 메서드를 연결하는 방식입니다.

```
[Stage_03 Timeline]
  Signal Track
    ├── 0:05  Signal "CameraEffect_Anxious"  → PsychologicalCameraEffect.ApplyPreset("Anxious")
    ├── 0:12  Signal "CameraEffect_Fearful"  → PsychologicalCameraEffect.ApplyPreset("Fearful")
    └── 0:20  Signal "CameraEffect_Reset"    → PsychologicalCameraEffect.ResetEffect()
```

### 합의 사항 (작업 전 확정)

1. **Signal 이름 규칙:** `CameraEffect_{프리셋이름}`, `AudioFade_{In/Out}` 등
2. **CameraEffectPreset 목록:** 어떤 프리셋이 필요한지 기획 단계에서 확정
3. **SignalReceiver 세팅:** B가 프리셋과 메서드를 만들면, A/B 누구든 타임라인에서 Signal 배치 가능

---

## 1. 디오라마 스케일 시스템 (A) — 구현 완료

### 1-2. DioramaScaleTrack (Custom Timeline Track)

타임라인에서 스케일 전환을 클립으로 제어. 프로젝트 내 유일한 Custom Track.

**파일 위치:** `Assets/01_KHY/Scripts_khy/DiolamaScale/`

| 구성 요소 | 역할 |
|-----------|------|
| `DioramaScaleTrack` | TrackAsset. Transform 바인딩. 클립 이름에 "1.0x → 0.7x" 형태 표시 |
| `DioramaScaleClip` | PlayableAsset. Blending + Extrapolation 지원 |
| `DioramaScaleBehaviour` | 클립 데이터. fromScale, targetScale, transitionIn, easeType |
| `DioramaScaleMixerBehaviour` | 클립 간 블렌딩 처리. 원본 스케일 기준으로 적용 (누적 없음) |
| `DioramaScaleOrigin` | 원본 스케일(1x 기준) 저장. 타임라인 프리뷰 오염 방지 |
| `DioramaScaleClipEditor` | Inspector 확장. 스케일 시각화, 이징 파라미터 편집 |

**클립 필드:**
- `float fromScale` — 시작 스케일 배율
- `float targetScale` — 목표 스케일 배율
- `float transitionIn` — 전환 시간(초). 0이면 즉시
- `EaseType easeType` — Linear, EaseOut, EaseInOut, ExaggeratedBounce, Elastic
- `float overshootStrength` — 오버슈트 강도 (0~0.8)
- `float anticipationStrength` — 안티시페이션 강도 (0~0.5)

**사용 예시:** Stage_03 타임라인에 클립 배치 → 0:05~0:08 구간에서 스케일 1.0→0.5 전환

---

## 2. 디오라마 Stage 관리 (A) — 미구현

### 1-1. DioramaStageManager (A)

Stage 활성화/비활성화를 관리하는 최상위 컨트롤러.

| 항목 | 내용 |
|------|------|
| 역할 | Stage GameObject 활성화/비활성화, 디오라마 루트 관리 |
| 주요 필드 | `Transform dioramaRoot` — 디오라마 월드 루트 |
| | `List<GameObject> stages` — Stage 오브젝트 목록 |
| | `int activeStageIndex` — 현재 활성 Stage 인덱스 |
| | `float defaultScale` — 기본 미니어처 스케일 (예: 0.1) |
| | `Vector3 stageCenter` — 디오라마 중심 월드 좌표 |
| 주요 메서드 | `ActivateStage(int index)` — 해당 Stage만 활성화, 나머지 비활성화 |
| | `DeactivateAllStages()` — 전체 비활성화 |
| | `ResetToDefaultScale()` — 디폴트 스케일로 복귀 |
| | `GetCurrentScale() → float` — 현재 스케일 반환 |

---

## 3. 타임라인 내러티브 시스템 (A) — 미구현

### 2-1. NarrativeSequencer (A)

Stage 간 전환을 관리하는 시퀀서. 각 Stage의 개별 타임라인을 순차 재생.

| 항목 | 내용 |
|------|------|
| 역할 | Stage 순서 정의, Stage 전환 트리거, 진행 상태 추적 |
| 주요 필드 | `List<StageData> stages` — Stage 목록 |
| | `int currentStageIndex` — 현재 Stage 인덱스 |
| | `bool isPaused` — 시퀀서 일시정지 상태 |
| 주요 메서드 | `StartNarrative()` — 첫 Stage부터 시작 |
| | `AdvanceToNextStage()` — 다음 Stage로 진행 |
| | `GoToStage(int index)` — 특정 Stage로 점프 (디버그용) |
| | `PauseNarrative()` / `ResumeNarrative()` |
| 이벤트 | `OnStageStart(int index, StageData data)` |
| | `OnStageEnd(int index)` |
| | `OnNarrativeComplete()` |

**동작 흐름:** Stage 타임라인 재생 → 타임라인 종료 감지 → 전환 연출 → 다음 Stage 활성화 → 반복

### 2-2. StageData (ScriptableObject) (A)

각 Stage의 메타데이터.

| 항목 | 내용 |
|------|------|
| 역할 | Stage 단위 설정값 보관 |
| 필드 | `string stageName` — Stage 식별 이름 |
| | `int stageIndex` — Stage 순서 (씬 오브젝트 바인딩 대신 인덱스로 참조) |
| | `PlayableAsset timeline` — 해당 Stage의 마스터 타임라인 에셋 |
| | `float dioramaScale` — 이 Stage의 기본 디오라마 스케일 |
| | `TransitionType entryTransition` — 진입 전환 방식 (Fade, Dissolve, Physical) |
| | `float transitionDuration` — 전환 시간 |
| | `LightingPreset lightingPreset` — 라이팅 프리셋 참조 |
| | `AudioClip ambientAudio` — 환경음 |

> **설계 변경 (원본 대비):** `GameObject stageRoot`와 `PlayableDirector`를 SO 필드에서 제거했습니다.
> ScriptableObject는 에셋이므로 씬 오브젝트를 직접 참조하면 런타임에 null이 됩니다.
> 대신 `stageIndex`로 `DioramaStageManager.stages[index]`를 통해 씬 오브젝트에 접근하고,
> `PlayableAsset timeline`(에셋 참조)을 런타임에 `PlayableDirector`에 바인딩하는 방식으로 변경했습니다.

### 2-3. TimelineController (A)

PlayableDirector 래퍼.

| 항목 | 내용 |
|------|------|
| 역할 | 개별 Stage의 타임라인 재생 제어 |
| 주요 필드 | `PlayableDirector director` |
| 주요 메서드 | `Play()` / `Pause()` / `Resume()` / `Stop()` |
| | `JumpTo(double time)` — 특정 시간으로 이동 |
| 이벤트 | `OnTimelineFinished()` — NarrativeSequencer가 구독하여 다음 Stage 진행 |

> **검토 사항:** `PlayableDirector`가 이미 `Play/Pause/Stop`과 `stopped` 이벤트를 제공합니다.
> 이 래퍼의 가치는 디버그 로깅, 상태 검증, `JumpTo` 같은 편의 메서드 제공에 있습니다.
> 불필요하다고 판단되면 `NarrativeSequencer`가 `PlayableDirector.stopped`을 직접 구독하는 것도 가능합니다.

### 2-4. StageTransitionHandler (A)

Stage 전환 시 시각 효과.

| 항목 | 내용 |
|------|------|
| 역할 | 페이드, 디졸브 등 전환 연출 실행 |
| 주요 필드 | `Material fadeMaterial` — 페이드용 머터리얼 |
| | `float defaultFadeDuration` |
| 주요 메서드 | `FadeOut(float duration)` → Coroutine |
| | `FadeIn(float duration)` → Coroutine |
| | `CrossDissolve(GameObject fromStage, GameObject toStage, float duration)` → Coroutine |
| | `ExecuteTransition(TransitionType type, float duration)` → Coroutine |

---

## 4. 카메라 연출 시스템 (B) — 미구현

### 3-1. PsychologicalCameraEffect (B)

타임라인 Signal로 호출되는 카메라 효과 실행기.

| 항목 | 내용 |
|------|------|
| 역할 | Signal 수신 → 프리셋 기반 카메라 효과 적용 |
| 주요 필드 | `Volume postProcessVolume` — URP Volume 참조 |
| | `List<CameraEffectPreset> presets` — 프리셋 목록 |
| | `CameraEffectPreset currentPreset` — 현재 적용 중인 프리셋 |
| 주요 메서드 | `ApplyPreset(string presetName)` — Signal에서 호출 |
| | `ResetEffect()` — 모든 효과 초기화 |
| | `ResetEffect(float duration)` — 서서히 초기화 |

**Signal 연결 예시:** SignalReceiver에서 `CameraEffect_Anxious` Signal → `ApplyPreset("Anxious")` 연결

### 3-2. CameraEffectPreset (ScriptableObject) (B)

카메라 효과 프리셋. Inspector에서 값을 조정하고 Signal에서 이름으로 호출.

| 필드 | 설명 |
|------|------|
| `string presetName` | 프리셋 식별 이름 |
| `float vignetteIntensity` | 비네팅 강도 |
| `float colorTemperature` | 색온도 |
| `float dollyZoomIntensity` | 돌리 줌 강도 |
| `float bloomIntensity` | 블룸 강도 |
| `float transitionDuration` | 효과 전환 시간 |
| `AnimationCurve transitionCurve` | 효과 전환 커브 |

**예상 프리셋 목록:**

| 프리셋 이름 | 비네팅 | 색온도 | 돌리줌 | 블룸 | 용도 |
|-------------|--------|--------|--------|------|------|
| Calm | 0.0 | 6500 | 0.0 | 0.2 | 기본 상태 |
| Curious | 0.1 | 6500 | 0.0 | 0.3 | 주변 탐색 |
| Anxious | 0.4 | 5500 | 0.0 | 0.1 | 불안 시작 |
| Fearful | 0.7 | 4500 | 0.8 | 0.0 | 공포 절정 |
| Relieved | 0.0 | 7000 | 0.0 | 0.4 | 안도 |
| Joyful | 0.0 | 7500 | 0.0 | 0.6 | 성공/귀가 |

---

## 5. 오디오 시스템 (B) — 미구현

### 4-1. SpatialAudioManager (B)

디오라마 공간 내 3D 오디오 관리.

| 항목 | 내용 |
|------|------|
| 역할 | 스케일에 따른 오디오 믹싱, 환경음 관리 |
| 주요 필드 | `AudioMixer masterMixer` — 마스터 믹서 |
| | `float scaleToVolumeRatio` — 스케일 대비 볼륨 비율 |
| | `List<AudioSource> spatialSources` — 공간 오디오 소스 목록 |
| 주요 메서드 | `UpdateMixForScale(float currentScale)` — 스케일 변경 시 믹스 갱신 |
| | `SetAmbientLayer(AudioClip clip, float volume)` |
| | `FadeAmbient(float targetVolume, float duration)` |
| 연동 | DioramaScaleTrack 재생 종료 시 믹스 갱신 |

> **참고:** BGM, 나레이션, 효과음의 재생/전환은 각 Stage 타임라인의 Audio Track에서 직접 제어합니다. SpatialAudioManager는 스케일 연동 믹싱만 담당합니다.

---

## 6. 비주얼 / 라이팅 시스템 (A) — 미구현

### 5-1. LightingPresetManager (A)

| 항목 | 내용 |
|------|------|
| 역할 | 라이팅 프리셋 적용, 프리셋 간 보간 |
| 주요 필드 | `LightingPreset currentPreset` |
| | `Light directionalLight` — 메인 디렉셔널 라이트 |
| 주요 메서드 | `ApplyPreset(LightingPreset preset, float blendDuration)` |
| | `SetTimeOfDay(float normalizedTime)` — 0~1로 시간대 지정 |

### 5-2. LightingPreset (ScriptableObject) (A)

| 필드 | 설명 |
|------|------|
| `Color ambientColor` | 앰비언트 색상 |
| `float ambientIntensity` | 앰비언트 강도 |
| `Color directionalColor` | 디렉셔널 라이트 색상 |
| `float directionalIntensity` | 디렉셔널 라이트 강도 |
| `Quaternion lightRotation` | 그림자 방향 |
| `Material skyboxMaterial` | 스카이박스 |
| `float fogDensity` | 안개 밀도 |
| `Color fogColor` | 안개 색상 |

### 5-3. DioramaVisualEffect (A)

| 항목 | 내용 |
|------|------|
| 역할 | 미니어처 느낌 연출, 경계 처리 |
| 주요 필드 | `float tiltShiftAmount` — 틸트시프트 강도 |
| | `float edgeFadeDistance` — 디오라마 가장자리 페이드 거리 |
| | `Material edgeFadeMaterial` — 경계 페이드 셰이더 |
| 주요 메서드 | `SetTiltShift(float amount)` |
| | `SetEdgeFade(float distance)` |
| | `UpdateForScale(float scale)` — 스케일에 따른 자동 조정 |

---

## 7. VR 컴포트 (공통) — 미구현

### 6-1. VRComfortManager

| 항목 | 내용 |
|------|------|
| 역할 | 스케일 전환 시 컴포트 처리, 프레임레이트 모니터링 |
| 주요 필드 | `bool useComfortVignette` — 전환 시 비네팅 사용 여부 |
| | `float comfortVignetteIntensity` — 비네팅 강도 |
| | `float minFrameRate` — 최소 프레임레이트 임계값 |
| 주요 메서드 | `OnScaleTransitionStart()` — 전환 시작 시 비네팅 활성화 |
| | `OnScaleTransitionEnd()` — 전환 종료 시 비네팅 해제 |
| | `MonitorPerformance()` — 프레임레이트 모니터링 |

---

## 스크립트 의존성 다이어그램

```
[A 영역 — 무대] ─────────────────────────────────────

NarrativeSequencer (A)
  ├── StageData (A, SO)
  │     └── stageIndex → DioramaStageManager.stages[index]
  ├── TimelineController (A)
  │     └── PlayableDirector
  │           ├── Animation Track ─── 캐릭터/NPC (빌트인)
  │           ├── Audio Track ─────── BGM/효과음 (빌트인)
  │           ├── DioramaScale Track (A, Custom, 구현 완료)
  │           ├── Activation Track ── 오브젝트 온오프 (빌트인)
  │           └── Signal Track ────── 카메라 효과 호출 ──→ [B]
  ├── StageTransitionHandler (A)
  ├── LightingPresetManager (A)
  │     └── LightingPreset (A, SO)
  └── DioramaStageManager (A)

              ┌──────────────────────┐
              │   접점: Signal Track  │
              │                      │
  Timeline ──→│ Signal "CameraEffect_Anxious" │──→ PsychologicalCameraEffect (B)
  Timeline ──→│ Signal "AudioFade_Out"        │──→ SpatialAudioManager (B)
              │                      │
              └──────────────────────┘

[B 영역 — 연출] ─────────────────────────────────────

PsychologicalCameraEffect (B)
  └── CameraEffectPreset (B, SO)

SpatialAudioManager (B)

[공통 — 후반 합류] ──────────────────────────────────

VRComfortManager
```

---

## 핵심 이벤트 플로우 (Stage 전환)

```
 1.  TimelineController.OnTimelineFinished()           [A] Stage 타임라인 종료 감지
 2.  → NarrativeSequencer.AdvanceToNextStage()          [A]
 3.  → StageTransitionHandler.FadeOut()                 [A]
 4.  → DioramaStageManager.DeactivateAllStages()        [A]
 5.  → StageData 참조 (다음 Stage)                       [A]
 6.  → DioramaStageManager.ActivateStage(nextIndex)     [A]
 7.  → LightingPresetManager.ApplyPreset(preset)        [A]
 8.  → SpatialAudioManager.SetAmbientLayer(audio)       [B] OnStageStart 구독
 9.  → StageTransitionHandler.FadeIn()                  [A]
10.  → TimelineController.Play()                        [A] 다음 Stage 타임라인 시작
11.  → (타임라인 내부) Animation/Audio/Signal 자동 실행   [A+B]
```

---

## Stage 내부 연출 플로우 (예: Stage_03_큰길)

```
[Stage_03 Timeline - 30초]

0:00 ─── Animation Track: 아이 걷기 시작
         Audio Track: 거리 환경음 재생
         DioramaScale Clip: 스케일 1.0 유지

0:05 ─── Signal "CameraEffect_Anxious"
         → PsychologicalCameraEffect.ApplyPreset("Anxious")
         → 비네팅 0.4, 색온도 한랭으로 전환

0:10 ─── DioramaScale Clip: 스케일 1.0 → 0.4 (세계가 커지는 느낌)
         Animation Track: 아이 두리번거리는 동작

0:15 ─── Signal "CameraEffect_Fearful"
         → 돌리 줌 + 강한 비네팅
         Audio Track: 긴장 BGM 크로스페이드

0:22 ─── Signal "CameraEffect_Relieved"
         → 비네팅 해제 + 색온도 따뜻
         DioramaScale Clip: 스케일 0.4 → 1.0 (다시 작아짐)

0:28 ─── Animation Track: 아이 다시 걷기
         Signal "CameraEffect_Reset"

0:30 ─── 타임라인 종료 → NarrativeSequencer가 다음 Stage로
```

---

## 작업 순서

| 순서 | A (무대) | B (연출) | 비고 |
|------|----------|----------|------|
| 0 | Signal 이름 규칙 + CameraEffectPreset 목록 | 공동 확정 | 이게 없으면 타임라인 작업 못 함 |
| 1 | StageData SO 스키마 | CameraEffectPreset SO 스키마 | 병렬 가능 |
| 2 | DioramaStageManager | PsychologicalCameraEffect | 각자 핵심 스크립트 |
| 3 | ~~ScaleTransitionController~~ **구현 완료** | SpatialAudioManager | A는 다음 단계로 진행 가능 |
| 4 | ~~ScaleTransitionPlayableTrack~~ **구현 완료** | CameraEffectPreset 값 세팅 | A는 다음 단계로 진행 가능 |
| 5 | TimelineController | 테스트용 Stage 타임라인 1개 제작 | B가 먼저 타임라인 작업 시작 가능 |
| 6 | NarrativeSequencer | Stage 타임라인 Signal 배치 | ⚠️ 통합 지점 |
| 7 | StageTransitionHandler | Stage 타임라인 나머지 작업 | 병렬 가능 |
| 8 | LightingPreset SO + LightingPresetManager | Audio Track 세팅 | 병렬 가능 |
| 9 | DioramaVisualEffect | — | B는 타임라인 마무리 |
| 10 | VRComfortManager | 공동 | 전체 플로우 테스트 |

---

## 원본 명세서 대비 변경 사항

| 항목 | 변경 내용 | 이유 |
|------|-----------|------|
| ScaleTransitionPlayableTrack 이름 | → DioramaScaleTrack 등 현재 코드명 반영 | 이미 구현 완료된 코드와 일치시킴 |
| ScaleTransitionController 제거 | Custom Track이 역할 대체 | 타임라인 외부 스케일 전환이 필요하면 추후 추가 |
| StageData.stageRoot 필드 | → stageIndex로 변경 | SO에서 씬 오브젝트 직접 참조 불가 (런타임 null) |
| StageData.PlayableDirector | → PlayableAsset timeline으로 변경 | SO에서 씬 오브젝트 직접 참조 불가 |
| DioramaScaleOrigin 추가 | 명세서에 항목 추가 | 이미 구현된 유용한 컴포넌트 |
| DioramaScaleClipEditor 추가 | 명세서에 항목 추가 | 이미 구현된 에디터 확장 |
| 스크립트 총 개수 | 14개 → 17개 | 기존 구현 포함 |
| Stage 내부 연출 플로우 스케일값 | 0.1→0.5 등 → 1.0→0.4 등 | 현재 코드가 배율 기반 (1.0 = 원본) |
