using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using TowerDefense.Components;

namespace TowerDefense.Editor
{
    public class PrefabDefinitionEditorWindow : EditorWindow
    {
        private const int TilePreviewSize = 256;
        private const int DefaultSpacing = 10;
        private const int CameraFieldOfView = 13;
        
        private readonly Rect _thumbnailSize = new(0, 0, 64, 64);
        private readonly Rect _iconSize = new(0, 0, 512, 512);
        
        private List<string> _prefabPaths = new();
        private int _selectedIndex = -1;
        
        private GameObject _previewContents;
        private SerializedObject _selectedSerializedObject;

        private GameObject _newModelPrefab;

        private readonly Dictionary<string, Texture2D> _thumbnails = new();
        
        private Vector2 _leftScroll;

        [MenuItem("Tools/Tile Prefab Editor")]
        public static void ShowWindow()
        {
            PrefabDefinitionEditorWindow window = GetWindow<PrefabDefinitionEditorWindow>("Tile Prefab Editor");
            window.minSize = new Vector2(600, 600);
        }

        private void OnEnable()
        {
            RefreshPrefabList();
        }

        private void OnDisable()
        {
            UnloadPreview();
        }

        private void RefreshPrefabList()
        {
            _prefabPaths = AssetDatabase.FindAssets("t:Prefab")
                .Select(AssetDatabase.GUIDToAssetPath)
                .Where(p =>
                {
                    GameObject go = AssetDatabase.LoadAssetAtPath<GameObject>(p);
                    return go != null && go.GetComponent<TileDefinition>() != null;
                })
                .ToList();

            if (_selectedIndex >= _prefabPaths.Count)
            {
                _selectedIndex = -1;
            }
            
            GenerateThumbnails();
        }
        
        private void GenerateThumbnails()
        {
            _thumbnails.Clear();
            GameObject previousPreviewContents = _previewContents;

            try
            {
                foreach (string path in _prefabPaths)
                {
                    _previewContents = PrefabUtility.LoadPrefabContents(path);

                    Texture2D tex = GenerateThumbnails(_thumbnailSize);
                    PrefabUtility.UnloadPrefabContents(_previewContents);
                    _previewContents = null;

                    _thumbnails[path] = tex;
                }
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                throw;
            }
            finally
            {
                _previewContents = previousPreviewContents;
            }
        }

        private void OnGUI()
        {
            EditorGUILayout.BeginHorizontal();
            {
                _leftScroll = EditorGUILayout.BeginScrollView(_leftScroll, GUILayout.Width(250));
                {
                    DrawLeftPanel();
                }
                EditorGUILayout.EndScrollView();

                EditorGUILayout.BeginVertical();
                {
                    DrawRightPanel();
                }
                EditorGUILayout.EndVertical();
            }
            EditorGUILayout.EndHorizontal();
        }

        private void DrawRightPanel()
        {
            if (_selectedIndex >= 0)
            {
                GUILayout.Label("Editing: " + _prefabPaths[_selectedIndex], EditorStyles.boldLabel);
                LoadForEdit(_prefabPaths[_selectedIndex]);

                EditorGUILayout.PropertyField(_selectedSerializedObject.FindProperty("_definitionId"));
                GUILayout.Space(DefaultSpacing);
                GUILayout.Label("Connections:", EditorStyles.boldLabel);

                Rect previewRect = GUILayoutUtility.GetRect(TilePreviewSize, TilePreviewSize, GUILayout.ExpandWidth(false));
                DrawDirections();
                DrawPreview(previewRect);

                _selectedSerializedObject.ApplyModifiedProperties();

                GUILayout.Space(DefaultSpacing);
                        
                if (GUILayout.Button("Save Changes"))
                {
                    SaveEditedPrefab();
                    RefreshPrefabList();
                }
            }
        }

