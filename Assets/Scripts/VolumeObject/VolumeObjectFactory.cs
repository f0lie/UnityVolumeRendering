using System;
using UnityEngine;

namespace UnityVolumeRendering
{
    public class VolumeObjectFactory
    {
        public static VolumeRenderedObject CreateObject(VolumeDataset dataset)
        {
            Debug.Log("Start of CreateObj");
            GameObject outerObject = new GameObject("VolumeRenderedObject_" + dataset.datasetName);
            Debug.Log("After new GameObject");
            VolumeRenderedObject volObj = outerObject.AddComponent<VolumeRenderedObject>();
            Debug.Log("After of VolObj");

            GameObject meshContainer = GameObject.Instantiate((GameObject)Resources.Load("VolumeContainer"));
            Debug.Log("After of MeshContainer");
            meshContainer.transform.parent = outerObject.transform;
            meshContainer.transform.localScale = Vector3.one;
            meshContainer.transform.localPosition = Vector3.zero;
            meshContainer.transform.parent = outerObject.transform;
            outerObject.transform.localRotation = Quaternion.Euler(90.0f, 0.0f, 0.0f);

            Debug.Log("Before of MeshRenderer");
            MeshRenderer meshRenderer = meshContainer.GetComponent<MeshRenderer>();
            meshRenderer.sharedMaterial = new Material(meshRenderer.sharedMaterial);
            volObj.meshRenderer = meshRenderer;
            volObj.dataset = dataset;

            Debug.Log("Before TF");
            TransferFunction tf = TransferFunctionDatabase.CreateTransferFunction();
            Debug.Log("After TF");
            TransferFunctionDatabase.SaveTransferFunction(tf, "C:\\Users\\austi\\OneDrive\\Desktop\\simpleTF2.json");
            Texture2D tfTexture = tf.GetTexture();
            Debug.Log("After texture");
            volObj.transferFunction = tf;


            meshRenderer.sharedMaterial.SetTexture("_DataTex", dataset.GetDataTexture());
            Debug.Log("After of dataset texture");
            meshRenderer.sharedMaterial.SetTexture("_GradientTex", null);
            meshRenderer.sharedMaterial.SetTexture("_TFTex", tfTexture);

            meshRenderer.sharedMaterial.EnableKeyword("MODE_DVR");
            meshRenderer.sharedMaterial.DisableKeyword("MODE_MIP");
            meshRenderer.sharedMaterial.DisableKeyword("MODE_SURF");

            Debug.Log("Before if Statement");
            if(dataset.scaleX != 0.0f && dataset.scaleY != 0.0f && dataset.scaleZ != 0.0f)
            {
                float maxScale = Mathf.Max(dataset.scaleX, dataset.scaleY, dataset.scaleZ);
                volObj.transform.localScale = new Vector3(dataset.scaleX / maxScale, dataset.scaleY / maxScale, dataset.scaleZ / maxScale);
            }

            return volObj;
        }
    }
}
