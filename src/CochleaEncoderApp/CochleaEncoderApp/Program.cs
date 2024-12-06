using System;
using System.Collections.Generic;
using System.IO;
using MathNet.Numerics;
using NAudio.Wave;


namespace CochleaEncoderApp
{
    /// <summary>
    /// Main program class for encoding WAV files into SDRs.
    /// </summary>
    internal class Program
    {
        /// <summary>
        /// Main entry point of the program.
        /// </summary>
        /// <param name="args">Command-line arguments.</param>
        static void Main(string[] args)
        {
            string datapath = "free-spoken-digit-dataset/recordings/";
            double fs = 100000; // Target sampling frequency in Hz
            CochleaEncoder encoder = new CochleaEncoder(normalizeInput: false);

            List<string> matches = new List<string>
                {
                    Path.Combine(datapath, "0_george_1.wav")
                };

            bool overwriteAll = true; // Set this to false to skip existing files

            foreach (var filename in matches)
            {
                Console.WriteLine($"\nProcessing WAV file: {filename}");
                string outputFile = filename.Replace(".wav", ".sdr.npy");
                Console.WriteLine($"Output file path: {outputFile}");

                if (overwriteAll || !File.Exists(outputFile))
                {
                    float[] samples = ReadWavFile(filename, out int sampleRate);
                    float[] resampledSamples = Resample(samples, sampleRate, fs);

                    // Generate SDRs
                    var sdrs = WavToSDR(resampledSamples, fs);

                    // Save SDRs to file
                    SaveSDR(outputFile, sdrs);
                }
                else
                {
                    Console.WriteLine($"File {outputFile} already exists. Skipping processing.");
                }
            }
        }

        /// <summary>
        /// Converts WAV samples to SDRs.
        /// </summary>
        /// <param name="samples">Array of WAV samples.</param>
        /// <param name="fs">Sampling frequency.</param>
        /// <returns>List of SDR arrays.</returns>
        static List<int[]> WavToSDR(float[] samples, double fs)
        {
            // Placeholder MFCC generation
            int numMFCC = 16; // Number of MFCC coefficients
            double[][] mfccs = GenerateMFCC(samples, fs, numMFCC);

            double mean = Mean(mfccs);
            double stddev = StdDev(mfccs);

            // Normalize MFCCs
            for (int i = 0; i < mfccs.Length; i++)
            {
                for (int j = 0; j < mfccs[i].Length; j++)
                {
                    mfccs[i][j] = (mfccs[i][j] - mean) / stddev;
                }
            }

            double resolution = Math.Max(0.001, (Max(mfccs) - Min(mfccs)) / 1024);
            var rdse = new RandomDistributedScalarEncoder(resolution, 512, 21, mean);

            List<int[]> sdrs = new List<int[]>();

            // Encode MFCCs into SDRs
            foreach (var mfccRow in mfccs)
            {
                List<int> combinedSDR = new List<int>();
                foreach (var coef in mfccRow)
                {
                    int[] sdr = new int[rdse.OutputBits];
                    rdse.EncodeIntoArray(coef, sdr);
                    combinedSDR.AddRange(sdr);
                }
                int[] combinedSDRArray = combinedSDR.ToArray();
                sdrs.Add(combinedSDRArray);

                // Print the SDR to the output window
                Console.WriteLine(string.Join("", combinedSDRArray));
            }

            return sdrs;
        }

        /// <summary>
        /// Reads a WAV file and returns the samples.
        /// </summary>
        /// <param name="filepath">Path to the WAV file.</param>
        /// <param name="sampleRate">Output sample rate.</param>
        /// <returns>Array of samples.</returns>
        static float[] ReadWavFile(string filepath, out int sampleRate)
        {
            using (var reader = new AudioFileReader(filepath))
            {
                sampleRate = reader.WaveFormat.SampleRate;
                var samples = new List<float>();
                float[] buffer = new float[reader.WaveFormat.SampleRate];
                int bytesRead;
                while ((bytesRead = reader.Read(buffer, 0, buffer.Length)) > 0)
                {
                    for (int i = 0; i < bytesRead; i++)
                    {
                        samples.Add(buffer[i]);
                    }
                }
                return samples.ToArray();
            }
        }

