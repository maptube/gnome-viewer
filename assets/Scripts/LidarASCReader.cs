﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

using UnityEngine;
using UnityEditor;

namespace assets.Scripts
{
    /// <summary>
    /// Read a Lidar file in ASC format and create a point cloud object from it.
    /// ASC is ASCII.
    /// NOTE: park data is as follows (1 metre DSM, north part of stadium 3784, but 50cm is labelled identically):
    ///  |---------------------------|
    ///  | TQ38ne 3785 | TQ38ne 3885 |
    ///  |---------------------------|
    ///  | TQ38se 3784 | TQ38se 3884 |
    ///  |---------------------------|
    ///  | TQ38se 3783 | TQ38se 3883 |
    ///  |---------------------------|
    ///  TODO: get all the unity out of this and make it a general class for MapTube
    /// </summary>
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
    public class LidarASCReader : MonoBehaviour
    {
        //This is useful: https://wiki.research.data.ac.uk/ASCII_LIDAR
        //It even includes a link to a txt to las tool.
        //http://catlikecoding.com/unity/tutorials/procedural-grid/

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
            //string path = Directory.GetCurrentDirectory();
            //Debug.Log("Current directory = " + path);

            //NOTE: current directory is the top level folder containing the project, so a relative filename
            //needs to be something like: Assets\Data\my-file.asc
            //if (!File.Exists(Filename))
            //{
            //    Debug.Log("LidarASCReader.Start File does not exist: " + Filename);
            //    return;
            //}

            //ReadASCLidar();
            //Debug.Log("Mesh Loaded");
            //CreateMesh2();

        }

        public void Awake()
        {
            //TODO: test for the existence of the asset before creating a new one
            //CreateMesh2();
            ReadASCLidar();
            //CreateMesh();
            //CreateTerrain();

            TerrainGenerator terragen = new TerrainGenerator(CoverageData, cellsize);
            terragen.CreateTerrain(Filename);
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

                //cellsize
                Line = reader.ReadLine();
                Fields = Line.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                if (Fields[0] != "cellsize") { Debug.Log("LidarASCReader.ReadASCLidar: Error no cellsize field in header"); return; }
                cellsize = Convert.ToSingle(Fields[1]);

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

        protected void CreateMesh2()
        {
            //THIS IS A TEST PYRAMID MESH
            MeshFilter mf = GetComponent<MeshFilter>();
            Mesh mesh = new Mesh();
            mf.mesh = mesh;

            mesh.vertices = new Vector3[] {
                new Vector3(-1, 0, -1),
                new Vector3(-1, 0, 1),
                new Vector3(1, 0, 1),
                new Vector3(1, 0, -1),
                new Vector3(0, 1, 0)
            };


            mesh.triangles = new int[] {4,0,1,  4,1,2,  4,2,3,  4,3,0,  2,1,0,  3,2,0  };

            mesh.RecalculateNormals();

        }

        /// <summary>
        /// Build a mesh from the data in CoverageData.
        /// Step 1: create a simple mesh made up out of square tiles (triangles) - once that works, you can think about point cloud shaders
        /// NOTE: I've removed the xllcorner and yllcorner offsets and started the mesh from zero to make positioning easier - this might be better as an option?
        /// </summary>
        protected void CreateMesh()
        {
            MeshFilter mf = GetComponent<MeshFilter>();
            Mesh mesh = new Mesh();
            mf.mesh = mesh;

            //mesh.vertices, mesh.triangles, mesh.normals, mesh.uv

            //HACK! there's a limit of 65000 indices per mesh, so we need to split big coverages into smaller mesh parts
            //HACK! I'm going to hack the number of rows for testing
            nrows = 20;

            //First, build the grid of vertices
            Vector3 [] vertices = new Vector3[ncols * nrows];
            for (int y=0; y<nrows; y++)
            {
                for (int x=0; x<ncols; x++)
                {
                    float val = CoverageData[x, y];
                    if (float.IsNaN(val)) val = 0; //NOTE: any missing values in the coverage data appear as zero height triangles - need to fix this to remove triangles with no data
                    vertices[x + y * ncols] = new Vector3(/*xllcorner+*/ x*cellsize,val,/*yllcorner+*/ y*cellsize);
                }
            }
            mesh.vertices = vertices;

            //now build the normals, which are per vertex and smoothed according to the neighbours - if you want 
            /*mesh.normals = new Vector3[ncols * nrows];
            //build the normals in the middle first, as there's a bit of an issue with the edges
            for (int y=1; y< nrows-1; y++)
            {
                for (int x=1; x< ncols-1; x++)
                {
                    int v0i = x + y * ncols; //centre point
                    int v1i = x + (y-1) * ncols;
                    int v2i = (x + 1) + y * ncols;
                    int v3i = x + (y + 1) * ncols;
                    int v4i = (x - 1) + y * ncols;
                    Vector3 v0 = mesh.vertices[v0i];
                    Vector3 v1 = mesh.vertices[v1i];
                    Vector3 v2 = mesh.vertices[v2i];
                    Vector3 v3 = mesh.vertices[v3i];
                    Vector3 v4 = mesh.vertices[v4i];
                    //now make four normals for each side of the pyramid with v0 at its centre and v1..v4 around the outside
                    Vector3 n1 = Vector3.Cross(v1-v0, v2-v0);
                    Vector3 n2 = Vector3.Cross(v2 - v0, v3 - v0);
                    Vector3 n3 = Vector3.Cross(v3 - v0, v4 - v0);
                    Vector3 n4 = Vector3.Cross(v4 - v0, v1 - v0);
                    //now average the four normals
                    Vector2 n = Vector3.Normalize(n1 + n2 + n3 + n4);
                    mesh.normals[v0i] = n;
                }
            }
            //THIS IS A HACK! I've set the edge normals to a default value and not calculated them properly - need to revisit this
            //now the edge bits
            for (int x=0; x< ncols; x++)
            {
                //top row
                mesh.normals[x] = new Vector3();
                //bottom row
                mesh.normals[x + (nrows - 1) * ncols] = new Vector3();
            }
            for (int y=0; y< nrows; y++)
            {
                //left column
                mesh.normals[y * ncols] = new Vector3();
                //right column
                mesh.normals[(ncols - 1) + y * ncols] = new Vector3();
            }*/

            //Then wire the vertices up into triangular faces - there are (ncols-1)*(nrows-1) squares in the grid, times two for triangles and times 3 for points per face
            int [] triangles = new int[(ncols-1)*(nrows-1)*2*3];
            int f = 0; //face number
            for (int y=0; y<nrows-1; y++) //iterate over square cells and make two triangles per square
            {
                for (int x=0; x<ncols-1; x++)
                {
                    //indices of the four square cell vertices
                    int v0i = x + y * ncols;
                    int v1i = (x + 1) + y * ncols;
                    int v2i = (x + 1) + (y + 1) * ncols;
                    int v3i = x + (y + 1) * ncols;
                    //make the two triangular faces: v0,v1,v3 and v1,v2,v3
                    triangles[f] = v0i;
                    triangles[f + 1] = v3i;
                    triangles[f + 2] = v1i;
                    triangles[f + 3] = v1i;
                    triangles[f + 4] = v3i;
                    triangles[f + 5] = v2i;

                    f += 6; //update base vertex and normal index for the next two triangles
                }
            }
            mesh.triangles = triangles;

            //TODO: do we need uvs if there is no texture?

            //mesh.RecalculateBounds();
            mesh.RecalculateNormals();
        }

        

        
    }
}
