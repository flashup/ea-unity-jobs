using System;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace UI
{
	public class HeaderPanel : MonoBehaviour
	{
		public Test selectedTest;
		
		[SerializeField] private UIButton _functionTab;
		[SerializeField] private UIButton _tspTab;
		[SerializeField] private UIButton _parkingTab;

		private void Start()
		{
			var tabs = new UIButton[] { _functionTab, _tspTab, _parkingTab };
			var tabIndex = (int)selectedTest;

			for (var i = 0; i < tabs.Length; i++)
			{
				var toTab = i;

				var tab = tabs[i];
				tab.SetEnabled(toTab != tabIndex);
				tab.SetColor(toTab != tabIndex ?  Color.gray : new Color32(95, 194, 255, 255));

				if (toTab == tabIndex) continue;

				tab.OnClick(() =>
				{
					SceneManager.LoadScene(toTab);
				});
			}
		}
	}

	public enum Test
	{
		Function,
		Tsp,
		Parking
	}
}