using UnityEngine;

public class BulletSimulation
{
    private Bullet[] _bullets;
    private Rect _bound;
    private int _count;

    public int Count => _count;
    public Bullet[] Bullets => _bullets;


    public BulletSimulation(int capacity, Rect bounds)
    {
        _bullets = new Bullet[capacity];
        _bound = bounds;
    }

    public void Fire(Vector3 position, Vector3 velocity, float lifetime)
    {
        if (_count == _bullets.Length) return;

        //어차피 struct는 지정 안하면 0
        _bullets[_count] = new Bullet { position = position, velocity = velocity, lifetime = lifetime };

        _count++;
    }


    public void Tick(float dt)
    {
        for (int i = _count - 1; i >= 0; i--)
        {
            _bullets[i].position += _bullets[i].velocity * dt;
            _bullets[i].age += dt;

            //사망제거
            if (_bullets[i].age >= _bullets[i].lifetime || !_bound.Contains(_bullets[i].position))
            {
                //swap-remove
                //역순인 이유: 루프가 뒤에서 앞으로, i 자리로 끌려오는 마지막 탄환은 항상 이미 검사를 마친 놈, 건너뛰어도 되는 놈만 건너뜀
                _bullets[i] = _bullets[_count - 1];
                //자기 자신 대입은 값 복사라 무해, 실제 제거는 _count 감소가 담당하므로 특별 처리가 필요 없음
                _count--;
            }
        }
    }
    
}
