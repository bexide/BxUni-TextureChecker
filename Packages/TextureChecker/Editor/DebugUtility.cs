// 2022-11-17 BeXide,Inc.
// by Y.Hayashi

using System;
using System.Collections;
using UnityEngine;
using UnityEditor;

namespace BX.TextureChecker
{
    public static class DebugUtility
    {
        /// <summary>
        /// SerializedProperty の内容を表示する（デバッグ用）
        /// </summary>
        /// <param name="prop"></param>
        public static IEnumerator DumpSerializedProperty(SerializedProperty prop)
        {
            switch (prop.propertyType)
            {
            case SerializedPropertyType.Generic:
                // 配列とObject
                Debug.Log($"{prop.depth}:{prop.propertyPath}:Generic");
#if true
                var child = prop.Copy();
                var end   = prop.GetEndProperty(true);
                if (child.Next(true))
                {
                    while (!SerializedProperty.EqualContents(child, end))
                    {
                        yield return DumpSerializedProperty(child);
                        if (!child.Next(false))
                            break;
                    }
                }
#else
                Debug.Log($" snip.");
                prop = prop.GetEndProperty(true);
                yield break;
#endif
                break;
            case SerializedPropertyType.Integer:
                Debug.Log($"{prop.depth}:{prop.propertyPath}:Int({prop.intValue})");
                break;
            case SerializedPropertyType.Boolean:
                Debug.Log($"{prop.depth}:{prop.propertyPath}:Bool({prop.boolValue})");
                break;
            case SerializedPropertyType.Float:
                Debug.Log($"{prop.depth}:{prop.propertyPath}:Float({prop.floatValue})");
                break;
            case SerializedPropertyType.String:
                Debug.Log($"{prop.depth}:{prop.propertyPath}:String({prop.stringValue})");
                break;
            case SerializedPropertyType.Color:
                Debug.Log($"{prop.depth}:{prop.propertyPath}:Color({prop.colorValue})");
                break;
            case SerializedPropertyType.ObjectReference:
                Debug.Log(
                    $"{prop.depth}:{prop.propertyPath}:Reference({prop.objectReferenceValue}),type={prop.type}");
#if true
                var child1 = prop.Copy();
                var end1   = prop.GetEndProperty(true);
                if (child1.Next(true))
                {
                    while (!SerializedProperty.EqualContents(child1, end1))
                    {
                        yield return DumpSerializedProperty(child1);
                        if (!child1.Next(false))
                            break;
                    }
                }
#endif
                break;
            case SerializedPropertyType.LayerMask:
                Debug.Log($"{prop.depth}:{prop.propertyPath}:LayerMask({prop.intValue})");
                break;
            case SerializedPropertyType.Enum:
                Debug.Log($"{prop.depth}:{prop.propertyPath}:Enum({prop.enumValueIndex})");
                break;
            case SerializedPropertyType.Vector2:
                Debug.Log($"{prop.depth}:{prop.propertyPath}:Vector2({prop.vector2Value})");
                break;
            case SerializedPropertyType.Vector3:
                Debug.Log($"{prop.depth}:{prop.propertyPath}:Vector3({prop.vector3Value})");
                break;
            case SerializedPropertyType.Rect:
                Debug.Log($"{prop.depth}:{prop.propertyPath}:Rect({prop.rectValue})");
                break;
            case SerializedPropertyType.ArraySize:
                Debug.Log($"{prop.depth}:{prop.propertyPath}:ArraySize({prop.intValue})");
                break;
            case SerializedPropertyType.Character:
                Debug.Log($"{prop.depth}:{prop.propertyPath}:Character");
                break;
            case SerializedPropertyType.AnimationCurve:
                Debug.Log($"{prop.depth}:{prop.propertyPath}:AnimationCurve");
                break;
            case SerializedPropertyType.Bounds:
                Debug.Log($"{prop.depth}:{prop.propertyPath}:Bounds({prop.boundsValue})");
                break;
            case SerializedPropertyType.Gradient:
                Debug.Log($"{prop.depth}:{prop.propertyPath}:Gradient");
                break;
            case SerializedPropertyType.Quaternion:
                Debug.Log($"{prop.depth}:{prop.propertyPath}:Quaternion({prop.quaternionValue})");
                break;
            default:
                Debug.Log($"{prop.depth}:{prop.propertyPath}:(other type={prop.propertyType})");
                break;

            }
        }
    }

}
