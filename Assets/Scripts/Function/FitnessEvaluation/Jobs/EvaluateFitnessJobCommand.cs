using System;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

namespace Function.FitnessEvaluation.Jobs
{
	public class EvaluateFitnessJobCommand : IEvaluateFitnessCommand
	{
		private NativeArray<float> _populationGenomes;

		private NativeArray<float> _fitnessValues;

		private readonly int _populationSize;
		private readonly int _genomeSize;

		private readonly FitnessFunction _fitnessFunction;

		private static readonly int _batchCount = JobSettings.BatchCount;

		public EvaluateFitnessJobCommand(FitnessFunction fitnessFunction, int populationSize, int genomeSize)
		{
			_fitnessFunction = fitnessFunction;
			_populationSize = populationSize;
			_genomeSize = genomeSize;

			_populationGenomes = new NativeArray<float>(populationSize * genomeSize, Allocator.Persistent);
			_fitnessValues = new NativeArray<float>(populationSize, Allocator.Persistent);
		}

		public NativeArray<float> Execute(Point[] generation)
		{
			for (int i = 0; i < _populationSize; i++)
			{
				var point = generation[i];
				for (int j = 0; j < _genomeSize; j++)
				{
					_populationGenomes[i * _genomeSize + j] = point.coordinates[j];
				}
			}

			RunJob();

			return _fitnessValues;
		}
		
		// inverse relationship
		// less value = better fitness
		// private static float ScoreToFitness(float value)
		// {
		// 	if (value < 0) value = -value;
		// 	return 1.0f - (value / (value + 1.0f));
		// }

		private void RunJob()
		{
			switch (_fitnessFunction)
			{
				case FitnessFunction.Rastrigin:
				{
					var job = new FitnessRastriginJob
					{
						populationGenomes = _populationGenomes,
						fitness = _fitnessValues,
						genomeSize = _genomeSize,
					};
					var handle = job.Schedule(_populationSize, _batchCount);
					handle.Complete();
					break;
				}

				case FitnessFunction.Rosenbrock:
				{
					var job = new FitnessRosenbrockJob
					{
						populationGenomes = _populationGenomes,
						fitness = _fitnessValues,
						genomeSize = _genomeSize,
					};
					var handle = job.Schedule(_populationSize, _batchCount);
					handle.Complete();
					break;
				}

				case FitnessFunction.Ackley:
				{
					var job = new FitnessAckleyJob
					{
						populationGenomes = _populationGenomes,
						fitness = _fitnessValues,
						genomeSize = _genomeSize,
					};
					var handle = job.Schedule(_populationSize, _batchCount);
					handle.Complete();
					break;
				}
	
				case FitnessFunction.Levy:
				{
					var job = new FitnessLevyJob
					{
						populationGenomes = _populationGenomes,
						fitness = _fitnessValues,
						genomeSize = _genomeSize,
					};
					var handle = job.Schedule(_populationSize, _batchCount);
					handle.Complete();
					break;
				}

				case FitnessFunction.Schwefel:
				{
					var job = new FitnessSchwefelJob
					{
						populationGenomes = _populationGenomes,
						fitness = _fitnessValues,
						genomeSize = _genomeSize,
					};
					var handle = job.Schedule(_populationSize, _batchCount);
					handle.Complete();
					break;
				}

				case FitnessFunction.Comp1:
				{
					var job = new FitnessComplexityO1Job
					{
						populationGenomes = _populationGenomes,
						fitness = _fitnessValues,
						genomeSize = _genomeSize,
					};
					var handle = job.Schedule(_populationSize, _batchCount);
					handle.Complete();
					break;
				}
				
				case FitnessFunction.CompLogN:
				{
					var job = new FitnessComplexityOLogNJob
					{
						populationGenomes = _populationGenomes,
						fitness = _fitnessValues,
						genomeSize = _genomeSize,
					};
					var handle = job.Schedule(_populationSize, _batchCount);
					handle.Complete();
					break;
				}

				case FitnessFunction.CompN:
				{
					var job = new FitnessComplexityOnJob
					{
						populationGenomes = _populationGenomes,
						fitness = _fitnessValues,
						genomeSize = _genomeSize,
					};
					var handle = job.Schedule(_populationSize, _batchCount);
					handle.Complete();
					break;
				}

				case FitnessFunction.CompNLogN:
				{
					var job = new FitnessComplexityONLogNJob
					{
						populationGenomes = _populationGenomes,
						fitness = _fitnessValues,
						genomeSize = _genomeSize,
					};
					var handle = job.Schedule(_populationSize, _batchCount);
					handle.Complete();
					break;
				}

				case FitnessFunction.CompN2:
				{
					var job = new FitnessComplexityOn2Job
					{
						populationGenomes = _populationGenomes,
						fitness = _fitnessValues,
						genomeSize = _genomeSize,
					};
					var handle = job.Schedule(_populationSize, _batchCount);
					handle.Complete();
					break;
				}

				// case FitnessFunction.CompN3:
				// {
				// 	var job = new FitnessComplexityOn3Job
				// 	{
				// 		populationGenomes = _populationGenomes,
				// 		fitness = _fitnessValues,
				// 		genomeSize = _genomeSize,
				// 	};
				// 	var handle = job.Schedule(_populationSize, _batchCount);
				// 	handle.Complete();
				// 	break;
				// }

				// case FitnessFunction.ComplOnF:
				// {
				// 	var job = new FitnessComplexityOnFJob
				// 	{
				// 		populationGenomes = _population,
				// 		fitness = _fitnessValues,
				// 		genomeSize = _genomeSize,
				// 	};
				// 	var handle = job.Schedule(_populationSize, _batchSize);
				// 	handle.Complete();
				// 	break;
				// }

				default:
					throw new ArgumentOutOfRangeException();
			}
		}

