using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine;

namespace UnityVolumeRendering
{
    /// <summary>
    /// An imported dataset. Has a dimension and a 3D pixel array.
    /// </summary>
    [Serializable]
    public class VolumeDataset : ScriptableObject
    {
        public string filePath;
        
        // Flattened 3D array of data sample values.
        [SerializeField]
        public float[] data;

        [SerializeField]
        public int dimX, dimY, dimZ;

        [SerializeField]
        public float scaleX = 0.0f, scaleY = 0.0f, scaleZ = 0.0f;
        public float volumeScale;

        [SerializeField]
        public string datasetName;

        private float minDataValue = float.MaxValue;
        private float maxDataValue = float.MinValue;

        private Texture3D dataTexture = null;
        private Texture3D gradientTexture = null;

        public static Dictionary<int, BrainSection> table;

        public async void FillLookupTable()
        {
            table = new Dictionary<int, BrainSection>();
            TextAsset file = Resources.Load<TextAsset>("FreeSurferValues");
            string lookupTable = file.ToString();
            string[] rows = lookupTable.Split('\n');
            for(int i = 0; i < rows.Length - 1; ++i)
            {
                string[] elements = rows[i].Split('\t'); // each element is seperated by a tab
                int index = Int32.Parse(elements[0]);
                table[index] = new BrainSection(Int32.Parse(elements[2]), Int32.Parse(elements[3]), Int32.Parse(elements[4]), Int32.Parse(elements[5]), elements[1]);
            }
        }
        
        public Texture3D GetDataTexture()
        {
            dataTexture = CreateTextureInternal();
            return dataTexture;
        }

        public Texture3D GetGradientTexture()
        {
            gradientTexture = CreateGradientTextureInternal();
            return gradientTexture;
        }

        public float GetMinDataValue()
        {
            if (minDataValue == float.MaxValue)
                CalculateValueBounds();
            return minDataValue;
        }

        public float GetMaxDataValue()
        {
            if (maxDataValue == float.MinValue)
                CalculateValueBounds();
            return maxDataValue;
        }

        /// <summary>
        /// Ensures that the dataset is not too large.
        /// </summary>
        public void FixDimensions()
        {
            int MAX_DIM = 2048; // 3D texture max size. See: https://docs.unity3d.com/Manual/class-Texture3D.html

            while (Mathf.Max(dimX, dimY, dimZ) > MAX_DIM)
            {
                Debug.LogWarning("Dimension exceeds limits (maximum: "+MAX_DIM+"). Dataset is downscaled by 2 on each axis!");
                DownScaleData();
            }
        }

        /// <summary>
        /// Downscales the data by averaging 8 voxels per each new voxel,
        /// and replaces downscaled data with the original data
        /// </summary>
        public void DownScaleData()
        {
            int halfDimX = dimX / 2 + dimX % 2;
            int halfDimY = dimY / 2 + dimY % 2;
            int halfDimZ = dimZ / 2 + dimZ % 2;
            float[] downScaledData = new float[halfDimX * halfDimY * halfDimZ];

            for (int x = 0; x < halfDimX; x++)
            {
                for (int y = 0; y < halfDimY; y++)
                {
                    for (int z = 0; z < halfDimZ; z++)
                    {
                        downScaledData[x + y * halfDimX + z * (halfDimX * halfDimY)] = Mathf.Round(GetAvgerageVoxelValues(x * 2, y * 2, z * 2));
                    }
                }
            }

            //Update data & data dimensions
            data = downScaledData;
            dimX = halfDimX;
            dimY = halfDimY;
            dimZ = halfDimZ;
        }

        private void CalculateValueBounds()
        {
            minDataValue = float.MaxValue;
            maxDataValue = float.MinValue;

            if (data != null)
            {
                for (int i = 0; i < dimX * dimY * dimZ; i++)
                {
                    float val = data[i];
                    minDataValue = Mathf.Min(minDataValue, val);
                    maxDataValue = Mathf.Max(maxDataValue, val);
                }
            }
        }

        private Texture3D CreateTextureInternal()
        {
            if(table == null) FillLookupTable();

            TextureFormat texformat = TextureFormat.RGBA32;
            Texture3D texture = new Texture3D(dimX, dimY, dimZ, texformat, false);
            texture.wrapMode = TextureWrapMode.Clamp;

            //float minValue = GetMinDataValue(); //probably need to get rid of min and max entirely
            //float maxValue = GetMaxDataValue();
            //float maxRange = maxValue - minValue;

            bool isHalfFloat = texformat == TextureFormat.RGBA32;
            Debug.Log(isHalfFloat);
            int iData = 0;
            try
            {
                // Create a byte array for filling the texture. RGBA64 has 4 2 byte ints, RGBA32 has 4 1 byte ints
                int sampleSize = isHalfFloat ? 1 : 2;
                byte[] bytes = new byte[data.Length * sampleSize * 4]; // This can cause OutOfMemoryException
                for (; iData < data.Length; iData++)
                {
                    int val = (int)data[iData];
                    BrainSection pixelValue = table[val];
                    
                    byte[] RBytes = isHalfFloat ? BitConverter.GetBytes(Convert.ToByte(pixelValue.r)) : BitConverter.GetBytes(Convert.ToInt16(pixelValue.r));
                    Array.Copy(RBytes, 0, bytes, iData * sampleSize * 4, sampleSize);

                    byte[] GBytes = isHalfFloat ? BitConverter.GetBytes(Convert.ToByte(pixelValue.g)) : BitConverter.GetBytes(Convert.ToInt16(pixelValue.g));
                    Array.Copy(GBytes, 0, bytes, iData * sampleSize * 4 + sampleSize, sampleSize);

                    byte[] BBytes = isHalfFloat ? BitConverter.GetBytes(Convert.ToByte(pixelValue.b)) : BitConverter.GetBytes(Convert.ToInt16(pixelValue.b));
                    Array.Copy(BBytes, 0, bytes, iData * sampleSize * 4 + (sampleSize * 2), sampleSize);

                    int alpha = val == 0 ? 0 : 250;
                    byte[] ABytes = isHalfFloat ? BitConverter.GetBytes(Convert.ToByte(alpha)) : BitConverter.GetBytes(Convert.ToInt16(alpha));
                    Array.Copy(ABytes, 0, bytes, iData * sampleSize * 4 + (sampleSize * 3), sampleSize);
                }

                texture.SetPixelData(bytes, 0);
            }

            catch (OutOfMemoryException ex)
            {
                Debug.LogWarning("Out of memory when creating texture. Using fallback method.");
                for (int x = 0; x < dimX; x++)
                    for (int y = 0; y < dimY; y++)
                        for (int z = 0; z < dimZ; z++)
                            texture.SetPixel(x, y, z, new Color(0, 0.0f, 0.0f, 0.0f));
            }
            texture.Apply();
            return texture;
        }

