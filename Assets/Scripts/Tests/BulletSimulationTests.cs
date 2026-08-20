using NUnit.Framework;
using UnityEngine;

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
        var sim = new BulletSimulation(10, new Rect(-10, -10, 20, 20));
        sim.Fire(Vector3.zero, Vector3.right, 2f);
        sim.Fire(Vector3.zero, Vector3.right*0.5f, 0.5f);
        sim.Fire(Vector3.zero, Vector3.right * 2f, 2f);
        sim.Tick(1f);
        Assert.AreEqual(2, sim.Count);
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
