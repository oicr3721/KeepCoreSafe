# 5. Core Block의 Tutorial / In-Game Prefab 분리 구조 확립

## 핵심 목표

Core Block은 더 이상 **하나의 Core Block Prefab에 Sprite만 변경해서 Tutorial / In-Game 상태를 표현하는 구조를 사용하지 않는다.**

앞으로는 다음과 같이 **Tutorial용 Core Block과 In-Game용 Core Block을 서로 다른 Prefab으로 관리**한다.

* Tutorial Core Block Prefab
* In-Game Core Block Prefab

두 Prefab은 각각 독립적인 GameObject 구조를 가질 수 있어야 한다.

특히 In-Game Core Block에는 향후 Core와 함께 동작하는 **Lily Animation Object 등의 자식 GameObject를 포함할 수 있어야 한다.**

따라서 Core의 종류를 단순히 Sprite 교체로 표현하는 기존 구조를 제거하고, **Prefab 자체를 교체하는 구조로 통일한다.**

# 5-1. Core Prefab 선택의 Source of Truth

Tutorial Core와 In-Game Core 중 어떤 Prefab을 사용할 것인지는 **반드시 BlockData를 기준으로 결정한다.**

현재 BlockData에는 이미 다음과 같이 Core Prefab에 대한 참조가 각각 존재한다.

* Tutorial용 Core Prefab 참조
* In-Game용 Core Prefab 참조

이 구조를 앞으로 Core Prefab 선택의 **단일 Source of Truth**로 사용한다.

즉:

> `BlockData`
> → 현재 필요한 Core 종류에 해당하는 Prefab 선택
> → 해당 Prefab을 Instantiate
> → Core Block GameObject로 사용

이 구조를 기본 원칙으로 한다.

## 5-2. Core 종류를 Sprite로 판단하지 않는다

다음과 같은 방식은 더 이상 사용하지 않는다.

* Core가 Tutorial Core인지 Sprite를 보고 판단
* Core가 In-Game Core인지 Sprite를 보고 판단
* Core의 상태에 따라 Sprite를 직접 교체해서 Tutorial / In-Game Core를 표현
* `CoreBlock_Legacy`를 기준으로 Core의 외형을 복구
* 특정 상황에서 기존 Core Sprite를 직접 할당

**Core의 종류는 Sprite가 아니라 BlockData와 해당 Prefab에 의해 결정되어야 한다.**

따라서 Core에 대한 외형 변경이 필요한 경우에도 먼저:

> "현재 Core가 어떤 Prefab에서 생성되었는가?"

를 기준으로 처리해야 한다.

# 5-3. Tutorial → In-Game 전환 방식 변경

프롤로그에서 Tutorial Core에서 In-Game Core로 전환되는 연출은 **Sprite 교체가 아니라 GameObject / Prefab 자체를 교체하는 방식**으로 구현한다.
굳이 Instantiate/Destroy 하지 않고 에디터 내에 미리 생성해둔 후 참조를 따라 SetActive(true)/(false) 하는 방식도 가능하다. (둘 중에 나은 쪽으로 구현할 것.)

예:

> Tutorial Core Prefab
> → 프롤로그 종료 시점
> → In-Game Core Prefab으로 교체
> → 이후 In-Game Core Prefab을 실제 게임의 Core로 사용

In-Game Core Prefab에는 Tutorial Core에는 존재하지 않는 자식 GameObject를 포함할 수 있다.

예:

```text
InGameCorePrefab
├── Core Sprite / Visual
├── Core 관련 VFX
├── Lily Animation Object
└── 기타 In-Game 전용 Components / Objects
```

따라서 Tutorial Core와 In-Game Core는 단순히 Sprite만 다른 객체가 아니라 **GameObject Hierarchy 자체가 달라도 정상적인 구조**여야 한다.

프롤로그에서 Core 전환이 필요한 경우에도 기존 Core Object의 Sprite만 변경하지 않는다.

가능하다면 기존 Core의 상태를 적절히 보존한 뒤:

1. Tutorial Core Object 정리
2. BlockData에서 In-Game Core Prefab 참조
3. In-Game Core Prefab 생성
4. 필요한 Runtime State를 새 Core에 전달
5. 이후 In-Game Core를 사용

하는 방식으로 처리한다.

단, 현재 프로젝트에 이미 더 적절한 Block 생성 / 교체 시스템이 존재한다면 해당 시스템을 우선 재사용한다.

# 5-4. `CoreBlock_Legacy` 사용 완전 제거

현재 `Sprites` 폴더에 있는:

`CoreBlock_Legacy`

는 더 이상 Core Block의 외형 표현에 사용하지 않는다.

이번 작업을 통해 프로젝트 전체에서 `CoreBlock_Legacy`를 사용하고 있는 코드를 먼저 검색한다.

