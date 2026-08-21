using Unity.Profiling;
using UnityEngine;

//측정용 오버레이 - 표의 네 행(탄환 수 / Batches / SetPass / 메인스레드 ms)을 한 곳에서, 같은 구간 평균으로 읽는다.
//씬에 붙일 필요 없음: 드라이버가 있는 씬이면 에디터·개발 빌드에서 자동 생성.
//새 Input System 프로젝트라 Input.*는 쓰지 않고, 기록은 컴포넌트 컨텍스트 메뉴 "Log Row"로.
public class BulletStatsOverlay : MonoBehaviour
{
#if UNITY_EDITOR || DEVELOPMENT_BUILD
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Bootstrap()
    {
        if (FindFirstObjectByType<BulletSystemDriver>() == null) return;
        new GameObject("BulletStatsOverlay").AddComponent<BulletStatsOverlay>();
    }
#endif

    const int Window = 120; //평균 구간(프레임) - 2초@60fps. 안정 상태에 들어간 뒤 읽을 것

    private BulletSystemDriver _driver;
    private ProfilerRecorder _batches, _setPass, _drawCalls, _mainThread;
    private readonly int[] _bulletSamples = new int[Window];
    private int _sampleHead, _sampleCount;

    void OnEnable()
    {
        _driver = FindFirstObjectByType<BulletSystemDriver>();
        _batches = StartRecorder(ProfilerCategory.Render, "Batches Count", "Total Batches Count");
        _setPass = StartRecorder(ProfilerCategory.Render, "SetPass Calls Count");
        _drawCalls = StartRecorder(ProfilerCategory.Render, "Draw Calls Count");
        _mainThread = StartRecorder(ProfilerCategory.Internal, "Main Thread");
    }

    void OnDisable()
    {
        Release(ref _batches); Release(ref _setPass); Release(ref _drawCalls); Release(ref _mainThread);
    }

    //카운터 이름이 버전마다 달라 후보를 순서대로 시도
    static ProfilerRecorder StartRecorder(ProfilerCategory category, params string[] names)
    {
        foreach (var name in names)
        {
            var r = ProfilerRecorder.StartNew(category, name, Window);
            if (r.Valid) return r;
            r.Dispose();
        }
        return default;
    }

    static void Release(ref ProfilerRecorder r)
    {
        if (r.Valid) r.Dispose();
        r = default;
    }

    void Update()
    {
        if (_driver == null) return;
        _bulletSamples[_sampleHead] = _driver.ActiveBullets;
        _sampleHead = (_sampleHead + 1) % Window;
        if (_sampleCount < Window) _sampleCount++;
    }

    float AvgBullets()
    {
        if (_sampleCount == 0) return 0f;
        long sum = 0;
        for (int i = 0; i < _sampleCount; i++) sum += _bulletSamples[i];
        return (float)sum / _sampleCount;
    }

    static double Avg(ProfilerRecorder r)
    {
        if (!r.Valid || r.Count == 0) return 0.0;
        double sum = 0.0;
        for (int i = 0; i < r.Count; i++) sum += r.GetSample(i).Value;
        return sum / r.Count;
    }

    string Row() =>
        $"mode={_driver.mode}  bullets={AvgBullets():0}  batches={Avg(_batches):0.0}  setPass={Avg(_setPass):0.0}  drawCalls={Avg(_drawCalls):0.0}  mainThread={Avg(_mainThread) / 1e6:0.00}ms";

    [ContextMenu("Log Row")]
    void LogRow()
    {
        if (_driver == null) return;
        Debug.Log($"[BulletStats] {Row()}  (avg of last {Window} frames)");
    }

    void OnGUI()
    {
        if (_driver == null) return;
        //이 박스 자체가 배치 1~2개를 먹는다 - 두 열에 똑같이 들어가는 상수라 대조엔 영향 없음
        GUI.Box(new Rect(10, 10, 820, 26), Row());
    }
}