        /// <summary>
        /// Resamples the given samples to the target sample rate.
        /// </summary>
        /// <param name="samples">Array of samples.</param>
        /// <param name="inputSampleRate">Input sample rate.</param>
        /// <param name="targetSampleRate">Target sample rate.</param>
        /// <returns>Resampled array of samples.</returns>
        static float[] Resample(float[] samples, int inputSampleRate, double targetSampleRate)
        {
            double ratio = targetSampleRate / inputSampleRate;
            int newLength = (int)(samples.Length * ratio);
            var x = new double[samples.Length];
            for (int i = 0; i < samples.Length; i++)
            {
                x[i] = i;
            }
            var interpolator = MathNet.Numerics.Interpolation.CubicSpline.InterpolateNatural(x, samples.Select(s => (double)s));
            var resampled = new float[newLength];
            for (int i = 0; i < newLength; i++)
            {
                double time = i / targetSampleRate * inputSampleRate;
                resampled[i] = (float)interpolator.Interpolate(time);
            }
            return resampled;
        }

        /// <summary>
        /// Generates MFCCs from the given samples.
        /// </summary>
        /// <param name="samples">Array of samples.</param>
        /// <param name="sampleRate">Sample rate.</param>
        /// <param name="numCoefficients">Number of MFCC coefficients.</param>
        /// <returns>2D array of MFCCs.</returns>
        static double[][] GenerateMFCC(float[] samples, double sampleRate, int numCoefficients)
        {
            int numFrames = samples.Length / 400; // Example: 400-sample frames
            double[][] mfccs = new double[numFrames][];
            for (int i = 0; i < numFrames; i++)
            {
                mfccs[i] = new double[numCoefficients];
                for (int j = 0; j < numCoefficients; j++)
                {
                    mfccs[i][j] = Math.Sin(i + j); // Placeholder values
                }
            }
            return mfccs;
        }

        /// <summary>
        /// Calculates the mean of the given 2D array.
        /// </summary>
        /// <param name="data">2D array of data.</param>
        /// <returns>Mean value.</returns>
        static double Mean(double[][] data)
        {
            double sum = 0;
            int count = 0;
            foreach (var row in data)
            {
                foreach (var value in row)
                {
                    sum += value;
                    count++;
                }
            }
            return sum / count;
        }

        /// <summary>
        /// Calculates the standard deviation of the given 2D array.
        /// </summary>
        /// <param name="data">2D array of data.</param>
        /// <returns>Standard deviation value.</returns>
        static double StdDev(double[][] data)
        {
            double mean = Mean(data);
            double sumSquares = 0;
            int count = 0;
            foreach (var row in data)
            {
                foreach (var value in row)
                {
                    sumSquares += Math.Pow(value - mean, 2);
                    count++;
                }
            }
            return Math.Sqrt(sumSquares / count);
        }

        /// <summary>
        /// Finds the maximum value in the given 2D array.
        /// </summary>
        /// <param name="data">2D array of data.</param>
        /// <returns>Maximum value.</returns>
        static double Max(double[][] data)
        {
            double max = double.MinValue;
            foreach (var row in data)
            {
                foreach (var value in row)
                {
                    if (value > max) max = value;
                }
            }
            return max;
        }

        /// <summary>
        /// Finds the minimum value in the given 2D array.
        /// </summary>
        /// <param name="data">2D array of data.</param>
        /// <returns>Minimum value.</returns>
        static double Min(double[][] data)
        {
            double min = double.MaxValue;
            foreach (var row in data)
            {
                foreach (var value in row)
                {
                    if (value < min) min = value;
                }
            }
            return min;
        }

        /// <summary>
        /// Saves the SDRs to a file.
        /// </summary>
        /// <param name="filepath">Path to the output file.</param>
        /// <param name="sdrs">List of SDR arrays.</param>
        static void SaveSDR(string filepath, List<int[]> sdrs)
        {
            using (var writer = new StreamWriter(filepath))
            {
                foreach (var sdr in sdrs)
                {
                    writer.WriteLine(string.Join("", sdr));
                }
            }
        }
    }
}
