using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UI.Chart;
using UnityEngine;
using UnityEngine.UI;

namespace UI
{
	public abstract class UiChart : MonoBehaviour
	{
		[Header("Prefabs")] 
		[SerializeField] private LegendItem _legendItemPrefab;
		[SerializeField] private TextMeshProUGUI _labelPrefab;
		[SerializeField] private Image _dotPrefab;
		[SerializeField] private Image _linePrefab;

		[Header("Containers")] 
		[SerializeField] private RectTransform _legendContainer;

		[SerializeField] private RectTransform _elementsContainer;
		[SerializeField] private RectTransform _labelsContainer;

		private readonly Dictionary<string, ChartData> _charts = new();

		private float _canvasWidth;
		private float _canvasHeight;
		private bool _isDirty;

		protected float _yAxisMax = 1f;
		private readonly Dictionary<float, Image> _horizontalLines = new();

		private void Start()
		{
			Canvas.ForceUpdateCanvases();

			Clear();
		}

		protected ChartData GetChart(string chartId)
		{
			return _charts[chartId];
		}

		protected void SetChartParams(string chartId, ChartParams chartParams)
		{
			if (!_charts.ContainsKey(chartId))
			{
				CreateNewChart(chartId, chartParams);
			}
		}

		private void CreateNewChart(string chartId, ChartParams chartParams = null)
		{
			_charts.Add(chartId, new ChartData
			{
				chartParams = chartParams ?? new ChartParams
				{
					color = Color.white,
					type = ChartType.Normal,
				},
			});

			// var legend = Instantiate(_legendItemPrefab, _legendContainer);
			// legend.SetChartData(chartId, chartParams.color);
		}

		protected void AddValue(string chartId, float value)
		{
			if (!_charts.ContainsKey(chartId))
			{
				CreateNewChart(chartId);
			}

			var chartData = _charts[chartId];
			var values = chartData.values;

			values.Add(value);

			_isDirty = true;
		}

		private void Update()
		{
			if (!_isDirty) return;

			DrawLines();
			
			DrawCharts();

			_isDirty = false;
		}

		public void Redraw()
		{
			_isDirty = true;
		}

		protected void DrawLines()
		{
			foreach (var (coordY, line) in _horizontalLines)
			{
				DrawHorizontalLine(coordY, line);
			}
		}

		private void DrawCharts()
		{
			var labelYCoords = new List<(float value, float y)>();

			// var lastValues = _charts.Values.Select(d => d.values[^1]).ToArray();

			foreach (var chartData in _charts.Values)
			{
				var values = chartData.values;
				var dots = chartData.dots;
				var lines = chartData.lines;
				var chartParams = chartData.chartParams;
				var maxPointsRendered = chartParams.maxPointsRendered;
				var yAxisScale = chartParams.yAxisScale;

				if (chartData.label == null)
				{
					var label = Instantiate(_labelPrefab, _labelsContainer);
					label.color = Color.Lerp(Color.white, chartData.chartParams.color, 0.2f);
					chartData.label = label;
				}

				// if value hasn't changed, just move last dot further
				// but don't move first dot
				// if (values.Count > 1)
				// {
				// 	var lastIndex = values.Count - 1;
				// 	var prevValue = values[lastIndex];
				//
				// 	if (prevValue.Equals(value))
				// 	{
				// 		var prevDot = chartData.Dots[lastIndex];
				// 		chartData.Dots[lastIndex] = null;
				// 		chartData.Dots.Add(prevDot);
				// 	}
				// }

				var valuesToRender = chartParams.type == ChartType.Sma
					? CalculateSMA(values, chartParams.smaInterval)
					: chartData.values;

				if (maxPointsRendered > 0)
					valuesToRender = values.TakeLast(maxPointsRendered).ToList();

				var numValues = valuesToRender.Count;
				var stepX = Mathf.Min(_canvasWidth / 10, Mathf.FloorToInt(_canvasWidth / numValues));

				var maxValue = valuesToRender.Max();

				for (int i = 0; i < numValues; i++)
				{
					var value = valuesToRender[i];

					// if (i > 0 && i < numValues - 1)
					// {
					// var nextValue = valuesToRender[i + 1];
					// if (value == nextValue) continue;

					// var lastIndex = values.Count - 1;
					// var prevValue = values[i - 1];
					//
					// if (prevValue.Equals(value))
					// {
					// 	var prevDot = chartData.Dots[lastIndex];
					// 	chartData.Dots[lastIndex] = null;
					// 	chartData.Dots.Add(prevDot);
					// }
					// }

					var dot = dots.Count > i ? dots[i] : CreateDot(chartData);

					if (dot == null) continue;

					var yNorm = value / maxValue;

					dot.rectTransform.localPosition = new Vector2(
						i * stepX,
						_canvasHeight * yNorm * yAxisScale
					);
				}

				var existingDots = dots.Where(d => d != null).ToArray();
				for (var i = 1; i < existingDots.Length; i++)
				{
					var prevDot = existingDots[i - 1];
					var dot = existingDots[i];

					var lineIndex = i - 1;

					var line = lines.Count > lineIndex ? lines[lineIndex] : CreateLine(chartData);

					SetLineBetweenPoints(line, prevDot.rectTransform.localPosition, dot.rectTransform.localPosition);
				}
			}
			
			// align labels
			var orderedChartsByY = _charts.Values
				.Select(c => (c.dots[^1].rectTransform.localPosition, c))
				.OrderBy(cd => cd.Item1.y)
				.ToArray();

			foreach (var (dotPos, chartData) in orderedChartsByY)
			{
				var chartParams = chartData.chartParams;
				var value = chartData.values[^1];
				chartData.label.text = string.IsNullOrEmpty(chartParams.label)
					? $"{value}"
					: $"{chartParams.label}: {value}";
				var height = chartData.label.rectTransform.rect.height;
				var pos = dotPos;

				var newPosY = pos.y;

				var intersection = labelYCoords.Find(data => Mathf.Abs(data.y - newPosY) < height);
				if (intersection != default)
					newPosY = intersection.y + height;

				chartData.label.transform.localPosition = new Vector3(pos.x, newPosY, 0);
				chartData.label.rectTransform.pivot = new Vector2(
					chartData.label.rectTransform.localPosition.x / _canvasWidth,
					0
				);

				labelYCoords.Add((value, newPosY));
			}
		}