        private void DrawPreview(Rect previewRect)
        {
            if (_previewContents == null)
            {
                return;
            }

            PreviewRenderUtility previewUtility = new PreviewRenderUtility
            {
                cameraFieldOfView = CameraFieldOfView
            };
            
            GameObject instance = null;

            try
            {
                SetupCameraAndLights(previewUtility);

                instance = Instantiate(_previewContents);
                previewUtility.BeginPreview(previewRect, GUIStyle.none);

                RenderMeshes(previewUtility, instance);

                previewUtility.camera.Render();
                Texture result = previewUtility.EndPreview();
                GUI.DrawTexture(previewRect, result, ScaleMode.ScaleToFit, false);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
            finally
            {
                previewUtility.Cleanup();
                
                if (instance != null)
                {
                    DestroyImmediate(instance);
                }
            }
        }
        
        private Texture2D GenerateThumbnails(Rect rect)
        {
            PreviewRenderUtility previewUtility = new PreviewRenderUtility
            {
                cameraFieldOfView = CameraFieldOfView
            };
            RenderTexture prev = RenderTexture.active;
            GameObject instance = null;
            Texture2D previewTexture;

            try
            {
                SetupCameraAndLights(previewUtility);

                instance = Instantiate(_previewContents);
                previewUtility.BeginPreview(rect, GUIStyle.none);

                RenderMeshes(previewUtility, instance);

                previewUtility.camera.Render();
                Texture rt = previewUtility.EndPreview();
                RenderTexture.active = (RenderTexture) rt;
                previewTexture = new Texture2D((int) rect.width, (int) rect.height, TextureFormat.ARGB32, false);
                previewTexture.ReadPixels(new Rect(0, 0, rect.width, rect.height), 0, 0);
                previewTexture.Apply();
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                throw;
            }
            finally
            {
                RenderTexture.active = prev;
                previewUtility.Cleanup();
                
                if (instance != null)
                {
                    DestroyImmediate(instance);
                }
            }

            return previewTexture;
        }

        private void SetupCameraAndLights(PreviewRenderUtility util)
        {
            Camera cam = util.camera;
            cam.transform.position = new Vector3(0, 5, 0);
            cam.transform.rotation = Quaternion.Euler(90, 0, 0);

            Light light0 = util.lights[0];
            light0.intensity = 1.4f;
            light0.transform.rotation = Quaternion.Euler(40f, 40f, 0f);

            Light light1 = util.lights[1];
            light1.intensity = 1.4f;
            light1.transform.rotation = Quaternion.Euler(-40f, -40f, 0f);
        }

        private void RenderMeshes(PreviewRenderUtility util, GameObject root)
        {
            MeshFilter[] meshFilters = root.GetComponentsInChildren<MeshFilter>();
            
            foreach (MeshFilter mf in meshFilters)
            {
                Mesh mesh = mf.sharedMesh;
                Renderer renderer = mf.GetComponent<Renderer>();

                if (mesh == null || renderer == null || renderer.sharedMaterials == null)
                {
                    continue;
                }

                Material[] materials = renderer.sharedMaterials;
                Matrix4x4 matrix = mf.transform.localToWorldMatrix;
                
                for (int i = 0; i < mesh.subMeshCount; i++)
                {
                    Material mat = i < materials.Length ? materials[i] : materials[0];
                    util.DrawMesh(mesh, matrix, mat, i);
                }
            }
        }

        private void DrawLeftPanel()
        {
            GUILayout.Label("Create New Prefab", EditorStyles.boldLabel);
            _newModelPrefab =
                (GameObject) EditorGUILayout.ObjectField("Model Prefab:", _newModelPrefab, typeof(GameObject), false);

            if (GUILayout.Button("Create New") && _newModelPrefab != null)
            {
                CreateNew(_newModelPrefab);
                RefreshPrefabList();
            }

            if (GUILayout.Button("Refresh List"))
            {
                RefreshPrefabList();
            }
            
            if (GUILayout.Button("Create icons"))
            {
                GenerateAndSaveIcons();
            }

            GUILayout.Space(DefaultSpacing);
            GUILayout.Label("Existing Prefabs:", EditorStyles.boldLabel);
        
            foreach ((string path, int i) in _prefabPaths.Select((p, i) => (p, i)))
            {
                EditorGUILayout.BeginHorizontal();
                {
                    if (_thumbnails.TryGetValue(path, out Texture2D thumbnail) && thumbnail != null)
                    {
                        GUILayout.Label(thumbnail, GUILayout.Width(_thumbnailSize.width), GUILayout.Height(_thumbnailSize.height));
                    }
                    else
                    {
                        GUILayout.Space(_thumbnailSize.height);
                    }

                    bool selected = i == _selectedIndex;
                    
                    if (GUILayout.Toggle(selected, System.IO.Path.GetFileNameWithoutExtension(path), "Button"))
                    {
                        if (_selectedIndex != i)
                        {
                            UnloadPreview();
                            _selectedIndex = i;
                            _selectedSerializedObject = null;
                        }
                    }
                }
                EditorGUILayout.EndHorizontal();
            }
        }

        private void DrawDirections()
        {
            EditorGUILayout.PropertyField(_selectedSerializedObject.FindProperty("_north"), new GUIContent("↑"));
            EditorGUILayout.PropertyField(_selectedSerializedObject.FindProperty("_south"), new GUIContent("↓"));
            EditorGUILayout.PropertyField(_selectedSerializedObject.FindProperty("_west"), new GUIContent("←"));
            EditorGUILayout.PropertyField(_selectedSerializedObject.FindProperty("_east"), new GUIContent("→"));
        }

        private void CreateNew(GameObject modelSource)
        {
            GameObject temp = new GameObject(modelSource.name);
            TileDefinition definition = temp.AddComponent<TileDefinition>();
            definition.GetType().GetField("_definitionId",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.SetValue(definition, modelSource.name);

            string path = EditorUtility.SaveFilePanelInProject("Save New Tile Prefab", modelSource.name + ".prefab", "prefab",
                "New prefab location", "/Prefabs/Tiles");
            
            if (string.IsNullOrEmpty(path))
            {
                DestroyImmediate(temp);
                return;
            }

            PrefabUtility.SaveAsPrefabAssetAndConnect(temp, path, InteractionMode.UserAction);
            GameObject child = PrefabUtility.LoadPrefabContents(path);
            GameObject model = (GameObject) PrefabUtility.InstantiatePrefab(modelSource, child.transform);
            model.transform.localPosition = Vector3.zero;
            PrefabUtility.SaveAsPrefabAsset(child, path);
            PrefabUtility.UnloadPrefabContents(child);
            DestroyImmediate(temp);
        }

        private void LoadForEdit(string assetPath)
        {
            if (_selectedSerializedObject == null)
            {
                _previewContents = PrefabUtility.LoadPrefabContents(assetPath);
                TileDefinition def = _previewContents.GetComponent<TileDefinition>();
                _selectedSerializedObject = new SerializedObject(def);
            }

            _selectedSerializedObject.Update();
        }

        private void SaveEditedPrefab()
        {
            PrefabUtility.SaveAsPrefabAsset(_previewContents, _prefabPaths[_selectedIndex]);
            AssetDatabase.Refresh();
            Debug.Log($"Prefab saved: {_prefabPaths[_selectedIndex]}");
        }

        private void UnloadPreview()
        {
            if (_previewContents != null)
            {
                PrefabUtility.UnloadPrefabContents(_previewContents);
                _previewContents = null;
            }

            _selectedSerializedObject = null;
        }
        
        private void GenerateAndSaveIcons()
        {
            const string folder = "Assets/UI/Icons/Tiles";

            foreach (var path in _prefabPaths)
            {
                _previewContents = PrefabUtility.LoadPrefabContents(path);
                Texture2D tex = GenerateThumbnails(_iconSize);
                PrefabUtility.UnloadPrefabContents(_previewContents);
                _previewContents = null;
                
                byte[] pngData = tex.EncodeToPNG();
                string fileName = System.IO.Path.GetFileNameWithoutExtension(path) + ".png";
                string assetPath = $"{folder}/{fileName}";
                System.IO.File.WriteAllBytes(assetPath, pngData);
                AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);
                
                TextureImporter importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
                importer!.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.mipmapEnabled = false;
                importer.filterMode = FilterMode.Bilinear; 
                importer.SaveAndReimport();
            }

            AssetDatabase.Refresh();
        }
    }
}
