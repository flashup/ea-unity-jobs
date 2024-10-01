using System;
using TMPro;
using UI;
using UnityEngine;

namespace TSP
{
	public class CityView : MonoBehaviour
	{
		[SerializeField] private TextMeshProUGUI _idTxt;

		public City City { get; private set; }

		public int ID => City.id;
		
		public void SetCity(City city, Action<CityView> clickCallback)
		{
			City = city;

			_idTxt.text = city.id.ToString();
			transform.localPosition = city.coords;
			gameObject.name = city.id.ToString();

			GetComponent<UIButton>().OnClick(() =>
			{
				clickCallback?.Invoke(this);
			});
		}
	}
}