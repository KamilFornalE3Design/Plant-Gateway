using PlantGateway.Domain.Services.Engines.Abstractions;
using SMSgroup.Aveva.Config.Models.DTO;

namespace SMSgroup.Aveva.Utilities.Engines
{
    /// <summary>
    /// Performs 4x4 matrix multiplication on double[,] arrays.
    /// </summary>
    public sealed class TransformationEngine : IEngine
    {
        /// <summary>
        /// Multiplies two 4x4 matrices (result = local * parent).
        /// </summary>
        public double[,] Transform(MatrixMetadata local, double[,] parent)
        {
            if (local == null || parent == null)
                throw new ArgumentNullException("Matrix cannot be null");

            var result = new double[4, 4];

            for (int i = 0; i < 4; i++)
            {
                for (int j = 0; j < 4; j++)
                {
                    double sum = 0;
                    for (int k = 0; k < 4; k++)
                        sum += local.Value[i, k] * parent[k, j];
                    result[i, j] = sum;
                }
            }

            return result;
        }
    }
}
