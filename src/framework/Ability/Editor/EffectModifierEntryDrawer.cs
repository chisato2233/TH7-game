using UnityEditor;
using UnityEngine;

namespace GameFramework.Editor
{
    /// <summary>
    /// EffectModifierEntry 的自定义 PropertyDrawer
    /// 单行显示: [Attribute] [Operation] [Value]
    /// </summary>
    [CustomPropertyDrawer(typeof(EffectModifierEntry))]
    public class EffectModifierEntryDrawer : PropertyDrawer
    {
        const float OperationWidth = 70f;
        const float ValueWidth = 60f;
        const float Spacing = 4f;

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            // 获取属性
            var attributeProp = property.FindPropertyRelative("Attribute");
            var operationProp = property.FindPropertyRelative("Operation");
            var valueProp = property.FindPropertyRelative("Value");

            // 计算布局
            float attributeWidth = position.width - OperationWidth - ValueWidth - Spacing * 2;

            var attributeRect = new Rect(position.x, position.y, attributeWidth, position.height);
            var operationRect = new Rect(position.x + attributeWidth + Spacing, position.y, OperationWidth, position.height);
            var valueRect = new Rect(position.x + attributeWidth + OperationWidth + Spacing * 2, position.y, ValueWidth, position.height);

            // 绘制字段
            EditorGUI.PropertyField(attributeRect, attributeProp, GUIContent.none);
            EditorGUI.PropertyField(operationRect, operationProp, GUIContent.none);
            EditorGUI.PropertyField(valueRect, valueProp, GUIContent.none);

            EditorGUI.EndProperty();
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return EditorGUIUtility.singleLineHeight;
        }
    }
}
