using UnityEngine;
using System.Collections;

public class cuberSpawner : MonoBehaviour
{
	public int Count;
	public GameObject spawnObject;

	void Start()
	{
		while (Count-- > 0)
			GameObject.Instantiate(spawnObject);
	}
}
