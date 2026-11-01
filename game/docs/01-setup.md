# 01. 프로젝트 셋업

이 폴더의 `Assets/BlueLike/`를 Unity 프로젝트에 넣으면 끝입니다. 씬 파일도, 머티리얼도,
프리팹도 필요 없습니다. 코드가 전투 씬을 스스로 조립합니다.

## 1. Unity 프로젝트 생성

1. **Unity Hub** → New Project
2. 에디터 버전: **Unity 6000.x LTS** 또는 **2022.3 LTS** (둘 다 동작합니다)
3. 템플릿: **Universal 3D** (= URP). Built-in RP나 HDRP를 고르면 셰이더가 분홍색으로 깨집니다.
4. 프로젝트 생성

> URP가 아니면 안 됩니다. 셰이더가 `RenderPipeline = UniversalPipeline` 태그와
> URP ShaderLibrary 인클루드에 의존합니다.

## 2. 파일 복사

이 리포의 `game/Assets/BlueLike/` 폴더를 통째로 새 프로젝트의 `Assets/` 안에 복사합니다.

```
<UnityProject>/Assets/BlueLike/
    Scripts/
    Shaders/
    Editor/
```

Unity가 자동으로 임포트하고 `.meta` 파일을 생성합니다. 컴파일 에러 0개가 정상입니다.

## 3. 실행

아무 씬이나 열고 **Play**를 누릅니다. 템플릿이 만들어준 `SampleScene`이면 됩니다.
씬에 뭐가 있든 상관없습니다 — `BattleBootstrap`이 `AfterSceneLoad` 시점에 전투를 조립합니다.

보이는 것:

- 3라인 스테이지, 엄폐물, 아군 6명(스트라이커 4 + 스페셜 2) 대 적 4기
- 자동 전투가 바로 시작됨
- 우하단에 **코스트 게이지 + EX 스킬 카드 3장**
- 카드를 클릭하면 코스트를 소모하고 EX 스킬이 발동 (카메라가 줌인 + 슬로모)
- 카드의 `x` 버튼은 코스트 1을 내고 그 카드를 버리고 새로 뽑기
- 캐릭터 머리 위의 링이 헤일로입니다 (프로시저럴 토러스 메쉬)

## 4. 빌드 전에 반드시 할 것

### Always Included Shaders

코드가 `Shader.Find()`로 셰이더를 찾습니다. 머티리얼 에셋이 참조하지 않는 셰이더는
빌드에서 **스트립되어 사라집니다.** 그러면 빌드에서만 전부 분홍색이 됩니다.

`Project Settings` → `Graphics` → `Always Included Shaders`에 추가:

- `BlueLike/AnimeToon`
- `BlueLike/AnimeToonHair`
- `BlueLike/UnlitAdditive`

에디터에서는 없어도 돌아가기 때문에 이걸 빼먹고 빌드해서 당황하는 경우가 많습니다.

### Color Space

`Project Settings` → `Player` → `Other Settings` → **Color Space: Linear**

Gamma로 두면 셀셰이딩 그림자 경계가 더럽게 나옵니다. URP 템플릿은 기본 Linear입니다.

## 5. 모바일 설정

### Player Settings (Android)

| 항목 | 값 | 이유 |
|---|---|---|
| Scripting Backend | IL2CPP | Mono는 스토어 업로드 불가 |
| Target Architectures | ARM64 | ARMv7은 더 이상 필요 없음 |
| Graphics APIs | Vulkan, OpenGLES3 (이 순서) | Vulkan이 드로우콜에서 유리 |
| Minimum API Level | 26+ | |
| Default Orientation | Landscape Left | `BattleBootstrap`도 코드로 강제합니다 |

### URP Asset (Assets/Settings/ 안에 있음)

| 항목 | 값 | 이유 |
|---|---|---|
| HDR | Off | 폰에서 대역폭 낭비, 셀셰이딩에 불필요 |
| MSAA | 2x | 아웃라인 계단 제거에 효과적. 4x는 과함 |
| Render Scale | 0.85 ~ 1.0 | 발열 잡을 때 가장 먼저 내릴 값 |
| Main Light Shadow Resolution | 1024 | |
| Shadow Distance | 30 | 기본 50은 전투 스테이지에 과함 |
| Cascade Count | 1 | 2 이상은 폰에서 손해 |
| Soft Shadows | Off | `_ReceivedShadowStrength`로 이미 부드럽게 처리 |
| Depth Texture | Off | 안 쓰면 끄는 게 공짜 이득 |
| Opaque Texture | Off | 동일 |

### 발열 / 프레임

`BattleBootstrap.ConfigureMobile()`이 `Application.targetFrameRate = 60`을 세팅합니다.
중급 기기에서 뜨거워지면 30으로 내리세요. 이 장르는 30fps로도 충분히 괜찮게 보입니다.

## 6. 직접 만든 씬으로 옮기기

프로토타입을 벗어나면 씬을 손으로 만들고 싶어집니다. 그때는
빈 GameObject에 `BattleManager` 컴포넌트를 하나 붙이면 됩니다.
`BattleBootstrap`은 씬에 `BattleManager`가 이미 있으면 자동으로 물러납니다.

단, 그러면 다음을 직접 배치해야 합니다:

- `CoverSystem` + `CoverPoint`들 (각각 `lane` 값 설정)
- `MainCamera` 태그가 붙은 카메라 + `BattleCameraRig`
- Directional Light 하나
- `BattleUnit` + `UnitBrain`을 가진 유닛들, `BattleManager.RegisterUnit()`으로 등록
- `BattleHUD` 하나
- 마지막에 `BattleManager.StartBattle()` 호출

`BattleBootstrap.cs`가 이 순서를 그대로 코드로 보여주고 있으니 그걸 베끼면 됩니다.
