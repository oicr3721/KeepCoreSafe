# 구현 상태

완료 (2026-08-17)

- Tutorial Director에서 Lily의 Core 기준 Grid Offset과 Transform 참조를 편집할 수 있다.
- Lily 셀은 공통 배치 검증 경로에서 차단되며, 실제 시도 시 현지화 대사와 `Happy` 반응을 재생한다.
- Prologue의 설치된 Lily 회수 입력을 좌클릭으로 수정했다.
- Lily의 비-Core 재배치/재회수, Core 강조, 융합 VFX와 GameScene 전환 흐름을 검증했다.
- 후속 변경으로 White Flash를 제거하고 기존 검정 Scene Transition을 그대로 사용한다.
- Lily 클릭 즉시 선택 상태가 되며, 들어올림/배치 Clip을 각각 재생한다.
- Prologue 카메라는 Core와 Inspector의 Camera Offset을 기준으로 시작한다.

---

좋아. 지금 상태가 **Prologue Scene이 이미 어느 정도 구현되어 있지만 동작이 완전하지 않은 상태**라서, 코덱스에게 단순 신규 구현으로 요청하기보다는 **현재 구현을 먼저 검수하고, 기존 구조를 최대한 유지하면서 누락/버그를 수정하라**는 방향으로 작성하는 게 좋겠어.

아래처럼 전달하면 될 것 같아.

---

# Tutorial / Prologue 인터랙션 및 연출 검수·구현

## 작업 목표

Tutorial Scene에서 Lily의 위치와 배치 충돌 처리를 추가하고, 이후 Prologue Scene에서 Lily를 직접 Core에 동면시키는 인터랙티브 연출이 **현재 기획대로 완전히 동작하도록 검수 및 수정한다.**

Prologue Scene은 현재 일부 구현되어 있으므로, **처음부터 새로 만드는 것이 아니라 현재 구현을 먼저 확인하고 기존 구조를 최대한 재사용한다.**

특히 현재 Prologue Scene에서 **쓰러져 있는 Lily를 클릭해도 수거되지 않는 문제**가 있으므로 해당 문제를 반드시 확인하고 수정한다.

---

# 1. Tutorial Scene - Lily 위치 및 배치 충돌 처리

Tutorial Scene에서 Lily가 특정 Grid Cell에 서 있도록 한다.

### Lily 위치 설정

Lily가 서 있을 위치는 `Tutorial Director`에서 에디터상으로 편집 가능하도록 한다.

Core를 기준으로 Lily의 위치를 다음과 같이 설정할 수 있도록 한다.

```text
Lily Offset X
Lily Offset Y
```

또는 적절한 Vector2/Vector2Int 직렬화 필드를 사용한다.

즉,

> Core 위치 + 지정된 X/Y Offset = Lily 위치

가 되도록 한다.

### 요구사항

* `Tutorial Director`가 씬 내의 Lily 객체를 Serialized Reference로 참조한다.
* Tutorial Scene 시작 시 `Tutorial Director`가 설정된 Offset을 기준으로 Lily의 위치를 결정한다.
* Lily는 해당 Grid Cell에 정확하게 배치되어야 한다.
* Lily의 위치를 Scene View에서 직접 수정하는 것이 아니라, **Tutorial Director의 Inspector에서 Core 기준 Offset을 수정하는 방식**을 기본으로 한다.
* 기존 Grid/Block 위치 계산 방식을 우선 재사용한다.

---

# 2. Tutorial Scene - Lily가 있는 칸에 블록 배치 차단

플레이어가 Lily가 서 있는 Grid Cell에 블록을 배치하려고 하면 해당 배치를 차단한다.

배치가 차단될 때 Lily가 다음 대사를 출력한다.

> **"엥?! 여기엔 내가 서 있잖아…!"**

동시에 Lily Animator에:

```csharp
Animator.SetTrigger("Happy");
```

를 실행한다.

`Happy`라는 이름이 실제 상황과 완전히 일치하지는 않지만, 현재 애니메이션이 Lily의 점프/반응 연출로 사용하기 적합하므로 그대로 사용한다.

### 중요

이 상황에서는 **블록 배치가 실제로 이루어지면 안 된다.**

즉:

```text
플레이어가 Lily가 있는 칸 선택
↓
배치 가능 여부 검사
↓
Lily가 해당 위치에 있음
↓
배치 차단
↓
Lily 대사 출력
↓
Happy Trigger
```

가 되어야 한다.

기존 Block Placement 시스템에 이미 배치 가능 여부를 검사하는 지점이 있다면 해당 로직을 우선 활용한다.

별도의 중복 배치 시스템을 만들지 않는다.

---

# 3. Prologue Scene - 현재 구현 검수

현재 Prologue Scene에는 아래 기능들이 일부 구현되어 있으므로 **먼저 현재 씬과 관련 스크립트를 확인한다.**

특히 다음 문제를 반드시 검증한다.

