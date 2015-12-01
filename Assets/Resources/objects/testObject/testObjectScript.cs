using UnityEngine;
using System.Collections;

public class testObjectScript : MonoBehaviour
{
	private Vector3 m_Direction;
	private Vector3 m_Rotation;
	public float speed;

	void Start()
	{
		m_Direction.x = Random.Range(-1.0f, 1.0f);
		m_Direction.y = Random.Range(0.0f, 1.0f);
		m_Direction.z = Random.Range(-1.0f, 1.0f);

		m_Rotation.x = Random.Range(-1.0f, 1.0f) * 100.0f;
		m_Rotation.y = Random.Range(0.0f, 1.0f) * 100.0f;
		m_Rotation.z = Random.Range(-1.0f, 1.0f) * 100.0f;
	}

	void Update()
	{
		transform.position = new Vector3(
				transform.position.x + m_Direction.x * speed * Time.deltaTime,
				transform.position.y + m_Direction.y * speed * Time.deltaTime,
				transform.position.z + m_Direction.z * speed * Time.deltaTime
			);
		transform.Rotate(m_Rotation.x * Time.deltaTime, m_Rotation.x * Time.deltaTime, m_Rotation.z * Time.deltaTime);
	}
}
