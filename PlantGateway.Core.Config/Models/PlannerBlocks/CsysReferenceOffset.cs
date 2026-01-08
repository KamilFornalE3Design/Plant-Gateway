using System.Numerics;

namespace PlantGateway.Core.Config.Models.PlannerBlocks
{
    /// <summary>
    /// Represents a coordinate system reference used for relative positioning.
    /// Defines both local origin (translation) and orientation using a 4x4 matrix.
    /// </summary>
    public sealed class CsysReferenceOffset
    {
        /// <summary>
        /// The X (East) coordinate of the local origin in millimeters.
        /// </summary>
        public double OriginX { get; set; } = 0.0;

        /// <summary>
        /// The Y (North) coordinate of the local origin in millimeters.
        /// </summary>
        public double OriginY { get; set; } = 0.0;

        /// <summary>
        /// The Z (Up) coordinate of the local origin in millimeters.
        /// </summary>
        public double OriginZ { get; set; } = 0.0;

        /// <summary>
        /// The full 4x4 transformation matrix defining local orientation and translation.
        /// Stored in row-major layout. Default is identity.
        /// </summary>
        public double[,] Matrix4x4 { get; set; } = Identity();

        /// <summary>
        /// Builds a transformation from translation and rotation (3x3).
        /// </summary>
        public static CsysReferenceOffset FromComponents(
            (double X, double Y, double Z) origin,
            Matrix3x3 rotation)
        {
            var m = Identity();

            m[0, 0] = rotation.M11; m[0, 1] = rotation.M12; m[0, 2] = rotation.M13;
            m[1, 0] = rotation.M21; m[1, 1] = rotation.M22; m[1, 2] = rotation.M23;
            m[2, 0] = rotation.M31; m[2, 1] = rotation.M32; m[2, 2] = rotation.M33;

            m[3, 0] = origin.X;
            m[3, 1] = origin.Y;
            m[3, 2] = origin.Z;

            return new CsysReferenceOffset
            {
                OriginX = origin.X,
                OriginY = origin.Y,
                OriginZ = origin.Z,
                Matrix4x4 = m
            };
        }

        /// <summary>
        /// Returns an identity 4x4 matrix.
        /// </summary>
        public static double[,] Identity()
        {
            var m = new double[4, 4];
            for (int i = 0; i < 4; i++) m[i, i] = 1.0;
            return m;
        }

        /// <summary>
        /// Returns a human-readable summary (origin + orientation basis).
        /// </summary>
        public override string ToString()
        {
            var m = Matrix4x4;

            return $"Origin(E={m[3, 0]:F2}, N={m[3, 1]:F2}, U={m[3, 2]:F2})  |  " +
                   $"X→({m[0, 0]:F3},{m[0, 1]:F3},{m[0, 2]:F3})  " +
                   $"Y→({m[1, 0]:F3},{m[1, 1]:F3},{m[1, 2]:F3})  " +
                   $"Z→({m[2, 0]:F3},{m[2, 1]:F3},{m[2, 2]:F3})";
        }
    }

    /// <summary>
    /// Simple 3x3 rotation matrix structure (float or double interchangeable).
    /// </summary>
    public readonly struct Matrix3x3
    {
        public readonly double M11, M12, M13;
        public readonly double M21, M22, M23;
        public readonly double M31, M32, M33;

        public Matrix3x3(
            double m11, double m12, double m13,
            double m21, double m22, double m23,
            double m31, double m32, double m33)
        {
            M11 = m11; M12 = m12; M13 = m13;
            M21 = m21; M22 = m22; M23 = m23;
            M31 = m31; M32 = m32; M33 = m33;
        }

        public static Matrix3x3 Identity => new Matrix3x3(1, 0, 0, 0, 1, 0, 0, 0, 1);
    }
}
