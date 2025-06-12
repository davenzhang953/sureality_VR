using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System;
using UnityEngine.Rendering;
using System.Linq;
using UnityEngine.UI;
using System.Threading;

namespace Seengene.XDK
{
    public class PlyDataLoader
    {
        private string m_PlyPath = "";
        private bool m_IsReading = false;
        Thread mThreadReadPlyBody = null;

        public bool isLoading {
            get {
                return mThreadReadPlyBody.ThreadState == ThreadState.Running || m_IsReading;
            }
        }

        public float progress {
            get;
            private set;
        }

        public DataHeader dataHeader {
            get;
            private set;
        }

        public DataBody dataBody {
            get;
            private set;
        }


        public void LoadPly(string plyPath) {
            m_PlyPath = plyPath;
            if (string.IsNullOrEmpty(m_PlyPath)) {
                Debug.LogError("文件路径错误" + m_PlyPath);
                return;
            }
            if (File.Exists(m_PlyPath)) {
                m_IsReading = true;
                Debug.Log("Start Read PLY Data: " + m_PlyPath);
                mThreadReadPlyBody = new Thread(ThreadLoadPly);
                mThreadReadPlyBody.IsBackground = true;
                mThreadReadPlyBody.Start();
            } else {
                Debug.LogError("文件不存在：" + m_PlyPath);
            }
        }

        public void StopLoadPly() {
            m_IsReading = false;
            try {
                if (mThreadReadPlyBody != null) {
                    mThreadReadPlyBody.Abort();
                    mThreadReadPlyBody = null;
                }
                Debug.Log("结束线程 mThreadReadPlyBody 成功");
            } catch (Exception e) {
                Debug.LogErrorFormat("结束线程 mThreadReadPlyBody 失败：{0}", e.Message);
            }
        }

        private void ThreadLoadPly() {
            var stream = File.Open(m_PlyPath, FileMode.Open, FileAccess.Read, FileShare.Read);
            string err = ReadDataHeader(new StreamReader(stream));
            Debug.Log("Read PlyFile Header is Done");
            Thread.Sleep(10);
            if (err != null) {
                dataHeader = null;
            } else {
                var body = ReadDataBody(dataHeader, new BinaryReader(stream));
                dataBody = body;
            }
            m_IsReading = false;
            Debug.Log("Finish Read PLY Data: " + m_PlyPath+ " err="+ err);
        }

        /// <summary>
        /// 填充mesh的
        /// </summary>
        /// <param name="mesh"></param>
        /// <param name="meshFilter"></param>
        public void addMeshData(Mesh mesh, MeshFilter meshFilter) {
            mesh.indexFormat = dataHeader.vertexCount > 65535 ? IndexFormat.UInt32 : IndexFormat.UInt16;
            mesh.SetVertices(dataBody.vertices);
            mesh.SetColors(dataBody.colors);
            mesh.SetIndices(
                Enumerable.Range(0, dataHeader.vertexCount).ToArray(),
                MeshTopology.Points, 0
            );
            mesh.UploadMeshData(false);
            meshFilter.mesh = mesh; // 实际去显示一个mesh
        }



        private string ReadDataHeader(StreamReader reader) {
            var data = new DataHeader();
            var readCount = 0;

            // Magic number line ("ply")
            var line = reader.ReadLine();
            readCount += line.Length + 1;
            if (line != "ply") {
                return "Magic number ('ply') mismatch.";
            }

            // Data format: check if it's binary/little endian.
            line = reader.ReadLine();
            readCount += line.Length + 1;
            if (line != "format binary_little_endian 1.0") {
                return "Invalid data format ('" + line + "'). Should be binary/little endian.";
            }
            // Read header contents.
            for (var skip = false; ;) {
                // Read a line and split it with white space.
                line = reader.ReadLine();
                readCount += line.Length + 1;
                if (line == "end_header") break;
                var col = line.Split();

                // Element declaration (unskippable)
                if (col[0] == "element") {
                    if (col[1] == "vertex") {
                        data.vertexCount = Convert.ToInt32(col[2]);
                        skip = false;
                    } else {
                        // Don't read elements other than vertices.
                        skip = true;
                    }
                }

                if (skip) {
                    continue;
                }
                // Property declaration line
                if (col[0] == "property") {
                    var prop = DataProperty.Invalid;

                    // Parse the property name entry.
                    switch (col[2]) {
                        case "red": prop = DataProperty.R8; break;
                        case "green": prop = DataProperty.G8; break;
                        case "blue": prop = DataProperty.B8; break;
                        case "alpha": prop = DataProperty.A8; break;
                        case "x": prop = DataProperty.SingleX; break;
                        case "y": prop = DataProperty.SingleY; break;
                        case "z": prop = DataProperty.SingleZ; break;
                    }

                    // Check the property type.
                    if (col[1] == "char" || col[1] == "uchar" ||
                        col[1] == "int8" || col[1] == "uint8") {
                        if (prop == DataProperty.Invalid)
                            prop = DataProperty.Data8;
                        else if (GetPropertySize(prop) != 1) {
                            return "Invalid property type ('" + line + "').";
                        }
                    } else if (col[1] == "short" || col[1] == "ushort" ||
                               col[1] == "int16" || col[1] == "uint16") {
                        switch (prop) {
                            case DataProperty.Invalid: prop = DataProperty.Data16; break;
                            case DataProperty.R8: prop = DataProperty.R16; break;
                            case DataProperty.G8: prop = DataProperty.G16; break;
                            case DataProperty.B8: prop = DataProperty.B16; break;
                            case DataProperty.A8: prop = DataProperty.A16; break;
                        }
                        if (GetPropertySize(prop) != 2) {
                            return "Invalid property type ('" + line + "').";
                        }
                    } else if (col[1] == "int" || col[1] == "uint" || col[1] == "float" ||
                               col[1] == "int32" || col[1] == "uint32" || col[1] == "float32") {
                        if (prop == DataProperty.Invalid)
                            prop = DataProperty.Data32;
                        else if (GetPropertySize(prop) != 4) {
                            return "Invalid property type ('" + line + "').";
                        }
                    } else if (col[1] == "int64" || col[1] == "uint64" ||
                               col[1] == "double" || col[1] == "float64") {
                        switch (prop) {
                            case DataProperty.Invalid: prop = DataProperty.Data64; break;
                            case DataProperty.SingleX: prop = DataProperty.DoubleX; break;
                            case DataProperty.SingleY: prop = DataProperty.DoubleY; break;
                            case DataProperty.SingleZ: prop = DataProperty.DoubleZ; break;
                        }
                        if (GetPropertySize(prop) != 8) {
                            return "Invalid property type ('" + line + "').";
                        }
                    } else {
                        return "Unsupported property type ('" + line + "').";
                    }

                    data.properties.Add(prop);
                }
            }

            // Rewind the stream back to the exact position of the reader.
            reader.BaseStream.Position = readCount;
            dataHeader = data;

            return null;
        }