		public void Dispose()
		{
			if (_populationGenomes.IsCreated) _populationGenomes.Dispose();
			if (_fitnessValues.IsCreated) _fitnessValues.Dispose();
		}
	}

	[BurstCompile]
	public struct FitnessRastriginJob : IJobParallelFor
	{
		[ReadOnly] public NativeArray<float> populationGenomes;
		[ReadOnly] public int genomeSize;

		[WriteOnly] public NativeArray<float> fitness;

		private const float PI_2 = 2f * math.PI;
		private const float A_CONST = 10f;

		public void Execute(int index)
		{
			var value = A_CONST * genomeSize;

			for (int i = 0; i < genomeSize; i++)
			{
				var xi = populationGenomes[index * genomeSize + i];
				value += xi * xi - A_CONST * math.cos(PI_2 * xi);
			}

			if (value < 0) value = -value;
			fitness[index] = 1.0f - (value / (value + 1.0f));
		}
	}

	[BurstCompile]
	public struct FitnessRosenbrockJob : IJobParallelFor
	{
		[ReadOnly] public NativeArray<float> populationGenomes;
		[ReadOnly] public int genomeSize;

		[WriteOnly] public NativeArray<float> fitness;

		private const float A = 1.0f;
		private const float B = 100.0f;

		public void Execute(int index)
		{
			var value = 0f;
			var startIndex = index * genomeSize;

			for (int i = 0; i < genomeSize - 1; i++)
			{
				var xi = populationGenomes[startIndex + i];
				var xnext = populationGenomes[startIndex + i + 1];

				value += B * math.pow(xnext - xi * xi, 2f) + math.pow(A - xi, 2f);
			}

			if (value < 0) value = -value;
			fitness[index] = 1.0f - (value / (value + 1.0f));
		}
	}

	[BurstCompile]
	public struct FitnessAckleyJob : IJobParallelFor
	{
		[ReadOnly] public NativeArray<float> populationGenomes;
		[ReadOnly] public int genomeSize;

		[WriteOnly] public NativeArray<float> fitness;

		private const float A = 20f;
		private const float B = 0.2f;
		private const float PI_2 = 2f * math.PI;

		public void Execute(int index)
		{
			var sum1 = 0f;
			var sum2 = 0f;
			var startIndex = index * genomeSize;

			for (int i = 0; i < genomeSize; i++)
			{
				var xi = populationGenomes[startIndex + i];
				sum1 += xi * xi;
				sum2 += math.cos(PI_2 * xi);
			}

			var term1 = -A * math.exp(-B * math.sqrt(sum1 / genomeSize));
			var term2 = -math.exp(sum2 / genomeSize);

			var value = term1 + term2 + A + math.E;

			if (value < 0) value = -value;
			fitness[index] = 1.0f - (value / (value + 1.0f));
		}
	}

	[BurstCompile]
	public struct FitnessLevyJob : IJobParallelFor
	{
		[ReadOnly] public NativeArray<float> populationGenomes;
		[ReadOnly] public int genomeSize;

		[WriteOnly] public NativeArray<float> fitness;

		private const float PI = math.PI;

		public void Execute(int index)
		{
			var term1 = 0f;
			var termSum = 0f;
			var termLast = 0f;
			var startIndex = index * genomeSize;

			for (int i = 0; i < genomeSize; i++)
			{
				var val = 1 + (populationGenomes[startIndex + i] - 1) / 4f;

				if (i > 0 && i < genomeSize)
					termSum += (val - 1) * (val - 1) * (1 + 10 * math.sin(PI * val + 1) * math.sin(PI * val + 1));
				else if (i == 0)
					term1 = math.sin(PI * val) * math.sin(PI * val);
				else if (i == genomeSize - 1)
					termLast = (val - 1) * (val - 1) * (1 + math.sin(2 * PI * val) * math.sin(2 * PI * val));
			}

			var value = term1 + termSum + termLast;

			if (value < 0) value = -value;
			fitness[index] = 1.0f - (value / (value + 1.0f));
		}
	}

