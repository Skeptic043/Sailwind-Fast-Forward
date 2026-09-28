using System;
using System.Reflection;
using UnityEngine;
using UnityEngine.Rendering;

namespace SailwindFastForward
{
    // All native assets are borrowed read-only. Only the cached render texture
    // belongs to this indicator. Cosmetic failures never affect speed ownership.
    internal sealed class SpeedIndicator : IDisposable
    {
        private readonly Action<string> warning;
        private Font font;
        private FontStyle fontStyle;
        private Color ink = Color.black;
        private string multiplier = "\u00d7";
        private RenderTexture scroll;
        private bool scrollAttempted;
        private bool warned;
        private float nextLookup;
        private GUIStyle boxStyle;
        private GUIStyle textStyle;
        private float styleScale;

        internal SpeedIndicator(Action<string> warning) { this.warning = warning; }

        internal void Update(bool canPrepareScroll)
        {
            try
            {
                if (!ReferenceEquals(scroll, null) && (!scroll || !scroll.IsCreated()))
                {
                    if (scroll) { scroll.Release(); UnityEngine.Object.Destroy(scroll); }
                    scroll = null;
                    scrollAttempted = false;
                }
                if (font && (!canPrepareScroll || scroll && scroll.IsCreated() || scrollAttempted)) return;
                if (Time.unscaledTime < nextLookup) return;
                nextLookup = Time.unscaledTime + 2f;
                // The native menu exists even when its child panels are hidden.
                var menu = UnityEngine.Object.FindObjectOfType<StartMenu>();
                if (!menu) return;
                var field = typeof(StartMenu).GetField("chooseIslandUI", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                var root = (field?.GetValue(menu) as GameObject)?.transform;
                if (!root) { Warn("Sailwind's menu templates were not found. Using the original display style."); return; }
                var text = root.Find("button confirm/text")?.GetComponent<TextMesh>();
                if (text && text.font)
                {
                    font = text.font;
                    fontStyle = FontStyle.Normal;
                    ink = text.color;
                    ink.a = 1f;
                    multiplier = font.HasCharacter('\u00d7') ? "\u00d7" : "x";
                    boxStyle = null;
                }
                // Camera rendering and its validation readback can stall a frame.
                // Prepare in a menu/loading window, never when a player selects
                // the background during normal gameplay.
                if (canPrepareScroll && !scroll && !scrollAttempted)
                {
                    var donor = root.Find("bg");
                    if (!donor) return;
                    scrollAttempted = true;
                    scroll = RenderScroll(donor);
                }
            }
            catch (Exception error) { Warn("Native indicator styling unavailable. Using the original display style. " + error.Message); }
        }

        internal void Draw(float speed, float scale, IndicatorBackground background)
        {
            try
            {
                if (boxStyle == null || styleScale != scale)
                {
                    styleScale = scale;
                    boxStyle = new GUIStyle(GUI.skin.box)
                    {
                        font = font ? font : GUI.skin.box.font,
                        fontStyle = font ? fontStyle : GUI.skin.box.fontStyle,
                        fontSize = Mathf.RoundToInt(16f * scale),
                        alignment = TextAnchor.MiddleCenter,
                        // Border is a source-texture pixel slice, not a screen
                        // measurement. Keep the inherited slice at every scale.
                        padding = Scale(GUI.skin.box.padding, scale)
                    };
                    textStyle = new GUIStyle(GUI.skin.label)
                    {
                        font = boxStyle.font, fontStyle = boxStyle.fontStyle,
                        fontSize = boxStyle.fontSize, alignment = boxStyle.alignment,
                        padding = boxStyle.padding
                    };
                }
                var rect = new Rect(Screen.width - 16 - 60 * scale, 16, 60 * scale, 28 * scale);
                string label = speed + " " + multiplier;
                if (background == IndicatorBackground.None)
                {
                    textStyle.normal.textColor = boxStyle.normal.textColor;
                    GUI.Label(rect, label, textStyle);
                }
                else if (background == IndicatorBackground.Scroll && scroll && scroll.IsCreated())
                {
                    GUI.DrawTexture(rect, scroll, ScaleMode.StretchToFill, true);
                    textStyle.normal.textColor = ink;
                    GUI.Label(rect, label, textStyle);
                }
                else GUI.Box(rect, label, boxStyle);
            }
            catch (Exception error) { Warn("Speed indicator could not be drawn. " + error.Message); }
        }

        private static RectOffset Scale(RectOffset source, float scale) => new RectOffset(
            Mathf.RoundToInt(source.left * scale), Mathf.RoundToInt(source.right * scale),
            Mathf.RoundToInt(source.top * scale), Mathf.RoundToInt(source.bottom * scale));

        private static RenderTexture RenderScroll(Transform donor)
        {
            var source = donor.GetComponent<MeshFilter>()?.sharedMesh;
            var nativeMaterial = donor.GetComponent<MeshRenderer>()?.sharedMaterial;
            if (!source || !nativeMaterial || !nativeMaterial.mainTexture || !nativeMaterial.shader ||
                nativeMaterial.shader.name != "Legacy Shaders/VertexLit" || !nativeMaterial.shader.isSupported ||
                !nativeMaterial.HasProperty("_Emission") || !nativeMaterial.HasProperty("_SpecColor"))
                throw new InvalidOperationException("Native scroll mesh, texture or expected shader was not available.");

            Material material = null;
            RenderTexture result = null;
            GameObject cameraObject = null;
            CommandBuffer commands = null;
            Texture2D sample = null;
            var previousTarget = RenderTexture.active;
            try
            {
                var rotation = donor.localRotation;
                // The installed mesh is not CPU-readable. Borrow its GPU data
                // and obtain the projected size from its eight bounds corners.
                var bounds = new Bounds(rotation * source.bounds.center, Vector3.zero);
                var sourceBounds = source.bounds;
                for (int corner = 0; corner < 8; corner++)
                {
                    var point = sourceBounds.center + Vector3.Scale(sourceBounds.extents, new Vector3(
                        (corner & 1) == 0 ? -1 : 1, (corner & 2) == 0 ? -1 : 1, (corner & 4) == 0 ? -1 : 1));
                    bounds.Encapsulate(rotation * point);
                }
                // Verified native pass 0 outputs texture * vertex lighting * 2
                // plus specular. Black diffuse/specular and half-tint emission
                // keep the scroll readable without changing game lights/fog.
                material = new Material(nativeMaterial) { color = Color.black, shaderKeywords = new string[0] };
                material.SetColor("_SpecColor", Color.black);
                material.SetColor("_Emission", nativeMaterial.color * 0.5f);
                result = new RenderTexture(600, 280, 24, RenderTextureFormat.ARGB32)
                {
                    name = "Fast Forward scroll indicator", hideFlags = HideFlags.HideAndDontSave,
                    filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp
                };
                if (!result.Create()) throw new InvalidOperationException("Indicator render texture could not be created.");
                float halfHeight = Mathf.Max(bounds.extents.y, bounds.extents.x * 28f / 60f) * 1.01f;
                // A real, one-shot camera supplies the render lifecycle and GPU
                // projection/depth convention. It renders no scene objects.
                cameraObject = new GameObject("Fast Forward scroll cache") { hideFlags = HideFlags.HideAndDontSave };
                var camera = cameraObject.AddComponent<Camera>();
                camera.enabled = false;
                camera.cullingMask = 0;
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = Color.clear;
                camera.orthographic = true;
                camera.orthographicSize = halfHeight;
                camera.aspect = 60f / 28f;
                camera.nearClipPlane = 0.01f;
                camera.farClipPlane = 20f;
                camera.transform.position = new Vector3(0, 0, -10);
                camera.targetTexture = result;
                camera.allowHDR = false;
                camera.allowMSAA = false;
                commands = new CommandBuffer { name = "Fast Forward native scroll" };
                commands.DrawMesh(source, Matrix4x4.TRS(-bounds.center, rotation, Vector3.one), material, 0, 0);
                camera.AddCommandBuffer(CameraEvent.AfterEverything, commands);
                camera.Render();
                // A created RT can still be transparent. Do not cache that as a
                // successful background and leave dark text floating by itself.
                RenderTexture.active = result;
                sample = new Texture2D(1, 1, TextureFormat.RGBA32, false);
                sample.ReadPixels(new Rect(result.width / 2, result.height / 2, 1, 1), 0, 0);
                sample.Apply();
                if (sample.GetPixel(0, 0).a < 0.01f)
                    throw new InvalidOperationException("Native scroll render was empty.");
                return result;
            }
            catch
            {
                if (result) { result.Release(); UnityEngine.Object.Destroy(result); }
                throw;
            }
            finally
            {
                RenderTexture.active = previousTarget;
                if (cameraObject)
                {
                    var camera = cameraObject.GetComponent<Camera>();
                    camera.targetTexture = null;
                    camera.RemoveAllCommandBuffers();
                    UnityEngine.Object.Destroy(cameraObject);
                }
                commands?.Release();
                if (sample) UnityEngine.Object.Destroy(sample);
                if (material) UnityEngine.Object.Destroy(material);
            }
        }

        private void Warn(string message)
        {
            if (warned) return;
            warned = true;
            warning(message);
        }

        internal void Invalidate()
        {
            // Borrowed scene assets need refreshing. The owned rendered pixels
            // remain valid across scenes and do not need another GPU readback.
            font = null;
            boxStyle = null;
            scrollAttempted = false;
            nextLookup = 0;
        }

        public void Dispose()
        {
            if (scroll) { scroll.Release(); UnityEngine.Object.Destroy(scroll); }
            scroll = null;
            Invalidate();
        }
    }
}
