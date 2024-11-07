using UnityEngine;
using UnityEngine.Assertions;
using UnityEditor;
using UnityEditorInternal;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace VeryAnimation
{
    public class UAnimationWindowUtility_2023_1 : UAnimationWindowUtility
    {
        public UAnimationWindowUtility_2023_1()
        {
        }

        public override bool IsNodeLeftOverCurve(object state, object node)
        {
            return (bool)mi_IsNodeLeftOverCurve.Invoke(null, new object[] { state, node });
        }
    }
}
