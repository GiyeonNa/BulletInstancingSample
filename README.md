# BulletInstancingSample

> 같은 탄막 시뮬레이션에 렌더링 방식만 갈아끼워, **GPU Instancing이 드로우콜에 주는 효과를 실측으로 증명**하는 샘플입니다.
> 탄환 약 800발 기준 — 드로우콜 **8 vs 810**. 시뮬레이션 코어는 EditMode 테스트 6종으로 검증했습니다.

## 결과

| 탄환 760~810발 안정 상태 | GPU Instancing | Naive (GameObject 풀) |
| --- | --- | --- |
| Batches / Draw Calls | **8** | **760~810** |
| SetPass Calls | 7 | 9~10 |
| 메인스레드 | 7.5~8.0 ms | 9.5~10.0 ms |

- 탄환 800발이 드로우콜에 더하는 비용: **+2 vs +800 (약 100배)**
- 측정 환경: Unity 6000.0.77f1 · URP (Universal Renderer) · 에디터 게임 뷰 · 포스트 프로세싱/SSAO 제거 · [CPU/GPU 모델명]

![탄막 시연](Docs/gameview.png)

## 실측에서 나온 발견 2가지

### 1. 인스턴싱도 무한이 아니다 — 드로우당 511 인스턴스 상한

Frame Debugger로 확인하면 탄환 742발이 `Draw Mesh (instanced)` **2개 항목(511 + 231)**으로 제출됩니다. 이 환경에서는 한 번의 인스턴스드 드로우에 묶이는 개수에 상한이 있고, 초과분은 자동으로 분할됩니다. "인스턴싱 = 무조건 1콜"이 아니라는 것까지 실측으로 확인했습니다.

| Instanced — 511개 묶음 | Instanced — 나머지 231개 |
| --- | --- |
| ![](Docs/fd_instanced_511.png) | ![](Docs/fd_instanced_231.png) |

### 2. SRP Batcher는 SetPass를 줄이지, 드로우콜을 줄이지 않는다

Naive 모드의 GameObject 800개는 SRP Batcher를 통해 그려집니다(같은 URP/Unlit 머티리얼이라 호환). 그 결과 **SetPass는 7 vs 9~10으로 거의 차이가 없지만**, SRP Batch 그룹 안을 열어보면 **Draw Calls 208씩 4그룹 = 약 832회의 개별 제출**이 그대로 남아 있습니다.

즉 "SRP Batcher가 있으니 괜찮다"는 흔한 오해와 달리 — SRP Batcher가 흡수하는 것은 머티리얼 상태 전환이고, **드로우콜 제출 폭증을 막는 것은 인스턴싱뿐**입니다.

![Naive — SRP Batch 내부의 Draw Calls 208](Docs/fd_naive_srpbatch.png)

## 설계 결정

- **struct + 고정 배열 (List 아님)** — 배열 인덱서는 ref 반환이라 제자리 수정이 되고, 생성자에서 한 번만 할당해 런타임 재할당(GC)을 원천 차단. 프레임당 힙 할당 0.
- **역순 순회 + swap-remove** — 죽은 탄환 자리에 마지막 요소를 덮고 count 감소. 역순이므로 끌려오는 요소는 항상 이번 프레임 처리를 마친 상태라 갱신 누락이 없다. 순서가 섞이는 대가는 탄환에서는 무비용.
- **상한 초과 시 발사 무시** — 최고령 재활용은 날아가던 탄이 사라지는 팝핑이 보이지만, 새 탄 하나 안 나가는 것은 탄막 절정에서 보이지 않는다. 시각적 결함이 덜 보이는 쪽을 선택.
- **시뮬레이션 / 렌더링 분리** — 시뮬레이션은 MonoBehaviour가 아닌 일반 C# 클래스. dt와 경계 Rect를 값으로 주입받고 씬을 모른다. 덕분에 ① 씬 없이 EditMode 테스트 가능 ② 두 렌더 모드가 **같은 시뮬레이션을 공유**해 측정 차이가 순수 렌더링 비용이 된다.
- **대조군이 GameObject 풀인 이유** — "실제로 순진하게 구현하면 이렇게 된다"와 같은 형태(Transform 갱신 비용 포함)라 대조가 정직하다. 풀의 활성/비활성은 경계 구간만 토글 — 전체를 매 프레임 껐다 켜면 대조군을 고의로 느리게 만드는 셈이므로.
- 전제: 탄환은 XY 평면에서 움직인다 (경계 판정이 x·y만 본다).

## 구조

```
Assets/Scripts/
  Bullet.cs               탄환 데이터 (순수 struct)
  BulletSimulation.cs     시뮬레이션 코어 — 고정 배열, Fire/Tick, 엔진 씬 비의존
  BulletSystemDriver.cs   Unity 다리 — 경계 계산·발사 패턴·렌더링 2모드
  BulletStatsOverlay.cs   측정 오버레이 — 표의 수치를 같은 구간 평균으로 표시
  Tests/
    BulletSimulationTests.cs   EditMode 테스트 6종 (상한 정책·swap-remove 정합·슬롯 재사용 age 등)
```

## 실행·재현

1. Unity 6000.0.77f1로 열기 → `SampleScene` 플레이
2. 좌상단 오버레이에 탄환 수·Batches·SetPass·메인스레드가 **직전 120프레임 평균**으로 표시됨
3. Driver 인스펙터에서 **Mode**를 Instanced ↔ Naive로 전환하며 비교 (Fire Interval로 밀도 조절)
4. 테스트: `Window > General > Test Runner` → EditMode → Run All

## 측정 방법과 한계

- 씬 기본 비용을 통제하기 위해 URP 템플릿의 SSAO(렌더러 피처)와 Bloom 등 포스트 프로세싱을 제거 — 기준선 배치 약 6.
- 탄환 수가 안정된 상태(발사량 = 소멸량)에서 두 모드를 같은 조건으로 측정.
- 에디터 측정이므로 절대값에는 에디터 오버헤드가 포함됨. 메인스레드 차이(+2ms)는 고성능 PC 기준이며, 드로우 제출 비용이 상대적으로 큰 모바일에서는 더 벌어지는 항목.

## 작성 방식

시뮬레이션·렌더러·테스트 코드는 전부 직접 작성했습니다. AI는 설계 토론 상대와 코드 리뷰어로 활용했고, 측정 오버레이(BulletStatsOverlay)와 이 문서의 초안 정리에 사용했습니다. 수치와 발견은 전부 직접 측정한 것입니다.
