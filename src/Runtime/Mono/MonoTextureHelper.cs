#if MONO
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;
using UniverseLib;
using UniverseLib.Utility;

namespace UniverseLib.Runtime.Mono
{
    internal class MonoTextureHelper : TextureHelper
    {
        private static MethodInfo EncodeToPNGMethod => encodeToPNGMethod ?? GetEncodeToPNGMethod();
        private static MethodInfo encodeToPNGMethod;

        /// <inheritdoc/>
        protected internal override void Internal_Blit(Texture2D tex, RenderTexture rt)
            => Graphics.Blit(tex, rt);

        /// <inheritdoc/>
        protected internal override Sprite Internal_CreateSprite(Texture2D texture)
            => Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), Vector2.zero);

        /// <inheritdoc/>
        protected internal override Sprite Internal_CreateSprite(Texture2D texture, Rect rect, Vector2 pivot, float pixelsPerUnit, uint extrude, Vector4 border)
             => Sprite.Create(texture, rect, pivot, pixelsPerUnit, extrude, SpriteMeshType.Tight, border);

        /// <inheritdoc/>
        protected internal override Texture2D Internal_NewTexture2D(int width, int height)
            => new(width, height);

        /// <inheritdoc/>
        protected internal override byte[] Internal_EncodeToPNG(Texture2D tex)
            => EncodeToPNGSafe(tex);

        private static byte[] EncodeToPNGSafe(Texture2D tex)
        {
            if (EncodeToPNGMethod == null)
                throw new MissingMethodException("Could not find any Texture2D EncodeToPNG method!");

            return EncodeToPNGMethod.IsStatic
                    ? (byte[])EncodeToPNGMethod.Invoke(null, new object[] { tex })
                    : (byte[])EncodeToPNGMethod.Invoke(tex, ArgumentUtility.EmptyArgs);
        }

        private static MethodInfo GetEncodeToPNGMethod()
        {
            if (ReflectionUtility.GetTypeByName("UnityEngine.ImageConversion") is Type imageConversion)
                return encodeToPNGMethod = imageConversion.GetMethod("EncodeToPNG", ReflectionUtility.FLAGS);

            return typeof(Texture2D).GetMethod("EncodeToPNG", ReflectionUtility.FLAGS);
        }
    }
}
#endif