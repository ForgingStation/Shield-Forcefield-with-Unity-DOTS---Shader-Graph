using Unity.Entities;
using Unity.Rendering;
using Unity.Mathematics;

[GenerateAuthoringComponent]
[MaterialProperty("_sphereMaskCenter", MaterialPropertyFormat.Float3)]
public struct MaskCenter : IComponentData
{
    public float3 value;
}
