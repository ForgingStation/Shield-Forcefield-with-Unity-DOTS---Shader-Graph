using Unity.Entities;
using Unity.Transforms;
using Unity.Mathematics;
using Unity.Burst;
using Unity.Physics;
using Unity.Physics.Systems;
using Unity.Jobs;

public partial class TurretSystem : SystemBase
{
    private BeginSimulationEntityCommandBufferSystem bs_ecb;
    private EndSimulationEntityCommandBufferSystem es_ecb_job;
    public BuildPhysicsWorld bpw;
    public StepPhysicsWorld spw;

    protected override void OnCreate()
    {
        bs_ecb = World.GetOrCreateSystem<BeginSimulationEntityCommandBufferSystem>();
        es_ecb_job = World.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();
        bpw = World.DefaultGameObjectInjectionWorld.GetExistingSystem<BuildPhysicsWorld>();
        spw = World.DefaultGameObjectInjectionWorld.GetExistingSystem<StepPhysicsWorld>();
    }

    protected override void OnUpdate()
    {
        float deltaTime = Time.DeltaTime;
        Entities.ForEach((ref Rotation rot, in TurretComponentData tcd, in Translation trans) =>
        {
            rot.Value = math.slerp(rot.Value, quaternion.LookRotation((tcd.allyPosition - trans.Value),math.up()), deltaTime * 5);

        }).ScheduleParallel();

        var ecb = bs_ecb.CreateCommandBuffer().AsParallelWriter();
        Entities.ForEach((int entityInQueryIndex, ref TurretComponentData tcd, in Translation trans, in LocalToWorld ltw) =>
        {
            tcd.elapsedTime += deltaTime;
            if (tcd.elapsedTime >= tcd.firingInterval)
            {
                tcd.elapsedTime = 0;
                tcd.projectileSpawnPosition = math.transform(ltw.Value, new float3(0, 0.3f, 1.7f));
                Entity projectile = ecb.Instantiate(entityInQueryIndex,tcd.projectile);
                ecb.SetComponent(entityInQueryIndex, projectile, new Translation { Value = tcd.projectileSpawnPosition });
                ecb.AddComponent(entityInQueryIndex, projectile, new ProjectileFired());
                ecb.SetComponent(entityInQueryIndex, projectile, new ProjectileFired
                {
                    elapsedTime = 0,
                    projectileSpeed = tcd.projectileSpeed,
                    projectileLifeTime = tcd.projectileLifeTime,
                    directionToShoot = tcd.allyPosition - trans.Value
                });
            }
        }).ScheduleParallel();

        Entities.ForEach((Entity e, int entityInQueryIndex, ref ProjectileFired pf, ref Translation trans) =>
        {
            pf.elapsedTime += deltaTime;
            trans.Value += pf.directionToShoot * pf.projectileSpeed * deltaTime;
            if (pf.elapsedTime > pf.projectileLifeTime)
            {
                ecb.DestroyEntity(entityInQueryIndex, e);
            }
        }).ScheduleParallel();

        bs_ecb.AddJobHandleForProducer(Dependency);

        JobHandle jh = new ProjectileCollisionJob()
        {
            allyGroup = GetComponentDataFromEntity<AllyComponentData>(),
            maskGroup = GetComponentDataFromEntity<MaskCenter>(),
            displacementGroup = GetComponentDataFromEntity<Displacement>(),
            activeProjectileGroup = GetComponentDataFromEntity<ProjectileFired>(),
            translationGroup = GetComponentDataFromEntity<Translation>(),
            childrenBufferGroup = GetBufferFromEntity<Child>(),
            ecb = es_ecb_job.CreateCommandBuffer(),
            psw = bpw.PhysicsWorld,
        }.Schedule(spw.Simulation, Dependency);
        jh.Complete();

        es_ecb_job.AddJobHandleForProducer(jh);

        Entities.ForEach((ref Displacement d) =>
        {
            d.value = math.lerp(d.value, 0, deltaTime*8.5f);
        }).ScheduleParallel();
    }

    [BurstCompile]
    public struct ProjectileCollisionJob : ICollisionEventsJob
    {
        public ComponentDataFromEntity<AllyComponentData> allyGroup;
        public ComponentDataFromEntity<MaskCenter> maskGroup;
        public ComponentDataFromEntity<Displacement> displacementGroup;
        public ComponentDataFromEntity<ProjectileFired> activeProjectileGroup;
        public ComponentDataFromEntity<Translation> translationGroup;
        public BufferFromEntity<Child> childrenBufferGroup;
        public EntityCommandBuffer ecb;

        public PhysicsWorld psw;
        private float3 hitPoint;
        private MaskCenter m;
        private Displacement d;
        private Translation t;
        private DynamicBuffer<Child> cb;

        public void Execute(CollisionEvent collisionEvent)
        {
            hitPoint = collisionEvent.CalculateDetails(ref psw).EstimatedContactPointPositions[0];
            Entity entityA = collisionEvent.EntityA;
            Entity entityB = collisionEvent.EntityB;
            if (entityA != Entity.Null && entityB != Entity.Null)
            {
                bool isBodyAProjectile = activeProjectileGroup.HasComponent(entityA);
                bool isBodyBProjectile = activeProjectileGroup.HasComponent(entityB);
                bool isBodyAAlly = allyGroup.HasComponent(entityA);
                bool isBodyBAlly = allyGroup.HasComponent(entityB);

                if (isBodyAProjectile && isBodyBAlly)
                {
                    ecb.DestroyEntity(entityA);
                    if (childrenBufferGroup.TryGetBuffer(entityB, out cb))
                    {
                        translationGroup.TryGetComponent(entityB, out t);
                        if (maskGroup.TryGetComponent(cb[0].Value, out m) && displacementGroup.TryGetComponent(cb[0].Value, out d))
                        {
                            m.value = (hitPoint - t.Value) + new float3(0,0,1.5f);
                            d.value += 25;
                            ecb.SetComponent(cb[0].Value, m);
                            ecb.SetComponent(cb[0].Value, d);
                        }
                    }
                }

                if (isBodyBProjectile && isBodyAAlly)
                {
                    ecb.DestroyEntity(entityB);
                    if (childrenBufferGroup.TryGetBuffer(entityA, out cb))
                    {
                        translationGroup.TryGetComponent(entityA, out t);
                        if (maskGroup.TryGetComponent(cb[0].Value, out m) && displacementGroup.TryGetComponent(cb[0].Value, out d))
                        {
                            m.value = (hitPoint - t.Value) + new float3(0, 0, 1.5f); ;
                            d.value += 25;
                            ecb.SetComponent(cb[0].Value, m);
                            ecb.SetComponent(cb[0].Value, d);
                        }
                    }
                } 
            }
        }
    }
}

public struct ProjectileFired : IComponentData
{
    public float projectileSpeed;
    public float projectileLifeTime;
    public float projectileImpulseForce;
    public float elapsedTime;
    public float3 directionToShoot;
}