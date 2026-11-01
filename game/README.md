# 풀 3D 애니풍 전술 전투 프로토타입

블루아카이브식 전투 시스템(코스트 + EX 스킬 카드 + 3라인 엄폐 + 속성 상성)을
**등신 비율 3D 애니 캐릭터**로 구현하기 위한 Unity URP 프로젝트입니다. 모바일 타겟.

## 지금 상태

**캐릭터 모델이 없어도 바로 플레이 가능합니다.** 코드가 전투 씬을 스스로 조립하고,
캐릭터는 헤일로가 달린 캡슐로 대체되어 있습니다. 모델이 준비되면 캡슐 자리에
끼우면 되고, 전투 코드는 한 줄도 바뀌지 않습니다.

이 순서인 이유: 혼자 개발할 때 가장 흔한 실패가 "캐릭터 모델 만들다 지쳐서
게임은 시작도 못 함"입니다. 재미가 있는지부터 확인하고 예쁘게 만드는 게 맞습니다.

## 5분 안에 돌려보기

1. Unity Hub → New Project → **Universal 3D (URP)** 템플릿
2. 이 폴더의 `Assets/BlueLike/`를 새 프로젝트의 `Assets/` 안에 복사
3. 아무 씬이나 열고 **Play**

자세한 셋업과 모바일 설정은 [docs/01-setup.md](docs/01-setup.md).

## 구현된 것

### 전투
- **코스트 시스템** — 초당 연속 회복, 최대 10, EX 스킬이 코스트를 소모
- **EX 스킬 카드 3장 핸드** — 스트라이커 덱에서 드로우, 사용 후 쿨다운 뒤 재진입,
  코스트 1로 버리고 새로 뽑기
- **4×4 속성 상성** — Explosive/Piercing/Mystic/Sonic × Light/Heavy/Special/Elastic,
  최대 4배 격차
- **3라인 엄폐 시스템** — 엄폐물 점유, 지형 적성에 따른 엄폐율, 근접 공격은 엄폐 무시
- **지형 적성 S~D** — 공격 보정과 엄폐율에 동시 작용. D등급은 엄폐 불가
- **스트라이커 / 스페셜 비대칭** — 스페셜은 카드가 없고 자동·무료로 스킬 발동, 전진하지 않음
- **자동 전투 AI** — 타겟 선정(동일 라인 우선), 엄폐물까지 전진, 사격, 탄창/재장전 사이클
- **데미지 파이프라인** — 명중 → 엄폐 → 상성 → 지형 → 방어 감쇠 → 안정성 롤 → 치명타
- **EX 스킬 6종** — 단일/다단/범위/힐/공버프/방어버프

### 연출
- **쿼터뷰 카메라** — 전선 추적, EX 발동 시 줌인 + 슬로모 + 복귀
- **데미지 표시** — 치명타/상성/블록/미스 구분, 부유 텍스트
- **탄도 트레이서** — 풀링된 LineRenderer (URP에서는 GL 라인이 동작하지 않음)
- **프로토타입 HUD** — 코스트 게이지, 카드, 유닛 HP 바, 엄폐/재장전 표시, 타이머, 결과

### 셰이더 (핵심)
- **`BlueLike/AnimeToon`** — URP 셀셰이더
  - 램프 텍스처 또는 1~4단 계단식 음영
  - **SDF 얼굴 그림자** (조명 각도에 따라 얼굴 그림자가 자연스럽게 흐름)
  - 픽셀 단위 백페이스 아웃라인 (정점 알파로 부위별 두께 조절 가능)
  - 림 라이트, 툰 스페큘러, 감쇠된 캐스트 섀도우
- **`BlueLike/AnimeToonHair`** — Kajiya-Kay 아니소트로픽 머리카락 하이라이트
  (2단 하이라이트 + 지터 맵)
- **`BlueLike/UnlitAdditive`** — 트레이서용 가산 셰이더

### 에디터 툴
메뉴 `Tools / BlueLike / ...`

- **Convert Selection To Toon Shader** — 임포트한 캐릭터의 머티리얼을 일괄 전환.
  이름으로 머리/얼굴/눈을 자동 판별
- **Generate Shadow Ramp Texture** — 3단 음영 램프 생성
- **Generate Hair Jitter Map** — 머리카락 하이라이트 지터 노이즈 생성
- **Generate Face SDF Starter Map** — SDF 얼굴 맵 출발점 생성

## 파일 구조

