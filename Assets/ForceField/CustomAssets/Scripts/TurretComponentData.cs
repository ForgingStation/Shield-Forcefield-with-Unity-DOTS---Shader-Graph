using Unity.Entities;
using Unity.Mathematics;

public struct TurretComponentData : IComponentData
{
    public float3 projectileSpawnPosition;
    public float firingInterval;
    public Entity projectile;
    public float projectileSpeed;
    public float projectileLifeTime;
    public float3 allyPosition;
    public float elapsedTime;
}
