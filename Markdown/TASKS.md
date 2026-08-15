# WaveData 및 WaveDifficultyData 웨이브 밸런싱 구조 개편

현재 웨이브 밸런싱 구조를 **WaveDifficultyData와 WaveData의 역할을 분리하는 방식으로 개선**한다.

핵심적인 책임 분리는 다음과 같다.

* **WaveDifficultyData**: 해당 웨이브의 전체적인 난이도와 규모를 결정한다.
* **WaveData**: 해당 웨이브에 어떤 Enemy가 어떤 비율로 등장하는지를 결정한다.
* **EnemyData**: 개별 Enemy의 스탯 및 행동 데이터를 기존 구조 그대로 관리한다.

즉:

> WaveDifficultyData = "이번 웨이브를 얼마나 어렵게 만들 것인가?"
> WaveData = "이번 웨이브는 어떤 적 구성과 기믹을 가지는가?"
> EnemyData = "각 적은 어떻게 행동하고 얼마나 강한가?"

---

# 1. WaveData 추가

새로운 `WaveData` ScriptableObject를 생성한다.

WaveData는 Unity Editor에서 별도의 Asset으로 생성하고 Inspector를 통해 편집할 수 있어야 한다.

## Enemy Composition

WaveData는 다음 정보를 저장하는 리스트를 가진다.

* 기존 `EnemyData` 참조
* 해당 EnemyData의 Weight

즉, 다음과 같은 형태로 구성한다.

```text
EnemyData + Weight
EnemyData + Weight
EnemyData + Weight
...
```

예:

```text
MeleeEnemy   - Weight 60
FastEnemy    - Weight 30
SuicideEnemy - Weight 10
```

이 Weight를 기반으로 해당 Wave에서 등장할 Enemy 종류의 비율을 결정한다.

### Weight의 의미

Weight는 **해당 Wave에서 Enemy 종류를 선택하는 상대적인 등장 비율**이다.

예를 들어:

```text
MeleeEnemy   = 60
FastEnemy    = 30
TankEnemy    = 10
```

이라면 전체 Enemy가 대략 `60 : 30 : 10`의 비율로 구성되도록 한다.

**WaveData가 전체 Enemy 수를 결정하지는 않는다.**

전체 Enemy 수는 기존 `WaveDifficultyData`의 웨이브별 난이도 곡선에 의해 결정되는 값을 사용한다.

따라서:

* `WaveDifficultyData` → 총 몇 마리 등장하는가
* `WaveData` → 그 적들이 어떤 종류로 구성되는가

로 역할을 분리한다.

---

# 2. WaveData의 추가 정보

WaveData는 밸런싱 및 Editor 관리가 편하도록 최소한 식별 가능한 이름을 가질 수 있도록 한다.

예:

```text
Basic Wave
Fast Rush
Tank Wave
Explosive Assault
Mixed Wave
```

필요하다면 WaveData Inspector에 해당 Wave의 기믹이나 의도를 기록할 수 있는 `[TextArea]` 설명 필드를 추가한다.

예:

```text
Design Intent:
자폭 적을 통해 블록이 지나치게 밀집되지 않도록 압박한다.

Key Strategy:
자폭 적을 코어에서 멀리 떨어진 위치에서 자폭시키는 것이 중요하다.
```

이 정보는 게임 로직에서 사용하기 위한 것이 아니라, **향후 웨이브 밸런싱 및 관리 시 해당 Wave의 설계 의도를 확인하기 위한 메타데이터**다.

필요 이상으로 복잡한 필드는 추가하지 않는다.

---

# 3. Min Wave는 사용하지 않는다

WaveData에는 `Min Wave` 또는 이와 유사한 **등장 가능 웨이브 조건 필드를 추가하지 않는다.**

현재 웨이브 난이도는 `WaveDifficultyData`에서 전체적인 진행 곡선에 따라 관리하고 있으며, WaveData에서는 해당 웨이브의 **적 구성과 전략적 성격**만 관리하도록 역할을 명확히 분리한다.

따라서:

* WaveData → 적 구성
* WaveDifficultyData → 웨이브 진행 및 난이도

로 책임을 나눈다.

별도의 `Min Wave`, `Max Wave` 등의 등장 조건은 이번 구현에서는 추가하지 않는다.

---

# 4. WaveDifficultyData 변경

기존의 `WaveDifficultyData`를 WaveData 구조에 맞게 수정한다.

## 기존 Enemy 비율 관련 데이터 제거

기존 `WaveDifficultyData`에 Enemy 종류별 등장 비율을 직접 조정하는 필드가 있다면 제거한다.

예:

```text
MeleeEnemy Weight
FastEnemy Weight
TankEnemy Weight
...
```

이러한 Enemy 구성/비율 정보는 이제 `WaveData`가 담당한다.

