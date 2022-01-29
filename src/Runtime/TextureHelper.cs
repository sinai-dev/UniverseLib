using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;

namespace UniverseLib.Runtime
{
    /// <summary>
    /// Helper class for working with <see cref="Texture2D"/>s, including some runtime-specific helpers and some general utility.
    /// </summary>
    public abstract class TextureHelper
    {
        public static TextureHelper Instance { get; private set; }

        public TextureHelper()
        {
            Instance = this;
        }

        /// <summary>
        /// Helper for invoking Unity's <see cref="ImageConversion.EncodeToPNG"/> method.
        /// </summary>
        public static byte[] EncodeToPNG(Texture2D tex) 
            => Instance.Internal_EncodeToPNG(tex);

        protected internal abstract byte[] Internal_EncodeToPNG(Texture2D tex);

        /// <summary>
        /// Helper for creating a new <see cref="Texture2D"/>.
        /// </summary>
        public static Texture2D NewTexture2D(int width, int height)
            => Instance.Internal_NewTexture2D(width, height);
        
        protected internal abstract Texture2D Internal_NewTexture2D(int width, int height);

        /// <summary>
        /// Helper for calling Unity's <see cref="Graphics.Blit"/> method.
        /// </summary>
        public static void Blit(Texture2D tex, RenderTexture rt)
            => Instance.Internal_Blit(tex, rt);

        protected internal abstract void Internal_Blit(Texture2D tex, RenderTexture rt);

        /// <summary>
        /// Helper for creating a <see cref="Sprite" /> from the provided <paramref name="texture"/>.
        /// </summary>
        public static Sprite CreateSprite(Texture2D texture)
            => Instance.Internal_CreateSprite(texture);
        
        protected internal abstract Sprite Internal_CreateSprite(Texture2D texture);

        /// <summary>
        /// Helper for creating a <see cref="Sprite" /> from the provided <paramref name="texture"/>.
        /// </summary>
        public static void CreateSprite(Texture2D texture, Rect rect, Vector2 pivot, float pixelsPerUnit, uint extrude, Vector4 border)
            => Instance.Internal_CreateSprite(texture, rect, pivot, pixelsPerUnit, extrude, border);
        
        protected internal abstract Sprite Internal_CreateSprite(Texture2D texture, Rect rect, Vector2 pivot, float pixelsPerUnit, uint extrude, Vector4 border);

        /// <summary>
        /// Helper for checking <see cref="Texture2D.isReadable"/>.
        /// </summary>
        public static bool IsReadable(Texture2D tex)
        {
            try
            {
                // This will cause an exception if it's not readable
                tex.GetPixel(0, 0);
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Creates a pixel copy of the provided <paramref name="orig"/>.
        /// </summary>
        public static Texture2D Copy(Texture2D orig) => Copy(orig, new(0, 0, orig.width, orig.height));

        /// <summary>
        /// Creates a pixel copy of the provided <paramref name="orig"/>, within the <paramref name="rect"/> dimensions.
        /// </summary>
        public static Texture2D Copy(Texture2D orig, Rect rect)
        {
            if (!IsReadable(orig))
                return ForceReadTexture(orig, rect);

            Texture2D newTex = Instance.Internal_NewTexture2D((int)rect.width, (int)rect.height);
            newTex.SetPixels(orig.GetPixels((int)rect.x, (int)rect.y, (int)rect.width, (int)rect.height));
            return newTex;
        }

        /// <summary>
        /// Copies the provided <paramref name="origTex"/> into a readable <see cref="Texture2D"/>.
        /// </summary>
        public static Texture2D ForceReadTexture(Texture2D origTex, Rect dimensions = default)
        {
            try
            {
                if (dimensions == default)
                    dimensions = new(0, 0, origTex.width, origTex.height);

                FilterMode origFilter = origTex.filterMode;
                RenderTexture origRenderTexture = RenderTexture.active;

                origTex.filterMode = FilterMode.Point;

                var rt = RenderTexture.GetTemporary(origTex.width, origTex.height, 0, RenderTextureFormat.ARGB32);
                rt.filterMode = FilterMode.Point;
                RenderTexture.active = rt;

                Instance.Internal_Blit(origTex, rt);

                var newTex = Instance.Internal_NewTexture2D((int)dimensions.width, (int)dimensions.height);
                newTex.ReadPixels(dimensions, 0, 0);
                newTex.Apply(false, false);

                RenderTexture.active = origRenderTexture;
                origTex.filterMode = origFilter;

                return newTex;
            }
            catch (Exception e)
            {
                Universe.Log($"Exception on ForceReadTexture: {e.ToString()}");
                return default;
            }
        }

        /// <summary>
        /// Saves the provided <paramref name="tex"/> as a PNG file, into the provided <paramref name="dir"/> as "<paramref name="name"/>.png"
        /// </summary>
        public static void SaveTextureAsPNG(Texture2D tex, string dir, string name)
        {
            if (tex.format != TextureFormat.ARGB32 || !IsReadable(tex))
                tex = ForceReadTexture(tex);

            byte[] data = Instance.Internal_EncodeToPNG(tex);

            if (data == null || !data.Any())
                throw new Exception("The data from calling EncodeToPNG on the provided Texture was null or empty.");

            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            File.WriteAllBytes(Path.Combine(dir, $"{name}.png"), data);
        }
    }
}
