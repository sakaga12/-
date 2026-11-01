# 03. 전투 수식과 밸런스 조정

모든 숫자는 두 파일에만 있습니다. 밸런스를 만지고 싶으면 다른 파일은 열 필요가 없습니다.

- `Scripts/Core/DamageTable.cs` — 상성표, 지형 보정, 방어 곡선, 코스트 경제
- `Scripts/Setup/SampleRoster.cs` — 개별 캐릭터/적 스탯

## 데미지 수식

`DamageCalculator.Compute()`의 실제 순서입니다.

```
1. 명중 판정      hitChance = ACC / (ACC + EVA * 0.5)        [0.25 ~ 1.0 클램프]
                  실패 → MISS, 데미지 0

2. 엄폐 판정      엄폐 중이고 공격자가 근접이 아니면
                  coverRate = 방어자 지형등급의 엄폐율
                  적중 → BLOCK, 데미지 0

3. 기본 데미지    ATK × powerRatio × 상성배율 × (1 + 공격자 지형보정) × 버프

4. 방어 감쇠      × K / (K + DEF)                            K = 1200

5. 피해량 배율    × 방어자 피해증감 버프

6. 안정성 롤      × Random(stability, 1.0)

7. 치명타         critChance = (CRIT - RESIST) / ((CRIT - RESIST) + 1000)
                  적중 시 × critDamage
```

### 방어 곡선 읽는 법

`K / (K + DEF)`에서 `K = 1200`입니다. 즉 **DEF가 1200이면 받는 데미지가 정확히 절반**입니다.
선형이 아니라 점근이라서 DEF를 아무리 올려도 0이 되지 않습니다. 탱커가 단단하지만
무적은 아니게 하려는 설계입니다.

`DamageTable.DefenceConstant`를 올리면 방어력의 가치가 전체적으로 떨어집니다.

### 상성표

| 공격 \ 방어 | Light | Heavy | Special | Elastic |
|---|---|---|---|---|
| **Explosive** | **2.00** | 0.50 | 1.00 | 0.90 |
| **Piercing** | 0.50 | **2.00** | 1.00 | 0.90 |
| **Mystic** | 1.00 | 1.00 | **2.00** | 0.90 |
| **Sonic** | 1.00 | 1.00 | 0.80 | **2.00** |

2배와 0.5배 사이가 4배 차이입니다. 이게 "스테이지마다 다른 편성을 꺼내야 하는" 압력의
원천이고, 이 장르의 수집 동기 전체가 여기 걸려 있습니다. 함부로 완화하지 마세요.

### 지형 적성

| 등급 | 공격 보정 | 엄폐율 |
|---|---|---|
| S | +30% | 80% |
| A | +20% | 75% |
| B | +10% | 70% |
| C | 0% | 60% |
| D | **-20%** | **0%** |

D등급은 공격력이 깎이는 데서 끝나지 않습니다. **엄폐를 전혀 못 합니다.** 적의 모든 탄이
그대로 들어옵니다. 스탯이 같아도 지형 하나로 전멸하는 이유입니다.

주의: 엄폐 판정은 **근접(Melee) 공격자에게는 적용되지 않습니다.** 근접 적은 엄폐를
무시하고 때립니다. 그래서 전열 구성이 필요합니다.

## 코스트 경제

| 항목 | 값 | 위치 |
|---|---|---|
| 최대 코스트 | 10 | `DamageTable.MaxCost` |
| 초당 회복 | 0.70 | `DamageTable.BaseCostRegenPerSecond` |
| 시작 코스트 | 3 | `DamageTable.StartingCost` |
| 카드 재사용 대기 | 6초 | `DamageTable.DefaultCardCooldown` |
| 카드 버리기 비용 | 1 | `SkillCardManager.DiscardCost` |
| 전투 제한 시간 | 180초 | `DamageTable.DefaultBattleSeconds` |

초당 0.70이면 3코스트 스킬을 약 4.3초마다 쓸 수 있습니다. 이걸 올리면 게임이
스킬 난사가 되고, 내리면 평타 비중이 커지면서 늘어집니다. **이 숫자 하나가
전투 템포를 가장 크게 좌우합니다.**

EX 스킬 발동 중(`ExCinematic` 페이즈)에는 코스트 회복이 멈춥니다. 슬로모 동안
공짜로 코스트가 차는 걸 막기 위한 것입니다.

## EX 스킬 종류

`UnitStats.exKind`로 지정합니다. `ExSkillRunner.ResolveEffect()`의 switch에 구현되어 있습니다.

| 종류 | 동작 | 주요 파라미터 |
|---|---|---|
| `SingleBurst` | 현재 타겟에게 1타 | `exPowerRatio` |
| `MultiHit` | 현재 타겟에게 N타 분산 | `exPowerRatio`, `exHitCount` |
| `AreaBlast` | 타겟 주변 반경 내 전원 | `exPowerRatio`, `exRadius`, `exHitCount` |
| `Heal` | 가장 많이 다친 아군 회복 | `exPowerRatio`, `healPower` |
| `AttackBuff` | 전원 공격력 +(ratio × 10%), 12초 | `exPowerRatio` |
| `Shield` | 전원 피해 -(ratio × 8%), 12초 | `exPowerRatio` |

EX 스킬은 **엄폐를 무시하고 절대 미스하지 않습니다.** 코스트를 지불하는 대가입니다.
(`ExSkillRunner.HitOne()`에서 `defenderInCover = false`, `cannotMiss = true`)

새 스킬 타입을 추가하려면 `ExSkillKind` enum에 값을 넣고 switch에 case를 추가하면 됩니다.
그 외에는 아무것도 건드릴 필요가 없습니다.

## 스트라이커 vs 스페셜

| | 스트라이커 | 스페셜 |
|---|---|---|
| EX 카드 | 있음 (수동, 코스트 소모) | **없음** |
| 스킬 발동 | 플레이어가 카드 클릭 | `SpecialSkillDriver`가 타이머로 자동, 무료 |
| 전진 | 엄폐물까지 전진 | **전진하지 않음**, 후방 고정 |
| 엄폐 | 엄폐물 점유 필요 | 항상 엄폐 상태로 취급 |

이 비대칭이 편성에 의미를 만듭니다. 스페셜의 자동 스킬 간격은
`SpecialSkillDriver.Interval`(기본 14초)로 조절합니다.

## 빠른 튜닝 레시피

**전투가 너무 빨리 끝난다** → `SampleRoster`의 적 `maxHp`를 1.5~2배.
`attack`은 건드리지 마세요. 적 공격력을 올리면 아군이 녹아서 전투가 더 짧아집니다.

**전투가 늘어진다** → `BaseCostRegenPerSecond`를 0.85로. 스킬 사이클이 빨라집니다.

**엄폐가 너무 강하다** → `TerrainCoverRate` 전체를 10%씩 내리거나,
적 편성에 근접(Melee) 유닛을 늘립니다.

**치명타가 안 터진다** → `CritConstant`(1000)를 내립니다. 600으로 하면
CRIT 300인 캐릭터의 치명타율이 23% → 33%로 올라갑니다.

**캐릭터 간 격차가 너무 크다** → `stability`를 확인하세요. 0.68 같은 낮은 값은
데미지가 32%까지 흔들립니다. 안정적이어야 하는 캐릭터는 0.9 이상으로.
