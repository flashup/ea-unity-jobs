using System;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;

namespace Evolution.Selection.Jobs
{
	public class TruncationJobCommand : ISelectionCommand
	{
		private static readonly int _batchCount = JobSettings.BatchCount;
		private readonly int _parentsAmount;

		private NativeArray<int> _selectedIndices;

		public TruncationJobCommand(int parentsAmount)
		{
			_parentsAmount = parentsAmount;
	
			_selectedIndices = new NativeArray<int>(parentsAmount, Allocator.Persistent);
		}

		public void Execute<T>(EvaluatedGenome<T>[] evaluatedGeneration, T[] parents)
			where T : IGenome<T>
		{
			Array.Sort(evaluatedGeneration, EvaluatedGenomeHelper.SortDescending);

			var job = new TruncationJob
			{
				selectedIndices = _selectedIndices,
			};
			var handle = job.Schedule(_parentsAmount, _batchCount);
			handle.Complete();

			for (int i = 0; i < _parentsAmount; i++)
			{
				var index = _selectedIndices[i];
				parents[i] = evaluatedGeneration[index].genome;
			}
		}

		public void Dispose()
		{
			if (_selectedIndices.IsCreated) _selectedIndices.Dispose();
		}
	}

	[BurstCompile]
	internal struct TruncationJob : IJobParallelFor
	{
		[WriteOnly] public NativeArray<int> selectedIndices;

		public void Execute(int index)
		{
			selectedIndices[index] = index;
		}
	}
}