		private void SetLineBetweenPoints(Image line, Vector2 pointA, Vector2 pointB)
		{
			var lineTr = line.rectTransform;

			Vector2 midPoint = (pointA + pointB) / 2f;
			lineTr.localPosition = midPoint;

			Vector2 direction = pointB - pointA;
			var angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
			lineTr.rotation = Quaternion.Euler(0, 0, angle);

			var distance = Vector2.Distance(pointA, pointB);
			lineTr.sizeDelta = new Vector2(distance, lineTr.sizeDelta.y);
		}

		private static List<float> CalculateSMA(List<float> values, int period)
		{
			if (period <= 0)
			{
				throw new ArgumentException(
					"Period must be greater than 0 and less than or equal to the number of prices.");
			}

			var sma = new List<float>();

			for (int i = 0; i < period - 1; i++)
			{
				if (values.Count > i)
					sma.Add(values[i]);
			}

			if (values.Count < period)
			{
				return sma;
			}

			for (int i = 0; i <= values.Count - period; i++)
			{
				float sum = 0;
				for (int j = i; j < i + period; j++)
				{
					sum += values[j];
				}

				sma.Add(sum / period);
			}

			return sma;
		}

		private Image CreateDot(ChartData chartData)
		{
			var dot = Instantiate(_dotPrefab, _elementsContainer);
			dot.color = chartData.chartParams.color;
			chartData.dots.Add(dot);
			return dot;
		}

		private Image CreateLine(ChartData chartData)
		{
			var line = Instantiate(_linePrefab, _elementsContainer);
			line.color = chartData.chartParams.color;
			chartData.lines.Add(line);
			return line;
		}

		public virtual void Clear()
		{
			foreach (Transform t in _legendContainer)
				Destroy(t.gameObject);

			foreach (Transform t in _labelsContainer)
				Destroy(t.gameObject);

			foreach (Transform t in _elementsContainer)
				Destroy(t.gameObject);

			_charts.Clear();
			_horizontalLines.Clear();

			var rt = _elementsContainer;
			_canvasWidth = rt.rect.width;
			_canvasHeight = rt.rect.height;
		}

		public void AddHorizontalLine(float y)
		{
			var line = Instantiate(_linePrefab, _elementsContainer);
			line.color = new Color(1, 1, 1, 0.1f);
			line.gameObject.name = $"HorLine_{y}";

			_horizontalLines.Add(y, line);
		}

		private void DrawHorizontalLine(float y, Image line)
		{
			var yCoord = _canvasHeight * (y / _yAxisMax);
			var startPoint = new Vector2(0, yCoord);
			var endPoint = new Vector2(_canvasWidth, yCoord);
			SetLineBetweenPoints(line, startPoint, endPoint);
		}
	}

	public class ChartData
	{
		public readonly List<float> values = new();
		public readonly List<Image> dots = new();
		public readonly List<Image> lines = new();
		public ChartParams chartParams;

		public TextMeshProUGUI label;
	}

	public class ChartParams
	{
		public Color color;
		public ChartType type;
		public int smaInterval;
		public int maxPointsRendered = 0;
		public string label;
		public float yAxisScale = 1f;
	}

	public enum ChartType
	{
		Normal,
		Sma,
	}
}