using UnityEngine;
using UnityEditor;
using System;
using System.Collections.Generic;
using System.Xml;
using System.IO;
using System.Reflection;
using System.Reflection.Emit;
using System.Security.Cryptography;

public class DivenetMenuWindow : EditorWindow
{
	const string m_DivenetResources = "Assets/Resources/divenet";

	//GUI variables
	private Vector2 m_ScrollPosition;
	private string m_WorldName;
	private string m_ProjectPath;
	private string m_ProjectFolder;
	private int m_MainSceneIndex;

	//Project data
	private List<aScene> m_ScenesList;
	private List<aPrefab> m_PrefabsList;

	//Represents prefab from Assets/Resources
	private class aPrefab
	{
		public string assetsPath;
		public string fullPath;
		public string path;
		public string assetsFullPath;
		public GameObject prefab;
	};

	private class aScene
	{
		public string filePath;
		public string outPath;
		public string tmpPath;
		public string sceneName;
		public List<aPrefab> objects;
	}

	//========================= EXPORTING =========================

	/* Main method which processing world, objects, scripts
     * and composing everything in objects which should be used
     * to save everything in xml.
     */
	private void ProcessWorld()
	{
		if (m_ScenesList == null)
			m_ScenesList = new List<aScene>(UnityEditor.EditorBuildSettings.scenes.Length);
		else
			m_ScenesList.Clear();

		string currentScene = EditorApplication.currentScene;
		aScene tmpScene;

		m_PrefabsList = ProcessPrefabs();

		//Going through all scenes which were mentioned by developer in build settings
		foreach (UnityEditor.EditorBuildSettingsScene scene in UnityEditor.EditorBuildSettings.scenes)
		{
			//If this scene is marked for building purpose
			if (scene.enabled)
			{
				tmpScene = new aScene();
				tmpScene.objects = new List<aPrefab>();
				tmpScene.filePath = scene.path;
				tmpScene.sceneName = scene.path.Substring(scene.path.LastIndexOf('/') + 1);
				tmpScene.sceneName = tmpScene.sceneName.Substring(0, tmpScene.sceneName.Length - 6);

				EditorApplication.OpenScene(tmpScene.filePath);

				m_ScenesList.Add(tmpScene);
			}
		}

		EditorApplication.OpenScene(currentScene);
	}

	//We goint through all scenes and converts all objects
	//to templorary prefabs
	private void ConvertObjectsToPrefabs()
	{
		string currentScene = EditorApplication.currentScene;
		string currentScenePrefabDirectory;

		foreach (aScene scene in m_ScenesList)
		{
			EditorApplication.OpenScene(scene.filePath);

			currentScenePrefabDirectory = Guid.NewGuid().ToString() + "/";

			scene.tmpPath = "Assets/Resources/" + currentScenePrefabDirectory;
			scene.outPath = m_ProjectFolder + scene.tmpPath;

			Directory.CreateDirectory(scene.tmpPath);

			GameObject[] sceneObjects = FindObjectsOfType<GameObject>();
			GameObject prefab;

			foreach(GameObject obj in sceneObjects)
			{
				if (obj.transform.parent == null)
				{
					prefab = PrefabUtility.ReplacePrefab(obj, PrefabUtility.CreateEmptyPrefab(scene.tmpPath + Guid.NewGuid().ToString() + ".prefab"));
					scene.objects.Add(ProcessPrefab(prefab));
				}
			}
		}

		EditorApplication.OpenScene(currentScene);
	}

	private void CleanTemploraryPrefabs()
	{
		foreach(aScene scene in m_ScenesList)
			Directory.Delete(scene.tmpPath, true);

		AssetDatabase.Refresh();
	}

	//Output list of objects actually represents prefabs in assets
	private List<aPrefab> ProcessWorldObjects()
	{
		List<aPrefab> objectsList = new List<aPrefab>();

		return objectsList;
	}

	private aPrefab ProcessPrefab(GameObject prefab)
	{
		aPrefab tmpPrefab = new aPrefab();
		string tmpString;

		tmpString = AssetDatabase.GetAssetPath(prefab);

		tmpString = tmpString.Substring(0, tmpString.LastIndexOf('/'));
		tmpPrefab.assetsPath = tmpString + "/";
		tmpPrefab.assetsFullPath = tmpPrefab.assetsPath + prefab.name + ".unity3d";
		tmpPrefab.path = m_ProjectFolder + tmpPrefab.assetsPath;
		tmpPrefab.fullPath = tmpPrefab.path + prefab.name;
		tmpPrefab.prefab = prefab;

		return tmpPrefab;
	}

	private List<aPrefab> ProcessPrefabs(string assetsPathToProcess = "")
	{
		AssetDatabase.Refresh(ImportAssetOptions.ForceUncompressedImport);

		List<aPrefab> prefabsList = new List<aPrefab>();
		UnityEngine.Object[] prefabs = Resources.LoadAll<GameObject>(assetsPathToProcess);

		foreach (UnityEngine.GameObject obj in prefabs)
			prefabsList.Add(ProcessPrefab(obj));

		return prefabsList;
	}

	private void ProcessScripts()
	{
		foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
		{
			if (assembly.GetName().ToString().Contains("Assembly"))
			{
				AssemblyBuilder builder = AppDomain.CurrentDomain.DefineDynamicAssembly(assembly.GetName(), AssemblyBuilderAccess.RunAndSave, m_ProjectFolder);

				builder.Save(m_WorldName + ".bytes");

				return;
			}
		}
	}

