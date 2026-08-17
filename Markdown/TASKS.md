# 빌드 전 프로젝트 리소스 및 불필요한 파일 전체 정리 요청

## 목적

현재 프로젝트는 개발 과정에서 여러 기능을 구현하고 테스트하면서 생성된 Script, Prefab, Sprite, Texture, Material, Particle, Audio, Editor Tool 등의 리소스가 누적되어 있다.

이제 실제 빌드를 진행하기 전에 **현재 게임에서 사용되지 않는 불필요한 리소스와 일회성 개발 파일을 전체적으로 정리한다.**

핵심 목표는:

> 현재 게임에서 실제로 사용되는 Asset은 유지
> 더 이상 사용되지 않는 Asset은 제거
> 과거 작업에서 생성된 일회성 파일과 테스트 리소스 제거
> 삭제 후 Missing Reference / Compile Error / Runtime Error가 없는지 검증
> 최종적으로 빌드에 불필요한 리소스가 최대한 남지 않도록 정리

이다.

---

# 1. 프로젝트 전체를 먼저 조사

특정 폴더만 보는 것이 아니라 `Assets` 전체를 대상으로 현재 사용 여부를 조사한다.

특히 다음 유형을 모두 확인한다.

* Sprite
* Texture
* Sprite Sheet
* Material
* Shader
* Particle System / Particle 관련 Prefab
* VFX
* Audio Clip
* Audio 관련 Prefab
* Animation
* Animator Controller
* Timeline
* Prefab
* ScriptableObject
* Script
* Editor Script
* UI Asset
* Font Asset
* TMP Font Atlas
* 테스트용 Asset
* 임시 생성물
* 과거 시스템의 잔여 Asset

---

# 2. 사용되지 않는 Asset 찾기

현재 프로젝트에서 실제로 사용되지 않는 Asset을 찾는다.

다음과 같은 Asset은 삭제 후보로 분류한다.

* 어떤 Scene에서도 사용되지 않음
* 어떤 Prefab에서도 사용되지 않음
* 어떤 ScriptableObject에서도 사용되지 않음
* 어떤 Script에서도 참조되지 않음
* Addressables에서 사용되지 않음
* Resources에서 런타임 로드되지 않음
* 현재 게임의 UI/Gameplay/VFX/Audio 등에 사용되지 않음
* 과거 기능을 위해 만들어졌지만 해당 기능이 제거됨
* 테스트용으로만 생성됨
* 작업 과정에서 임시로 생성됨
* 이름이 비슷한 중복 Asset 중 현재 사용되지 않는 것

특히 개발 과정에서 생성된 **사용하지 않는 Sprite와 Texture를 적극적으로 확인한다.**

---

# 3. Sprite / Texture 집중 정리

이번 정리에서 특히 Sprite와 Texture를 꼼꼼하게 확인한다.

예를 들어:

```text
Sprites/
VFX/
Particle/
UI/
Blocks/
Enemies/
Effects/
```

등에 있는 파일을 확인하고 현재 실제 사용 여부를 조사한다.

다음과 같은 파일은 삭제 후보로 본다.

* 과거 버전 Sprite
* 사용하지 않는 Sprite Sheet
* 더 이상 사용하지 않는 Sprite 상태
* 테스트용 이미지
* 임시로 만든 VFX Sprite
* 현재 Prefab에서 사용하지 않는 Texture
* 이전 연출을 위해 만들었지만 현재 사용하지 않는 이미지
* 이름만 남아 있는 이전 버전 Asset

단순히 파일 이름만 보고 삭제하지 않는다.

실제 참조 관계를 확인한 후 삭제한다.

---

# 4. Prefab과 생성물 정리

과거 작업에서 생성된 Prefab도 전체적으로 확인한다.

특히:

* 테스트용 Prefab
* 이전 버전 Prefab
* 더 이상 Scene에서 사용하지 않는 Prefab
* 다른 Prefab에서도 참조하지 않는 Prefab
* 한 번의 테스트를 위해 생성한 Prefab
* 과거 Editor Script가 자동 생성한 Prefab

등을 확인한다.

다만 현재 실제 게임에서 사용되는 Prefab은 유지한다.

---

# 5. Editor Script 및 일회성 개발 코드 정리

