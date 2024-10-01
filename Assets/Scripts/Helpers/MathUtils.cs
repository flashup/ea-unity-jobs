using System;
using System.Linq;
using Evolution;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Helpers
{
	public static class MathUtils
	{
		public static int WeightedRandomIndex(int[] weights)
		{
			var totalWeight = weights.Sum();
			var seed = Random.Range(0, totalWeight);

			var sum = 0;

			for (var i = 0; i < weights.Length; i++)
			{
				var value = weights[i];
				sum += value;

				if (sum > seed)
					return i;
			}

			Debug.LogWarning($"WeightedRandomIndex: {string.Join(",", weights)}");
			return weights.Length - 1;
		}

		public static double Sigmoid(double x)
		{
			return 1 / (1 + Math.Pow(Math.E, -x));
		}
	}

	public struct PrecisionConfig
	{
		public int SignBitsCount;
		public int ExponentBitsCount;
		public int FractionBitsCount;
	}
}