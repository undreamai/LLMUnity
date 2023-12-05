using UnityEngine;
using UnityEditor;

[CustomPropertyDrawer(typeof(HideAttribute))]
public class ConditionalHidePropertyDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        HideAttribute condHAtt = (HideAttribute)attribute;
        bool enabled = GetConditionalHideAttributeResult(condHAtt, property);
 
        bool wasEnabled = GUI.enabled;
        GUI.enabled = enabled;
        if (!condHAtt.HideInInspector || enabled)
        {
            if (condHAtt.Header != ""){
                int addHeight = (int) EditorGUIUtility.singleLineHeight/2;
                EditorGUILayout.Space(addHeight);
                position.width = EditorGUIUtility.currentViewWidth;
                position.x = 0;
                position.y += addHeight;
                string labelText = label.text;
                EditorGUILayout.Space(EditorGUIUtility.singleLineHeight - 2);
                EditorGUI.LabelField(position, condHAtt.Header, EditorStyles.boldLabel);
                label.text = labelText;
                position.y += EditorGUIUtility.singleLineHeight;
            }
            EditorGUI.PropertyField(position, property, label, true);
        }
        GUI.enabled = wasEnabled;
    }
 
    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        HideAttribute condHAtt = (HideAttribute)attribute;
        bool enabled = GetConditionalHideAttributeResult(condHAtt, property);
 
        if (!condHAtt.HideInInspector || enabled)
        {
            return EditorGUI.GetPropertyHeight(property, label);
        }
        else
        {
            return -EditorGUIUtility.standardVerticalSpacing;
        }
    }
 
    private bool GetConditionalHideAttributeResult(HideAttribute condHAtt, SerializedProperty property)
    {
        bool enabled = true;
        string propertyPath = property.propertyPath; //returns the property path of the property we want to apply the attribute to
        string conditionPath = propertyPath.Replace(property.name, condHAtt.ConditionalSourceField); //changes the path to the conditionalsource property path
        SerializedProperty sourcePropertyValue = property.serializedObject.FindProperty(conditionPath);
 
        if (sourcePropertyValue != null)
        {
            enabled = !sourcePropertyValue.boolValue;
        }
        else
        {
            Debug.LogWarning("Attempting to use a HideAttribute but no matching SourcePropertyValue found in object: " + condHAtt.ConditionalSourceField);
        }
 
        return enabled;
    }
}