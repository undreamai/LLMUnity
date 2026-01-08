using UnityEngine;
using UnityEngine.EventSystems;
using System;
using System.Reflection;

/// <summary>
/// Automatically sets up the correct Input Module for the EventSystem based on which input system is active.
/// This is not needed in your project ,you can use your own (or default) EventSystem
/// </summary>
namespace LLMUnity
{
    [RequireComponent(typeof(EventSystem))]
    [DefaultExecutionOrder(-1000)]
    public class EventSystemAutoSetup : MonoBehaviour
    {
        void Awake()
        {
            SetupInputModule();
        }

        private void SetupInputModule()
        {
            var eventSystem = GetComponent<EventSystem>();

            // Try to find and use the new Input System module
            Type inputSystemModuleType = FindInputSystemModuleType();

            if (inputSystemModuleType != null)
            {
                // New Input System is available
                // Remove old module if present
                var oldModule = eventSystem.GetComponent<StandaloneInputModule>();
                if (oldModule != null) DestroyImmediate(oldModule);

                // Add new module if not present
                if (eventSystem.GetComponent(inputSystemModuleType) == null) eventSystem.gameObject.AddComponent(inputSystemModuleType);
            }
            else
            {
                // Legacy Input System only
                // Remove new module if present (shouldn't happen, but just in case)
                Type newModuleType = Type.GetType("UnityEngine.InputSystem.UI.InputSystemUIInputModule, Unity.InputSystem");
                if (newModuleType != null)
                {
                    var newModule = eventSystem.GetComponent(newModuleType);
                    if (newModule != null)
                    {
                        DestroyImmediate(newModule);
                    }
                }

                // Add legacy module if not present
                if (eventSystem.GetComponent<StandaloneInputModule>() == null) eventSystem.gameObject.AddComponent<StandaloneInputModule>();
            }
        }

        private Type FindInputSystemModuleType()
        {
            // Try to find InputSystemUIInputModule type
            Type type = Type.GetType("UnityEngine.InputSystem.UI.InputSystemUIInputModule, Unity.InputSystem");
            return type;
        }
    }
}