`Editor` 폴더도 함께 확인한다.

특히 과거에 특정 Prefab이나 Scene을 한 번 생성하기 위해 만들었던 Script는 현재 필요성을 확인한다.

예:

```text
CreateXXXPrefab
GenerateXXX
SetupXXX
ConfigureXXX
InitializeXXX
MigrateXXX
FixXXX
```

이미 작업이 완료되었고 앞으로 반복적으로 사용할 필요가 없는 일회성 Tool이라면 삭제한다.

반대로 앞으로도 반복적으로 사용할 Editor Tool은 유지한다.

이번에 만드는:

```text
Tools > Localization > Build Font Atlases
```

도 앞으로 Localization 수정 시 반복적으로 사용할 것이므로 유지한다.

---

# 6. 테스트 / 임시 Asset 정리

개발 과정에서 생성된 테스트용 리소스를 확인한다.

예:

```text
Test*
Debug*
Temp*
Prototype*
Old*
Backup*
Unused*
Copy*
```

등의 이름을 가진 Asset은 삭제 후보로 확인한다.

단, 이름만으로 삭제하지 않는다.

현재 참조되고 있으면 유지한다.

---

# 7. Unity의 간접 참조를 반드시 고려

Unity 프로젝트에서는 단순한 C# 코드 검색만으로 사용 여부를 판단하면 안 된다.

다음 참조를 반드시 고려한다.

* Scene Serialized Reference
* Prefab Serialized Reference
* ScriptableObject Serialized Reference
* Inspector에서 지정된 Asset
* Material Reference
* Animator Controller
* Animation Clip
* Timeline
* Particle System
* SpriteRenderer
* UI Image
* TMP Font Asset
* TMP Material
* Font Fallback
* Resources.Load
* Addressables
* AssetReference
* Resources 폴더
* StreamingAssets
* Build Settings에 포함된 Scene
* 기타 런타임 Asset Loading 코드

특히 `Resources` 폴더에 있는 Asset은 코드에서 문자열 경로로 로드될 가능성이 있으므로 단순 참조 검색만으로 삭제하지 않는다.

---

# 8. Resources / Addressables 확인

`Resources` 폴더와 Addressables를 사용하는 경우 반드시 별도로 확인한다.

코드에서 직접적인 참조가 없더라도:

```csharp
Resources.Load(...)
```

또는 Addressables의 문자열/AssetReference를 통해 런타임에 로드될 수 있다.

따라서 이러한 Asset은 실제 사용 여부를 확인한 뒤 삭제한다.

---

# 9. 현재 게임에서 필요한 리소스는 절대 삭제하지 않는다

다음과 같은 경우에는 유지한다.

* 현재 Scene에서 사용
* 현재 Prefab에서 사용
* 현재 ScriptableObject에서 사용
* Runtime 코드에서 사용
* Addressables에서 사용
* Resources에서 사용
* Font/Fallback 구조에서 사용
* Build 과정에서 사용
* 현재 게임의 UI / Gameplay / VFX / Audio에서 사용

사용 여부가 확실하지 않은 Asset은 삭제하지 말고 **판단 보류 목록**에 남긴다.

---

# 10. 중복 Asset 확인

이름이 다르더라도 사실상 동일한 Asset이 여러 개 존재하는 경우를 확인한다.

예:

```text
BlockHit.png
BlockHit_1.png
BlockHit_Final.png
BlockHit_Final2.png
BlockHit_New.png
```

처럼 개발 과정에서 여러 버전이 생성된 경우:

* 현재 실제 사용되는 버전 확인
* 사용되지 않는 이전 버전 삭제
* 현재 사용 중인 버전은 유지

한다.

단순히 파일 크기나 이름만 보고 자동 삭제하지 않는다.

---

# 11. Meta 파일 주의

Unity Asset을 삭제할 때는 관련 `.meta` 파일도 Unity의 AssetDatabase를 통해 정상적으로 함께 삭제되도록 한다.

파일 시스템에서 임의로 `.meta`만 삭제하거나 남기는 방식으로 처리하지 않는다.

---

# 12. 삭제 후 프로젝트 검증

리소스 정리 후 반드시 전체 프로젝트가 정상적으로 동작하는지 확인한다.

### 컴파일

