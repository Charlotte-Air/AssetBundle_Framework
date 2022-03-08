using System;
using System.Linq;
using UnityEditor;
using UnityEngine;
using System.Collections.Generic;

namespace Builder
{

    public static class AppExtension
    {
        public static Rect GetEditorMainWindowPos()
        {
            var containerWinType = ReflectionUtility.GetAllChildClasses(typeof(ScriptableObject), true).Where(t => t.Name == "ContainerWindow").FirstOrDefault();
            if (containerWinType == null)
                throw new System.MissingMemberException("Can't find internal type ContainerWindow. Maybe something has changed inside Unity");
            var showModeField = containerWinType.GetField("m_ShowMode", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var positionProperty = containerWinType.GetProperty("position", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
            if (showModeField == null || positionProperty == null)
                throw new System.MissingFieldException("Can't find internal fields 'm_ShowMode' or 'position'. Maybe something has changed inside Unity");
            var windows = Resources.FindObjectsOfTypeAll(containerWinType);
            foreach (var win in windows)
            {
                var showmode = (int)showModeField.GetValue(win);
                if (showmode == 4) // main window
                {
                    var pos = (Rect)positionProperty.GetValue(win, null);
                    return pos;
                }
            }
            throw new System.NotSupportedException("Can't find internal main window. Maybe something has changed inside Unity");
        }
    }

    public static class EditorWindowExtension
    {
        public static void CenterOnMainWin(this EditorWindow win, Vector2 offset)
        {
            var main = AppExtension.GetEditorMainWindowPos();
            var pos = win.position;
            float w = (main.width - pos.width) * 0.5f + offset.x;
            float h = (main.height - pos.height) * 0.5f + offset.y;
            pos.x = main.x + w;
            pos.y = main.y + h;
            win.position = pos;
        }
    }

    public static class ReflectionUtility
    {
        /// <summary>
        /// Alternative version of <see cref="Type.IsSubclassOf"/> that supports raw generic types (generic types without
        /// any type parameters).
        /// </summary>
        /// <param name="baseType">The base type class for which the check is made.</param>
        /// <param name="toCheck">To type to determine for whether it derives from <paramref name="baseType"/>.</param>
        public static bool IsSubclassOfRawGeneric(this Type toCheck, Type baseType)
        {
            while (toCheck != typeof(object))
            {
                Type cur = toCheck.IsGenericType ? toCheck.GetGenericTypeDefinition() : toCheck;
                if (baseType == cur)
                {
                    return true;
                }

                toCheck = toCheck.BaseType;
            }

            return false;
        }

        public static Type[] GetAllChildClasses(Type baseType, bool allowInvisible = false, bool allowAbstract = false)
        {
            var types = new List<Type>();
            var assemblies = AppDomain.CurrentDomain.GetAssemblies();
            var isSubclass = baseType.IsGenericType
                ? (Func<Type, bool>)
                    ((type) => IsSubclassOfRawGeneric(type, baseType))
                : ((type) => type.IsSubclassOf(baseType));

            foreach (var assembly in assemblies)
            {
                foreach (var type in assembly.GetTypes())
                {
                    if (!isSubclass(type) || (!type.IsVisible && !allowInvisible) || (!allowAbstract && type.IsAbstract))
                    {
                        continue;
                    }

                    types.Add(type);
                }
            }

            return types.ToArray();
        }
    }

}
