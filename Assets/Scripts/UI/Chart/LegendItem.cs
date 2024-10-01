using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UI.Chart
{
	public class LegendItem : MonoBehaviour
	{
		[SerializeField] private Image _line;
		[SerializeField] private TextMeshProUGUI _label;

		public void SetChartData(string chartId, Color chartColor)
		{
			_label.text = chartId;
			_line.color = chartColor;
		}
	}
}