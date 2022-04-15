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

        [System.Serializable]
        private struct TF2DSerialisationData
        {
            public int version;
            public List<TransferFunction2D.TF2DBox> boxes;

            public const int VERSION_ID = 1;
        }

        public static TransferFunction CreateTransferFunction()
        {
            string TFfile = Resources.Load<TextAsset>("15TF").text;
            TF1DSerialisationData data = JsonUtility.FromJson<TF1DSerialisationData>(TFfile);
            Debug.Log(data.colourPoints.ToString());
            Debug.Log(data.alphaPoints.ToString());
            TransferFunction tf = new TransferFunction();
            tf.colourControlPoints = data.colourPoints;
            tf.alphaControlPoints = data.alphaPoints;
            return tf;
        }

        public static TransferFunction2D CreateTransferFunction2D()
        {
            TransferFunction2D tf2D = new TransferFunction2D();
            tf2D.AddBox(0.05f, 0.1f, 0.8f, 0.7f, Color.white, 0.4f);
            return tf2D;
        }

        public static TransferFunction LoadTransferFunction(string filepath)
        {
            if(!File.Exists(filepath))
            {
                Debug.LogError(string.Format("File does not exist: {0}", filepath));
                return null;
            }
            string jsonstring = File.ReadAllText(filepath);
            TF1DSerialisationData data = JsonUtility.FromJson<TF1DSerialisationData>(jsonstring);
            Debug.Log(jsonstring);
            Debug.Log(data.colourPoints.ToString());
            Debug.Log(data.alphaPoints.ToString());
            TransferFunction tf = new TransferFunction();
            tf.colourControlPoints = data.colourPoints;
            tf.alphaControlPoints = data.alphaPoints;
            return tf;
        }

        public static TransferFunction2D LoadTransferFunction2D(string filepath)
        {
            if(!File.Exists(filepath))
            {
                Debug.LogError(string.Format("File does not exist: {0}", filepath));
                return null;
            }
            string jsonstring = File.ReadAllText(filepath);
            TF2DSerialisationData data = JsonUtility.FromJson<TF2DSerialisationData>(jsonstring);
            TransferFunction2D tf = new TransferFunction2D();
            tf.boxes = data.boxes;
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

        public static void SaveTransferFunction2D(TransferFunction2D tf2d, string filepath)
        {
            TF2DSerialisationData data = new TF2DSerialisationData();
            data.version = TF2DSerialisationData.VERSION_ID;
            data.boxes = new List<TransferFunction2D.TF2DBox>(tf2d.boxes);
            string jsonstring = JsonUtility.ToJson(data);
            File.WriteAllText(filepath, jsonstring);
        }
    }
}
