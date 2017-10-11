using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Assets.Scripts
{
    /// <summary>
    /// Double vector class to handle geographic coordinates as Unity doesn't have one.
    /// </summary>
    class DVec3
    {
        public double x, y, z;
        public DVec3()
        {
            x = 0; y = 0; z = 0;
        }
        public DVec3(double x,double y,double z)
        {
            this.x = x;
            this.y = y;
            this.z = z;
        }

        #region Operators

        public static DVec3 operator +(DVec3 v1, DVec3 v2)
        {
            return new DVec3(v1.x + v2.x, v1.y + v2.y, v1.z + v2.z);
        }
        public static DVec3 operator -(DVec3 v1, DVec3 v2)
        {
            return new DVec3(v1.x - v2.x, v1.y - v2.y, v1.z - v2.z);
        }
        public static DVec3 operator *(DVec3 v1, DVec3 v2)
        {
            return new DVec3(v1.x * v2.x, v1.y * v2.y, v1.z * v2.z);
        }
        public static DVec3 operator /(DVec3 v1, DVec3 v2)
        {
            return new DVec3(v1.x / v2.x, v1.y / v2.y, v1.z / v2.z);
        }
        public static DVec3 operator *(double S, DVec3 v1)
        {
            return new DVec3(S * v1.x, S * v1.y, S * v1.z);
        }

        #endregion Operators

        #region Methods

        /// <summary>
        /// Scalar length, or magnitude of vector
        /// </summary>
        /// <param name="v"></param>
        /// <returns>A double value which is the length along the vector</returns>
        public static double length(DVec3 v)
        {
            return Math.Sqrt(v.x * v.x + v.y * v.y + v.z * v.z);

        }

        /// <summary>
        /// Normalise the vector and return the unit vector pointing in the same direction
        /// </summary>
        /// <param name="v"></param>
        /// <returns></returns>
        public static DVec3 normalize(DVec3 v)
        {
            double mag = DVec3.length(v);
            return new DVec3(v.x / mag, v.y / mag, v.z / mag);
        }

        #endregion Methods
    }
}
