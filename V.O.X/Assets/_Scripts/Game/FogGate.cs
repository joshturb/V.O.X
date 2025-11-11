using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

[ExecuteAlways]
public class FogGate : MonoBehaviour
{
    [Header("Transition Settings")]
    public float transitionDuration = 5f;
    public float transitionLevel = 25f; // Y level to determine top vs bottom fog
    public float updateInterval = 0.1f; // How often to update fog (in seconds)

    [Header("Fog Presets")]
    public FogRenderSettings topFogRenderSettings;
    public FogRenderSettings bottomFogRenderSettings;

    private FogRenderSettings currentTarget;
    private float timeSinceLastUpdate = 0f;

#if UNITY_EDITOR
    void OnEnable()
    {
        // Subscribe to editor updates when not playing
        if (!Application.isPlaying)
            EditorApplication.update += EditorUpdate;
    }

    void OnDisable()
    {
        if (!Application.isPlaying)
            EditorApplication.update -= EditorUpdate;
    }

    private void EditorUpdate()
    {
        if (SceneView.lastActiveSceneView != null)
        {
            Camera sceneCam = SceneView.lastActiveSceneView.camera;
            UpdateFog(sceneCam, false);
            SceneView.lastActiveSceneView.Repaint(); // force refresh scene view
        }
    }
#endif

    void Update()
    {
        if (Application.isPlaying)
        {
            // Runtime -> Use main camera
            UpdateFog(Camera.main, true);
        }
    }

    private void UpdateFog(Camera cam, bool useDeltaTime)
    {
        if (cam == null) return;

        timeSinceLastUpdate += useDeltaTime ? Time.deltaTime : updateInterval;

        if (timeSinceLastUpdate >= updateInterval)
        {
            // Decide which target we are blending toward
            currentTarget = cam.transform.position.y >= transitionLevel
                ? topFogRenderSettings
                : bottomFogRenderSettings;

            float t = timeSinceLastUpdate / transitionDuration;

            // Lerp values on each interval
            RenderSettings.fog = currentTarget.enableFog;
            RenderSettings.fogMode = currentTarget.fogMode;
            RenderSettings.fogColor = Color.Lerp(RenderSettings.fogColor, currentTarget.fogColor, t);
            RenderSettings.fogDensity = Mathf.Lerp(RenderSettings.fogDensity, currentTarget.fogDensity, t);
            RenderSettings.fogStartDistance = Mathf.Lerp(RenderSettings.fogStartDistance, currentTarget.linearStart, t);
            RenderSettings.fogEndDistance = Mathf.Lerp(RenderSettings.fogEndDistance, currentTarget.linearEnd, t);

            DynamicGI.UpdateEnvironment();

            timeSinceLastUpdate = 0f;
        }
    }
}
