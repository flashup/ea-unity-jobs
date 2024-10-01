using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Random = System.Random;

namespace Evolution.Mutation.Jobs
{
	public class RandomFloatJobCommand : IMutationCommand<float>
	{
		private static readonly int _batchCount = JobSettings.BatchCount;

		private readonly float _min;
		private readonly float _range;
		private readonly Random _random;
		private readonly float _mutationRate;
		private readonly int _allGenomesLength;

		private NativeArray<float> _genomes;

		public RandomFloatJobCommand(
			int mutationPercent,
			Random random,
			float min,
			float max,
			int numGenomes,
			int genomeLength)
		{
			_mutationRate = mutationPercent / 100f;
			_random = random;
			_min = min;
			_range = max - min;

			_allGenomesLength = numGenomes * genomeLength;
			_genomes = new NativeArray<float>(_allGenomesLength, Allocator.Persistent);
		}

		public void Execute(float[] genomes)
		{
			for (int i = 0; i < _allGenomesLength; i++)
				_genomes[i] = genomes[i];

			var r = new Unity.Mathematics.Random((uint)_random.Next(1, 10000));

			var job = new RandomFloatMutationJob
			{
				mutationRate = _mutationRate,
				min = _min,
				range = _range,
				r = r,
				genomes = _genomes,
			};

			var handle = job.Schedule(_allGenomesLength, _batchCount);
			handle.Complete();

			for (int i = 0; i < _allGenomesLength; i++)
			{
				genomes[i] = _genomes[i];
			}
		}

		public void Dispose()
		{
			if (_genomes.IsCreated) _genomes.Dispose();
		}
	}

	[BurstCompile]
	internal struct RandomFloatMutationJob : IJobParallelFor
	{
		[ReadOnly] public float mutationRate;
		[ReadOnly] public float min;
		[ReadOnly] public float range;
		[ReadOnly] public Unity.Mathematics.Random r;

		[WriteOnly] public NativeArray<float> genomes;

		public void Execute(int index)
		{
			if (r.NextFloat() < mutationRate)
				genomes[index] = min + r.NextFloat() * range;
		}
	}
}