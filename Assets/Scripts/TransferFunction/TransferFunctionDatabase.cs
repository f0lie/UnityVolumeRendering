using UnityEngine;
using System.Collections.Generic;
using System.IO;

namespace UnityVolumeRendering
{
    public class TransferFunctionDatabase
    {
        [System.Serializable]
        private struct TF1DSerialisationData
        {
            public int version;
            public List<TFColourControlPoint> colourPoints;
            public List<TFAlphaControlPoint> alphaPoints;

            public const int VERSION_ID = 1;
        }

        public static TransferFunction CreateTransferFunction()
        {
            string TFfile = Resources.Load<TextAsset>("15TF").text;
            TF1DSerialisationData data = JsonUtility.FromJson<TF1DSerialisationData>(TFfile);
            TransferFunction tf = new TransferFunction();
            tf.colourControlPoints = data.colourPoints;
            tf.alphaControlPoints = data.alphaPoints;
            return tf;
        }

        public static void SaveTransferFunction(TransferFunction tf, string filepath)
        {
            TF1DSerialisationData data = new TF1DSerialisationData();
            data.version = TF1DSerialisationData.VERSION_ID;
            data.colourPoints = new List<TFColourControlPoint>(tf.colourControlPoints);
            data.alphaPoints =　new List<TFAlphaControlPoint>(tf.alphaControlPoints);
            string jsonstring = JsonUtility.ToJson(data);
            File.WriteAllText(filepath, jsonstring);
        }
    }
}
