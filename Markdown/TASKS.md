# 작업 요청: Particle Effect 및 보급 블록 등장 시퀀스 구현

아래 3가지 작업을 진행한다.

## 1. Block 피격 Hit Particle 구현

`Prefabs/Particle` 내부에 있는 Particle System들을 실제 게임 내 이펙트로 활용한다.

### Dust Particle

`Dust Particle System`은 **Block이 피격당했을 때 재생되는 Hit Particle**로 사용한다.

기존 `Damage Feedback`에서 Hit Particle을 직접 직렬화하지 않은 것은, 기존 설계상 모든 Block 객체가 자신의 Particle System을 가지고 있고 해당 Particle System을 직접 Play하는 구조를 고려했기 때문이다.

다만 앞으로 Block의 종류와 개수가 많아질 것을 고려하여 다음 두 가지 구조를 검토한다.

### 방식 A - Block이 각자 Particle System 보유

각 Block이 자신의 Dust Particle System을 가지고 있다가 피격 시 직접 `Play()`한다.

### 방식 B - 중앙 Effect Manager + Particle Pool

별도의 Effect Manager를 만들고 Dust Particle System을 풀링한다.

Block이 피격될 경우:

1. Effect Manager에 자신의 월드 위치를 전달한다.
2. Effect Manager가 Pool에서 사용 가능한 Dust Particle System을 가져온다.
3. 해당 Particle System을 전달받은 위치에 배치한다.
4. Particle System을 `Play()`한다.
5. 재생이 끝난 Particle System은 Pool로 반환한다.

### 구현 방식 결정 기준

위 두 방식 중 하나를 **무조건 특정 방식으로 구현하지 않는다.**

현재 프로젝트의 예상 Block 수와 Particle System의 생성/활성화 비용을 고려하여 어느 방식이 더 적합한지 판단한 후 채택한다.

특히 다음을 고려한다.

* Block마다 Particle System GameObject를 하나씩 가지고 있는 것이 실제 성능상 큰 부담이 없는지
* Particle System의 `Play()` 자체는 가볍고, 굳이 풀링할 필요가 없는 수준인지
* Block 수가 증가했을 때 비활성 Particle System GameObject가 많아지는 것이 문제가 될 가능성이 있는지
* 반대로 Effect Manager + Pool을 도입했을 때 구조가 불필요하게 복잡해지는 것은 아닌지
* 향후 다른 Hit/Impact Effect에도 동일한 구조를 확장할 수 있는지

**성능상 Block별 Particle System 방식이 충분히 적합하다면 굳이 Pool을 도입하지 않는다.**

반대로 Block 수 증가를 고려했을 때 중앙 Effect Manager + Pool 구조가 명확하게 더 적합하다면 해당 구조를 구현한다.

최종적으로 선택한 이유를 코드 주석 또는 작업 결과에 간단히 남긴다.

### 추가 조건

* 기존 `Damage Feedback`의 책임을 불필요하게 확장하지 않는다.
* 기존 Block 피격 로직을 최대한 유지한다.
* 현재 프로젝트의 Prefab/Inspector 설정을 임의로 덮어쓰지 않는다.
* 기존에 설정되어 있는 Particle 관련 값이 있다면 먼저 확인하고 재사용한다.

---

## 2. Heal Particle 구현

`Heal Particle System`은 **Block이 회복될 때 재생되는 Particle**로 사용한다.

이 Particle은 별도의 중앙 Pool 시스템을 만들지 않고 **회복을 수행하는 Heal Block이 관리하는 방식**으로 구현한다.

동작은 다음과 같다.

1. Heal Block이 회복 타겟을 결정한다.
2. 회복 타겟 Block의 월드 위치를 가져온다.
3. Heal Block이 가지고 있는 `Heal Particle System`을 해당 위치로 이동시킨다.
4. `Play()`한다.
5. 회복 대상에게 실제 Heal 처리를 수행한다.

즉, Heal Particle은 **Heal Block → Heal Target 위치에 Particle을 배치 → Play**하는 구조로 한다.

이 부분은 현재 규모에서 별도의 Particle Pool을 만드는 것은 오히려 구조를 복잡하게 만들 가능성이 높으므로, 우선 Heal Block이 직접 관리하는 방식으로 구현한다.

단, 실제 현재 구조를 확인했을 때 이미 공통 Effect 시스템이 존재하거나 더 적합한 구조가 있다면 기존 아키텍처를 우선한다.

---

# 3. Supply Block 등장 시퀀스 추가

현재 게임 흐름에서 **Wave Clear 직후 Supply Block이 등장하면서 동시에 Block 지급/배치 단계가 진행되는 문제를 개선한다.**

Supply가 발생하는 경우 다음과 같은 순서로 진행되어야 한다.

> Wave Clear
> → Supply Block 등장 연출
> → 플레이어가 Supply Block을 인지할 수 있는 짧은 대기
> → Block 지급
> → Placement Phase 시작

핵심은 **Supply Block 등장 연출과 Block 지급/배치 단계가 동시에 진행되지 않는 것**이다.

플레이어가 "이번 웨이브가 끝났고, 새로운 보급이 발생했다"는 사실을 명확하게 인지할 수 있도록 별도의 짧은 시퀀스를 만든다.

## 등장 연출

