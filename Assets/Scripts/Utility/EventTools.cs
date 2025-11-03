using System.Linq;
using UnityEngine;
using UnityEngine.Events;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace DelaysExpected.RuntimeUtilities
{
    public static class EventTools
    {
        //Base Overload without generic parameters.
        /// <summary>
        /// Usage: check a UnityEvent's subscribed methods.
        /// </summary>
        /// <param name="unityEvent"></param>
        /// <param name="action"></param>
        /// <returns>True if the provided action is subscribed to the provided UnityEvent.</returns>
        public static bool IsMethodAlreadySubscribed(UnityEvent unityEvent, UnityAction action)
        {
            if (unityEvent == null)
                return false;

            for (int i = 0; i < unityEvent.GetPersistentEventCount(); i++)
            {
                if (unityEvent.GetPersistentMethodName(i) == action.Method.Name)
                {
                    return true;
                }
            }
            return false;
        }

        // Generic Overload containing generic parameters.
        /// <summary>
        /// Usage: check a UnityEvent<T>'s subscribed methods.
        /// </summary>
        /// <param name="unityEvent"></param>
        /// <param name="action"></param>
        /// <returns>True if the provided action is subscribed to the provided UnityEvent<T>.</returns>
        public static bool IsMethodAlreadySubscribed<T>(UnityEventBase unityEvent,
            params UnityAction<T>[] actions)
        {
            if (unityEvent == null || actions == null || actions.Length == 0)
                return false;

            for (int i = 0; i < unityEvent.GetPersistentEventCount(); i++)
            {
                string methodName = unityEvent.GetPersistentMethodName(i);

                // Check each action passed in the parameters
                if (actions.Any(action => action.Method.Name == methodName))
                {
                    return true;
                }
            }
            return false;
        }
        // Use Reflection to attain Unity's internal icons for the scene view
        public static void AddSceneViewIcon(GameObject gameObject, string iconName)
        {
#if UNITY_EDITOR
            var editorGuiUtilityType = typeof(EditorGUIUtility);
            var setIconMethod = editorGuiUtilityType.GetMethod("SetIconForObject", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public);
            var icon = EditorGUIUtility.IconContent(iconName).image as Texture2D;
            setIconMethod?.Invoke(null, new object[] { gameObject, icon });
#endif
        }

    }
}