        private Texture3D CreateGradientTextureInternal() 
        {
            if(table == null) FillLookupTable();
            
            TextureFormat texformat = SystemInfo.SupportsTextureFormat(TextureFormat.RGBAHalf) ? TextureFormat.RGBAHalf : TextureFormat.RGBAFloat;
            Texture3D texture = new Texture3D(dimX, dimY, dimZ, texformat, false);
            texture.wrapMode = TextureWrapMode.Clamp;

            float minValue = GetMinDataValue();
            float maxValue = GetMaxDataValue();
            float maxRange = maxValue - minValue;

            Color[] cols;
            try
            {
                cols = new Color[data.Length];
            }
            catch (OutOfMemoryException ex)
            {
                cols = null;
            }
            for (int x = 0; x < dimX; x++)
            {
                for (int y = 0; y < dimY; y++)
                {
                    for (int z = 0; z < dimZ; z++)
                    {
                        int iData = x + y * dimX + z * (dimX * dimY);

                        float x1 = data[Math.Min(x + 1, dimX - 1) + y * dimX + z * (dimX * dimY)] - minValue;
                        float x2 = data[Math.Max(x - 1, 0) + y * dimX + z * (dimX * dimY)] - minValue;
                        float y1 = data[x + Math.Min(y + 1, dimY - 1) * dimX + z * (dimX * dimY)] - minValue;
                        float y2 = data[x + Math.Max(y - 1, 0) * dimX + z * (dimX * dimY)] - minValue;
                        float z1 = data[x + y * dimX + Math.Min(z + 1, dimZ - 1) * (dimX * dimY)] - minValue;
                        float z2 = data[x + y * dimX + Math.Max(z - 1, 0) * (dimX * dimY)] - minValue;

                        Vector3 grad = new Vector3((x2 - x1) / maxRange, (y2 - y1) / maxRange, (z2 - z1) / maxRange);
                        
                        if (cols == null)
                        {
                            texture.SetPixel(x, y, z, new Color(grad.x, grad.y, grad.z, (float)(data[iData] - minValue) / maxRange));
                        }
                        else
                        {
                            cols[iData] = new Color(grad.x, grad.y, grad.z, (float)(data[iData] - minValue) / maxRange);
                        }
                    }
                }
            }
            if (cols != null) texture.SetPixels(cols);
            texture.Apply();
            return texture;
        }

        public float GetAvgerageVoxelValues(int x, int y, int z)
        {
            // if a dimension length is not an even number
            bool xC = x + 1 == dimX;
            bool yC = y + 1 == dimY;
            bool zC = z + 1 == dimZ;

            //if expression can only be true on the edges of the texture
            if (xC || yC || zC)
            {
                if (!xC && yC && zC) return (GetData(x, y, z) + GetData(x + 1, y, z)) / 2.0f;
                else if (xC && !yC && zC) return (GetData(x, y, z) + GetData(x, y + 1, z)) / 2.0f;
                else if (xC && yC && !zC) return (GetData(x, y, z) + GetData(x, y, z + 1)) / 2.0f;
                else if (!xC && !yC && zC) return (GetData(x, y, z) + GetData(x + 1, y, z) + GetData(x, y + 1, z) + GetData(x + 1, y + 1, z)) / 4.0f;
                else if (!xC && yC && !zC) return (GetData(x, y, z) + GetData(x + 1, y, z) + GetData(x, y, z + 1) + GetData(x + 1, y, z + 1)) / 4.0f;
                else if (xC && !yC && !zC) return (GetData(x, y, z) + GetData(x, y + 1, z) + GetData(x, y, z + 1) + GetData(x, y + 1, z + 1)) / 4.0f;
                else return GetData(x, y, z); // if xC && yC && zC
            }
            return (GetData(x, y, z) + GetData(x + 1, y, z) + GetData(x, y + 1, z) + GetData(x + 1, y + 1, z)
                    + GetData(x, y, z + 1) + GetData(x, y + 1, z + 1) + GetData(x + 1, y, z + 1) + GetData(x + 1, y + 1, z + 1)) / 8.0f;
        }

        public float GetData(int x, int y, int z)
        {
            return data[x + y * dimX + z * (dimX * dimY)];
        }
    }
}
