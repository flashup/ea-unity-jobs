using System;
using Unity.Collections;

namespace Function.FitnessEvaluation.Functions
{
	public class EvaluateFitnessFuncCommand : IEvaluateFitnessCommand
	{
		private readonly Func<float[], float> _testFunction;

		private NativeArray<float> _fitnessValues;

		public EvaluateFitnessFuncCommand(FitnessFunction fitnessFunction, int populationSize)
		{
			_testFunction = fitnessFunction switch
			{
				FitnessFunction.Rastrigin => RastriginFunction,
				FitnessFunction.Rosenbrock => RosenbrockFunction,
				FitnessFunction.Ackley => AckleyFunction,
				FitnessFunction.Levy => LevyFunction,
				FitnessFunction.Schwefel => SchwefelFunction,
				FitnessFunction.Comp1 => ComplexityO1Function,
				FitnessFunction.CompLogN => ComplexityOLogNFunction,
				FitnessFunction.CompN => ComplexityOnFunction,
				FitnessFunction.CompNLogN => ComplexityONlogNFunction,
				FitnessFunction.CompN2 => ComplexityOn2Function,
				// FitnessFunction.CompN3 => ComplexityOn3Function,
				// FitnessFunction.ComplOnF => ComplexityOnFFunction,
				_ => throw new ArgumentOutOfRangeException()
			};

			_fitnessValues = new NativeArray<float>(populationSize, Allocator.Persistent);
		}

		public NativeArray<float> Execute(Point[] generation)
		{
			for (var i = 0; i < generation.Length; i++)
				_fitnessValues[i] = _testFunction.Invoke(generation[i].coordinates);

			return _fitnessValues;
		}

		public void Dispose()
		{
			if (_fitnessValues.IsCreated) _fitnessValues.Dispose();
		}

		private static float RastriginFunction(float[] variables)
		{
			const double pi2 = 2.0 * Math.PI;
			const float a = 10f;

			var value = a * variables.Length;
			for (var i = 0; i < variables.Length; i++)
			{
				var xi = variables[i];
				value += xi * xi - a * (float)Math.Cos(pi2 * xi);
			}

			if (value < 0) value = -value;
			return 1.0f - (value / (value + 1.0f));
		}

		private static float RosenbrockFunction(float[] variables)
		{
			const double a = 1.0f;
			const double b = 100.0f;

			double val = 0;

			for (int i = 0; i < variables.Length - 1; i++)
			{
				var xi = variables[i];
				var xnext = variables[i + 1];
				val += (b * Math.Pow(xnext - xi * xi, 2) + Math.Pow(a - xi, 2));
			}

			var value = (float)val;

			if (value < 0) value = -value;
			return 1.0f - (value / (value + 1.0f));
		}

		private static float AckleyFunction(float[] variables)
		{
			const double a = 20.0;
			const double b = 0.2;
			const double pi2 = 2.0 * Math.PI;

			var n = variables.Length;

			var sum1 = 0.0;
			var sum2 = 0.0;

			for (int i = 0; i < n; i++)
			{
				var xi = variables[i];
				sum1 += xi * xi;
				sum2 += Math.Cos(pi2 * xi);
			}

			var term1 = -a * Math.Exp(-b * Math.Sqrt(sum1 / n));
			var term2 = -Math.Exp(sum2 / n);

			var value = (float)(term1 + term2 + a + Math.E);

			if (value < 0) value = -value;
			return 1.0f - (value / (value + 1.0f));
		}

		private static float LevyFunction(float[] variables)
		{
			const double pi = Math.PI;

			var n = variables.Length;

			var w = new double[n];
			for (int i = 0; i < n; i++)
			{
				w[i] = 1 + (variables[i] - 1) / 4.0;
			}

			var term1 = Math.Sin(pi * w[0]) * Math.Sin(pi * w[0]);

			var termSum = 0.0;
			for (int i = 0; i < n - 1; i++)
			{
				termSum += (w[i] - 1) * (w[i] - 1) * (1 + 10 * Math.Sin(pi * w[i] + 1) * Math.Sin(pi * w[i] + 1));
			}

			var termLast = (w[n - 1] - 1) * (w[n - 1] - 1) *
			               (1 + Math.Sin(2 * pi * w[n - 1]) * Math.Sin(2 * pi * w[n - 1]));

			var value = (float)(term1 + termSum + termLast);

			if (value < 0) value = -value;
			return 1.0f - (value / (value + 1.0f));
		}

		private static float SchwefelFunction(float[] variables)
		{
			var n = variables.Length;
			var sum = 0.0;

			for (int i = 0; i < n; i++)
			{
				var xi = variables[i];
				sum += xi * Math.Sin(Math.Sqrt(Math.Abs(xi)));
			}

			var value = (float)(418.9829 * n - sum);

			if (value < 0) value = -value;
			return 1.0f - (value / (value + 1.0f));
		}

		private static float ComplexityO1Function(float[] variables)
		{
			return (float)Math.Abs(Math.Sin(variables[0]));
		}

		private static float ComplexityOLogNFunction(float[] variables)
		{
			int steps = 0;
			float n = variables.Length;
        
			while (n > 1)
			{
				n /= 2;
				steps++;
			}

			return (float)Math.Abs(Math.Sin(steps));
		}

		private static float ComplexityOnFunction(float[] variables)
		{
			var genomeSize = variables.Length;
			var sum = 0.0;

			for (int i = 0; i < genomeSize; i++)
			{
				sum += Math.Sin(variables[i]);
			}

			var value = (float)Math.Sin(sum);

			if (value < 0) value = -value;
			return 1.0f - (value / (value + 1.0f));
		}

		// Heap Sort
		private static float ComplexityONlogNFunction(float[] array)
		{
			// var sum = 0.0;

			var n = array.Length;

			for (int i = n / 2 - 1; i >= 0; i--)
				Heapify(array, n, i);

			for (int i = n - 1; i > 0; i--)
			{
				(array[0], array[i]) = (array[i], array[0]);
				Heapify(array, i, 0);
			}

			return 1;
		}
		
		static void Heapify(float[] array, int n, int i)
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

		private static float ComplexityOn2Function(float[] variables)
		{
			var genomeSize = variables.Length;
			var sum = 0.0;

			for (int i = 0; i < genomeSize; i++)
			{
				for (int j = 0; j < genomeSize; j++)
				{
					sum += Math.Sin(variables[j]);
				}
			}

			var value = (float)Math.Sin(sum);

			if (value < 0) value = -value;
			return 1.0f - (value / (value + 1.0f));
		}

		private static float ComplexityOn3Function(float[] variables)
		{
			var genomeSize = variables.Length;
			var sum = 0.0;

			for (int i = 0; i < genomeSize; i++)
			{
				for (int j = 0; j < genomeSize; j++)
				{
					for (int k = 0; k < genomeSize; k++)
					{
						sum += Math.Sin(variables[k]);
					}
				}
			}

			var value = (float)Math.Sin(sum);

			if (value < 0) value = -value;
			return 1.0f - (value / (value + 1.0f));
		}

		private static float ComplexityOnFFunction(float[] variables)
		{
			var genomeSize = variables.Length;
			var sum = 0.0;

			for (int i = 0; i < genomeSize; i++)
			{
				for (int j = 0; j < genomeSize; j++)
				{
					for (int k = 0; k < genomeSize; k++)
					{
						sum += Math.Sin(variables[k]);
					}
				}
			}

			var value = (float)Math.Sin(sum);

			if (value < 0) value = -value;
			return 1.0f - (value / (value + 1.0f));
		}
	}
}