* C# Compile Error 없음
* Missing Script 없음

### Scene

현재 주요 Scene을 열어서:

* Missing Reference 없음
* Missing Sprite 없음
* Missing Material 없음
* Missing Prefab 없음
* Missing Audio 없음
* Missing Font 없음

을 확인한다.

### Prefab

주요 Prefab을 확인하여:

* Missing Reference 없음
* Missing Component 없음
* Sprite / Material / VFX / Audio 정상 연결

을 확인한다.

### Runtime

게임을 실행하여:

* UI 정상 표시
* Localization 정상 표시
* VFX 정상 표시
* Audio 정상 재생
* Gameplay 정상 동작
* Font 정상 표시

를 확인한다.

---

# 13. 빌드 테스트

정리 작업이 끝나면 실제 Build를 수행한다.

Build 과정에서:

* Missing Asset
* Missing Script
* Build Error
* Serialization Error
* Shader Error
* Font Error

등이 발생하지 않는지 확인한다.

가능하다면 정리 전후의 Build Size 차이도 확인한다.

---

# 14. 삭제 목록 보고

작업 완료 후 삭제한 파일을 유형별로 정리해서 보고한다.

예:

```text
## Deleted Sprites
- Assets/.../OldBlock.png
- Assets/.../TestLaser.png

## Deleted Prefabs
- Assets/.../TestEnemy.prefab

## Deleted Editor Scripts
- Assets/.../CreateOldPrefab.cs

## Deleted Materials
- Assets/.../OldMaterial.mat
```

각 파일마다 가능하면 간단하게 삭제 이유를 적는다.

---

# 15. 판단 보류 목록

사용 여부가 확실하지 않아 삭제하지 않은 Asset은 별도로 보고한다.

예:

```text
## Review Required

- Assets/.../XXX.png
  → Runtime에서 문자열 기반으로 로드될 가능성이 있어 유지

- Assets/.../YYY.prefab
  → 현재 Scene에서는 사용되지 않지만 Addressables 참조 여부 확인 필요
```

이렇게 남겨서 나중에 사람이 판단할 수 있도록 한다.

---

# 16. 앞으로의 프로젝트 관리 원칙

이번 정리 이후에도 새로운 기능을 구현하면서 불필요한 Asset을 프로젝트에 남겨두지 않는다.

새로운:

* Sprite
* Texture
* Prefab
* Material
* Audio
* VFX
* Editor Script
* 테스트 Asset

등을 만들었다가 더 이상 사용하지 않게 된 경우, 해당 작업이 끝난 뒤 필요성을 다시 확인한다.

특히 **일회성 테스트를 위해 만든 Asset이나 Tool은 테스트가 끝난 뒤 삭제를 우선 검토한다.**

앞으로 새로운 기능을 구현할 때도:

> "일단 파일을 만들고 남겨두는 것"

이 아니라

> "현재 실제 게임에서 필요한 Asset만 프로젝트에 남긴다"

는 원칙을 따른다.

---

# 핵심 원칙

이번 작업은 프로젝트 구조를 대규모로 리팩토링하는 작업이 아니다.

**현재 게임을 정상적으로 실행하고 빌드하는 데 필요한 리소스만 남기는 정리 작업이다.**

따라서:

1. 먼저 전체 프로젝트의 Asset과 참조 관계를 조사한다.
2. 현재 사용되지 않는 Asset을 식별한다.
3. 확실히 불필요한 Asset만 삭제한다.
4. 사용 여부가 불확실하면 삭제하지 않는다.
5. 삭제 후 Missing Reference / Missing Script / Build Error를 검사한다.
6. 최종적으로 현재 게임에 필요한 리소스만 남긴다.

특히 **개발 과정에서 생성된 사용하지 않는 Sprite, Texture, Prefab, Material, VFX, Audio, Editor Script 등을 적극적으로 찾아 정리한다.**

이번 작업 이후에도 앞으로 기능 구현 과정에서 더 이상 필요하지 않게 된 리소스는 가능한 한 즉시 정리한다.

---
# TMP Font Atlas 사전 빌드 시스템 구현 요청

## 목적

현재 프로젝트는 TextMeshPro Font Asset을 `Dynamic` 방식으로 사용하고 있다.

