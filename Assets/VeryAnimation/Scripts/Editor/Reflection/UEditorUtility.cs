using UnityEngine;
using UnityEngine.Assertions;
using UnityEditor;
using UnityEditorInternal;
using System;
using System.Reflection;

namespace VeryAnimation
{
    public class UEditorUtility
    {
        private Func<UnityEngine.Object, GameObject> dg_InstantiateForAnimatorPreview;

        public UEditorUtility()
        {
            var asmUnityEditor = Assembly.LoadFrom(InternalEditorUtility.GetEditorAssemblyPath());

            var editorUtilityType = asmUnityEditor.GetType("UnityEditor.EditorUtility");
            Assert.IsNotNull(dg_InstantiateForAnimatorPreview = (Func<UnityEngine.Object, GameObject>)Delegate.CreateDelegate(typeof(Func<UnityEngine.Object, GameObject>), null, editorUtilityType.GetMethod("InstantiateForAnimatorPreview", BindingFlags.NonPublic | BindingFlags.Static)));
        }

        public GameObject InstantiateForAnimatorPreview(UnityEngine.Object o)
        {
            return dg_InstantiateForAnimatorPreview(o);
        }
    }
}