따라서 `WaveDifficultyData`에서 Enemy Composition을 직접 관리하지 않는다.

---

# 5. WaveDifficultyData가 WaveData Pool을 관리

`WaveDifficultyData`는 `WaveData`의 리스트를 가진다.

두 개의 별도 리스트를 사용한다.

### Normal Wave List

일반적인 Wave에 사용할 `WaveData` 목록.

특수 웨이브가 아닌 일반 웨이브가 시작될 때 이 리스트에서 WaveData를 랜덤하게 선택한다.

예:

```text
Normal Wave List
- Basic Wave
- Fast Rush
- Tank Wave
- Mixed Wave
```

### Special Wave List

특수 웨이브에 사용할 `WaveData` 목록.

Special Wave가 발생하는 웨이브에서는 Normal Wave List가 아니라 이 리스트에서 WaveData를 랜덤하게 선택한다.

예:

```text
Special Wave List
- Explosive Assault
- Heavy Assault
- Swarm Assault
```

Normal / Special Wave 모두 동일한 `WaveData` 구조를 사용한다.

---

# 6. Special Wave Interval

`WaveDifficultyData`에 다음 직렬화 필드를 추가한다.

```text
Special Wave Interval
```

타입은 `int`.

이 값은 **몇 웨이브마다 Special Wave가 발생하는지**를 의미한다.

예:

```text
Special Wave Interval = 5
```

이라면:

```text
Wave 1  → Normal
Wave 2  → Normal
Wave 3  → Normal
Wave 4  → Normal
Wave 5  → Special

Wave 6  → Normal
Wave 7  → Normal
Wave 8  → Normal
Wave 9  → Normal
Wave 10 → Special
```

과 같이 동작한다.

Special Wave가 발생하는 웨이브에서는 반드시 `Special Wave List`에서 WaveData를 선택한다.

그 외의 웨이브에서는 `Normal Wave List`에서 WaveData를 선택한다.

---

# 7. WaveDifficultyData의 난이도 곡선은 기존 구조 유지

기존 `WaveDifficultyData`에서 관리하고 있는 **웨이브별 재미 곡선 / 난이도 곡선**은 유지한다.

특히 다음 값들은 기존 시스템의 밸런싱 방식을 그대로 사용한다.

* 웨이브별 전체 Enemy 등장 수
* 웨이브별 Required Energy
* 기타 기존 난이도 조정 데이터

게임이 진행될수록:

* 전체 Enemy 수가 증가하고
* Required Energy도 증가한다.

단순한 등속 증가가 아니라 기존에 설계한 **재미 곡선에 따른 증가 방식**을 유지한다.

이번 작업에서는 해당 난이도 곡선 자체를 변경하지 않는다.

---

# 8. 최종 웨이브 결정 과정

한 웨이브가 시작될 때 다음 순서로 데이터를 결정한다.

### 1. 현재 웨이브의 난이도 데이터 확인

`WaveDifficultyData`에서 현재 웨이브에 해당하는:

* Total Enemy Count
* Required Energy
* 기타 기존 난이도 데이터

를 가져온다.

### 2. Special Wave 여부 확인

`Special Wave Interval`을 기준으로 현재 웨이브가 Special Wave인지 판단한다.

### 3. WaveData 선택

Special Wave라면:

```text
Special Wave List
```

에서 랜덤 선택.

Normal Wave라면:

```text
Normal Wave List
```

에서 랜덤 선택.

### 4. Enemy 구성

선택된 `WaveData`의 Enemy + Weight 목록을 사용하여 해당 웨이브의 Enemy 종류를 결정한다.

### 5. 전체 Enemy 수 적용

최종 Enemy 수는 `WaveDifficultyData`에서 결정된 현재 웨이브의 Total Enemy Count를 사용한다.

즉:

```text
WaveDifficultyData
    ↓
"이번 웨이브는 총 20마리"

WaveData
    ↓
"20마리를 Melee 60 / Fast 30 / Suicide 10 비율로 구성"

EnemyData
    ↓
"각 Enemy의 실제 스탯과 행동"
```

의 구조가 된다.

---

# 9. 랜덤 WaveData 선택 시 주의사항

Normal / Special Wave List에서 WaveData를 랜덤으로 선택하되, **직전 웨이브와 동일한 WaveData가 연속해서 선택되는 상황은 가능하면 방지한다.**

예를 들어:

```text
Wave 3 → Fast Rush
Wave 4 → Fast Rush
```

처럼 동일한 WaveData가 연속 등장하는 것을 기본적으로 피하도록 한다.

단, 리스트에 선택 가능한 WaveData가 하나뿐인 경우에는 해당 WaveData를 그대로 사용한다.

이를 통해 랜덤 선택으로 인해 동일한 패턴이 연속 발생하는 것을 줄인다.

---

