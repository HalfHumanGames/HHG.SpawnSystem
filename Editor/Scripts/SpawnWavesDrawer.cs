using HHG.Common.Runtime;
using HHG.SpawnSystem.Runtime;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace HHG.SpawnSystem.Editor
{
    [CustomPropertyDrawer(typeof(SpawnWaves))]
    public class SpawnWavesDrawer : PropertyDrawer
    {
        private const int columnWidth = 150;

        private SpawnWavesAsset asset;
        private List<ScriptableObject> choiceAssets = new List<ScriptableObject>();
        private List<string> choiceNames = new List<string>();
        private Columns columns;
        private MultiColumnListView table;
        private HashSet<SpawnPoint> selection = new HashSet<SpawnPoint>();
        private int copySpawnPoint = -1;
        private int copySpawnWave = -1;

        private SpawnWavesAsset GetAsset(SerializedProperty property) => property.serializedObject.targetObject as SpawnWavesAsset;

        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            asset = GetAsset(property);

            VisualElement container = new VisualElement();

            RefreshDropdownValues();

            columns = new Columns();
            columns.reorderable = true;

            // Use reflection since columnReordered is not public for whatever reason
            Action<Column, int, int> handler = OnColumnReordered;
            EventInfo eventInfo = typeof(Columns).GetEvent("columnReordered", BindingFlags.NonPublic | BindingFlags.Instance);
            MethodInfo methodInfo = eventInfo.GetAddMethod(true);
            methodInfo.Invoke(columns, new[] { handler });

            table = new MultiColumnListView(columns);
            table.itemsSource = asset == null ? new List<SpawnPoint>() : asset.SpawnPointsList;
            table.reorderable = true;
            table.reorderMode = ListViewReorderMode.Animated;
            table.selectionType = SelectionType.None;
            RebuildSpawnPointColumn();
            RebuildSpawnWaveColumns();
            container.Add(table);

            SpawnWavesEditorTool.SpawnEdited -= OnSpawnPointEdited;
            SpawnWavesEditorTool.SpawnEdited += OnSpawnPointEdited;

            return container;
        }

        private void RefreshDropdownValues()
        {
            DropdownUtility.GetChoiceList(ref choiceAssets, ref choiceNames, null, t => t.Implements(typeof(ISpawnAsset)));
        }

        private void OnSpawnPointEdited()
        {
            table.RefreshItems();
        }

        private void OnColumnReordered(Column column, int from, int to)
        {
            if (from == 0 || to == 0)
            {
                RebuildSpawnPointColumn();
                RebuildSpawnWaveColumns();
                return;
            }

            // Substract 1 to convert from column index to wave index
            int fromWave = from - 1;
            int toWave = to - 1;

            asset.ReorderSpawnPoint(fromWave, toWave);

            for (int i = 1; i < columns.Count; i++)
            {
                int wave = i - 1;
                columns[i].name = wave.ToString();
                columns[i].title = $"Spawn Wave {wave}";
            }

            RebuildSpawnWaveColumns();
            EditorUtility.SetDirty(asset);
        }

        #region Rebuild columns

        private void RebuildSpawnPointColumn()
        {
            //int index = columns.Count;
            Column column = new Column()
            {
                name = "Spawn Points",
                title = "Spawn Points",
                width = columnWidth,
                makeHeader = () =>
                {
                    VisualElement container = new VisualElement();
                    container.style.flexDirection = FlexDirection.Row;

                    Toggle toggle = new Toggle();
                    toggle.RegisterValueChangedCallback(OnSpawnPointHeaderToggleChanged);
                    container.Add(toggle);

                    Label label = new Label($"Spawn Points");
                    label.AddManipulator(CreateSpawnPointHeaderContextMenu(label));
                    container.Add(label);

                    return container;
                },
                bindHeader = (VisualElement element) => { },
                makeCell = () =>
                {
                    VisualElement container = new VisualElement();
                    container.style.flexDirection = FlexDirection.Row;

                    Toggle toggle = new Toggle();
                    container.Add(toggle);

                    TextField textField = new TextField();
                    textField.style.flexGrow = 1f;
                    textField.AddManipulator(CreateSpawnPointContextMenu(textField));
                    container.Add(textField);

                    return container;
                },
                bindCell = (VisualElement element, int index) =>
                {
                    Toggle toggle = element.Children().First() as Toggle;
                    toggle.UnregisterValueChangedCallback(OnSpawnPointToggleChanged);
                    toggle.RegisterValueChangedCallback(OnSpawnPointToggleChanged);
                    toggle.userData = index;
                    toggle.SetValueWithoutNotify(asset != null && index < asset.SpawnPoints.Count ? selection.Contains(asset.SpawnPoints[index]) : false);

                    TextField textField = element.Children().Skip(1).First() as TextField;
                    textField.UnregisterValueChangedCallback(OnSpawnPointTextChanged);
                    textField.RegisterValueChangedCallback(OnSpawnPointTextChanged);
                    textField.userData = index;
                    textField.SetValueWithoutNotify(asset != null && index < asset.SpawnPoints.Count ? asset.SpawnPoints[index].Name : $"Spawn Point {index}");
                },
            };

            if (columns.Count == 0)
            {
                columns.Add(column);
            }
            else
            {
                columns.RemoveAt(0);

                if (columns.Count == 0)
                {
                    columns.Add(column);
                }
                else
                {
                    columns.Insert(0, column);
                }
            }
        }


        private void RebuildSpawnWaveColumns()
        {
            while (columns.Count > 1)
            {
                columns.RemoveAt(1);
            }

            if (asset == null) return;

            for (int i = 0; i < asset.WaveCount; i++)
            {
                int wave = columns.Count - 1;
                columns.Add(new Column()
                {
                    name = wave.ToString(),
                    title = $"Spawn Wave {wave}",
                    width = columnWidth,
                    makeHeader = () =>
                    {
                        Label label = new Label($"Spawn Wave {wave}");
                        label.AddManipulator(CreateSpawnWaveContextMenu(label));
                        return label;
                    },
                    bindHeader = (VisualElement element) => {
                        Label label = element as Label;
                        label.tooltip = asset.GetWaveInfoText(wave);
                        label.UnregisterCallback<PointerEnterEvent>(OnSpawnWaveHeaderClicked);
                        label.RegisterCallback<PointerEnterEvent>(OnSpawnWaveHeaderClicked);
                        label.userData = wave;
                    },
                    makeCell = () =>
                    {
                        DropdownField dropdownField = new DropdownField(choiceNames, 0);
                        dropdownField.AddManipulator(CreateDropdownContextMenu(dropdownField));
                        return dropdownField;
                    },
                    bindCell = (VisualElement element, int index) =>
                    {
                        DropdownField dropdownField = element as DropdownField;
                        dropdownField.UnregisterValueChangedCallback(OnSpawnDropdownChanged);
                        dropdownField.RegisterValueChangedCallback(OnSpawnDropdownChanged);
                        dropdownField.userData = (index, wave);
                        dropdownField.SetValueWithoutNotify(index < asset.SpawnPoints.Count ? asset.SpawnPoints[index][wave]?.name ?? "None" : "None");
                    }
                });
            }
        }

        #endregion

        #region Input callbacks

        void OnSpawnWaveHeaderClicked(PointerEnterEvent evt)
        {
            Label label = evt.target as Label;
            label.tooltip = asset.GetWaveInfoText((int) label.userData);
        }

        void OnSpawnPointHeaderToggleChanged(ChangeEvent<bool> evt)
        {
            if (asset != null)
            {
                if (evt.newValue)
                {
                    foreach (SpawnPoint spawnPoint in asset.SpawnPoints)
                    {
                        selection.Add(spawnPoint);
                    }
                }
                else
                {
                    selection.Clear();
                }

                table.RefreshItems();
            }
        }

        private void OnSpawnPointToggleChanged(ChangeEvent<bool> evt)
        {
            int index = (int)(evt.target as Toggle).userData;

            if (evt.newValue)
            {
                selection.Add(asset.SpawnPoints[index]);
            }
            else
            {
                selection.Remove(asset.SpawnPoints[index]);
            }

            table.RefreshItems();
        }

        private void OnSpawnPointTextChanged(ChangeEvent<string> evt)
        {
            int index = (int)(evt.target as TextField).userData;
            asset.SpawnPoints[index].Name = evt.newValue;
            EditorUtility.SetDirty(asset);
        }

        private void OnSpawnDropdownChanged(ChangeEvent<string> evt)
        {
            (int index, int wave) = ((int, int))(evt.target as DropdownField).userData;

            ScriptableObject value = choiceAssets.FirstOrDefault(c => c != null && c.name == evt.newValue);

            asset.SpawnPoints[index][wave] = value;

            foreach (SpawnPoint spawnPoint in selection)
            {
                spawnPoint[wave] = value;
            }

            table.RefreshItems();
            EditorUtility.SetDirty(asset);
        }

        #endregion

        #region Context menus

        private ContextualMenuManipulator CreateSpawnPointHeaderContextMenu(VisualElement visualElement) => new ContextualMenuManipulator((evt) =>
        {
            if (asset == null) return;

            evt.menu.AppendAction("Add", (x) =>
            {
                asset.AddSpawnPoint();

                if (asset.WaveCount == 0)
                {
                    asset.AddSpawnWave();
                    RebuildSpawnWaveColumns();
                }

                table.RefreshItems();
                EditorUtility.SetDirty(asset);
            }, DropdownMenuAction.AlwaysEnabled, visualElement);

            evt.menu.AppendAction("Clear", (x) =>
            {
                asset.Reset();
                selection.Clear();
                RebuildSpawnWaveColumns();
                table.RefreshItems();
                EditorUtility.SetDirty(asset);
            }, DropdownMenuAction.AlwaysEnabled, visualElement);
        });

        private ContextualMenuManipulator CreateSpawnPointContextMenu(VisualElement visualElement) => new ContextualMenuManipulator((evt) =>
        {
            if (asset == null) return;

            evt.menu.AppendAction("Insert", (x) =>
            {
                int index = (int)((VisualElement)x.userData).userData;
                foreach (SpawnPoint spawnPoint in selection.Prepend(asset.SpawnPoints[index]).Distinct())
                {
                    asset.InsertNewSpawnPointAfter(spawnPoint);
                }

                if (asset.WaveCount == 0)
                {
                    asset.AddSpawnWave();
                    RebuildSpawnWaveColumns();
                }

                table.RefreshItems();
                EditorUtility.SetDirty(asset);
            }, DropdownMenuAction.AlwaysEnabled, visualElement);

            evt.menu.AppendAction("Duplicate", (x) =>
            {
                int index = (int)((VisualElement)x.userData).userData;
                foreach (SpawnPoint spawnPoint in selection.Prepend(asset.SpawnPoints[index]).Distinct())
                {
                    asset.DuplicateSpawnPoint(spawnPoint);
                }
                table.RefreshItems();
                EditorUtility.SetDirty(asset);
            }, DropdownMenuAction.AlwaysEnabled, visualElement);

            evt.menu.AppendAction("Copy", (x) =>
            {
                int index = (int)((VisualElement)x.userData).userData;
                copySpawnPoint = index;
            }, DropdownMenuAction.AlwaysEnabled, visualElement);

            evt.menu.AppendAction("Paste", (x) =>
            {
                int index = (int)((VisualElement)x.userData).userData;
                asset.CopyPasteSpawnPoint(copySpawnPoint, index);
                foreach (SpawnPoint spawnPoint in selection)
                {
                    asset.CopyPasteSpawnPoint(copySpawnPoint, spawnPoint);
                }
                copySpawnPoint = -1;
                table.RefreshItems();
                EditorUtility.SetDirty(asset);
            }, _ => copySpawnPoint >= 0 ? DropdownMenuAction.Status.Normal : DropdownMenuAction.Status.Disabled, visualElement);

            evt.menu.AppendAction("Delete", (x) =>
            {
                int index = (int)((VisualElement)x.userData).userData;

                SpawnPoint selectedPoint = asset.SpawnPoints[index];
                asset.RemoveSpawnPoint(selectedPoint);
                selection.Remove(selectedPoint);

                List<SpawnPoint> toRemoveFromSelection = new List<SpawnPoint>();

                foreach (SpawnPoint spawnPoint in selection)
                {
                    asset.RemoveSpawnPoint(spawnPoint);
                    toRemoveFromSelection.Add(spawnPoint);
                }

                foreach (SpawnPoint spawnPoint in toRemoveFromSelection)
                {
                    selection.Remove(spawnPoint);
                }

                table.RefreshItems();
                EditorUtility.SetDirty(asset);
            }, DropdownMenuAction.AlwaysEnabled, visualElement);
        });

        private ContextualMenuManipulator CreateSpawnWaveContextMenu(VisualElement visualElement) => new ContextualMenuManipulator((evt) =>
        {
            if (asset == null) return;

            evt.menu.AppendAction("Insert", (x) =>
            {
                int wave = (int)((VisualElement)x.userData).userData;

                if (asset.SpawnPoints.Count == 0)
                {
                    asset.AddSpawnPoint();
                }
                else
                {
                    asset.InsertNewSpawnWaveAt(wave + 1);
                }

                RebuildSpawnWaveColumns();
                EditorUtility.SetDirty(asset);
            }, DropdownMenuAction.AlwaysEnabled, visualElement);

            evt.menu.AppendAction("Duplicate", (x) =>
            {
                int wave = (int)((VisualElement)x.userData).userData;
                asset.DuplicateSpawnWaveAt(wave);
                RebuildSpawnWaveColumns();
                EditorUtility.SetDirty(asset);
            }, DropdownMenuAction.AlwaysEnabled, visualElement);

            evt.menu.AppendAction("Copy", (x) =>
            {
                int wave = (int)((VisualElement)x.userData).userData;
                copySpawnWave = wave;
            }, DropdownMenuAction.AlwaysEnabled, visualElement);

            evt.menu.AppendAction("Paste", (x) =>
            {
                int wave = (int)((VisualElement)x.userData).userData;
                asset.CopyPasteSpawnWave(copySpawnWave, wave);
                copySpawnWave = -1;
                table.RefreshItems();
                EditorUtility.SetDirty(asset);
            }, _ => copySpawnWave >= 0 ? DropdownMenuAction.Status.Normal : DropdownMenuAction.Status.Disabled, visualElement);

            evt.menu.AppendAction("Delete", (x) =>
            {
                int wave = (int)((VisualElement)x.userData).userData;
                asset.RemoveSpawnWaveAt(wave);
                columns.RemoveAt(wave + 1);
                RebuildSpawnWaveColumns();
                EditorUtility.SetDirty(asset);
            }, DropdownMenuAction.AlwaysEnabled, visualElement);
        });

        private ContextualMenuManipulator CreateDropdownContextMenu(VisualElement visualElement) => new ContextualMenuManipulator((evt) =>
        {
            if (asset == null) return;

            evt.menu.AppendAction("Refresh", (x) =>
            {
                RefreshDropdownValues();
            }, DropdownMenuAction.AlwaysEnabled, visualElement);
        });

        #endregion
    }
}
