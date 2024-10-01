using System;

namespace Evolution.Mutation.Functions
{
	public class RandomFloatFuncCommand : IMutationCommand<float>
	{
		private readonly float _min;
		private readonly float _range;
		private readonly Random _random;
		private readonly float _mutationRate;

		public RandomFloatFuncCommand(int mutationPercent, Random random, float min, float max)
		{
			_mutationRate = mutationPercent / 100f;
			_random = random;
			_min = min;

			_range = max - min;
		}

		private float GetRandomValue()
		{
			return _min + (float)_random.NextDouble() * _range;
		}

		public void Execute(float[] genomes)
		{
			for (int i = 0; i < genomes.Length; i++)
			{
				if (_random.NextDouble() < _mutationRate)
					genomes[i] = GetRandomValue();
			}
		}

		public void Dispose()
		{
		}
	}
}