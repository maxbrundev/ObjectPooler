using UnityEngine;

namespace Utils.Extensions
{
	public static class ComponentExtension
	{
		public static void SetActive(this Component component, bool value)
		{
			if (component != null)
			{
				component.gameObject.SetActive(value);
			}
		}

		public static void SetActiveAndEnable(this MonoBehaviour monoBehaviour, bool value)
		{
			if (monoBehaviour != null)
			{
				monoBehaviour.SetActive(value);
				monoBehaviour.enabled = value;
			}
		}
	}
}