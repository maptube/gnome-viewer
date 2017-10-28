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
        /// TODO: potSize could be a user parameter, but you're a bit limited in values: 513, 1025, 2049? It gets too big after that.
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
            for (int y = 0; y < potSize; y++) for (int x = 0; x < potSize; x++) if (data[x, y] > maxheight) maxheight = data[x, y];
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
        /// ALSO, the bilinear isn't the best as it assumes the square of four points is planar, rather than using two triangular faces.
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
        /// Split a coverage grid of heights into a list of smaller coverages. The primary reason for doing this is to split up a model for 3D printing so it can be made physically bigger.
        /// For example, if you pass in XCount=4 and YCount=4, then the CoverageData gets split up into 16 squares of equal size which are returned as a list of float arrays (top row (y=0) first left to right).
        /// </summary>
        /// <param name="CoverageData"></param>
        /// <param name="XCount">Number of grid squares to make in the X direction</param>
        /// <param name="YCount">Number of grid squares to make in the Y direction</param>
        /// <returns></returns>
        public static List<float[,]> SplitTerrain(float [,] CoverageData, int XCount, int YCount)
        {
            int ncols = CoverageData.GetLength(0);
            int nrows = CoverageData.GetLength(1);
            List<float[,]> Result = new List<float[,]>();
            int sizeX = ncols / XCount;
            int sizeY = nrows / YCount;
            for (int y=0; y<YCount; y++)
            {
                for (int x=0; x<XCount; x++)
                {
                    int MinX = x * sizeX;
                    int MaxX = (x + 1) * sizeX;
                    if (MaxX > ncols) MaxX = ncols;
                    int MinY = y * sizeY;
                    int MaxY = (y + 1) * sizeY;
                    if (MaxY > nrows) MaxY = nrows;
                    int dimX = MaxX - MinX, dimY = MaxY - MinY;
                    float[,] data = new float[dimX, dimY];
                    for (int dy=0; dy<dimY; dy++)
                    {
                        for (int dx=0; dx<dimX; dx++)
                        {
                            data[dx, dy] = CoverageData[MinX + dx,MinY+dy];
                        }
                    }
                    Result.Add(data);
                }
            }

            return Result;
        }

        #region OBJExporter

        /// <summary>
        /// Assumes that an array of points have been created for the OBJ conversion via a for y for x pair of nested loops.
        /// In this situation, the vertex number for a specific point on the grid can ve worked out easily using the formula below.
        /// </summary>
        /// <param name="xcol">The number of columns across for the desired point (NOT its x coordinate)</param>
        /// <param name="yrow">The number of rows down for the desired point (NOT its y coordinate)</param>
        /// <returns>The vertex number of the required position of the point on the xcol, yrow position in the grid. NOTE the +1 as OBJ vertices are 1 based.</returns>
        private int OBJPointIndex(int xcol, int yrow)
        {
            int v = xcol + yrow * ncols + 1;
            return v;
        }

        /// <summary>
        /// Assumes that points for the base have been created for the OBJ conversion as follows:
        /// 4 corner points (0,0), (0,ymax), (xmax,ymax), (xmax,0)
        /// y=0 z=-1 row
        /// y=nrows z=-1 row
        /// x=0 z=-1 column
        /// x=ncols-1 z=-1 column
        /// Given the X and Y position (same as for OBJPointIndex), return the index of the point.
        /// NOTE: only the points along the four base edges and corners are defined. Anything else will raise an error.
        /// IMPORTANT: pont numbers start from 1, so all have this offset added, so first corner index=1 and everything follows that
        /// </summary>
        /// <param name="xcol"></param>
        /// <param name="yrow"></param>
        /// <returns></returns>
        private int OBJBasePointIndex(int xcol, int yrow)
        {
            //defines base of 4 edges based on number of columns and rows - this is used as an offset by the caller to get the vertex number
            int basey0 = 5; //5 is the index of the first point after the 4 corners
            int baseymax = basey0 + ncols - 2;
            int basex0 = baseymax + ncols - 2;
            int basexmax = basex0 + nrows - 2;
            if (xcol==0) //base x min edge condition
            {
                if (yrow == 0) return 1; //x=0, y=0 corner
                else if (yrow == nrows - 1) return 2; //x=0, y=max corner
                else //point along x=0 edge
                {
                    return basex0 + (yrow - 1); //minus 1 because of no corner to the edge line (the three cases following are the same)
                }
            }
            else if (xcol==ncols-1) //base x max edge condition
            {
                if (yrow == 0) return 4; //x=max, y=0 corner
                else if (yrow == nrows - 1) return 3; //x=max, y=max corner
                else //point along x=max edge
                {
                    return basexmax + (yrow - 1);
                }
            }
            else if (yrow==0) //base y min edge condition
            {
                //corners already picked up, so only need edge index
                return basey0 + (xcol - 1);
            }
            else if (yrow==nrows-1) //base y max edge condition
            {
                //corners already picked up, so only need edge index
                return baseymax + (xcol - 1);
            }
            return -1; //AND THROW AN ERROR! should never happen
        }

        /// <summary>
        /// Export an OBJ file suitable for loading into an art package, for example to then export as STL for 3D printing.
        /// </summary>
        /// <param name="Filename"></param>
        public void ExportOBJFile(string Filename)
        {
            const float ZDepth = -10.0f; //how deep to make the plinth that the landscape sits on

            using (StreamWriter writer = new StreamWriter(Filename))
            {
                writer.WriteLine("#Wavefront obj file created by MapTube Lidar TerrainGenerator");

                int p = 0; //point counter
                //points - v 0.5 1.2 3.4
                for (int y=0; y<nrows; y++)
                {
                    for (int x=0; x<ncols; x++)
                    {
                        writer.WriteLine(string.Format("v {0} {1} {2}",((float)x)*cellsize,((float)y)*cellsize,CoverageData[x,y]));
                        p++;
                    }
                }
                //points forming base and sides
                //need points all around the base edges of the solid block to match the grid above - set height at -1 so as not to interfere with the landscape mesh
                //corners first
                int basecorners = p;
                writer.WriteLine("#Base corners");
                writer.WriteLine(string.Format("v {0} {1} {2}", 0, 0, ZDepth)); p++;
                writer.WriteLine(string.Format("v {0} {1} {2}", 0, ((float)(nrows-1))*cellsize, ZDepth)); p++;
                writer.WriteLine(string.Format("v {0} {1} {2}", ((float)(ncols-1))*cellsize, ((float)(nrows - 1)) * cellsize, ZDepth)); p++;
                writer.WriteLine(string.Format("v {0} {1} {2}", ((float)(ncols - 1)) * cellsize, 0, ZDepth)); p++;
                //now the four edge lines
                //y=0 z=-1 row
                writer.WriteLine("#Base y=0 z=-1 line");
                int basey0 = p;
                for (int x=1; x<ncols-1; x++) //NOTE missing first and last points, which make the corners
                {
                    writer.WriteLine(string.Format("v {0} {1} {2}", ((float)x) * cellsize, 0, ZDepth));
                    p++;
                }
                //y=nrows z=-1 row
                writer.WriteLine("#Base y=nrows z=-1 line");
                int baseymax = p;
                float yordinate = ((float)(nrows-1)) * cellsize;
                for (int x = 1; x < ncols-1; x++) //NOTE missing first and last points, which make the corners
                {
                    writer.WriteLine(string.Format("v {0} {1} {2}", ((float)x) * cellsize, yordinate, ZDepth));
                    p++;
                }
                //x=0 z=-1 column - NOTE: the first and last points duplicate the corners
                writer.WriteLine("#Base x=0 z=-1 line");
                int basex0 = p;
                for (int y = 1; y < nrows-1; y++) //Note missing first and last points, which make the corners
                {
                    writer.WriteLine(string.Format("v {0} {1} {2}", 0, ((float)y) * cellsize, ZDepth));
                    p++;
                }
                //x=ncols-1 z=-1 column - NOTE: the first and last points duplicate the corners
                writer.WriteLine("#Base x=ncols-1 z=-1 line");
                int basexmax = p;
                float xordinate = ((float)(ncols - 1)) * cellsize;
                for (int y = 1; y < nrows-1; y++) //NOTE missing first and last points, which make the corners
                {
                    writer.WriteLine(string.Format("v {0} {1} {2}", xordinate, ((float)y) * cellsize, ZDepth));
                    p++;
                }


                //////

                //faces - f 1 2 3 (NOTE 1 based index for vertices)
                for (int y=0; y<nrows-1; y++)
                {
                    for (int x=0; x<ncols-1; x++)
                    {
                        //it's a square tile divided into two triangles
                        //int v1 = x       +       y * ncols + 1;
                        //int v2 = (x + 1) +       y * ncols + 1;
                        //int v3 = (x + 1) + (y + 1) * ncols + 1;
                        //int v4 = x       + (y + 1) * ncols + 1;
                        int v1 = OBJPointIndex(x, y);
                        int v2 = OBJPointIndex(x + 1, y);
                        int v3 = OBJPointIndex(x + 1, y + 1);
                        int v4 = OBJPointIndex(x, y + 1);
                        writer.WriteLine(string.Format("f {0} {1} {2}", v1, v2, v3));
                        writer.WriteLine(string.Format("f {0} {1} {2}", v1, v3, v4));
                    }
                }

                //side faces
                //up to this point we just have the top face. In order to 3D print you need a solid, so I could make it into a box here...
                //y=0 face
                writer.WriteLine("#y=0 end face");
                for (int x=0; x<ncols-1; x++)
                {
                    int v1 = OBJBasePointIndex(x,0)+basecorners; // basey0 + x + 1;
                    int v2 = OBJPointIndex(x, 0);
                    int v3 = OBJPointIndex(x + 1, 0);
                    int v4 = OBJBasePointIndex(x+1,0) + basecorners; // basey0 + (x + 1) + 1;
                    writer.WriteLine(string.Format("f {0} {1} {2}", v1, v2, v3));
                    writer.WriteLine(string.Format("f {0} {1} {2}", v1, v3, v4));
                }
                //y=nrows-1 face
                writer.WriteLine("#y=nrows-1 end face");
                for (int x = 0; x < ncols - 1; x++)
                {
                    int v1 = OBJBasePointIndex(x,nrows-1)+basecorners; // baseymax + x + 1;
                    int v2 = OBJPointIndex(x, nrows-1);
                    int v3 = OBJPointIndex(x + 1, nrows-1);
                    int v4 = OBJBasePointIndex(x+1,nrows-1) + basecorners; //baseymax + (x + 1) + 1;
                    writer.WriteLine(string.Format("f {0} {1} {2}", v3, v2, v1)); //NOTE backwards from above as on the other side
                    writer.WriteLine(string.Format("f {0} {1} {2}", v4, v3, v1));
                }
                //x=0 face
                writer.WriteLine("#x=0 end face");
                for (int y = 0; y < nrows - 1; y++)
                {
                    int v1 = OBJBasePointIndex(0,y) + basecorners; //basex0 + y + 1;
                    int v2 = OBJPointIndex(0, y);
                    int v3 = OBJPointIndex(0, y+1);
                    int v4 = OBJBasePointIndex(0,y+1)+basecorners; //basex0 + (y + 1) + 1;
                    writer.WriteLine(string.Format("f {0} {1} {2}", v1, v2, v3));
                    writer.WriteLine(string.Format("f {0} {1} {2}", v1, v3, v4));
                }
                //x=ncols-1 face
                writer.WriteLine("#x=ncols-1 end face");
                for (int y = 0; y < nrows - 1; y++)
                {
                    int v1 = OBJBasePointIndex(ncols-1,y) + basecorners; //basexmax + y + 1;
                    int v2 = OBJPointIndex(ncols-1, y);
                    int v3 = OBJPointIndex(ncols-1, y + 1);
                    int v4 = OBJBasePointIndex(ncols-1,y+1) + basecorners; //basexmax + (y + 1) + 1;
                    writer.WriteLine(string.Format("f {0} {1} {2}", v3, v2, v1)); //NOTE backwards from above as on the other side
                    writer.WriteLine(string.Format("f {0} {1} {2}", v4, v3, v1));
                }
                //finally, wire up the bottom face, that's fan left and right single tile edge and tri strip the middle bits
                writer.WriteLine("#bottom face");
                //base y=0 row to x=0 column fan AND in parallel, the other side fan y=0 row 0 to x=nrows-1 column fan
                int lv1 = OBJBasePointIndex(1,0)+basecorners; // basey0+1; //left
                int rv1 = OBJBasePointIndex(ncols-2,0) + basecorners; // basey0 + nrows - 2; //right
                for (int y=0; y<nrows-2; y++)
                {
                    int lv2 = OBJBasePointIndex(0,y) + basecorners; // basex0 + y + 1;
                    int lv3 = OBJBasePointIndex(0, y+1) + basecorners; ; // basex0 + (y + 1) + 1;
                    writer.WriteLine(string.Format("f {0} {1} {2}", lv3, lv2, lv1));
                    int rv2 = OBJBasePointIndex(ncols - 1, y) + basecorners; //basexmax + y + 1;
                    int rv3 = OBJBasePointIndex(ncols - 1, y+1) + basecorners; //basexmax + (y + 1) + 1;
                    writer.WriteLine(string.Format("f {0} {1} {2}", rv1, rv2, rv3)); //NOTE backwards from previous to maintain winding
                }
                //now tristrip the middle
                for (int x=0; x<nrows-1; x++)
                {
                    int v1 = OBJBasePointIndex(x,0) + basecorners; //basey0 + x + 1;
                    int v2 = OBJBasePointIndex(x,nrows-1) + basecorners; //baseymax + x + 1;
                    int v3 = OBJBasePointIndex(x+1,nrows-1) + basecorners; //baseymax + (x + 1) + 1;
                    int v4 = OBJBasePointIndex(x+1,0) + basecorners; //basey0 + (x + 1) + 1;
                    //skip the first and last triangles which were tri-fanned in the previous block
                    if (x!=0)
                        writer.WriteLine(string.Format("f {0} {1} {2}", v1, v2, v4));
                    if (x!=(nrows-1))
                        writer.WriteLine(string.Format("f {0} {1} {2}", v4, v2, v3));
                }
            }

        }

        #endregion OBJExporter


    }
}
