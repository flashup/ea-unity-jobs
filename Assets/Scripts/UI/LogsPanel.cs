using System;
using System.Collections;
using System.Collections.Generic;
using Helpers;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UI
{
	public class LogsPanel : MonoBehaviour
	{
		[SerializeField] private ScrollRect _logsScrollRect;
		[SerializeField] private TextMeshProUGUI _logsTxt;
		[SerializeField] private UIButton _clearLogsBtn;

		[SerializeField] private int _maxLogsStrings = 10;

		private readonly Queue<string> _logs = new();

		private void Awake()
		{
			OnClickClearLogs();
		}

		private void Start()
		{
			_clearLogsBtn.OnClick(OnClickClearLogs);
		}
		
		private void OnClickClearLogs()
		{
			_logs.Clear();
			_logsTxt.text = "";
			_clearLogsBtn.SetEnabled(false);
		}

		public void Log(string log)
		{
			_logs.Enqueue($"{log}");

			if (_logs.Count > _maxLogsStrings) _logs.Dequeue();

			_logsTxt.text = _logs.Join("\n");

			_clearLogsBtn.SetEnabled(true);

			StartCoroutine(ScrollToBottom());
		}

		private IEnumerator ScrollToBottom()
		{
			yield return new WaitForEndOfFrame();
			_logsScrollRect.verticalNormalizedPosition = 0;
		}
	}
}