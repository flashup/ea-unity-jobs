using System;
using System.Collections.Generic;
using System.Linq;
using Data;
using Helpers;
using UnityEngine;
using MathUtils = Helpers.MathUtils;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Parking
{
	public static class LpEncoding
	{
		private const int TOTAL_BITS_COUNT = 10;

		private const int NUM_SENSORS = 8;
		private const int BIAS_UNITS = 1;
		private const int GENES_PER_NUMBER = TOTAL_BITS_COUNT;
		private const int ENGINE_FORMULA_GENES_NUM = (NUM_SENSORS + BIAS_UNITS) * GENES_PER_NUMBER;
		private const int WHEELS_FORMULA_GENES_NUM = (NUM_SENSORS + BIAS_UNITS) * GENES_PER_NUMBER;

		public const int GENOME_LENGTH = ENGINE_FORMULA_GENES_NUM + WHEELS_FORMULA_GENES_NUM;

		private static readonly PrecisionConfig _precisionConfig = new()
		{
			SignBitsCount = 1,
			ExponentBitsCount = 4,
			FractionBitsCount = 5,
		};

		private static double LinearPolynomial(List<double> coefficients, float[] variables)
		{
			if (coefficients.Count != variables.Length + 1)
				throw new Exception("Количество коэффициентов и переменных не совпадает");

			double result = 0;
			coefficients.ForEachIndex((coefficient, coefficientIndex) =>
			{
				if (coefficientIndex < variables.Length)
				{
					result += coefficient * variables[coefficientIndex];
				}
				else
				{
					result += coefficient;
				}
			});

			return result;
		}

		public static (int engineSignal, int wheelsSignal) SensorsToSignals(Bit[] genes, float[] sensors)
		{
			return SensorsToSignals(DecodeGenome(genes), sensors);
		}

		public static (int engineSignal, int wheelsSignal) SensorsToSignals(DecodedGenome decodedGenome, float[] sensors)
		{
			var engineCoefficients = decodedGenome.engineFormulaCoefficients;
			var engineResult = LinearPolynomial(engineCoefficients, sensors);
			// TODO SigmoidToCategories 0.4999
			var engineSignal = SigmoidToCategories(MathUtils.Sigmoid(engineResult), 0.2f);

			var wheelsCoefficients = decodedGenome.wheelsFormulaCoefficients;
			var wheelsResult = LinearPolynomial(wheelsCoefficients, sensors);
			// TODO SigmoidToCategories 0.4999
			var wheelsSignal = SigmoidToCategories(MathUtils.Sigmoid(wheelsResult), 0.2f);

			return (engineSignal, wheelsSignal);
		}

		private static List<double> GenomeToNumbers(Bit[] genome, int genesPerNumber)
		{
			if (genome.Length % genesPerNumber != 0)
			{
				throw new Exception("Wrong number of genes");
			}

			var numbers = new List<double>();

			for (var numberIndex = 0; numberIndex < genome.Length; numberIndex += genesPerNumber)
			{
				var number = BitsToFloat10(genome.Slice(numberIndex, numberIndex + genesPerNumber).ToArray());
				numbers.Add(number);
			}

			return numbers;
		}

		private static double BitsToFloat10(Bit[] bits)
		{
			return BitsToFloat(bits, _precisionConfig);
		}
		
		public static double BitsToFloat(Bit[] bits, PrecisionConfig precisionConfig)
		{
			var signBitsCount = precisionConfig.SignBitsCount;
			var exponentBitsCount = precisionConfig.ExponentBitsCount;
			// var fractionBitsCount = precisionConfig.FractionBitsCount;

			var sign = bits[0] == 1 ? (sbyte)-1 : (sbyte)1;

			var exponentBias = Math.Pow(2, exponentBitsCount - 1) - 1;
			var exponentBits = bits.Slice(signBitsCount, signBitsCount + exponentBitsCount);
			
			var index = 0;
			var exponentUnbiased = exponentBits.Aggregate(0, (value, element) =>
			{
				var bitPowerOfTwo = (int)Math.Pow(2, exponentBitsCount - index - 1);
				index++;
				return value + (byte)element * bitPowerOfTwo;
			});
			var exponent = exponentUnbiased - exponentBias;

			var fractionBits = bits.Slice(signBitsCount + exponentBitsCount);

			var bitIndex = 0;
			var fraction = fractionBits.Aggregate(0d, (value, element) =>
			{
				var bitPowerOfTwo = Math.Pow(2, -(bitIndex + 1));
				// Debug.Log($"bitPowerOfTwo: {bitPowerOfTwo} ({bitIndex})");
				bitIndex++;
				return value + (byte)element * bitPowerOfTwo;
			});

			var result = sign * Math.Pow(2, exponent) * (1 + fraction);
			return result;
		}

		public static DecodedGenome DecodeGenome(Bit[] genes)
		{
			var engineGenes = genes.Slice(0, ENGINE_FORMULA_GENES_NUM).ToArray();
			var wheelsGenes = genes.Slice(
				ENGINE_FORMULA_GENES_NUM,
				ENGINE_FORMULA_GENES_NUM + WHEELS_FORMULA_GENES_NUM
			).ToArray();

			var engineFormulaCoefficients = GenomeToNumbers(engineGenes, GENES_PER_NUMBER);
			var wheelsFormulaCoefficients = GenomeToNumbers(wheelsGenes, GENES_PER_NUMBER);

			return new DecodedGenome
			{
				engineFormulaCoefficients = engineFormulaCoefficients,
				wheelsFormulaCoefficients = wheelsFormulaCoefficients,
			};
		}

		private static int SigmoidToCategories(double sigmoidValue, double aroundZeroMargin = 0.49999d)
		{
			if (sigmoidValue < (0.5d - aroundZeroMargin))
				return -1;

			if (sigmoidValue > (0.5d + aroundZeroMargin))
				return 1;

			return 0;
		}

#if UNITY_EDITOR
		[MenuItem("Test/Math")]
		public static void TestMath()
		{
			// var b = (Bit)0;
			// var b2 = (Bit)0;
			// Debug.Log(b == b2);
			
			Debug.Assert(
				BitsToFloat10(new Bit[] { 1, 0, 0, 0, 1, 0, 0, 0, 0, 0 }).Equals(-0.015625),
				"Error result of BitsToFloat10"
			);

			Debug.Assert(
				BitsToFloat10(new Bit[] { 0, 1, 1, 1, 1, 1, 1, 1, 1, 1 }).Equals(504d),
				"Error result of BitsToFloat10"
			);
		}

		[MenuItem("Test/LpGenetic")]
		public static void TestGenetic()
		{
			var genomeString =
				"1 0 1 1 1 1 0 1 0 0 1 1 0 1 1 1 1 0 0 0 1 1 1 1 1 1 0 0 0 0 1 0 1 1 1 0 0 1 1 1 0 1 1 1 1 1 0 1 0 0 1 1 0 1 0 0 1 1 0 0 0 1 1 1 0 1 0 1 0 0 1 0 1 1 1 1 0 0 0 1 1 1 0 0 0 0 0 0 1 1 0 0 1 1 1 0 1 1 1 1 1 0 0 0 1 0 0 1 1 0 1 1 0 1 0 1 0 1 0 0 1 0 0 1 0 1 1 0 1 1 1 1 0 0 0 0 1 1 0 1 0 1 0 0 0 0 0 0 0 0 1 0 0 1 1 1 1 0 1 1 0 1 0 1 0 0 1 0 0 0 0 0 0 1 0 0 0 0 1 1";
			var genes = genomeString.Split(" ").Select(value => (Bit)Convert.ToByte(value)).ToArray();

			var result = DecodeGenome(genes);

			var sensors = new[] { 0, 0, 0, 0, 2.4f, 0, 0, 0 };

			var coefficients = result.engineFormulaCoefficients;
			var rawResult = LpEncoding.LinearPolynomial(coefficients, sensors);
			var normalizedResult = MathUtils.Sigmoid(rawResult);
			var cat = SigmoidToCategories(normalizedResult);

			Debug.Assert(cat.Equals(1), "Error genome decoding");
		}
#endif
	}

	public class DecodedGenome
	{
		public List<double> engineFormulaCoefficients;
		public List<double> wheelsFormulaCoefficients;
	}
}