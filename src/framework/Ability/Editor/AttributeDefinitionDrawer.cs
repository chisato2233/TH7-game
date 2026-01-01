using UnityEditor;
using UnityEngine;

namespace GameFramework.Editor
{
    /// <summary>
    /// AttributeDefinition 的自定义 PropertyDrawer
    /// 紧凑显示: Name | Default | Min~Max | Int
    /// </summary>
    [CustomPropertyDrawer(typeof(AttributeDefinition))]
    public class AttributeDefinitionDrawer : PropertyDrawer
    {
        const float DefaultWidth = 60f;
        const float RangeWidth = 100f;
        const float IntWidth = 30f;
        const float Spacing = 4f;

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            var nameProp = property.FindPropertyRelative("Name");
            var displayNameProp = property.FindPropertyRelative("DisplayName");
            var defaultProp = property.FindPropertyRelative("DefaultValue");
            var minProp = property.FindPropertyRelative("MinValue");
            var maxProp = property.FindPropertyRelative("MaxValue");
            var isIntProp = property.FindPropertyRelative("IsInteger");
            var categoryProp = property.FindPropertyRelative("Category");

            // 第一行: Name | DisplayName
            var firstLine = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);
            float halfWidth = (position.width - Spacing) / 2;

            var nameRect = new Rect(firstLine.x, firstLine.y, halfWidth, firstLine.height);
            var displayRect = new Rect(firstLine.x + halfWidth + Spacing, firstLine.y, halfWidth, firstLine.height);

            EditorGUI.PropertyField(nameRect, nameProp, new GUIContent("Name"));
            EditorGUI.PropertyField(displayRect, displayNameProp, new GUIContent("Display"));

            // 第二行: Default | Min | Max | IsInt
            float y2 = position.y + EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
            var secondLine = new Rect(position.x, y2, position.width, EditorGUIUtility.singleLineHeight);

            float fieldWidth = (position.width - Spacing * 3 - IntWidth) / 3;

            var defaultRect = new Rect(secondLine.x, secondLine.y, fieldWidth, secondLine.height);
            var minRect = new Rect(secondLine.x + fieldWidth + Spacing, secondLine.y, fieldWidth, secondLine.height);
            var maxRect = new Rect(secondLine.x + fieldWidth * 2 + Spacing * 2, secondLine.y, fieldWidth, secondLine.height);
            var intRect = new Rect(secondLine.x + fieldWidth * 3 + Spacing * 3, secondLine.y, IntWidth, secondLine.height);

            EditorGUI.PropertyField(defaultRect, defaultProp, new GUIContent("Def"));
            EditorGUI.PropertyField(minRect, minProp, new GUIContent("Min"));
            EditorGUI.PropertyField(maxRect, maxProp, new GUIContent("Max"));
            EditorGUI.PropertyField(intRect, isIntProp, GUIContent.none);

            // 第三行: Category
            float y3 = y2 + EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
            var thirdLine = new Rect(position.x, y3, position.width, EditorGUIUtility.singleLineHeight);
            EditorGUI.PropertyField(thirdLine, categoryProp, new GUIContent("Category"));

            EditorGUI.EndProperty();
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return EditorGUIUtility.singleLineHeight * 3 + EditorGUIUtility.standardVerticalSpacing * 2;
        }
    }
}
