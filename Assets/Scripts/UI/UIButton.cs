using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace UI
{
	public class UIButton : MonoBehaviour, IPointerClickHandler, IPointerDownHandler, IPointerUpHandler
	{
		[SerializeField] private TextMeshProUGUI _labelTxt;
		
		private Action _clickHandler;

		private bool _enabled = true;

		private Color _color;

		public Color disabledColor;

		private void Awake()
		{
			_color = GetComponent<Image>().color;
		}

		public void OnClick(Action clickHandler)
		{
			_clickHandler = clickHandler;
		}

		public void UpdateText(string text)
		{
			_labelTxt.text = text;
		}

		public void SetEnabled(bool value)
		{
			_enabled = value;

			SetColor(value ? _color : disabledColor);
		}

		public void SetColor(Color value)
		{
			GetComponent<Image>().color = value;
		}

		public void OnPointerClick(PointerEventData eventData)
		{
			if (!_enabled) return;

			_clickHandler?.Invoke();
		}

		public void OnPointerDown(PointerEventData eventData)
		{
			if (!_enabled) return;

			transform.localScale = Vector3.one * 0.95f;
		}

		public void OnPointerUp(PointerEventData eventData)
		{
			transform.localScale = Vector3.one;
		}
	}
}