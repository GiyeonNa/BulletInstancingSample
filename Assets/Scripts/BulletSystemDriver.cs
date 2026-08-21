using UnityEngine;

public class BulletSystemDriver : MonoBehaviour
{
    public enum Mode { Instanced, Naive }

    public Mesh mesh;
    public Material material;
    public Mode mode = Mode.Instanced;
    public int capacity = 4096;
    public float fireInterval = 0.1f;
    public float bulletScale = 0.2f;  //capacity처럼 Start에서 한 번만 읽음 - 런타임 변경 미반영

    private BulletSimulation _sim;
    private Matrix4x4[] _matrices;
    private RenderParams _renderParams;
    private GameObject[] _pool;   //Naive용
    private int _prevActive;      //직전 프레임 활성 개수
    private float _fireTimer;
    private float _aimDeg;
    private Vector3 _scale;       //두 모드가 같은 값을, 같은 시점에 읽도록 캐시

    public int ActiveBullets => _sim != null ? _sim.Count : 0;  //측정 오버레이용 읽기 전용

    void Start()
    {
        _scale = Vector3.one * bulletScale;

        //카메라를 넘기는 게 아니라 값을 계산해서 주입
        var cam = Camera.main;
        float halfH = cam.orthographicSize;
        float halfW = halfH * cam.aspect;
        var bounds = new Rect(cam.transform.position.x - halfW, cam.transform.position.y - halfH, halfW * 2, halfH * 2);

        _sim = new BulletSimulation(capacity, bounds);
        _matrices = new Matrix4x4[capacity];
        _renderParams = new RenderParams(material);
        //시뮬 경계와 같은 영역. z는 카메라가 아니라 탄환이 실제 놓이는 평면 - 카메라 z로 잡으면 near clip에 따라 통째로 컬링됨
        _renderParams.worldBounds = new Bounds(
            new Vector3(bounds.center.x, bounds.center.y, transform.position.z),
            new Vector3(bounds.width, bounds.height, 1f));

        //Naive용 풀 - 메시/머티리얼은 인스턴스드와 공유, 다른 건 그리는 방식 하나
        _pool = new GameObject[capacity];
        for (int i = 0; i < capacity; i++)
        {
            var go = new GameObject("Bullet" + i);
            go.transform.SetParent(transform);
            go.transform.localScale = _scale; //스케일은 생성 때 한 번만
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            go.AddComponent<MeshRenderer>().sharedMaterial = material;
            go.SetActive(false);
            _pool[i] = go;
        }
    }

    void Update()
    {
        _sim.Tick(Time.deltaTime);

        if (fireInterval > 0f) //0이면 아래 while이 무한 루프
        {
            _fireTimer += Time.deltaTime;
            while (_fireTimer >= fireInterval)   //밀린 만큼 따라잡고, 잔액은 다음 프레임으로
            {
                _fireTimer -= fireInterval;
                FireFan();
            }
        }

        if (mode == Mode.Instanced) RenderInstanced();
        else RenderNaive();
    }

    //부채꼴 8발, 조금씩 회전
    void FireFan()
    {
        _aimDeg += 5f;
        for (int i = 0; i < 8; i++)
        {
            float rad = (_aimDeg + i * 15f) * Mathf.Deg2Rad;
            var dir = new Vector3(Mathf.Cos(rad), Mathf.Sin(rad), 0);
            _sim.Fire(transform.position, dir * 3f, 5f);
        }
    }

    void RenderInstanced()
    {
        //Naive에서 넘어온 직후 - 켜진 풀 정리 (안 하면 얼어붙은 탄환 + 이중 드로우로 실측 오염)
        if (_prevActive > 0)
        {
            for (int i = 0; i < _prevActive; i++) _pool[i].SetActive(false);
            _prevActive = 0;
        }

        int count = _sim.Count;
        if (count == 0) return;

        var bullets = _sim.Bullets;
        for (int i = 0; i < count; i++)
        {
            _matrices[i] = Matrix4x4.TRS(bullets[i].position, Quaternion.identity, _scale);
        }
        //드로우콜 한 방
        Graphics.RenderMeshInstanced(_renderParams, mesh, 0, _matrices, count);
    }

    void RenderNaive()
    {
        int count = _sim.Count;

        //경계 넘나드는 구간만 토글 - 전체를 매 프레임 껐다 켜면 고의로 느리게 하는 셈
        for (int i = _prevActive; i < count; i++) _pool[i].SetActive(true);
        for (int i = count; i < _prevActive; i++) _pool[i].SetActive(false);

        var bullets = _sim.Bullets;
        for (int i = 0; i < count; i++)
        {
            _pool[i].transform.position = bullets[i].position;
        }
        _prevActive = count;
    }
}
