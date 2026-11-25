using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using MCPForUnity.Editor.Services;
using MCPForUnity.Editor.Services.Transport;

namespace Aiara.EditorTools
{
    [InitializeOnLoad]
    public static class MCPForceConnect
    {
        private static double _lastCheckTime = 0.0;
        private static bool _isConnecting = false;

        static MCPForceConnect()
        {
            ApplyNoThrottling();
            EditorApplication.update += OnEditorUpdate;
            EditorApplication.delayCall += () =>
            {
                ConnectHttp();
            };
        }

        private static void ApplyNoThrottling()
        {
            try
            {
                EditorPrefs.SetInt("ApplicationIdleTime", 0);
                EditorPrefs.SetInt("InteractionMode", 1);
                var method = typeof(EditorApplication).GetMethod(
                    "UpdateInteractionModeSettings",
                    BindingFlags.Static | BindingFlags.NonPublic
                );
                method?.Invoke(null, null);
                Application.runInBackground = true;
            }
            catch { }
        }

        private static void OnEditorUpdate()
        {
            if (EditorApplication.timeSinceStartup - _lastCheckTime < 2.5) return;
            _lastCheckTime = EditorApplication.timeSinceStartup;

            ApplyNoThrottling();

            if (_isConnecting) return;
            if (EditorApplication.isCompiling || EditorApplication.isUpdating) return;

            if (MCPServiceLocator.TransportManager != null && !MCPServiceLocator.TransportManager.IsRunning(TransportMode.Http))
            {
                ConnectHttp();
            }
        }

        private static async void ConnectHttp()
        {
            if (_isConnecting) return;
            _isConnecting = true;
            try
            {
                if (MCPServiceLocator.TransportManager != null && !MCPServiceLocator.TransportManager.IsRunning(TransportMode.Http))
                {
                    Debug.Log("[MCPForceConnect] Reconnecting MCP Http Transport...");
                    await MCPServiceLocator.TransportManager.StartAsync(TransportMode.Http);
                    Debug.Log("[MCPForceConnect] Reconnection finished. IsRunning: " + MCPServiceLocator.TransportManager.IsRunning(TransportMode.Http));
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"[MCPForceConnect] Error: {ex.Message}");
            }
            finally
            {
                _isConnecting = false;
            }
        }

        [MenuItem("Tools/MCP/Force Connect HTTP")]
        public static void ForceConnect()
        {
            ApplyNoThrottling();
            ConnectHttp();
        }
    }
}