        private DataBody ReadDataBody(DataHeader header, BinaryReader reader) {
            var data = new DataBody(header.vertexCount);

            float x = 0, y = 0, z = 0;
            Byte r = 255, g = 255, b = 255, a = 255;

            for (var i = 0; i < header.vertexCount; i++) {
                foreach (var prop in header.properties) {
                    switch (prop) {
                        case DataProperty.R8: r = reader.ReadByte(); break;
                        case DataProperty.G8: g = reader.ReadByte(); break;
                        case DataProperty.B8: b = reader.ReadByte(); break;
                        case DataProperty.A8: a = reader.ReadByte(); break;

                        case DataProperty.R16: r = (byte)(reader.ReadUInt16() >> 8); break;
                        case DataProperty.G16: g = (byte)(reader.ReadUInt16() >> 8); break;
                        case DataProperty.B16: b = (byte)(reader.ReadUInt16() >> 8); break;
                        case DataProperty.A16: a = (byte)(reader.ReadUInt16() >> 8); break;

                        case DataProperty.SingleX: x = reader.ReadSingle(); break;
                        case DataProperty.SingleY: y = reader.ReadSingle(); break;
                        case DataProperty.SingleZ: z = reader.ReadSingle(); break;

                        case DataProperty.DoubleX: x = (float)reader.ReadDouble(); break;
                        case DataProperty.DoubleY: y = (float)reader.ReadDouble(); break;
                        case DataProperty.DoubleZ: z = (float)reader.ReadDouble(); break;

                        case DataProperty.Data8: reader.ReadByte(); break;
                        case DataProperty.Data16: reader.BaseStream.Position += 2; break;
                        case DataProperty.Data32: reader.BaseStream.Position += 4; break;
                        case DataProperty.Data64: reader.BaseStream.Position += 8; break;
                    }
                }
                data.AddPoint(x, y, z, r, g, b, a);
                progress = (float)i / header.vertexCount;
            }
            progress = 1.0f;
            return data;
        }

        #region Internal data structure

        public enum DataProperty
        {
            Invalid,
            R8, G8, B8, A8,
            R16, G16, B16, A16,
            SingleX, SingleY, SingleZ,
            DoubleX, DoubleY, DoubleZ,
            Data8, Data16, Data32, Data64
        }

        static int GetPropertySize(DataProperty p) {
            switch (p) {
                case DataProperty.R8: return 1;
                case DataProperty.G8: return 1;
                case DataProperty.B8: return 1;
                case DataProperty.A8: return 1;
                case DataProperty.R16: return 2;
                case DataProperty.G16: return 2;
                case DataProperty.B16: return 2;
                case DataProperty.A16: return 2;
                case DataProperty.SingleX: return 4;
                case DataProperty.SingleY: return 4;
                case DataProperty.SingleZ: return 4;
                case DataProperty.DoubleX: return 8;
                case DataProperty.DoubleY: return 8;
                case DataProperty.DoubleZ: return 8;
                case DataProperty.Data8: return 1;
                case DataProperty.Data16: return 2;
                case DataProperty.Data32: return 4;
                case DataProperty.Data64: return 8;
            }
            return 0;
        }

        public class DataHeader
        {
            public List<DataProperty> properties = new List<DataProperty>();
            public int vertexCount = -1;
        }

        public class DataBody
        {
            public List<Vector3> vertices;
            public List<Color32> colors;

            public DataBody(int vertexCount) {
                vertices = new List<Vector3>(vertexCount);
                colors = new List<Color32>(vertexCount);
            }

            public void AddPoint(
                float x, float y, float z,
                byte r, byte g, byte b, byte a
            ) {
                vertices.Add(new Vector3(x, y, z));
                colors.Add(new Color32(r, g, b, a));
            }
        }

        #endregion
    }

}