### 현재 알려진 문제

> Prologue Scene에서 쓰러져 있는 Lily를 클릭해도 Lily가 수거되지 않는다.

Lily를 클릭하면 정상적으로:

```text
Lily 클릭
↓
Lily가 즉시 선택되어 들어올려짐
```

이 동작이 이루어져야 한다.

마우스 좌클릭/우클릭 중 기존 프로젝트의 배치 선택 방식과 충돌하지 않는 적절한 입력을 사용한다.

**이미 구현된 Lily 수거/배치 로직이 있다면 원인을 분석하여 수정하고, 동일 기능을 새로 중복 구현하지 않는다.**

---

# 4. Prologue Scene - 시작 상태

튜토리얼의 기존 연출은 그대로 유지한다.

```text
Tutorial 완료
↓
기존 Glitch / Error 연출
↓
기존 암전
```

**여기까지의 기존 연출은 변경하지 않는다.**

암전이 끝난 이후부터 Prologue Scene의 새로운 연출이 시작된다.

### 화면 구성

암전이 끝나면 튜토리얼과 완전히 다른 분위기를 만든다.

* 튜토리얼 맵의 조명을 붉은 분위기로 변경
* 기존 블록은 제거
* Core Block 하나만 남김
* Core 앞쪽, 즉 **Core의 아래쪽 Grid Cell**에 Lily 배치
* Lily는 `Coma` 상태

Lily의 Animator에는:

```csharp
Animator.SetTrigger("Coma");
```

를 사용한다.

---

# 5. Lily 동면 목표 안내

Prologue Scene이 화면에 나타난 직후 바로 문구를 출력하지 말고, 플레이어가 화면을 인식할 수 있도록 **짧은 지연 시간**을 둔다.

그 후 다음 문구를 표시한다.

> **"Lily를 Core에 동면시키면 살릴 수 있다."**

동시에 Lily가 위치한 Grid Cell에 기존 Highlight 시스템을 사용하여 강조한다.

목표는 플레이어가 별도의 설명 없이:

> **"Lily를 Core로 옮겨야 하는구나."**

라고 이해하는 것이다.

---

# 6. Prologue Scene - Lily 선택 / 배치

플레이어가 쓰러져 있는 Lily를 클릭하면 Lily를 선택한다.

클릭 즉시 Lily를 들어올린 선택 상태로 전환하며 별도의 배치 목록 버튼을 거치지 않는다.

### Lily 배치 규칙

* Lily를 클릭하면 즉시 선택 상태가 된다.
* 기존 블록을 배치하는 것과 유사한 방식으로 Lily를 배치할 수 있다.
* Lily는 Core가 아닌 위치에도 배치할 수 있다.
* 설치된 Lily는 다시 클릭하여 즉시 들어올릴 수 있다.
* 설치된 Lily가 있는 Grid Cell에는 계속 Highlight를 표시한다.
* Lily가 Core가 아닌 위치에 배치되었을 경우에도 Prologue는 완료되지 않는다.
* Lily를 선택한 상태에서는 Core의 Grid Cell에 Highlight를 표시한다.

가능하면 기존 Block Placement / Selection / Highlight 시스템을 최대한 재사용한다.

---

# 7. Lily 선택 시 Core 강조

Lily가 현재 선택된 상태라면 Core를 목표 위치로 명확하게 표시한다.

기존 Highlight / Effect Cell 시스템을 사용한다.

```text
Lily 선택
↓
Core Highlight 활성화
↓
플레이어가 Core 위치에 Lily 배치
```

가 자연스럽게 이어져야 한다.

---

# 8. Lily를 Core에 배치했을 때의 동면 연출

플레이어가 Lily를 Core 위치에 배치하면 일반적인 블록 배치 처리를 하지 않고 **Prologue 전용 동면 연출**을 실행한다.

### 연출 흐름

```text
Lily가 Core 위에 배치
↓
Lily가 Core 위에서 둥실둥실 떠오름
↓
Core Energy가 반응
↓
Energy Pulse
↓
Lily 주변에 빛/Particle 발생
↓
Lily와 Core가 강하게 빛남
↓
Lily가 Core의 에너지와 융합되는 듯한 연출
↓
융합 연출 완료
↓
기존 검정 Scene Transition
↓
GameScene 시작
```

---

# 9. 기존 VFX 재사용

새로운 VFX를 무조건 제작하지 말고 현재 프로젝트의 기존 연출을 우선 재사용한다.

다음 VFX를 검토한다.

* Core Energy 충격파
* Merge Burst
* Mask Flash
* Energy Pulse
* Light Particle
* 기존 Particle System

필요하다면 기존 VFX의 Scale / Alpha / Position 등을 조합하여 Prologue 전용 연출을 만든다.

연출의 핵심은:

> **Lily가 Core에 흡수되어 사라지는 것이 아니라, Core의 에너지와 융합되어 보호/동면되는 느낌**