특히 다음과 같은 로직을 확인한다.

* Core가 Damage를 받았을 때 Sprite 변경
* Core가 Shockwave를 발사할 때 Sprite 변경
* Core 상태 갱신 시 Sprite 변경
* Core 생성 시 Legacy Sprite 할당
* Core 초기화 시 Legacy Sprite 할당
* Core 복구 / Reset 시 Legacy Sprite 할당
* Tutorial → In-Game 전환 시 Legacy Sprite 할당
* 기타 Core 관련 Visual 갱신 코드

이러한 코드가 발견되면 **Sprite를 `CoreBlock_Legacy`로 변경하는 로직 자체를 제거하거나 새로운 Prefab 기반 구조에 맞게 수정한다.**

목표는 다음과 같다.

> **Core의 어떤 상태 변화가 발생하더라도 `CoreBlock_Legacy` Sprite가 다시 할당되지 않아야 한다.**

# 5-5. 현재 발생하고 있는 Sprite 되돌아감 문제 수정

현재 In-Game Core는 정상적으로 In-Game Core Prefab을 사용하더라도 특정 상황에서 기존 Sprite로 되돌아가는 문제가 있다.

대표적인 상황:

* Core가 Damage를 받을 때
* Core가 Shockwave를 발사할 때
* Core의 상태가 갱신될 때
* 기타 Core Visual Refresh가 발생할 때

이 과정에서 기존 `CoreBlock_Legacy` Sprite가 다시 할당되어 **In-Game Core Prefab에서 사용하던 외형이 사라지는 문제**가 발생한다.

이번 작업에서는 단순히 해당 한두 군데의 Sprite 할당만 제거하는 것이 아니라, **왜 Core 상태 갱신 과정에서 Legacy Sprite가 다시 적용되는지 전체 호출 흐름을 확인한다.**

예를 들어:

```text
Core Damage
→ Core Visual Refresh
→ Sprite 변경
→ CoreBlock_Legacy 할당
```

와 같은 기존 흐름이 있다면 해당 구조 자체를 확인하고 수정한다.

Core의 상태가 변경되더라도:

> **현재 사용 중인 Core Prefab의 Visual 구조를 유지해야 한다.**

Damage나 Shockwave 등의 상태 변화는 해당 Prefab이 제공하는 Visual / Animation / VFX를 변경하거나 재생하는 방식으로 처리하고, **Core 종류 자체를 Sprite 교체로 변경하지 않는다.**

# 5-6. Core Visual 갱신 시스템 점검

현재 프로젝트에 Core의 상태에 따라 Sprite를 갱신하는 공통 로직이 있다면 해당 로직을 반드시 확인한다.

특히 다음과 같은 형태의 코드가 있다면 주의한다.

* `spriteRenderer.sprite = ...`
* `SetSprite(...)`
* `SetCoreSprite(...)`
* `CoreBlock_Legacy` 참조
* Core 상태 변경 시 Sprite 재할당
* Damage / Shockwave 이벤트에서 Core Sprite 갱신

이러한 로직이 **Core Prefab 기반 구조와 충돌한다면 전면적으로 수정한다.**

단순히 `CoreBlock_Legacy` 참조만 삭제하고 다른 곳에서 동일한 방식으로 Sprite를 강제로 교체하는 구조를 남겨서는 안 된다.

이번 작업의 목적은:

> **Core의 외형을 Sprite 단위로 교체하는 기존 구조 자체에서 벗어나 Core Prefab을 기준으로 관리하는 것**

이다.

# 5-7. Runtime State 보존

Tutorial Core → In-Game Core 전환 시 Core의 Runtime State를 유지해야 하는 값이 있다면 기존 시스템을 확인하여 새 Prefab에 전달한다.

예:

* 현재 HP
* 최대 HP
* Core 관련 상태
* Shockwave 관련 상태
* 기타 Core Runtime State

단, 실제로 필요한 값만 기존 시스템을 확인하여 전달한다.

새 Core Prefab으로 교체한다고 해서 ScriptableObject / BlockData에 Runtime State를 저장하는 구조로 변경하지 않는다.

**BlockData는 어떤 Prefab을 사용할지를 결정하는 Source of Truth이고, Runtime State는 Runtime 객체가 관리한다.**

# 5-8. 기존 Block 시스템과의 일관성 유지

Core 역시 일반 Block과 마찬가지로 가능한 한 기존 Block 생성 / 배치 / 초기화 구조를 따른다.

단, Core는 일반 Block과 달리 Tutorial / In-Game Prefab이 분리되어 있고 Prefab의 Hierarchy가 서로 다를 수 있으므로, 이를 고려하여 기존 시스템을 확장한다.

새로운 Core 전용 시스템을 무조건 만드는 것이 아니라:

