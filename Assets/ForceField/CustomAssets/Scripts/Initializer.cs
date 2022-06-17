using Unity.Entities;
using UnityEngine;
using Unity.Mathematics;
using Unity.Transforms;

public class Initializer : MonoBehaviour
{
    public GameObject projectile;
    public GameObject turret;
    public GameObject ally;
    public float maxProjectileSpeed;
    public float projectileLifeTime;
    public float maxFiringInterval;
    public int numberOfShooters;

    private EntityManager em;
    private BlobAssetStore bas;
    private GameObjectConversionSettings gocs;
    private Entity projectileEntity;
    private Entity turretEntity;
    private Entity allyEntity;
    private float3 position;
    private float3 allyPosition;

    // Start is called before the first frame update
    void Start()
    {
        em = World.DefaultGameObjectInjectionWorld.EntityManager;
        bas = new BlobAssetStore();
        gocs = GameObjectConversionSettings.FromWorld(World.DefaultGameObjectInjectionWorld, bas);
        projectileEntity = GameObjectConversionUtility.ConvertGameObjectHierarchy(projectile, gocs);
        turretEntity = GameObjectConversionUtility.ConvertGameObjectHierarchy(turret, gocs);
        allyEntity = GameObjectConversionUtility.ConvertGameObjectHierarchy(ally, gocs);
        allyPosition = new float3(0, 1.45f, 20);

        em.AddComponent<AllyComponentData>(allyEntity);
        Entity allyInstance = em.Instantiate(allyEntity);
        em.SetComponentData(allyInstance, new Translation { Value = allyPosition });

        em.AddComponent<TurretComponentData>(turretEntity);
        for (int i = 0; i < numberOfShooters; i++)
        {
            Entity turretInstance = em.Instantiate(turretEntity);
            position = (float3)transform.position + (new float3(10.5f * i, 1.7f, -10));
            em.SetComponentData(turretInstance, new Translation { Value = position });
            em.SetComponentData(turretInstance, new TurretComponentData
            {
                projectile = projectileEntity,
                projectileLifeTime = projectileLifeTime,
                allyPosition = allyPosition,
                firingInterval = UnityEngine.Random.Range(0.1f, maxFiringInterval),
                projectileSpeed = UnityEngine.Random.Range(1, maxProjectileSpeed),
            });
        }

        em.DestroyEntity(turretEntity);
        em.DestroyEntity(allyEntity);
        //em.DestroyEntity(projectileEntity);
    }

    private void OnDestroy()
    {
        bas.Dispose();
    }
}