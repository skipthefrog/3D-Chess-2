#if UNITY_WEBGL && !UNITY_EDITOR
using UnityEngine;
using UnityEngine.Profiling;

namespace ChessWebGL
{
    /// <summary>
    /// Monitors and manages memory usage in WebGL builds to prevent browser crashes
    /// </summary>
    public class WebGLMemoryMonitor : MonoBehaviour
    {
        [Header("Memory Thresholds (MB)")]
        [SerializeField] private float warningThreshold = 400f;
        [SerializeField] private float criticalThreshold = 450f;

        [Header("Monitoring")]
        [SerializeField] private float checkInterval = 2f;
        [SerializeField] private bool logMemoryUsage = true;

        private float nextCheckTime;
        private bool hasWarned = false;

        private void Update()
        {
            if (Time.time < nextCheckTime) return;
            nextCheckTime = Time.time + checkInterval;

            CheckMemoryUsage();
        }

        private void CheckMemoryUsage()
        {
            long memoryBytes = Profiler.GetTotalAllocatedMemoryLong();
            float memoryMB = memoryBytes / (1024f * 1024f);

            if (logMemoryUsage)
            {
                Debug.Log($"📊 WebGL Memory Usage: {memoryMB:F1} MB");
            }

            // Critical threshold - aggressive cleanup
            if (memoryMB > criticalThreshold)
            {
                Debug.LogWarning($"⚠️ CRITICAL: WebGL memory usage at {memoryMB:F1} MB - performing aggressive cleanup!");
                PerformAggressiveCleanup();
                hasWarned = false; // Reset for next warning
            }
            // Warning threshold - normal cleanup
            else if (memoryMB > warningThreshold)
            {
                if (!hasWarned)
                {
                    Debug.LogWarning($"⚠️ WARNING: High WebGL memory usage at {memoryMB:F1} MB - cleaning up");
                    PerformCleanup();
                    hasWarned = true;
                }
            }
            else
            {
                hasWarned = false;
            }
        }

        private void PerformCleanup()
        {
            // Standard cleanup
            Resources.UnloadUnusedAssets();
            System.GC.Collect();
        }

        private void PerformAggressiveCleanup()
        {
            // More aggressive cleanup
            Resources.UnloadUnusedAssets();
            System.GC.Collect();
            System.GC.WaitForPendingFinalizers();
            System.GC.Collect();
        }

        private void OnApplicationFocus(bool hasFocus)
        {
            // Clean up when tab loses focus
            if (!hasFocus)
            {
                Debug.Log("🧹 WebGL: Tab lost focus - cleaning up resources");
                PerformCleanup();
            }
        }
    }
}
#endif