이 때문에 특정 언어의 문자가 해당 Font Atlas에 아직 등록되어 있지 않은 상태에서 게임을 처음 실행하면, 해당 문자가 일시적으로 `□`(Missing Glyph)로 표시되고 이후 런타임에서 Dynamic Atlas에 글리프가 추가된 뒤 다음 실행부터 정상적으로 표시되는 문제가 발생한다.

이를 방지하기 위해 **빌드 전에 Localization JSON에 사용되는 모든 문자를 추출하여 각 언어의 TMP Font Asset에 미리 등록하고 Atlas를 생성하는 Editor Tool**을 구현한다.

핵심 목표는 다음과 같다.

> Localization JSON에 실제로 사용되는 문자를 자동으로 수집
> → 언어별 TMP Font Asset에 필요한 문자 등록
> → Dynamic Atlas를 에디터에서 사전 생성
> → 게임 첫 실행부터 모든 번역 문자가 정상적으로 표시

---

## 1. 기존 시스템 분석 우선

작업 전에 반드시 현재 프로젝트의 구현을 먼저 확인한다.

특히 다음을 확인한다.

* 현재 Localization 시스템
* Localization JSON 파일의 위치 및 로딩 방식
* `LocalizedText` 또는 현재 사용 중인 TMP 로컬라이징 컴포넌트
* 현재 사용 중인 TMP Font Asset
* 언어별 Font Asset 설정 방식
* Font Asset의 Atlas Population Mode
* Font Fallback 설정
* 현재 Localization Manager / 관련 Manager 구조
* Scene 및 Prefab에 직렬화된 Font Asset 설정

**기존 시스템을 임의로 새 구조로 교체하지 않는다.**

현재 프로젝트의 구조를 최대한 활용하고, 필요한 부분만 추가한다.

---

# 2. Editor Tool 추가

에디터 전용으로 다음과 같은 기능을 추가한다.

예시 메뉴:

`Tools > Localization > Build Font Atlases`

메뉴를 실행하면 Localization JSON을 분석하여 필요한 문자를 자동으로 Font Asset에 등록하고 Atlas를 갱신한다.

이 기능은 **런타임 코드가 아니라 Editor 전용 기능**이어야 한다.

빌드된 게임에서는 이 기능이 실행되거나 관련 Editor 코드가 포함될 필요가 없다.

---

# 3. Localization JSON 전체 스캔

현재 프로젝트에서 사용하는 모든 Localization JSON을 검색한다.

예:

* `ko.json`
* `en.json`
* `ja.json`
* `zh-CN.json`

향후 언어가 추가되어도 별도의 코드 수정 없이 자동으로 발견할 수 있도록 구현한다.

각 JSON의 모든 localization value를 검사하여 실제 텍스트에 포함된 문자를 추출한다.

다음은 문자 추출 대상에 포함한다.

* 한국어
* 영어
* 일본어
* 중국어
* 숫자
* 기호
* 문장부호
* 현재 UI에서 실제 표시되는 기타 문자

예를 들어:

`"최대 HP {0}"`

에서는 다음과 같은 실제 문자들을 추출한다.

`최대 HP {}`

단, `{0}`, `{1:0.##}` 등의 **format placeholder 자체를 특수하게 처리할 필요는 없다.**

문자 추출 과정에서 숫자 및 `.` 등의 문자를 포함해도 무방하다.

---

# 4. 메타 데이터 제외

Localization JSON의 `_meta` 객체는 실제 게임 텍스트가 아니므로 문자 수집 대상에서 제외한다.

예:

```json
"_meta": {
  "locale": "ko",
  "displayName": "한국어"
}
```

여기서 `locale`, `displayName` 등의 내부 데이터는 Font Atlas 문자 수집에 포함하지 않는다.

단, 실제 게임 UI에서 `displayName`을 사용하고 있다면 현재 구현을 확인한 뒤 필요한 경우에만 포함한다.

---

# 5. 언어별 Font Asset 대응

현재 프로젝트에서 사용 중인 Font Asset 구조를 먼저 확인한다.

가능하다면 현재 Localization 시스템의 언어 코드와 Font Asset을 대응시킬 수 있도록 한다.

예:

```text
ko → Korean Font Asset
en → English Font Asset
ja → Japanese Font Asset
zh-CN → Chinese Font Asset
```

