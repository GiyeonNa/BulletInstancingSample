using System.Collections;
using System.Xml.Serialization;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

public class BulletSimulationTests
{
    [Test]
    public void 발사시_카운트증가()
    {
        var sim = new BulletSimulation(10, new Rect(-10, -10, 20, 20));
        sim.Fire(Vector3.zero, Vector3.right, 2f);
        Assert.AreEqual(1, sim.Count);
    }

    [Test]
    public void 상한_초과_무시()
    {
        var sim = new BulletSimulation(2, new Rect(-10, -10, 20, 20));
        sim.Fire(Vector3.zero, Vector3.right, 2f);
        sim.Fire(Vector3.zero, Vector3.right, 2f);
        sim.Fire(Vector3.zero, Vector3.right, 2f);
        Assert.AreEqual(2, sim.Count);
    }

    [Test]
    public void 수명_만료_제거()
    {
        var sim = new BulletSimulation(10, new Rect(-10, -10, 20, 20));
        sim.Fire(Vector3.zero, Vector3.right, 1f);
        sim.Tick(2f);
        Assert.AreEqual(0, sim.Count);
    }

    [Test]
    public void swap_remove_정합()
    {
        /* 3발을 서로 구분되는 데이터로 쏘세요 (예: velocity를 각각 다르게). 가운데
  놈만 죽게 만들려면? lifetime을 가운데만 짧게 주면 됩니다. Tick 후에: Count가 2인지 + 남은 두 탄환이 (순서는
  바뀌었어도) 정확히 1번·3번의 데이터인지 sim.Bullets[...]로 확인.*/

        var sim = new BulletSimulation(10, new Rect(-10, -10, 20, 20));
        sim.Fire(Vector3.zero, Vector3.right, 2f);
        sim.Fire(Vector3.zero, Vector3.right*0.5f, 0.5f);
        sim.Fire(Vector3.zero, Vector3.right * 2f, 2f);
        sim.Tick(1f);
        Assert.AreEqual(Vector3.right, sim.Bullets[0].velocity);
        Assert.AreEqual(Vector3.right * 2f, sim.Bullets[1].velocity);

    }

    [Test]
    public void 경계_밖_제거()
    {
        var sim = new BulletSimulation(10, new Rect(-10, -10, 20, 20));
        sim.Fire(Vector3.zero, Vector3.right * 200f, 2f);
        sim.Tick(1f);
        Assert.AreEqual(0, sim.Count);
    }

    [Test]
    public void 재사용_슬롯_age_초기화()
    {
        var sim = new BulletSimulation(1, new Rect(-10, -10, 20, 20));
        sim.Fire(Vector3.zero, Vector3.right * 200f, 1f);
        sim.Tick(1f);
        sim.Fire(Vector3.zero, Vector3.right * 200f, 1f);
        Assert.AreEqual(0, sim.Bullets[0].age);
    }
}