	[BurstCompile]
	public struct FitnessSchwefelJob : IJobParallelFor
	{
		[ReadOnly] public NativeArray<float> populationGenomes;
		[ReadOnly] public int genomeSize;

		[WriteOnly] public NativeArray<float> fitness;

		public void Execute(int index)
		{
			var startIndex = index * genomeSize;
			var sum = 0f;

			for (int i = 0; i < genomeSize; i++)
			{
				var xi = populationGenomes[startIndex + i];
				sum += xi * math.sin(math.sqrt(math.abs(xi)));
			}

			var value = 418.9829f * genomeSize - sum;
			
			if (value < 0) value = -value;
			fitness[index] = 1.0f - (value / (value + 1.0f));
		}
	}

	[BurstCompile]
	public struct FitnessComplexityO1Job : IJobParallelFor
	{
		[ReadOnly] public NativeArray<float> populationGenomes;
		[ReadOnly] public int genomeSize;

		[WriteOnly] public NativeArray<float> fitness;

		public void Execute(int index)
		{
			fitness[index] = math.abs(math.sin(populationGenomes[index * genomeSize]));
		}
	}

	[BurstCompile]
	public struct FitnessComplexityOLogNJob : IJobParallelFor
	{
		[ReadOnly] public NativeArray<float> populationGenomes;
		[ReadOnly] public int genomeSize;

		[WriteOnly] public NativeArray<float> fitness;

		public void Execute(int index)
		{
			int steps = 0;
			float n = genomeSize;
        
			while (n > 1)
			{
				n /= 2;
				steps++;
			}

			fitness[index] = math.abs(math.sin(steps));
		}
	}

	[BurstCompile]
	public struct FitnessComplexityOnJob : IJobParallelFor
	{
		[ReadOnly] public NativeArray<float> populationGenomes;
		[ReadOnly] public int genomeSize;

		[WriteOnly] public NativeArray<float> fitness;

		public void Execute(int index)
		{
			var value = 0f;
			var startIndex = index * genomeSize;

			for (int i = 0; i < genomeSize; i++)
			{
				value += math.sin(populationGenomes[startIndex + i]);
			}

			value = math.sin(value);
			
			if (value < 0) value = -value;
			fitness[index] = 1.0f - (value / (value + 1.0f));
		}
	}

	[BurstCompile]
	public struct FitnessComplexityONLogNJob : IJobParallelFor
	{
		[ReadOnly] public NativeArray<float> populationGenomes;
		[ReadOnly] public int genomeSize;

		[WriteOnly] public NativeArray<float> fitness;

		public void Execute(int index)
		{
			var array = new NativeArray<float>(populationGenomes.GetSubArray(index * genomeSize, genomeSize), Allocator.Temp);
			var n = array.Length;

			// Heap Sort
			for (int i = n / 2 - 1; i >= 0; i--)
				Heapify(array, n, i);

			for (int i = n - 1; i > 0; i--)
			{
				(array[0], array[i]) = (array[i], array[0]);
				Heapify(array, i, 0);
			}

			fitness[index] = 1.0f;

			array.Dispose();
		}

		private static void Heapify(NativeArray<float> array, int n, int i)
		{
			int largest = i;
			int left = 2 * i + 1;
			int right = 2 * i + 2;

			if (left < n && array[left] > array[largest])
				largest = left;

			if (right < n && array[right] > array[largest])
				largest = right;

			if (largest != i)
			{
				(array[i], array[largest]) = (array[largest], array[i]);
				Heapify(array, n, largest);
			}
		}
	}

	[BurstCompile]
	public struct FitnessComplexityOn2Job : IJobParallelFor
	{
		[ReadOnly] public NativeArray<float> populationGenomes;
		[ReadOnly] public int genomeSize;

		[WriteOnly] public NativeArray<float> fitness;

		public void Execute(int index)
		{
			var value = 0f;
			var startIndex = index * genomeSize;

			for (int i = 0; i < genomeSize; i++)
			{
				for (int j = 0; j < genomeSize; j++)
				{
					value += math.sin(populationGenomes[startIndex + j]);
				}
			}

			value = math.sin(value);
			
			if (value < 0) value = -value;
			fitness[index] = 1.0f - (value / (value + 1.0f));
		}
	}

	[BurstCompile]
	public struct FitnessComplexityOn3Job : IJobParallelFor
	{
		[ReadOnly] public NativeArray<float> populationGenomes;
		[ReadOnly] public int genomeSize;

		[WriteOnly] public NativeArray<float> fitness;

		public void Execute(int index)
		{
			var value = 0f;
			var startIndex = index * genomeSize;

			for (int i = 0; i < genomeSize; i++)
			{
				for (int j = 0; j < genomeSize; j++)
				{
					for (int k = 0; k < genomeSize; k++)
					{
						value += math.sin(populationGenomes[startIndex + k]);
					}
				}
			}

			value = math.sin(value);
			
			if (value < 0) value = -value;
			fitness[index] = 1.0f - (value / (value + 1.0f));
		}
	}
}