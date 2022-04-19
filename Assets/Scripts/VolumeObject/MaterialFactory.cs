using UnityEngine;

namespace UnityVolumeRendering
{
    public class MaterialFactory
    {
        public static Material CreateMaterialDVR(VolumeDataset dataset)
        {
            Shader shader = Shader.Find("VolumeRendering/DirectVolumeRenderingShader");
            Material material = new Material(shader);
            material.SetTexture("_DataTex", dataset.GetDataTexture());

            return material;
        }
    }
}