이미 프로젝트에 언어별 Font Asset을 지정하는 구조가 있다면 **그 구조를 그대로 사용한다.**

새로운 전역 Manager나 별도의 복잡한 Font 시스템을 만들지 않는다.

---

# 6. Font Asset에 문자 사전 등록

각 언어의 Localization JSON에서 추출한 문자들을 해당 언어의 TMP Font Asset에 등록한다.

중복 문자는 제거한다.

예:

```text
"빨간 블록"
"빨강 블록"
"빨간 블록 3개"
```

에서

```text
빨
간
블
록
강
...
```

처럼 고유 문자만 남긴다.

이미 Font Asset에 존재하는 문자는 다시 추가하지 않는다.

---

# 7. Dynamic Atlas 유지

현재 Font Asset을 Dynamic 방식으로 사용하고 있다면 **Static으로 변경하지 않는다.**

목표는 Dynamic Font Asset의 장점을 유지하면서, Localization에 사용되는 문자는 빌드 전에 미리 Atlas에 생성하는 것이다.

즉:

```text
Atlas Population Mode
= Dynamic
```

을 유지한다.

빌드 이후 런타임에서 예상하지 못한 문자가 등장하는 경우에는 기존 Dynamic 기능을 통해 추가될 수 있어야 한다.

---

# 8. Atlas 생성 및 저장

문자를 Font Asset에 등록한 뒤 TMP Font Asset의 Atlas가 실제로 생성/갱신되도록 한다.

중요한 것은 단순히 문자 목록만 등록하는 것이 아니라:

> **에디터에서 실제 Glyph / Character가 Atlas에 베이크된 상태로 저장되어야 한다.**

그래서 게임을 처음 실행했을 때 해당 문자가 이미 존재해야 한다.

생성/갱신된 Font Asset 및 Atlas 텍스처는 반드시 AssetDatabase에 정상적으로 저장되어야 한다.

Unity를 재시작하거나 프로젝트를 다시 열어도 결과가 유지되는지 확인한다.

---

# 9. 기존 Font Asset 설정 보존

Editor Tool을 실행하면서 다음 기존 설정을 임의로 변경하지 않는다.

* Atlas Size
* Padding
* Sampling Point Size
* Render Mode
* Font Weight
* Fallback Font Assets
* Material
* Material Preset
* Font Asset 설정
* 기존 Inspector 설정

특히 사용자가 Inspector에서 직접 설정해 둔 값이 Tool 실행으로 초기화되거나 덮어써지는 일이 없어야 한다.

---

# 10. Atlas가 너무 커지는 경우

중국어/일본어처럼 문자가 많은 언어를 고려한다.

**사용 중인 Localization JSON에 실제로 존재하는 문자만 수집한다.**

Unicode 전체 문자나 해당 언어의 모든 한자를 무조건 Atlas에 넣지 않는다.

예를 들어 중국어 Font Asset을 위해 전체 CJK 한자를 전부 Bake하는 방식은 사용하지 않는다.

현재 게임에서 실제로 필요한 문자만 추가한다.

---

# 11. Fallback 구조 고려

현재 프로젝트에 TMP Font Fallback Asset이 설정되어 있다면 해당 구조를 확인한다.

예를 들어 하나의 Font Asset에서 지원하지 않는 문자를 Fallback Font가 처리하도록 되어 있다면:

* 기본 Font Asset에 무조건 모든 문자를 복사하지 않는다.
* 현재 Fallback 구조를 최대한 활용한다.
* 실제로 어떤 Font Asset에 문자를 Bake해야 하는지 현재 프로젝트의 설정을 기준으로 판단한다.

기존 Fallback 시스템을 임의로 제거하거나 재구성하지 않는다.

---

# 12. 실행 결과 로그

Tool 실행이 끝나면 Unity Console에 결과를 명확하게 출력한다.

예:

```text
[Localization Font Builder]
Languages found: 4

ko:
  Unique characters: 128
  Added characters: 12

en:
  Unique characters: 54
  Added characters: 0

ja:
  Unique characters: 246
  Added characters: 246

zh-CN:
  Unique characters: 312
  Added characters: 312

Font Atlas Build Completed.
```

