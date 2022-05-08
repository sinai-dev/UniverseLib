using HarmonyLib;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
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
        /// Returns true if it is possible to force-read non-readable Cubemaps in this game.
        /// </summary>
        public static bool CanForceReadCubemaps => Instance.Internal_CanForceReadCubemaps;

        internal abstract bool Internal_CanForceReadCubemaps { get; }

        /// <summary>
        /// Helper for invoking Unity's <c>ImageConversion.EncodeToPNG</c> method.
        /// </summary>
        public static byte[] EncodeToPNG(Texture2D tex) 
            => Instance.Internal_EncodeToPNG(tex);

        protected internal abstract byte[] Internal_EncodeToPNG(Texture2D tex);

        /// <summary>
        /// Helper for creating a new <see cref="Texture2D"/>.
        /// </summary>
        public static Texture2D NewTexture2D(int width, int height)
            => Instance.Internal_NewTexture2D(width, height);

        /// <summary>
        /// Helper for creating a new <see cref="Texture2D"/>.
        /// </summary>
        public static Texture2D NewTexture2D(int width, int height, TextureFormat textureFormat, bool mipChain)
            => Instance.Internal_NewTexture2D(width, height, textureFormat, mipChain);
        
        protected internal abstract Texture2D Internal_NewTexture2D(int width, int height);
        protected internal abstract Texture2D Internal_NewTexture2D(int width, int height, TextureFormat textureFormat, bool mipChain);

        /// <summary>
        /// Helper for calling Unity's <see cref="Graphics.Blit"/> method.
        /// </summary>
        public static void Blit(Texture tex, RenderTexture rt)
            => Instance.Internal_Blit(tex, rt);

        protected internal abstract void Internal_Blit(Texture tex, RenderTexture rt);

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
        
        protected internal abstract Sprite Internal_CreateSprite(Texture2D texture, Rect rect, Vector2 pivot, float pixelsPerUnit, 
            uint extrude, Vector4 border);

        /// <summary>
        /// Helper for checking <c>Texture2D.isReadable</c>.
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
        /// Helper for checking <c>Cubemap.isReadable</c>.
        /// </summary>
        public static bool IsReadable(Cubemap tex)
        {
            try
            {
                // This will cause an exception if it's not readable
                tex.GetPixel(CubemapFace.PositiveX, 0, 0);
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Copies the provided <paramref name="source"/> into a readable <see cref="Texture2D"/>.
        /// <br/> Supports <see cref="Texture2D"/> and individual <see cref="Cubemap"/> faces.
        /// </summary>
        /// <param name="source">The original Texture to copy from.</param>
        /// <param name="dimensions">Optional dimensions to use from the original Texture. If set to default, uses the entire original.</param>
        /// <param name="cubemapFace">If copying a Cubemap, set this to the desired face index from 0 to 5.</param>
        /// <param name="dstX">Optional destination starting X value.</param>
        /// <param name="dstY">Optional destination starting Y value.</param>
        /// <returns>A new Texture2D, copied from the <paramref name="source"/>.</returns>
        public static Texture2D CopyTexture(Texture source, Rect dimensions = default, int cubemapFace = 0, int dstX = 0, int dstY = 0)
        {
            TextureFormat format = TextureFormat.ARGB32;
            if (source.TryCast<Texture2D>() is Texture2D tex2D)
                format = tex2D.format;
            else if (source.TryCast<Cubemap>() is Cubemap cmap)
                format = cmap.format;

            Texture2D newTex = NewTexture2D((int)dimensions.width, (int)dimensions.height, format, false);
            newTex.filterMode = FilterMode.Point;

            return CopyTexture(source, newTex, dimensions, cubemapFace, dstX, dstY);
        }

        /// <summary>
        /// Copies the provided <paramref name="source"/> into the <paramref name="destination"/> Texture.
        /// <br/><br/>Supports <see cref="Texture2D"/> and individual <see cref="Cubemap"/> faces.
        /// </summary>
        /// <param name="source">The original Texture to copy from.</param>
        /// <param name="destination">The destination Texture to copy into.</param>
        /// <param name="dimensions">Optional dimensions to use from the original Texture. If set to default, uses the entire original.</param>
        /// <param name="cubemapFace">If copying a Cubemap, set this to the desired face index from 0 to 5.</param>
        /// <param name="dstX">Optional destination starting X value.</param>
        /// <param name="dstY">Optional destination starting Y value.</param>
        /// <returns>The <paramref name="destination"/> Texture, copied from the <paramref name="source"/>.</returns>
        public static Texture2D CopyTexture(Texture source, Texture2D destination, 
            Rect dimensions = default, int cubemapFace = 0, int dstX = 0, int dstY = 0)
        {
            try
            {
                if (source.TryCast<Texture2D>() == null && source.TryCast<Cubemap>() == null)
                    throw new NotImplementedException($"TextureHelper.Copy does not support Textures of type '{source.GetActualType().Name}'.");

                if (dimensions == default)
                    dimensions = new(0, 0, source.width, source.height);

                if (source.TryCast<Texture2D>() is Texture2D texture2D)
                {
                    return CopyToARGB32(texture2D, dimensions, dstX, dstY);
                }
                else
                {
                    Instance.Internal_CopyTexture(source, cubemapFace, 0, 0, 0, source.width, source.height, destination, 0, 0, dstX, dstY);
                    return destination;
                }
            }
            catch (Exception e)
            {
                Universe.Log($"Failed to force-copy Texture: {e}");
                return default;
            }
        }

        internal abstract Texture Internal_CopyTexture(Texture src, int srcElement, int srcMip, int srcX, int srcY, 
            int srcWidth, int srcHeight, Texture dst, int dstElement, int dstMip, int dstX, int dstY);

        /// <summary>
        /// Unwraps the Cubemap into a Texture2D, showing the the X faces on the left, Y in the middle and Z on the right,
        /// with positive faces on the top row and negative on the bottom.
        /// </summary>
        public static Texture2D UnwrapCubemap(Cubemap cubemap)
        {
            if (!IsReadable(cubemap) && !Instance.Internal_CanForceReadCubemaps)
                throw new NotSupportedException("Unable to force-read non-readable Cubemaps in this game.");

            Texture2D newTex = NewTexture2D(cubemap.width * 3, cubemap.height * 2, cubemap.format, false);

            for (int i = 0; i < 6; i++)
            {
                int x = i % 3 * cubemap.width;
                int y = i % 2 * cubemap.height;

                // Using the Graphics.CopyTexture method is faster then SetPixels(GetPixels())
                if (Instance.Internal_CanForceReadCubemaps)
                    CopyTexture(cubemap, newTex, default, i, x, y);
                else
                    newTex.SetPixels(x, y, cubemap.width, cubemap.height, cubemap.GetPixels((CubemapFace)i));
            }

            return newTex;
        }

        /// <summary>
        /// Converts the <paramref name="origTex"/> into a readable <see cref="TextureFormat.ARGB32"/>-format <see cref="Texture2D"/>.
        /// <br /><br />Supports non-readable Textures.
        /// </summary>
        public static Texture2D CopyToARGB32(Texture2D origTex, Rect dimensions = default, int dstX = 0, int dstY = 0)
        {
            if (dimensions == default && origTex.format == TextureFormat.ARGB32 && IsReadable(origTex))
                return origTex;

            if (dimensions == default)
                dimensions = new(0, 0, origTex.width, origTex.height);

            FilterMode origFilter = origTex.filterMode;
            RenderTexture origRenderTexture = RenderTexture.active;

            origTex.filterMode = FilterMode.Point;

            RenderTexture rt = RenderTexture.GetTemporary(origTex.width, origTex.height, 0, RenderTextureFormat.ARGB32);
            rt.filterMode = FilterMode.Point;
            RenderTexture.active = rt;

            Instance.Internal_Blit(origTex, rt);

            Texture2D newTex = Instance.Internal_NewTexture2D((int)dimensions.width, (int)dimensions.height);
            newTex.ReadPixels(dimensions, dstX, dstY);
            newTex.Apply(false, false);

            RenderTexture.active = origRenderTexture;
            origTex.filterMode = origFilter;

            return newTex;
        }

        /// <summary>
        /// Saves the provided <paramref name="tex"/> as a PNG file, into the provided <paramref name="dir"/> as "<paramref name="name"/>.png".
        /// <br /><br />To save a <see cref="Cubemap"/>, use <see cref="Copy"/> for each face, 
        /// using the <c>cubemapFace</c> parameter to select the face.
        /// </summary>
        public static void SaveTextureAsPNG(Texture2D tex, string dir, string name)
            => SaveTextureAsPNG(tex, Path.Combine(dir, name));

        /// <summary>
        /// Saves the provided <paramref name="texture"/> as a PNG file to the provided path.
        /// <br /><br />To save a <see cref="Cubemap"/>, use <see cref="Copy"/> for each face, 
        /// using the <c>cubemapFace</c> parameter to select the face, and save each resulting Texture2D.
        /// </summary>
        public static void SaveTextureAsPNG(Texture2D texture, string fullPathWithFilename)
        {
            Texture2D tex = texture;

            if (!IsReadable(tex) || tex.format != TextureFormat.ARGB32)
                tex = CopyToARGB32(tex);

            byte[] data = Instance.Internal_EncodeToPNG(tex);

            if (data == null || !data.Any())
                throw new Exception("The data from calling EncodeToPNG on the provided Texture was null or empty.");

            string dir = Path.GetDirectoryName(fullPathWithFilename);
            string name = Path.GetFileNameWithoutExtension(fullPathWithFilename);

            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            File.WriteAllBytes(Path.Combine(dir, $"{name}.png"), data);

            if (tex != texture)
            {
                UnityEngine.Object.Destroy(tex);
            }
        }

        [Obsolete("Use TextureHelper.CopyTexture() instead. This method will be removed in a future version of UniverseLib.")]
        public static Texture2D ForceReadTexture(Texture2D origTex, Rect dimensions = default)
            => Copy(origTex, dimensions);

        [Obsolete("Use TextureHelper.CopyTexture() instead. This method will be removed in a future version of UniverseLib.")]
        public static Texture2D Copy(Texture2D orig) 
            => Copy(orig, new(0, 0, orig.width, orig.height));

        [Obsolete("Use TextureHelper.CopyTexture() instead. This method will be removed in a future version of UniverseLib.")]
        public static Texture2D Copy(Texture2D orig, Rect rect)
            => CopyTexture(orig, rect, 0, 0, 0);
    }
}
