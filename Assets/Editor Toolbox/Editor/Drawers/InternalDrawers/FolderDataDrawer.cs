﻿using UnityEditor;
using UnityEngine;

namespace Toolbox.Editor.Drawers
{
    [CustomPropertyDrawer(typeof(FolderData))]
    internal class FolderDataDrawer : PropertyDrawer
    {
        private const string selectorEventName = "ObjectSelectorUpdated";

        private const int largeIconPickedId = 1001;
        private const int smallIconPickedId = 1002;


        private void DrawFolderByNameIcon(Rect rect)
        {
            GUI.DrawTexture(rect, Style.folderIcon);
        }

        private void DrawFolderByPathIcon(Rect rect)
        {
            const float doubleIconOffsetRatio = 1.5f;

            GUI.DrawTexture(rect, Style.folderIcon);

            rect.y += Style.spacing;
            rect.x += Style.spacing * doubleIconOffsetRatio;
            GUI.DrawTexture(rect, Style.folderIcon);
        }

        private void DrawLabelWarningIcon(Rect rect)
        {
            GUI.DrawTexture(rect, Style.folderIcon);
            GUI.DrawTexture(rect, Style.warningIcon);
        }


        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {            
            //if expanded - draw foldout + 3 properties + buttons strip + additional icon 
            if (property.isExpanded)
            {
                var height = Style.height;
                var typeProperty = property.FindPropertyRelative("type");
                var nameProperty = property.FindPropertyRelative("name");
                var pathProperty = property.FindPropertyRelative("path");

                //check if type is path-based or name-based
                if (typeProperty.intValue == 0)
                {
                    height += EditorGUI.GetPropertyHeight(pathProperty);
                }
                else
                {
                    height += EditorGUI.GetPropertyHeight(nameProperty);
                }

                height += Style.spacing * 4;
                height += Style.height;
                height += Style.height;
                height += Style.height;
                height += Style.largeFolderHeight;
                return height;
            }

            return Style.height;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            var typeProperty = property.FindPropertyRelative("type");
            var nameProperty = property.FindPropertyRelative("name");
            var pathProperty = property.FindPropertyRelative("path");
            var toolProperty = property.FindPropertyRelative("tooltip");

            var largeIconProperty = property.FindPropertyRelative("largeIcon");
            var smallIconProperty = property.FindPropertyRelative("smallIcon");

            var isPathBased = typeProperty.intValue == 0;
            var propertyName = isPathBased 
                ? pathProperty.stringValue 
                : nameProperty.stringValue;

            //begin property
            label = EditorGUI.BeginProperty(position, label, property);
            label.text = string.IsNullOrEmpty(propertyName) ? label.text : propertyName;

            var propertyPosition = new Rect(position.x, position.y, position.width, Style.height);

            if (!(property.isExpanded = EditorGUI.Foldout(propertyPosition, property.isExpanded, label, true, Style.folderLabelStyle)))
            {
                EditorGUI.EndProperty();

                //adjust position for small icon and draw it in label field
                var typeIconRect = new Rect(position.xMax - Style.smallFolderWidth,
#if UNITY_2019_3_OR_NEWER
                                            position.yMin, Style.smallFolderWidth, Style.smallFolderHeight);
#else
                                            position.yMin - Style.spacing, Style.smallFolderWidth, Style.smallFolderHeight);
#endif
                if (isPathBased)
                {
                    //NOTE: if path property is bigger than typical row height it means that associated value is invalid
                    var isValid = EditorGUI.GetPropertyHeight(pathProperty) <= Style.height;
                    if (isValid)
                    {
                        DrawFolderByPathIcon(typeIconRect);
                    }
                    else
                    {
                        DrawLabelWarningIcon(typeIconRect);
                    }
                }
                else
                {
                    DrawFolderByNameIcon(typeIconRect);
                }

                return;
            }

            EditorGUI.indentLevel++;

            var rawPropertyHeight = propertyPosition.height + Style.spacing;
            var sumPropertyHeight = rawPropertyHeight;

            propertyPosition.y += rawPropertyHeight;
            sumPropertyHeight += rawPropertyHeight;
            //draw folder data type property
            EditorGUI.PropertyField(propertyPosition, typeProperty, false);
            propertyPosition.y += rawPropertyHeight;
            sumPropertyHeight += rawPropertyHeight;

            //decide what property should be drawn depending on folder data type
            if (isPathBased)
            {
                propertyPosition.height = EditorGUI.GetPropertyHeight(pathProperty);
                EditorGUI.PropertyField(propertyPosition, pathProperty, false);
                propertyPosition.y += propertyPosition.height + Style.spacing;
                sumPropertyHeight += propertyPosition.height + Style.spacing;
                propertyPosition.height = Style.height;
            }
            else
            {
                EditorGUI.PropertyField(propertyPosition, nameProperty, false);
                propertyPosition.y += rawPropertyHeight;
                sumPropertyHeight += rawPropertyHeight;
            }

            //draw tooltip property
            EditorGUI.PropertyField(propertyPosition, toolProperty, false);
            propertyPosition.y += rawPropertyHeight;
            sumPropertyHeight += rawPropertyHeight;

            //adjust rect for folder icons + button strip
            propertyPosition.y += Style.spacing;
            propertyPosition.x += Style.iconsPadding;
            propertyPosition.width = Style.largeFolderWidth;