최소한 다음 정보를 알 수 있어야 한다.

* 발견한 언어 수
* 언어별 Font Asset
* 추출된 고유 문자 수
* 새롭게 추가된 문자 수
* 처리 완료 여부
* 오류 발생 시 오류 내용

---

# 13. 누락 Font Asset 처리

Localization 언어는 존재하지만 해당 언어에 대응하는 Font Asset을 찾을 수 있는 경우가 있을 수 있다.

이 경우 Tool이 조용히 실패하지 않도록 한다.

예:

```text
[Localization Font Builder] Warning:
No Font Asset found for locale 'ja'.
Japanese localization was skipped.
```

처럼 명확한 Warning을 출력한다.

다른 언어의 처리는 계속 진행할 수 있어야 한다.

---

# 14. 안전성

이 Tool은 기존 게임 플레이 로직에 영향을 주면 안 된다.

수정 대상은 기본적으로:

* Editor Script
* TMP Font Asset
* TMP Atlas Asset / Texture
* 필요한 Localization 관련 Editor 설정

으로 제한한다.

다음은 생성하지 않는다.

* 새로운 Runtime Manager
* 새로운 Gameplay Manager
* 새로운 Global Singleton
* Dependency Injection 시스템
* 불필요한 Localization 구조 변경

---

# 15. 검증

구현 후 다음 상황을 반드시 테스트한다.

### 테스트 1 — 한국어

한국어 JSON의 모든 문자가 정상적으로 표시되는지 확인한다.

### 테스트 2 — 일본어

일본어 JSON의 문자가 첫 실행부터 `□` 없이 표시되는지 확인한다.

### 테스트 3 — 중국어

중국어 JSON의 문자가 첫 실행부터 정상적으로 표시되는지 확인한다.

### 테스트 4 — 영어

영어 문자 및 숫자/기호가 정상적으로 표시되는지 확인한다.

### 테스트 5 — 새 문자열 추가

Localization JSON에 새로운 문자를 추가한 뒤:

`Tools > Localization > Build Font Atlases`

를 다시 실행하면 해당 문자가 Atlas에 추가되는지 확인한다.

### 테스트 6 — 기존 문자

이미 Atlas에 존재하는 문자를 다시 Bake해도 불필요하게 중복 생성되거나 Font Asset이 손상되지 않는지 확인한다.

### 테스트 7 — Editor 재시작

Tool 실행 후 Unity Editor를 재시작해도 Bake된 문자가 유지되는지 확인한다.

### 테스트 8 — 실제 Build

최종적으로 Build한 게임을 **처음 실행했을 때도** 한국어/일본어/중국어 문자가 `□` 없이 정상적으로 표시되는지 확인한다.

---

# 16. 작업 원칙

이번 작업의 핵심은 **Localization JSON을 Source of Truth로 삼아 빌드 전에 TMP Font Atlas를 자동으로 준비하는 것**이다.

따라서 사용자가 언어를 추가하거나 기존 번역을 수정했을 때:

```text
Localization JSON 수정
        ↓
Build Font Atlases 실행
        ↓
필요한 문자 자동 탐색
        ↓
Font Asset에 등록
        ↓
Atlas Bake
        ↓
Build
```

라는 단순한 작업 흐름을 사용할 수 있어야 한다.

특히 **새로운 언어가 추가될 때 Editor Tool 자체를 수정하지 않아도 되는 구조**를 우선한다.

작업 전에 반드시 현재 프로젝트의 Localization 및 TMP Font Asset 구조를 확인하고, 기존 구조와 Inspector/Scene/Prefab 설정을 최대한 보존하면서 최소한의 변경으로 구현한다.

마지막으로 구현한 파일, 변경한 파일, Tool 사용 방법, Font Asset 대응 방식, 테스트 결과를 간단히 보고한다.

---

# 로컬라이징 언어 기본 설정을 영어로 변경

이제 itch.io같은 글로벌 사이트에 올릴 예정인데, 한국어로 기본 설정되어있으면 한국어 모르는 사람들이 설정 버튼을 누르기 어려움.
때문에 최초 언어 설정은 무조건 영어로 되어있게 할 것. 이후 언어 설정 변경사항은 PlayerPref에 저장하여 유지 (기존과 동일)