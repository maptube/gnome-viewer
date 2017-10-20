using System;
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
            CreateTerrain();
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

        /// <summary>
        /// Create a Unity terrain from the Lidar data.
        /// Creates a file in Assets/Filename.assets (where Filename is the name of the dem loaded without the path or extension).
        /// </summary>
        protected void CreateTerrain()
        {
            int potSize = 1025; //power of two size for the terrain data - we have to resample our data to fit this

            //first, we need to resample the data to fit the power of 2 texture
            float[,] data = new float[potSize, potSize];
            ResampleTerrainBilinear(potSize, ref data);

            //then we need to create a new array of height values here in order to get rid of the NODATA_values
            //first pass through data to determine maximum height
            float maxheight = -float.MaxValue;
            for (int y = 0; y < nrows; y++) for (int x = 0; x < ncols; x++) if (data[x,y] > maxheight) maxheight = data[x,y];
            Debug.Log("LidarASCReader.CreateTerrain: maxheight=" + maxheight);

            //second pass to normalise the height
            for (int y=0; y<potSize; y++)
            {
                for (int x=0; x<potSize; x++)
                {
                    float val = data[x, y];
                    if (float.IsNaN(val)) val = 0; //I've set missing data values to zero in order that I can display something
                    if (val < 0) val = 0; //you can't have negative heights
                    data[x, y] = val/maxheight; //normalise height to 0..1
                }
            }
            
            TerrainData terrainData = new TerrainData();
            terrainData.heightmapResolution = potSize; // size of texture map;
            Debug.Log("LidarASCReader.CreateTerrain: heightmapWidth=" + terrainData.heightmapWidth + " heightmapHeight=" + terrainData.heightmapWidth);
            //heightmapWidth and Height read only
            //heightmapScale readonly
            //terrainData.SetDetailResolution(1024,16)
            //terrainData.baseMapResolution=

            terrainData.size = new Vector3(ncols*cellsize, maxheight, nrows*cellsize);

            //terrainData.heightmapResolution = 512;
            //terrainData.baseMapResolution = 1024;
            //terrainData.SetDetailResolution(1024, terrainData.detailResolutionPerPatch);
            terrainData.SetHeights(0, 0, data);
            string Name = Path.GetFileNameWithoutExtension(Filename);
            AssetDatabase.CreateAsset(terrainData, "Assets/"+Name+".asset");
        }

        /// <summary>
        /// Resample the terrain to a power of 2 texture map size as required by the Unity terrain object.
        /// Input data comes from the CoverageData class and associated ncols, nrows, cellsize.
        /// Simple bilinear interpolation.
        /// TODO: this is designed to expand the data i.e. CoverageData[1000,1000] maps onto data[1025,1025] NOT the other way around (yet?)
        /// ALSO, it's square!
        /// </summary>
        /// <param name="size">Power of 2 plus one size i.e. 1025 (+1 comes from the edges)</param>
        /// <param name="data">Output data resampled from CoverageData</param>
        protected void ResampleTerrainBilinear(int size, ref float [,] data)
        {
            for (int y=0; y<size; y++)
            {
                float yf = ((float)y / (float)size) * nrows; //how far we are along the CoverageData grid
                float y0 = (float)Math.Floor(yf);
                float dy = yf - y0;

                for (int x=0; x<size; x++)
                {
                    float xf = ((float)x / (float)size) * ncols; //how far along in the x direction
                    float x0 = (float)Math.Floor(xf);
                    float dx = xf - x0;

                    //OK, so now we get four adjacent values with the corner (x0,y0) and bilinear interpolate using dx and dy
                    int xc = (int)x0, yc = (int)y0;
                    int xc2 = Math.Min(ncols-1, xc + 1); //work out opposite diagonal, but don't go off the edge
                    int yc2 = Math.Min(nrows-1, yc + 1);
                    float h0 = CoverageData[xc, yc]; //NOTE: xc and xc2 can be equal on the edge, same with y
                    float h1 = CoverageData[xc2, yc];
                    if (float.IsNaN(h0)) h0 = h1; //if one or other of h0,h1 is NaN, then that the other, if both then there's nothing you can do
                    else if (float.IsNaN(h1)) h1 = h0;
                    float h2 = CoverageData[xc, yc2];
                    float h3 = CoverageData[xc2, yc2];
                    if (float.IsNaN(h2)) h2 = h3; //same as above with h2,h3 and NaN
                    else if (float.IsNaN(h3)) h3 = h2;
                    //if we have a NaN in any of h0,h1,h2,h3, the val will be NaN too - that's by design
                    float bix0 = h0 + dx * (h1 - h0);
                    float bix1 = h2 + dy * (h2 - h2);
                    float val = bix0 + dy * (bix1 - bix0);
                    data[x, y] = val;
                }
            }
        }

    }
}
