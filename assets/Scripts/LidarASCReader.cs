using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

using UnityEngine;

namespace assets.Scripts
{
    /// <summary>
    /// Read a Lidar file in ASC format and create a point cloud object from it.
    /// ASC is ASCII.
    /// </summary>
    public class LidarASCReader : MonoBehaviour
    {
        //This is useful: https://wiki.research.data.ac.uk/ASCII_LIDAR
        //It even includes a link to a txt to las tool.

        //Missing value used to tag cells that aren't in the data
        public static float MissingValue = float.NaN;

        //editor properties
        [SerializeField]
        public string Filename;

        //properties not accessible in the editor
        public int ncols;
        public int nrows;
        public float xllcorner;
        public float yllcorner;
        public float cellsize;
        public string NODATA_value;
        public float [,] CoverageData;

        // Use this for initialization
        void Start()
        {
            if (!File.Exists(Filename))
            {
                Debug.Log("LidarASCReader.Start File does not exist: " + Filename);
                return;
            }

            ReadASCLidar();
            CreateMesh();

        }

        // Update is called once per frame
        void Update()
        {

        }

        /// <summary>
        /// Read lidar data in ASC format from the file.
        /// </summary>
        protected void ReadASCLidar()
        {
            //ASC format:
            //ncols 2000
            //nrows 2000
            //xllcorner    537000
            //yllcorner    184000
            //cellsize     0.5
            //NODATA_value - 9999
            //then data separated by spaces

            using (TextReader reader = new StreamReader(Filename))
            {
                string Line;
                string[] Fields;

                //ncols
                Line = reader.ReadLine();
                Fields = Line.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                if (Fields[0] != "ncols") { Debug.Log("LidarASCReader.ReadASCLidar: Error no ncols field in header"); return; }
                ncols = Convert.ToInt32(Fields[1]);

                //nrows
                Line = reader.ReadLine();
                Fields = Line.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                if (Fields[0] != "nrows") { Debug.Log("LidarASCReader.ReadASCLidar: Error no nrows field in header"); return; }
                nrows = Convert.ToInt32(Fields[1]);

                //xllcorner
                Line = reader.ReadLine();
                Fields = Line.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                if (Fields[0] != "xllcorner") { Debug.Log("LidarASCReader.ReadASCLidar: Error no xllcorner field in header"); return; }
                xllcorner = Convert.ToSingle(Fields[1]);

                //yllcorner
                Line = reader.ReadLine();
                Fields = Line.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                if (Fields[0] != "yllcorner") { Debug.Log("LidarASCReader.ReadASCLidar: Error no yllcorner field in header"); return; }
                yllcorner = Convert.ToInt32(Fields[1]);

                //NODATA_value
                Line = reader.ReadLine();
                Fields = Line.Split(new char[] { ' ' },StringSplitOptions.RemoveEmptyEntries);
                if (Fields[0] != "NODATA_value") { Debug.Log("LidarASCReader.ReadASCLidar: Error no NODATA_value field in header"); return; }
                NODATA_value = Fields[1];

                //OK, so if we've got to here, then we've got a valid header and we can start thinking about reading the actual data back
                CoverageData = new float[ncols,nrows];

                //Most of the methods suggest reading the whole file using Reader.ReadToEnd, but it's a big file and there isn't anything that reads a formatted number separately.
                //So, what I'm going to do is to chunk it by lines and read the data in the (x,y) array.
                int y = 0, x = 0;
                while ((y<nrows)&&(x<ncols))
                {
                    Line = reader.ReadLine();
                    string[] heights = Line.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                    foreach (string str in heights)
                    {
                        float val = MissingValue; //NOTE: NODATA_value gets set to zero!!!! This might not be a good idea!!
                        if (str!=NODATA_value)
                        {
                            val = Convert.ToSingle(str);
                        }
                        CoverageData[x, y] = val; //store the data
                        x += 1;
                        if (x>=ncols)
                        {
                            x = 0;
                            y += 1;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Build a mesh from the data in CoverageData
        /// </summary>
        protected void CreateMesh()
        {
            MeshFilter mf = GetComponent<MeshFilter >();
            Mesh mesh = new Mesh();
            mf.mesh = mesh;

            //mesh.vertices, mesh.triangles, mesh.normals, mesh.uv

        }

    }
}
