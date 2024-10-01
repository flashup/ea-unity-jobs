using Unity.Collections;
using UnityEngine;
using UnityEngine.Rendering;

namespace Function.FitnessEvaluation.Shaders
{
	public class EvaluateFitnessShaderCommand : IEvaluateFitnessCommand
	{
		private static readonly int Population = Shader.PropertyToID("population");
		private static readonly int FitnessResults = Shader.PropertyToID("fitnessResults");
		private static readonly int Dimension = Shader.PropertyToID("dimension");

		private readonly int _populationSize;
		private readonly int _genomeSize;

		private readonly FitnessFunction _fitnessFunction;

		private ComputeShader _computeShader;

		private ComputeBuffer _populationBuffer;
		private ComputeBuffer _fitnessBuffer;

		private NativeArray<float> _population;
		private NativeArray<float> _fitnessResults;

		public EvaluateFitnessShaderCommand(FitnessFunction fitnessFunction, int populationSize, int genomeSize, ComputeShader computeShader)
		{
			_fitnessFunction = fitnessFunction;
			_populationSize = populationSize;
			_genomeSize = genomeSize;
			_computeShader = computeShader;

			_population = new NativeArray<float>(populationSize * genomeSize, Allocator.Persistent);
			_fitnessResults = new NativeArray<float>(populationSize, Allocator.Persistent);
			
			_populationBuffer = new ComputeBuffer(_populationSize * _genomeSize, sizeof(float));
			// _population = new float[_populationSize * _dimensions];
			_population = new NativeArray<float>(_populationSize * _genomeSize, Allocator.Persistent);

			_fitnessBuffer = new ComputeBuffer(_populationSize, sizeof(float));
			_fitnessResults = new NativeArray<float>(_populationSize, Allocator.Persistent);
		}

		public NativeArray<float> Execute(Point[] generation)
		{
			var counter = 0;
			for (int i = 0; i < _populationSize; i++)
			{
				var point = generation[i];
				for (int j = 0; j < _genomeSize; j++)
				{
					_population[counter++] = point.coordinates[j];
				}
			}

			// cache
			var kernelHandle = _computeShader.FindKernel("FunctionFitness");

			_populationBuffer.SetData(_population);

			_computeShader.SetBuffer(kernelHandle, Population, _populationBuffer);
			_computeShader.SetBuffer(kernelHandle, FitnessResults, _fitnessBuffer);
			_computeShader.SetInt(Dimension, _genomeSize);

			// var numThreads = JobSettings.BatchCount;
			_computeShader.Dispatch(kernelHandle,
				_populationSize, // numThreads, 
				1, // Mathf.CeilToInt(_populationSize / numThreads), 
				1
			);

			// OR
			// _fitnessBuffer.GetData(_fitnessResults);

			var req = AsyncGPUReadback.RequestIntoNativeArray(ref _fitnessResults, _fitnessBuffer);
			req.WaitForCompletion();

			return _fitnessResults;
		}
		
		public void Dispose()
		{
			if (_fitnessResults.IsCreated) _fitnessResults.Dispose();
			if (_population.IsCreated) _population.Dispose();

			_populationBuffer.Release();
			_fitnessBuffer.Release();
		}
	}
}