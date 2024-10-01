using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace UI
{
	public class FitnessChart : UiChart
	{
		public const string MaxChart = "MaxChart";
		public const string AvgChart = "AvgChart";
		public const string TimeChart = "TimeChart";
		public const string MinChart = "MinChart";

		public int maxPointsRendered = 0;

		private readonly List<float> _avgMaxValues = new();

		public ChartMode chartMode;

		public override void Clear()
		{
			base.Clear();
			
			_avgMaxValues.Clear();

			SetChartParams(MaxChart, new ChartParams
			{
				color = Color.green,
				type = ChartType.Normal,
				label = "Max",
				maxPointsRendered = maxPointsRendered,
				yAxisScale = 1f,
			});

			SetChartParams(AvgChart, new ChartParams
			{
				color = new Color32(255, 100, 50, 255),
				type = ChartType.Normal,
				smaInterval = 5,
				label = "Avg",
				maxPointsRendered = maxPointsRendered,
				yAxisScale = 1f,
			});

			if (chartMode == ChartMode.MaxAvgTime)
			{
				SetChartParams(TimeChart, new ChartParams
				{
					color = Color.blue,
					type = ChartType.Normal,
					label = "Time",
					maxPointsRendered = maxPointsRendered,
					yAxisScale = 0.3f,
				});
			}

			// if (chartMode == ChartMode.MaxMinAvg)
			// {
			// 	SetChartParams(MinChart, new ChartParams
			// 	{
			// 		color = new Color32(255, 0, 0, 255),
			// 		type = ChartType.Normal,
			// 		smaInterval = 5,
			// 		label = "Min",
			// 		maxPointsRendered = maxPointsRendered,
			// 	});
			// }

			AddHorizontalLine(0);
			AddHorizontalLine(0.5f);
			AddHorizontalLine(1f);
			
			DrawLines();
		}

		public void AddBestValue(double value)
		{
			AddValue(MaxChart, (float)value);
		}
	
		public void AddAvgValue(double value)
		{
			AddValue(AvgChart, (float)value);
		}

		public void AddTime(double value)
		{
			AddValue(TimeChart, (float)value);
		}
	}

	public enum ChartMode
	{
		MaxAvgTime,
		MaxAvg,
	}
}