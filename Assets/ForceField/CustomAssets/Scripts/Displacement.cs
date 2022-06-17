using Unity.Entities;
using Unity.Rendering;

[GenerateAuthoringComponent]
[MaterialProperty("_displacementStrength", MaterialPropertyFormat.Float)]
public struct Displacement : IComponentData
{
    public float value;
}