# 10. 기존 시스템과의 호환성

이번 작업에서는 기존 `EnemyData`를 새로 만들거나 구조를 변경하지 않는다.

현재 사용 중인 `EnemyData` ScriptableObject를 `WaveData`가 참조하도록 한다.

또한 기존:

* Enemy Spawn 시스템
* Wave 진행 시스템
* EnemyData
* Difficulty Curve
* Required Energy 계산
* Total Enemy Count 계산

등을 먼저 확인하고, 현재 프로젝트의 구조를 최대한 유지하면서 WaveData 계층을 추가한다.

기존 기능을 불필요하게 재작성하지 않는다.

---

# 11. 최종 데이터 구조

최종적으로 다음과 같은 책임 분리를 갖도록 한다.

```text
EnemyData
└─ 개별 Enemy의 스탯 / 행동 / 특성


WaveData
├─ Wave 이름 / 식별 정보
├─ EnemyData + Weight 리스트
└─ (선택) Design Intent / Key Strategy


WaveDifficultyData
├─ 기존 난이도 곡선 데이터
│   ├─ Total Enemy Count
│   ├─ Required Energy
│   └─ 기타 기존 난이도 관련 데이터
│
├─ Normal Wave List
│   ├─ WaveData
│   ├─ WaveData
│   └─ ...
│
├─ Special Wave List
│   ├─ WaveData
│   ├─ WaveData
│   └─ ...
│
└─ Special Wave Interval
```

핵심적인 책임은 다음과 같이 유지한다.

> **EnemyData = "적 하나는 어떤 적인가?"**

> **WaveData = "이번 웨이브는 어떤 적 구성과 전략을 요구하는가?"**

> **WaveDifficultyData = "게임 진행상 이번 웨이브는 얼마나 많은 부하를 주는가?"**

이 구조를 기준으로 구현할 것.

구현 전에 현재 Wave / Enemy / Difficulty 관련 코드를 확인하여 실제 프로젝트 구조와 위 설계가 충돌하는 부분이 있는지 먼저 확인하고, 필요한 경우 기존 구조를 최대한 보존하는 방향으로 조정한다.

또한 기존 Scene, Prefab, ScriptableObject의 Inspector 설정값은 임의로 초기화하거나 덮어쓰지 않는다.

---

# 구현 결과

* `WaveData` ScriptableObject를 추가하고 식별 이름, Design Intent, Key Strategy, `EnemyData + Weight` 리스트를 Inspector에서 편집할 수 있도록 구성했다. Min/Max Wave 조건은 추가하지 않았다.
* 가중치는 현재 Wave의 전체 Enemy 수를 최대 나머지 방식으로 배분해 전체 구성이 설정 비율에 가깝게 유지되며, 최종 Spawn 순서는 별도로 Shuffle한다.
* `WaveDifficultyData`에서 Ranged/Suicide 비율과 추가 성장 필드를 제거하고 Normal Wave List, Special Wave List, Special Wave Interval을 추가했다. Enemy 수, Required Energy, Spawn Interval/Margin 및 Late Game 이후 성장 곡선은 기존 값을 유지한다.
* `WaveDifficultyController`가 직전 WaveData를 런타임 상태로 보관한다. 현재 Pool에 다른 유효 Asset이 있으면 직전 Asset을 제외하고 선택하며, 하나뿐이면 반복을 허용한다.
* `WaveManager`의 Melee/Ranged/Suicide 분기 기반 Spawn 요청을 범용 `EnemyData` 기반 구조로 교체했다. 따라서 WaveData에 기존 또는 향후 EnemyData를 추가해도 Spawn 파이프라인 변경이 필요 없다.
* Main Game에는 Basic Wave, Ranged Pressure, Mixed Assault를 Normal Pool로, Explosive Assault와 Ranged Barrage를 Special Pool로 생성하고 Special Interval을 5로 설정했다.
* Tutorial에는 기존 진행을 보존하는 Melee 전용 Tutorial Basic Wave를 생성하고 Special Interval을 0으로 설정했다.
* Game/Tutorial Scene에는 비정상적으로 비어 있는 WaveData에만 사용되는 Melee fallback 참조를 추가했으며, 기존 난이도 곡선과 Scene의 다른 Inspector 값은 유지했다.

## 자폭 적 Block 도달 조건 보정

* 자폭 적이 경로 Cell을 모두 소진했을 때 무조건 자폭하던 조건과 방해 Block을 직접 공격하던 동작을 제거했다.
* 다음 경로 Cell의 Block 또는 최종 경로 목표에 인접해 Melee Enemy라면 공격을 시작할 동일한 조건에서, 공격 대신 자폭 준비를 시작한다.
* 비치명 피해 후 HP가 설정 비율 이하가 되었을 때 현재 위치에서 자폭하는 독립 조건은 그대로 유지한다.
