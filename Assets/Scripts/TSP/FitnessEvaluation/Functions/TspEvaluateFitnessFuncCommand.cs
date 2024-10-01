using System.Collections.Generic;
using Unity.Collections;

namespace TSP.FitnessEvaluation.Functions
{
	public class TspEvaluateFitnessFuncCommand : ITspEvaluateFitnessCommand
	{
		private readonly Dictionary<int, int> _distances;

		private NativeArray<float> _fitnessValues;

		public TspEvaluateFitnessFuncCommand(Dictionary<int, int> distances, int populationSize)
		{
			_distances = distances;
			
			_fitnessValues = new NativeArray<float>(populationSize, Allocator.Persistent);
		}

		public NativeArray<float> Execute(Path[] generation)
		{
			for (var i = 0; i < generation.Length; i++)
				_fitnessValues[i] = DistanceToFitness(MeasureDistance(generation[i].cities));

			return _fitnessValues;
		}

		private int MeasureDistance(int[] cities)
		{
			var distance = 0;

			for (int i = 0; i < cities.Length - 1; i++)
			{
				var fromId = cities[i];
				var toId = cities[i + 1];
				distance += _distances[fromId * 10_000 + toId];
			}

			return distance;
		}

		private static float DistanceToFitness(int distance)
		{
			// less path = better fitness
			return 1000f / (1 + distance);
		} 

		public void Dispose()
		{
			if (_fitnessValues.IsCreated) _fitnessValues.Dispose();
		}
	}
}