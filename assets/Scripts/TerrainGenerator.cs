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
    /// Generate terrain for Unity from coverage data containing heights on a grid
    /// </summary>
    public class TerrainGenerator
    {
        protected float[,] CoverageData;
        int nrows, ncols;
        float cellsize;

        /// <summary>
        /// Create a terrain generator from a coverage grid of points and a size of cell.
        /// </summary>
        /// <param name="CoverageData">XY grid of points, regularly spaced with height data as the value</param>
        /// <param name="cellsize">Size of the XY cell spacing, which is needed to make the asset the same size as the Z value height from the data array. Otherwise you don't know how wide,deep and high the bounding box is.</param>
        public TerrainGenerator(float [,] CoverageData, float cellsize)
        {
            this.CoverageData = CoverageData;
            ncols = CoverageData.GetLength(0);
            nrows = CoverageData.GetLength(1);
            this.cellsize = cellsize;
        }

        /// <summary>
        /// Create a Unity terrain from the Lidar data.
        /// Creates a file in Assets/Filename.assets (where Filename is the name of the dem loaded without the path or extension).
        /// </summary>
        /// <param name="Filename">This determines the name given to the created asset - it strips off the filename with no extension</param>
        public void CreateTerrain(string Filename)
        {
            int potSize = 1025; //power of two size for the terrain data - we have to resample our data to fit this

            //first, we need to resample the data to fit the power of 2 texture
            float[,] data = new float[potSize, potSize];
            ResampleTerrainBilinear(potSize, ref data);

            //then we need to create a new array of height values here in order to get rid of the NODATA_values
            //first pass through data to determine maximum height
            float maxheight = -float.MaxValue;
            for (int y = 0; y < nrows; y++) for (int x = 0; x < ncols; x++) if (data[x, y] > maxheight) maxheight = data[x, y];
            Debug.Log("TerrainGenerator.CreateTerrain: maxheight=" + maxheight);

            //second pass to normalise the height
            for (int y = 0; y < potSize; y++)
            {
                for (int x = 0; x < potSize; x++)
                {
                    float val = data[x, y];
                    if (float.IsNaN(val)) val = 0; //I've set missing data values to zero in order that I can display something
                    if (val < 0) val = 0; //you can't have negative heights
                    data[x, y] = val / maxheight; //normalise height to 0..1
                }
            }

            TerrainData terrainData = new TerrainData();
            terrainData.heightmapResolution = potSize; // size of texture map;
            Debug.Log("TerrainGenerator.CreateTerrain: heightmapWidth=" + terrainData.heightmapWidth + " heightmapHeight=" + terrainData.heightmapWidth);
            //heightmapWidth and Height read only
            //heightmapScale readonly
            //terrainData.SetDetailResolution(1024,16)
            //terrainData.baseMapResolution=

            terrainData.size = new Vector3(ncols * cellsize, maxheight, nrows * cellsize);

            //terrainData.heightmapResolution = 512;
            //terrainData.baseMapResolution = 1024;
            //terrainData.SetDetailResolution(1024, terrainData.detailResolutionPerPatch);
            terrainData.SetHeights(0, 0, data);
            string Name = Path.GetFileNameWithoutExtension(Filename);
            AssetDatabase.CreateAsset(terrainData, "Assets/" + Name + ".asset");
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
        protected void ResampleTerrainBilinear(int size, ref float[,] data)
        {
            for (int y = 0; y < size; y++)
            {
                float yf = ((float)y / (float)size) * nrows; //how far we are along the CoverageData grid
                float y0 = (float)Math.Floor(yf);
                float dy = yf - y0;

                for (int x = 0; x < size; x++)
                {
                    float xf = ((float)x / (float)size) * ncols; //how far along in the x direction
                    float x0 = (float)Math.Floor(xf);
                    float dx = xf - x0;

                    //OK, so now we get four adjacent values with the corner (x0,y0) and bilinear interpolate using dx and dy
                    int xc = (int)x0, yc = (int)y0;
                    int xc2 = Math.Min(ncols - 1, xc + 1); //work out opposite diagonal, but don't go off the edge
                    int yc2 = Math.Min(nrows - 1, yc + 1);
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

        /// <summary>
        /// Export an OBJ file suitable for loading into an art package, for example to then export as STL for 3D printing.
        /// </summary>
        /// <param name="Filename"></param>
        public void ExportOBJFile(string Filename)
        {
            using (StreamWriter writer = new StreamWriter(Filename))
            {
                writer.WriteLine("#Wavefront obj file created by MapTube Lidar TerrainGenerator");

                //points - v 0.5 1.2 3.4
                for (int y=0; y<nrows; y++)
                {
                    for (int x=0; x<ncols; x++)
                    {
                        writer.WriteLine(string.Format("v {0} {1} {2}",((float)x)*cellsize,((float)y)*cellsize,CoverageData[x,y]));
                    }
                }

                //faces - f 1 2 3 (NOTE 1 based index for vertices)
                for (int y=0; y<nrows-1; y++)
                {
                    for (int x=0; x<ncols-1; x++)
                    {
                        //it's a square tile divided into two triangles
                        int v1 = x       +       y * ncols + 1;
                        int v2 = (x + 1) +       y * ncols + 1;
                        int v3 = (x + 1) + (y + 1) * ncols + 1;
                        int v4 = x       + (y + 1) * ncols + 1;
                        writer.WriteLine(string.Format("f {0} {1} {2}", v1, v2, v3));
                        writer.WriteLine(string.Format("f {0} {1} {2}", v1, v3, v4));
                    }
                }

                //TODO: up to this point we just have the top face. In order to 3D print you need a solid, so I could make it into a box here...
                //You need some additional points though.
            }

        }


    }
}
