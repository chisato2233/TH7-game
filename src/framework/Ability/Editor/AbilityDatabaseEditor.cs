using UnityEditor;
using UnityEngine;

namespace GameFramework.Editor
{
    /// <summary>
    /// AbilityDatabase 自定义 Inspector
    /// 提供技能/效果列表的快速预览和管理
    /// </summary>
    [CustomEditor(typeof(AbilityDatabase))]
    public class AbilityDatabaseEditor : UnityEditor.Editor
    {
        SerializedProperty abilitiesProp;
        SerializedProperty effectsProp;

        bool showAbilities = true;
        bool showEffects = true;

        Vector2 abilitiesScroll;
        Vector2 effectsScroll;

        void OnEnable()
        {
            abilitiesProp = serializedObject.FindProperty("Abilities");
            effectsProp = serializedObject.FindProperty("Effects");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            DrawHeader();

            EditorGUILayout.Space(8);

            showAbilities = DrawListSection("Abilities", showAbilities, abilitiesProp, ref abilitiesScroll, DrawAbilityItem);
            showEffects = DrawListSection("Effects", showEffects, effectsProp, ref effectsScroll, DrawEffectItem);

            serializedObject.ApplyModifiedProperties();
        }

        void DrawHeader()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);

            EditorGUILayout.BeginVertical();
            EditorGUILayout.LabelField("Ability Database", EditorStyles.boldLabel);
            EditorGUILayout.LabelField($"{abilitiesProp.arraySize} Abilities | {effectsProp.arraySize} Effects", EditorStyles.miniLabel);
            EditorGUILayout.EndVertical();

            if (GUILayout.Button("Rebuild Cache", GUILayout.Width(100), GUILayout.Height(36)))
            {
                var db = target as AbilityDatabase;
                db.RebuildCache();
                Debug.Log("AbilityDatabase cache rebuilt");
            }

            EditorGUILayout.EndHorizontal();
        }

        bool DrawListSection(string title, bool isExpanded, SerializedProperty listProp, ref Vector2 scroll, System.Action<SerializedProperty, int> drawItem)
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            // 标题栏
            EditorGUILayout.BeginHorizontal();
            isExpanded = EditorGUILayout.Foldout(isExpanded, $"{title} ({listProp.arraySize})", true, EditorStyles.foldoutHeader);

            GUILayout.FlexibleSpace();

            if (GUILayout.Button("+", GUILayout.Width(24)))
            {
                listProp.InsertArrayElementAtIndex(listProp.arraySize);
            }

            EditorGUILayout.EndHorizontal();

            if (isExpanded && listProp.arraySize > 0)
            {
                // 列表内容（带滚动）
                float maxHeight = Mathf.Min(listProp.arraySize * 24 + 8, 200);
                scroll = EditorGUILayout.BeginScrollView(scroll, GUILayout.MaxHeight(maxHeight));

                for (int i = 0; i < listProp.arraySize; i++)
                {
                    var element = listProp.GetArrayElementAtIndex(i);
                    drawItem(element, i);
                }

                EditorGUILayout.EndScrollView();
            }
            else if (isExpanded)
            {
                EditorGUILayout.HelpBox("No items. Click + to add.", MessageType.Info);
            }

            EditorGUILayout.EndVertical();

            return isExpanded;
        }

        void DrawAbilityItem(SerializedProperty element, int index)
        {
            EditorGUILayout.BeginHorizontal();

            // 序号
            EditorGUILayout.LabelField(index.ToString(), GUILayout.Width(24));

            // 引用字段
            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(element, GUIContent.none);

            // 快速信息
            var ability = element.objectReferenceValue as GameplayAbility;
            if (ability != null)
            {
                string info = $"[{ability.Type}]";
                if (ability.ManaCost > 0) info += $" Cost:{ability.ManaCost}";
                EditorGUILayout.LabelField(info, EditorStyles.miniLabel, GUILayout.Width(100));
            }

            // 删除按钮
            if (GUILayout.Button("x", GUILayout.Width(20)))
            {
                abilitiesProp.DeleteArrayElementAtIndex(index);
            }

            EditorGUILayout.EndHorizontal();
        }

        void DrawEffectItem(SerializedProperty element, int index)
        {
            EditorGUILayout.BeginHorizontal();

            // 序号
            EditorGUILayout.LabelField(index.ToString(), GUILayout.Width(24));

            // 引用字段
            EditorGUILayout.PropertyField(element, GUIContent.none);

            // 快速信息
            var effect = element.objectReferenceValue as GameplayEffect;
            if (effect != null)
            {
                string info = effect.DurationType switch
                {
                    EffectDurationType.Instant => "[Instant]",
                    EffectDurationType.Duration => $"[{effect.Duration}s]",
                    EffectDurationType.Infinite => "[Infinite]",
                    EffectDurationType.Periodic => $"[P:{effect.Period}s]",
                    _ => ""
                };
                EditorGUILayout.LabelField(info, EditorStyles.miniLabel, GUILayout.Width(80));
            }

            // 删除按钮
            if (GUILayout.Button("x", GUILayout.Width(20)))
            {
                effectsProp.DeleteArrayElementAtIndex(index);
            }

            EditorGUILayout.EndHorizontal();
        }
    }
}