1. 현재 Block 생성 구조 확인
2. BlockData에서 Prefab을 선택하는 현재 방식 확인
3. Core만 필요한 추가 분기 확인
4. 기존 구조로 처리 가능한 경우 기존 구조 재사용
5. 필요한 부분만 최소한으로 확장

하는 순서로 작업한다.

# 5-9. 반드시 제거해야 하는 기존 구조

이번 작업을 통해 다음과 같은 구조가 남아 있지 않도록 한다.

* `CoreBlock_Legacy`를 Core의 기본 Sprite로 사용하는 코드
* Damage 발생 시 Legacy Sprite로 변경하는 코드
* Shockwave 발사 시 Legacy Sprite로 변경하는 코드
* Core 상태 변경 시 Legacy Sprite로 변경하는 코드
* Tutorial / In-Game Core를 Sprite 차이로 구분하는 코드
* Tutorial Core → In-Game Core 전환을 Sprite 교체로 처리하는 코드
* 현재 Core Prefab의 외형을 무시하고 Sprite를 강제로 재할당하는 코드

**다른 코드에서 이러한 구조가 발견되며 새로운 Core Prefab 구조와 충돌한다면 해당 규칙에 맞게 전면 수정한다.**

단순히 이번에 발견된 한 군데의 버그만 수정하는 것이 아니라, **프로젝트 전체에서 Core를 Sprite 기반으로 교체하고 있는 기존 흐름을 찾아 새로운 Prefab 기반 구조로 통일하는 것**이 목표다.

# 6. 구현 전후 검증

작업 전에 프로젝트 전체에서 다음을 검색한다.

* `CoreBlock_Legacy`
* Core Sprite 직접 할당
* Core Sprite 변경 함수
* Core Visual Refresh
* Damage 시 Core Visual 변경
* Shockwave 시 Core Visual 변경
* Tutorial Core 관련 생성 / 교체 코드
* In-Game Core 관련 생성 / 교체 코드

작업 후에는 다음을 확인한다.

### Tutorial

* Tutorial에서는 Tutorial Core Prefab이 생성되는가?
* Tutorial Core의 Hierarchy가 정상적으로 유지되는가?
* Tutorial 진행 중 Sprite를 Legacy Sprite로 교체하는 코드가 실행되지 않는가?

### 프롤로그 → In-Game

* Tutorial Core에서 In-Game Core Prefab으로 정상적으로 교체되는가?
* Sprite만 변경되는 것이 아니라 실제 GameObject / Prefab 자체가 변경되는가?
* In-Game Core Prefab의 자식 GameObject가 정상적으로 생성되는가?
* Lily Animation Object 등 In-Game 전용 Child Object가 정상적으로 동작하는가?

### In-Game

* Core가 Damage를 받아도 In-Game Core Prefab이 유지되는가?
* Core가 Shockwave를 발사해도 In-Game Core Prefab이 유지되는가?
* Core의 상태가 갱신되어도 Legacy Sprite로 되돌아가지 않는가?
* `CoreBlock_Legacy`가 더 이상 사용되지 않는가?
* 일반 Wave의 Core 동작에 문제가 없는가?

### 구조

* Core Prefab 선택이 BlockData를 Source of Truth로 사용하는가?
* Runtime State가 BlockData에 저장되는 구조로 변하지 않았는가?
* 기존 Block 시스템과 불필요하게 중복되는 새로운 Core 시스템이 생기지 않았는가?
* 기존 에디터에서 설정된 Inspector 값과 오브젝트 배치를 임의로 덮어쓰지 않았는가?

# 7. 최종 구현 원칙

이번 Core 작업의 핵심은 **"Core Sprite를 올바르게 바꾸는 것"이 아니다.**

Core의 종류와 구조를 다음과 같이 명확하게 분리하는 것이 목적이다.

```text
BlockData
 ├─ Tutorial Core Prefab
 └─ In-Game Core Prefab
          ↓
       Prefab 자체가
       Core의 종류와 구조를 결정
```

따라서:

> **Core의 종류 = BlockData가 선택한 Prefab**

이며,

> **Core의 외형 = 해당 Prefab의 Hierarchy / Components / Visual**

이다.

Core가 Damage를 받거나 Shockwave를 발사하는 등의 상태 변화가 발생하더라도 **현재 사용 중인 Prefab의 정체성을 Sprite 교체로 덮어쓰지 않는다.**

이번 작업에서 기존 구조가 이 원칙과 충돌하는 부분이 발견된다면, 단순히 해당 버그만 우회하지 말고 **새로운 Prefab 기반 Core 구조에 맞도록 관련 로직을 수정한다.**

단, 수정 범위는 현재 프로젝트 구조를 먼저 분석한 후 결정하며, 기존 시스템을 최대한 재사용하고 불필요한 신규 시스템과 중복 구조를 만들지 않는다.