```
Assets/BlueLike/
  Scripts/
    Core/           숫자와 규칙. 밸런스는 여기서만 만집니다
      BattleEnums.cs       속성/장갑/지형/역할/상태 정의
      DamageTable.cs       상성표, 지형 보정, 방어 곡선, 코스트 경제
      DamageCalculator.cs  데미지 수식 (프로젝트에서 유일한 데미지 계산 지점)
      UnitStats.cs         스탯 블록
      UnitDataAsset.cs     인스펙터 편집용 ScriptableObject 래퍼
    Battle/         전투 진행
      BattleManager.cs     로스터, 타이머, 페이즈, 타겟 질의, 히트 리포트
      BattleUnit.cs        유닛 몸통 (상태, HP, 탄창, 버프)
      UnitBrain.cs         자동 전투 AI (교체 가능)
      CoverSystem.cs       엄폐물 레지스트리
      CoverPoint.cs        엄폐물 하나
    Skills/
      CostManager.cs       코스트 풀
      SkillCard.cs         카드 하나
      SkillCardManager.cs  3장 핸드, 덱, 드로우/버리기
      ExSkillRunner.cs     스킬 실행 + 시네마틱 연출
      SpecialSkillDriver.cs 스페셜의 자동 스킬 타이머
    Presentation/
      BattleCameraRig.cs   쿼터뷰 + EX 카메라
      BattleHUD.cs         프로토타입 HUD (나중에 uGUI로 교체)
      FloatingTextLayer.cs 데미지 숫자
      TracerLayer.cs       탄도선
      HitFeedback.cs       히트 → 연출 연결
      DeathFade.cs         사망 연출
    Characters/     게임과 아트의 접합부
      CharacterModelBinder.cs 캡슐 → 실제 모델 교체, 애니메이션 연결
      FaceDirectionBinder.cs  SDF 얼굴 그림자용 머리 방향 전달
    Setup/
      BattleBootstrap.cs   씬을 코드로 조립 (에셋 의존성 0)
      SampleRoster.cs      샘플 캐릭터/적 스탯 — 자유롭게 수정
      PrimitiveFactory.cs  캡슐 + 헤일로 토러스 플레이스홀더
      HaloSpin.cs
  Shaders/
  Editor/
```

## 문서

- **[01-setup.md](docs/01-setup.md)** — Unity 프로젝트 생성, 모바일 설정,
  빌드 전 필수 작업(Always Included Shaders)
- **[02-character-pipeline.md](docs/02-character-pipeline.md)** — 캐릭터를 만들어
  넣는 경로. AI 3D 생성 도구의 한계, VRoid 경로, 얼굴 법선 편집, SDF 맵 제작,
  머리카락 설정, 모바일 폴리곤 예산
- **[03-balance.md](docs/03-balance.md)** — 데미지 수식 전문, 상성표, 지형표,
  코스트 경제, 빠른 튜닝 레시피

## 다음에 손댈 것

우선순위 순입니다.

1. **캐릭터 1명 넣어보기** — VRoid로 하나 만들어서 `CharacterModelBinder`로 연결.
   캡슐이 아닌 상태의 느낌을 먼저 확인
2. **애니메이션 클립** — `Idle`/`Move`/`Fire`/`Reload`/`ExSkill`/`Death` 6개.
   Mixamo나 에셋스토어에서 받아도 애니풍 캐릭터에 그럭저럭 맞습니다
3. **HUD를 uGUI로 재작성** — 현재 IMGUI는 프로토타입용입니다. 읽는 데이터는
   전부 매니저의 public API라서 다른 코드는 안 바뀝니다
4. **스테이지 데이터화** — 지금은 `SampleRoster`에 하드코딩. 스테이지별 적 구성과
   지형을 ScriptableObject로
5. **EX 스킬 전용 연출** — 캐릭터별 카메라 워크와 VFX. 작업량이 가장 큰 영역이니
   시스템이 전부 확정된 뒤에
6. **편성 화면 / 육성** — 전투가 재미있어진 다음에

## 알려진 한계

- HUD는 IMGUI입니다. 매 프레임 할당이 있고 프로덕션에 쓸 품질이 아닙니다 (의도된 것)
- 조명은 Directional Light 1개 전제입니다. 애니 셰이딩에서 다중 광원은
  거의 항상 더 나빠지고, 폰에서 비싸기도 합니다
- 패스파인딩이 없습니다. 유닛은 X축으로만 이동하고 라인은 Z로 고정입니다.
  이 장르에는 충분하지만 자유 이동이 필요하면 구조를 바꿔야 합니다
- 적 AI는 아군과 같은 `UnitBrain`을 씁니다. 보스 패턴 같은 걸 넣으려면
  별도 브레인이 필요합니다