이 되도록 한다.

---

# 10. Scene Transition

White Flash는 사용하지 않고 현재 프로젝트에서 실제 사용 중인 **기존 검정 Scene Transition Fade 시스템을 그대로 재사용한다.**

### 전환 흐름

```text
기존 Transition Image
Color = Black

        ↓

Prologue 동면 연출 시작

        ↓

GameScene Load

        ↓

기존 Black Fade Out
```

중요한 것은 **Scene Transition 시스템 자체를 새로 만들지 않는 것**이다.

Transition Image 색상은 변경하지 않는다.

이후 다른 Scene Transition에 영향을 주면 안 된다.

---

# 11. 기존 구현 및 버그 검수

이번 작업은 단순 신규 기능 추가가 아니라 **현재 Prologue 구현의 검수 및 완성 작업**이다.

특히 아래 항목을 실제 플레이하면서 모두 확인한다.

### Tutorial

* [ ] Tutorial Director에서 Core 기준 Lily X/Y 위치를 설정할 수 있는가?
* [ ] Tutorial 시작 시 Lily가 해당 위치로 이동하는가?
* [ ] Tutorial Director가 씬의 Lily 객체를 정상적으로 참조하는가?
* [ ] Lily가 있는 칸에 블록을 배치할 수 없는가?
* [ ] 배치 시 Lily 대사가 출력되는가?
* [ ] 배치 시 `Happy` Trigger가 실행되는가?
* [ ] 실제 블록 배치가 차단되는가?

### Prologue

* [ ] 기존 Glitch / Error → 암전 연출이 그대로 유지되는가?
* [ ] 암전 이후 붉고 절망적인 분위기가 조성되는가?
* [ ] Core 하나만 남는가?
* [ ] Lily가 Core 아래 칸에 배치되는가?
* [ ] Lily가 `Coma` 상태인가?
* [ ] 안내 문구가 정상적으로 출력되는가?
* [ ] Lily 위치에 Highlight가 표시되는가?
* [ ] Lily 클릭 시 정상적으로 수거되는가?
* [ ] Lily 클릭 즉시 선택 상태가 되는가?
* [ ] Lily를 다시 배치할 수 있는가?
* [ ] 설치된 Lily를 다시 수거할 수 있는가?
* [ ] 설치된 Lily 위치의 Highlight가 정상적으로 유지되는가?
* [ ] Lily 선택 중 Core Highlight가 표시되는가?
* [ ] Lily를 Core에 배치하면 동면 연출이 시작되는가?
* [ ] Core Energy / Merge Burst 등의 기존 VFX가 자연스럽게 연계되는가?
* [ ] GameScene으로 정상 전환되는가?
* [ ] 기존 Black Scene Transition이 정상 동작하는가?
* [ ] Prologue 종료 후 기존 게임 Scene Transition에 영향이 없는가?

---

# 12. 구현 원칙

* 구현 전에 반드시 현재 `Tutorial Director`, Lily 관련 스크립트, Block Placement, Highlight, Scene Transition, Prologue 관련 스크립트를 먼저 확인한다.
* 현재 구현된 Prologue 기능을 함부로 삭제하고 처음부터 재작성하지 않는다.
* 기존 시스템을 확장할 수 있다면 확장한다.
* 동일한 기능을 수행하는 별도 시스템을 중복해서 만들지 않는다.
* 기존 Animator의 `Happy`, `Idle`, `Coma` Trigger를 그대로 사용한다.
* 기존 Highlight / Effect Cell 시스템을 우선 사용한다.
* 기존 Scene Transition 시스템을 재사용한다.
* 기존 VFX를 우선 재사용한다.
* 기존 Localization 시스템을 사용한다.
* Scene / Prefab / Inspector에 사용자가 설정해둔 값은 임의로 초기화하거나 덮어쓰지 않는다.
* 코드 수정 전에 현재 Scene과 Prefab의 실제 상태를 확인한다.
* 연출 관련 값은 필요한 범위에서 간단한 `[SerializeField]`로 Inspector에서 조정 가능하게 한다.
* 연출에 DOTween이 적합하면 사용하되, 단순한 연출에 과도한 구조를 만들 필요는 없다.
* 구현 완료 후 **Tutorial 시작 → Tutorial 완료 → Glitch → Prologue → Lily 선택 → Lily 배치 → Core 동면 → 기존 Transition → GameScene 진입**을 실제 플레이 흐름으로 검증한다.

### 최종 목표

이번 작업의 최종 목표는 단순히 기능을 추가하는 것이 아니라,

> **튜토리얼에서 Lily와 상호작용한다 → 튜토리얼 종료 후 Lily가 위기에 처한다 → 플레이어가 직접 Lily를 Core로 옮긴다 → Lily가 Core에 동면된다 → 기존 Transition → 실제 게임 시작**

이라는 하나의 자연스러운 플레이 흐름을 완성하는 것이다.