당장은 기존 Merge 연출의 느낌을 참고해서 구현한다.

예를 들어:

* Supply Block이 등장할 위치를 강조
* Block이 생성되면서 Scale/Position 등을 이용한 간단한 등장 연출
* Particle 또는 기존 Merge 계열 Effect를 활용한 시각적 강조
* 등장 직후 짧은 유지 시간을 두어 플레이어가 Supply Block을 인지할 수 있도록 함

중요한 것은 복잡하고 화려한 연출을 만드는 것이 아니라,

**"Wave가 끝났다 → Supply가 발생했다 → 이제 배치 단계가 시작된다"**

라는 흐름을 플레이어가 확실히 이해할 수 있도록 만드는 것이다.

## 진행 제어

Supply Block이 생성되는 경우에는 다음 단계가 반드시 완료된 이후에 Placement Phase를 시작한다.

```text
Wave Clear
    ↓
Supply Block Spawn / 등장 연출 시작
    ↓
Supply Block 등장 연출 완료
    ↓
짧은 인지 대기
    ↓
Block 지급
    ↓
Placement Phase 시작
```

Supply가 없는 경우에는 기존 Wave Clear → Block 지급 → Placement Phase 흐름을 유지한다.

### 구현 시 주의사항

* 기존 Wave Clear 처리와 Placement Phase 진입 구조를 먼저 확인한다.
* 단순히 `WaitForSeconds`를 여러 곳에 추가해서 임시로 처리하지 말고, 현재 Wave/Phase 전환 구조에 맞는 방식으로 구현한다.
* Supply 등장 연출이 끝나기 전에 Placement 입력이 가능해지는 일이 없어야 한다.
* Supply Block이 생성되는 순간부터 Placement Phase가 시작되는 것으로 처리하지 않는다.
* 기존 Supply Block 생성/배치 규칙 자체는 변경하지 않는다.
* Supply가 없는 경우 기존 게임 흐름에 불필요한 지연이 발생하지 않도록 한다.
* 기존 Merge Effect를 재사용할 수 있다면 우선 재사용하고, 새로운 Effect를 불필요하게 중복 제작하지 않는다.

---

# 작업 원칙

작업 전에 반드시 현재 구현을 먼저 확인한다.

특히 다음을 확인한 후 수정한다.

* 현재 Block Damage Feedback 구조
* Block Heal 구조
* `Prefabs/Particle` 내부 Particle Prefab 구조
* Wave Clear → Placement Phase 전환 로직
* Supply Block 생성 로직
* Block 지급 로직
* Merge Effect 구현 방식
* 현재 Scene/Prefab/Inspector에 저장된 값

현재 프로젝트의 구조를 확인한 뒤 **기존 구조와 가장 자연스럽게 연결되는 방식으로 구현한다.**

기존 기능을 단순히 새 시스템으로 교체하기보다는 필요한 부분만 최소한으로 수정하고, 현재 동작하고 있는 다른 시스템에 영향을 주지 않도록 한다.

특히 Inspector에서 이미 설정되어 있는 값이나 에디터에서 의도적으로 배치된 오브젝트 위치/상태를 코드 수정 과정에서 임의로 덮어쓰지 않는다.

구현 후에는 다음을 확인한다.

* Block 피격 시 Dust Particle이 정상적으로 표시되는가
* 여러 Block이 동시에 피격되어도 Effect가 누락되지 않는가
* Block Heal 시 올바른 대상 위치에 Heal Particle이 표시되는가
* Supply Block이 생성될 때 등장 연출이 정상적으로 재생되는가
* Supply 등장 연출 중 Placement 입력이 가능하지 않은가
* Supply 등장 연출이 끝난 후에만 Block 지급 및 Placement Phase가 시작되는가
* Supply가 없는 일반 Wave Clear에는 기존 흐름이 유지되는가
* 기존 Merge 및 기타 Particle Effect에 문제가 발생하지 않는가

---

# 구현 결과

* 모든 Block 프리팹이 각자 비활성 대기 상태의 `Dust Particle System`을 소유하고, 기존 `DamageFeedback`의 선택적 파티클 슬롯을 통해 피격 시 독립적으로 재생한다. 현재 그리드 규모와 동시 피격 수에서는 중앙 풀보다 이 구성이 단순하고 충분히 저렴하므로 별도 Effect Manager는 추가하지 않았다.
* `HealerBlock`이 `Heal Particle System`을 직접 소유하며, 투사체가 대상에 도착한 뒤 파티클을 대상의 월드 위치로 옮겨 재생하고 나서 실제 Heal을 적용한다.
* 공급 이벤트 판정과 생성은 Wave Clear 직후의 전용 전환 단계에서 수행한다. 공급이 생성되면 재사용한 Merge 계열 Pulse/Shockwave/Burst 프리팹으로 등장 연출과 인지 대기를 마친 뒤에만 `Preparation`으로 전환하므로, 그 전에는 블록 지급과 Placement 입력이 시작되지 않는다.
* 공급이 생성되지 않거나 공급 시스템이 비활성인 경우에는 별도 지연 없이 기존처럼 즉시 `Preparation`으로 전환한다.
* 프리팹/씬 설정과 누락 검증은 `ParticleAndSupplySequenceSetup`에서 재실행할 수 있다.
