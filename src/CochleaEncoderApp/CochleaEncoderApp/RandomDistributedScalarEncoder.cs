using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CochleaEncoderApp
{
    /// <summary>
    /// Class for encoding scalar values into a sparse distributed representation (SDR).
    /// </summary>
    public class RandomDistributedScalarEncoder
    {
        /// <summary>
        /// Gets the number of output bits.
        /// </summary>
        public int OutputBits { get; private set; }

        private double Resolution;
        private double Offset;

        /// <summary>
        /// Initializes a new instance of the <see cref="RandomDistributedScalarEncoder"/> class.
        /// </summary>
        /// <param name="resolution">Resolution of the encoder.</param>
        /// <param name="n">Number of output bits.</param>
        /// <param name="w">Width of the active bits.</param>
        /// <param name="offset">Offset value for encoding.</param>
        public RandomDistributedScalarEncoder(double resolution, int n, int w, double offset = 0)
        {
            Resolution = resolution;
            OutputBits = n;
            Offset = offset;
        }

        /// <summary>
        /// Encodes a scalar value into an SDR array.
        /// </summary>
        /// <param name="value">The scalar value to encode.</param>
        /// <param name="output">The output array to store the SDR.</param>
        public void EncodeIntoArray(double value, int[] output)
        {
            // Clear the output array
            Array.Clear(output, 0, output.Length);

            // Calculate the center index based on the value, offset, and resolution
            int center = (int)((value - Offset) / Resolution);

            // Set the active bits in the output array
            for (int i = center - 10; i <= center + 10; i++)
            {
                int index = (i + OutputBits) % OutputBits;
                if (index >= 0 && index < output.Length)
                {
                    output[index] = 1;
                }
            }
        }
    }
}
