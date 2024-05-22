using HHG.Common.Runtime;
using HHG.SpawnSystem.Runtime;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.EditorTools;
using UnityEngine;

namespace HHG.SpawnSystem.Editor
{
    [EditorTool("Spawn Waves Editor")]
    public class SpawnWavesEditorTool : EditorTool
    {
        private const string showNamesKey = nameof(SpawnWavesEditorTool) + nameof(showNames);

        public static event System.Action SpawnEdited;

        private const float imageScaleFactor = 1000f;
        private const float imageSize = 100f;
        private const float buttonSize = 80f;
        private const float handleOffsetY = 2f;
        private const float nameWidth = 100f;
        private const float nameHeight = 20f;

        private SpawnManager _manager;
        private SpawnManager manager
        {
            get
            {
                if (_manager == null)
                {
                    _manager = ObjectUtil.FindObjectOfType<SpawnManager>();
                }
                return _manager;
            }
        }
        private ScriptableObject[] spawns;
        private int wave = 0;
        private bool showNames;
        private bool shouldReturn;

        public override void OnActivated()
        {
            showNames = EditorPrefs.GetBool(showNamesKey, false);

            DropdownUtil.GetChoiceArray(ref spawns, t => t.IsBaseImplementationOf(typeof(ISpawnAsset)), o => (o as ISpawnAsset).IsEnabled);
        }

        public override void OnToolGUI(EditorWindow window)
        {
            if (window is not SceneView || manager is null || manager.SpawnWaves is null) return;

            DrawToolbar(window);
            DrawSpawnPoint();
        }

        private void DrawToolbar(EditorWindow window)
        {
            Handles.BeginGUI();
            GUILayout.Space(5f);
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();

            if (GUILayout.Button("◀"))
            {
                wave--;
            }

            if (GUILayout.Button("▶"))
            {
                wave++;
            }

            string[] options = Enumerable.Range(0, manager.SpawnWaves.WaveCount).Select(w => $"Spawn Wave {w}").ToArray();
            wave = EditorGUILayout.Popup(wave, options);

            string on = "Names ✔";
            string off = "Names ✘";
            if (GUILayout.Button(showNames ? on : off))
            {
                showNames = !showNames;
                EditorPrefs.SetBool(showNamesKey, showNames);
            }

            // Clamp since decrement and increment
            wave = Mathf.Clamp(wave, 0, manager.SpawnWaves.WaveCount - 1);

            GUILayout.Space(5f);
            GUILayout.EndHorizontal();
            Handles.EndGUI();
        }

        private void DrawSpawnPoint()
        {
            if (manager != null)
            {
                IReadOnlyList<SpawnPoint> spawnPoints = manager.SpawnWaves.SpawnPoints;

                shouldReturn = false;

                // Draw all handles first so always prioritize moving
                for (int i = 0; i < spawnPoints.Count; i++)
                {
                    DrawSpawnPointHandle(spawnPoints[i]);

                    if (shouldReturn) return;
                }

                for (int i = 0; i < spawnPoints.Count; i++)
                {
                    DrawSpawnPointButton(spawnPoints[i], wave);

                    if (shouldReturn) return;
                }
            }
        }

        private void DrawSpawnPointButton(SpawnPoint spawnPoint, int wave)
        {
            Handles.BeginGUI();

            float orthSize = SceneView.currentDrawingSceneView.camera.orthographicSize;
            float imageSize = imageScaleFactor / orthSize;

            Vector2 guiPosition = HandleUtility.WorldToGUIPoint(spawnPoint.Position);
            Rect spriteRect = new Rect(0f, 0f, imageSize, imageSize);
            Vector2 spritePivot = new Vector2(imageSize / 2f, imageSize / 2f);
            Sprite sprite = spawnPoint.GetSpawn(wave).Asset?.Icon;

            if (sprite != null)
            {
                spriteRect = sprite.rect;
                spritePivot = sprite.pivot;
            }

            float ratio = spriteRect.width / spriteRect.height;
            float height = imageSize;
            float width = height * ratio;
            float pivotY = spriteRect.height - spritePivot.y;
            Vector2 size = new Vector2(width, height);
            Vector2 scale = size / spriteRect.size;
            Vector2 pivot = new Vector2(spritePivot.x, pivotY);
            Vector2 offset = scale * pivot;
            Vector2 center = guiPosition - offset;
            Rect rect = new Rect(center, size);

            if (sprite != null)
            {
                GUI.DrawTexture(rect, sprite.texture);
            }
            else
            {
                GUI.DrawTexture(rect, Texture2D.whiteTexture, ScaleMode.StretchToFill, true, 1f, new Color(1f, 1f, 1f, .35f), 0f, 0f);
            }

            Vector2 size1 = new Vector2(buttonSize, buttonSize);
            Rect rect1 = new Rect(center, size1);
            if (GUI.Button(rect1, new GUIContent(string.Empty, spawnPoint.Name), new GUIStyle()))
            {
                DrawSpawnPointButtonDropdown(spawnPoint);
            }

            if (showNames)
            {
                Vector2 size2 = new Vector2(nameWidth, nameHeight);
                Vector2 offset2 = new Vector2(nameWidth / 2f, -nameHeight);
                Vector2 center2 = guiPosition - offset2;
                Rect rect2 = new Rect(center2, size2);
                
                if (GUI.Button(rect2, spawnPoint.Name))
                {
                    DrawSpawnPointButtonDropdown(spawnPoint);
                }
            }

            Handles.EndGUI();
        }

        private void DrawSpawnPointHandle(SpawnPoint spawnPoint)
        {
            EditorGUI.BeginChangeCheck();

            Vector2 handlePosition = spawnPoint.Position + Vector2.up * handleOffsetY;
            Vector3 position = Handles.PositionHandle(handlePosition, Quaternion.identity);

            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(manager, "Change Spawn Point Position");
                spawnPoint.Position = (Vector2)position + Vector2.down * handleOffsetY;
                EditorUtility.SetDirty(manager.SpawnWaves);
            }
        }

        private void DrawSpawnPointButtonDropdown(SpawnPoint spawnPoint)
        {
            GenericMenu menu = new GenericMenu();

            for (int i = 0; i < spawns.Length; i++)
            {
                ScriptableObject spawn = spawns[i];
                bool on = spawnPoint[wave] == spawn;
                string name = DropdownUtil.FormatChoiceText(spawn?.name ?? "None");
                menu.AddItem(new GUIContent($"Spawn/{name}"), on, () =>
                {
                    spawnPoint[wave] = spawn;
                    EditorUtility.SetDirty(manager.SpawnWaves);
                    SpawnEdited?.Invoke();
                    shouldReturn = true;
                });
            }

            menu.AddItem(new GUIContent("Duplicate"), false, () =>
            {
                manager.SpawnWaves.DuplicateSpawnPoint(spawnPoint);
                EditorUtility.SetDirty(manager.SpawnWaves);
                SpawnEdited?.Invoke();
            });

            menu.AddItem(new GUIContent("Delete"), false, () =>
            {
                manager.SpawnWaves.RemoveSpawnPoint(spawnPoint);
                EditorUtility.SetDirty(manager.SpawnWaves);
                SpawnEdited?.Invoke();
            });

            menu.ShowAsContext();
        }
    }
}