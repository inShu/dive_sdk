using UnityEngine;
using System.Collections;

public class commonScript : MonoBehaviour
{
	public float speed;
	private float m_Timer;

	void Update()
	{
		transform.position = new Vector3(transform.position.x, transform.position.y + speed * Time.deltaTime, transform.position.z);

		Light lightComp = transform.GetComponent<Light>();

		if (lightComp)
		{
			m_Timer += 0.4f * Time.deltaTime;
			lightComp.color = Color.Lerp(Color.red, Color.blue, m_Timer);

			if (m_Timer >= 1.0f)
				m_Timer = 0.0f;
		}
	}
}
