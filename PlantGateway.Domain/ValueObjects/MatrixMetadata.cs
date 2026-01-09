using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PlantGateway.Domain.ValueObjects
{
    public sealed class MatrixMetadata
    {
        public double[,] Value { get; set; } = new double[4, 4];
        public bool HasInput { get; set; } = false;
        public MatrixOrigin Origin { get; set; } = MatrixOrigin.None;

        public bool IsValid =>
            Value != null &&
            Value.Length == 16 &&
            !IsZeroMatrix(Value);

        private static bool IsZeroMatrix(double[,] m)
        {
            for (int i = 0; i < 4; i++)
                for (int j = 0; j < 4; j++)
                    if (Math.Abs(m[i, j]) > 1e-9) return false;
            return true;
        }
    }
}