	private void SaveProjectToXML()
	{
		XmlDocument doc = new XmlDocument();
		XmlElement worldElem = doc.CreateElement("world");

		XmlElement scriptsElem = doc.CreateElement("scripts");
		XmlElement scriptElem;
		XmlElement fieldElem;

		XmlElement prefabsElem = doc.CreateElement("prefabs");
		XmlElement prefabElem;

		XmlElement scenesElem = doc.CreateElement("scenes");
		XmlElement sceneElem;

		XmlElement objectsElem;
		XmlElement objectElem;

		doc.AppendChild(worldElem);

		scriptElem = doc.CreateElement("script");
		scriptElem.SetAttribute("path", m_WorldName + ".bytes");
		scriptsElem.AppendChild(scriptElem);
		worldElem.AppendChild(scriptsElem);

		foreach(aPrefab prefab in m_PrefabsList)
		{
			prefabElem = doc.CreateElement("prefab");
			prefabElem.SetAttribute("path", prefab.assetsFullPath);

			prefabsElem.AppendChild(prefabElem);
		}

		foreach(aScene scene in m_ScenesList)
		{
			sceneElem = doc.CreateElement("scene");
			sceneElem.SetAttribute("name", scene.sceneName);
			objectsElem = doc.CreateElement("objects");

			foreach(aPrefab prefab in scene.objects)
			{
				objectElem = doc.CreateElement("object");
				objectElem.SetAttribute("name", prefab.assetsFullPath);

				scriptsElem = doc.CreateElement("scripts");
				foreach(UnityEngine.MonoBehaviour mono in prefab.prefab.GetComponents<MonoBehaviour>())
				{
					scriptElem = doc.CreateElement("script");
					scriptElem.SetAttribute("name", mono.GetType().ToString());

					foreach(FieldInfo field in mono.GetType().GetFields())
					{
						fieldElem = doc.CreateElement("field");
						fieldElem.SetAttribute("name", field.Name);
						fieldElem.SetAttribute("value", field.GetValue(mono).ToString());
						scriptElem.AppendChild(fieldElem);
					}

					scriptsElem.AppendChild(scriptElem);
				}
				objectElem.AppendChild(scriptsElem);
				objectsElem.AppendChild(objectElem);
			}

			sceneElem.AppendChild(objectsElem);
			scenesElem.AppendChild(sceneElem);
		}

		worldElem.AppendChild(prefabsElem);
		worldElem.AppendChild(scenesElem);
		doc.Save(m_ProjectPath);
	}

	//TODO: Move asset bundle build process to 5.x version
	private void SaveProcessedData()
	{
		if ((m_ProjectPath.Length <= 0) || (m_ScenesList.Count <= 0))
			return;

		ConvertObjectsToPrefabs();

		foreach (aPrefab prefab in m_PrefabsList)
		{
			Directory.CreateDirectory(prefab.path);
			BuildPipeline.BuildAssetBundle(prefab.prefab, new UnityEngine.Object[] { prefab.prefab }, prefab.fullPath + ".unity3d", BuildAssetBundleOptions.CollectDependencies | BuildAssetBundleOptions.CompleteAssets, BuildTarget.StandaloneWindows);
		}

		foreach (aScene scene in m_ScenesList)
			foreach (aPrefab prefab in scene.objects)
			{
				Directory.CreateDirectory(prefab.path);
				BuildPipeline.BuildAssetBundle(prefab.prefab, new UnityEngine.Object[] { prefab.prefab }, prefab.fullPath + ".unity3d", BuildAssetBundleOptions.CollectDependencies | BuildAssetBundleOptions.CompleteAssets, BuildTarget.StandaloneWindows);
			}

		ProcessScripts();

		SaveProjectToXML();

		CleanTemploraryPrefabs();
	}

	//========================= EXPORTING =========================

	//Convert list of scenes to strings array through file's paths.
	private string[] GetScenesAsStrings()
	{
		if (m_ScenesList.Count <= 0)
			return null;

		List<string> scenes = new List<string>(m_ScenesList.Count);

		foreach (aScene scene in m_ScenesList)
			scenes.Add(scene.filePath);

		return scenes.ToArray();
	}

	[MenuItem("Divenet/Build world")]
	public static void ShowWindow()
	{
		DivenetMenuWindow window = (DivenetMenuWindow)EditorWindow.GetWindow<DivenetMenuWindow>(false, "Building world");

		window.Show();
	}

	void OnGUI()
	{
		m_ScrollPosition = EditorGUILayout.BeginScrollView(m_ScrollPosition);
		{
			if ((m_ScenesList == null) || (m_ScenesList.Count <= 0))
			{
				EditorGUILayout.BeginHorizontal();
				if (GUILayout.Button("Scan project scenes"))
				{
					ProcessWorld();
				}
				EditorGUILayout.EndHorizontal();
			}
			else
			{
				m_WorldName = EditorGUILayout.TextField("World name", m_WorldName);
				EditorGUILayout.BeginHorizontal();
				{
					m_ProjectPath = EditorGUILayout.TextField("Project path", m_ProjectPath);
					if (GUILayout.Button("Browse"))
					{
						string path = EditorUtility.SaveFilePanel("Build world", "", "newworld", "xml");

						if (path.Length != 0)
						{
							m_ProjectPath = path;

							m_ProjectFolder = path.Substring(0, path.LastIndexOf('/') + 1);
						}
					}
				}
				EditorGUILayout.EndHorizontal();

				m_MainSceneIndex = EditorGUILayout.Popup("Main scene", m_MainSceneIndex, GetScenesAsStrings(), EditorStyles.popup);
				EditorGUILayout.Space();
				if (GUILayout.Button("Build world"))
				{
					SaveProcessedData();
				}
			}
		}
		EditorGUILayout.EndScrollView();
	}
}
