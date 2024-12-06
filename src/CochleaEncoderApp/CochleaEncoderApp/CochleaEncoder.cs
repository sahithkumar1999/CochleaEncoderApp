using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CochleaEncoderApp
{
    /// <summary>
    /// Class for encoding audio data into a neurogram.
    /// </summary>
    public class CochleaEncoder
    {
        private readonly double SamplingFrequency;
        private readonly bool NormalizeInput;

        /// <summary>
        /// Initializes a new instance of the <see cref="CochleaEncoder"/> class.
        /// </summary>
        /// <param name="fs">Sampling frequency in Hz.</param>
        /// <param name="normalizeInput">Whether to normalize the input data.</param>
        public CochleaEncoder(double fs = 100000, bool normalizeInput = true)
        {
            SamplingFrequency = fs;
            NormalizeInput = normalizeInput;
        }

        /// <summary>
        /// Gets the output width of the neurogram.
        /// </summary>
        public int OutputWidth => 1024;

        /// <summary>
        /// Encodes the input data into a neurogram.
        /// </summary>
        /// <param name="inputData">Array of input data samples.</param>
        /// <returns>2D array representing the neurogram.</returns>
        public float[,] EncodeIntoNeurogram(float[] inputData)
        {
            // Normalize the input data if required
            if (NormalizeInput)
            {
                for (int i = 0; i < inputData.Length; i++)
                {
                    inputData[i] = inputData[i] / (float)Math.Pow(2, 15);
                }
            }

            // Calculate the number of time steps
            int timeSteps = inputData.Length / OutputWidth;
            float[,] neurogram = new float[OutputWidth, timeSteps];

            // Encode the input data into the neurogram
            for (int cf = 0; cf < OutputWidth; cf++)
            {
                for (int t = 0; t < timeSteps; t++)
                {
                    neurogram[cf, t] = (inputData[t * OutputWidth + cf] > 0) ? 1 : 0;
                }
            }

            return neurogram;
        }
    }
}