            //draw normal icon property picker
            if (GUI.Button(propertyPosition, Style.largeIconPickerContent, Style.largeIconPickerStyle))
            {
                var propertyHash = property.propertyPath.GetHashCode() + property.serializedObject.GetHashCode();
                EditorGUIUtility.ShowObjectPicker<Texture>(largeIconProperty.objectReferenceValue, false, null, propertyHash + largeIconPickedId);
            }

            propertyPosition.x += Style.largeFolderWidth;
            propertyPosition.width = Style.smallFolderWidth;

            //draw small icon property picker
            if (GUI.Button(propertyPosition, Style.smallIconPickerContent, Style.smallIconPickerStyle))
            {
                var propertyHash = property.propertyPath.GetHashCode() + property.serializedObject.GetHashCode();
                EditorGUIUtility.ShowObjectPicker<Texture>(smallIconProperty.objectReferenceValue, false, null, propertyHash + smallIconPickedId);
            }

            //catch object selection event and assign it to proper property
            if (Event.current.commandName == selectorEventName)
            {
                //get proper action id by removing unique property hash code
                var rawPickId = EditorGUIUtility.GetObjectPickerControlID();
                var controlId = rawPickId - property.propertyPath.GetHashCode() - property.serializedObject.GetHashCode();
                switch (controlId)
                {
                    case largeIconPickedId:
                        largeIconProperty.objectReferenceValue = EditorGUIUtility.GetObjectPickerObject();
                        largeIconProperty.serializedObject.ApplyModifiedProperties();
                        break;
                    case smallIconPickedId:
                        smallIconProperty.objectReferenceValue = EditorGUIUtility.GetObjectPickerObject();
                        smallIconProperty.serializedObject.ApplyModifiedProperties();
                        break;
                }                
            }

            position.x += Style.iconsPadding;

            //adjust rect for each icon
            var largeFolderIconRect = new Rect(position.x, position.yMin + sumPropertyHeight, Style.largeFolderWidth, Style.largeFolderHeight);
            var smallFolderIconRect = new Rect(position.x + Style.largeFolderWidth, 
                         largeFolderIconRect.y + Style.smallFolderHeight / 2 - Style.spacing, Style.smallFolderWidth, Style.smallFolderHeight);

            //draw default folder icons
            GUI.DrawTexture(largeFolderIconRect, Style.folderIcon, ScaleMode.ScaleToFit);
            GUI.DrawTexture(smallFolderIconRect, Style.folderIcon, ScaleMode.ScaleToFit);

            if (largeIconProperty.objectReferenceValue)
            {
                var previewTexture = largeIconProperty.objectReferenceValue as Texture;
                GUI.DrawTexture(ToolboxEditorProject.GetLargeIconRect(largeFolderIconRect), previewTexture, ScaleMode.ScaleToFit);
            }

            if (smallIconProperty.objectReferenceValue)
            {
                var previewTexture = smallIconProperty.objectReferenceValue as Texture;
                GUI.DrawTexture(ToolboxEditorProject.GetSmallIconRect(smallFolderIconRect), previewTexture, ScaleMode.ScaleToFit);
            }

            //end property and fix indent level
            EditorGUI.indentLevel--;
            EditorGUI.EndProperty();
        }


        private static class Style
        {
            internal static readonly float height = EditorGUIUtility.singleLineHeight;
            internal static readonly float spacing = EditorGUIUtility.standardVerticalSpacing;

            internal const float iconsPadding = 12.0f;

            internal const float largeFolderWidth = ToolboxEditorProject.Style.maxFolderWidth;
            internal const float largeFolderHeight = ToolboxEditorProject.Style.maxFolderHeight;

            internal const float smallFolderWidth = ToolboxEditorProject.Style.minFolderWidth;
            internal const float smallFolderHeight = ToolboxEditorProject.Style.minFolderHeight;

            internal static readonly Texture2D folderIcon;
            internal static readonly Texture2D warningIcon;

            internal static readonly GUIStyle folderLabelStyle;
            internal static readonly GUIStyle largeIconPickerStyle;
            internal static readonly GUIStyle smallIconPickerStyle;

            internal static readonly GUIContent largeIconPickerContent;
            internal static readonly GUIContent smallIconPickerContent;

            static Style()
            {
                folderIcon = EditorGUIUtility.FindTexture("Folder Icon");
                warningIcon = EditorGUIUtility.FindTexture("console.warnicon.sml");

                folderLabelStyle = new GUIStyle(EditorStyles.foldout)
                {
                    padding = new RectOffset(17, 2, 0, 0),                                                         
                };
                
                largeIconPickerStyle = new GUIStyle(EditorStyles.miniButtonLeft)
                {
                    fixedHeight = 16.0f,
                    fontSize = 9
                };
                smallIconPickerStyle = new GUIStyle(EditorStyles.miniButtonRight)
                {
                    fixedHeight = 16.0f,
                    padding = new RectOffset(2, 2, 2, 2)
                };
  
                largeIconPickerContent = new GUIContent("Select");
                smallIconPickerContent = EditorGUIUtility.IconContent("RawImage Icon");
                largeIconPickerContent.tooltip = "Select large icon";
                smallIconPickerContent.tooltip = "Select small icon";
            }
        }
    }
}