#if MONO
using HarmonyLib;
using System;
using System.Collections;
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
        static MethodInfo mi_EncodeToPNG;
        static MethodInfo mi_Graphics_CopyTexture;

        internal override bool Internal_CanForceReadCubemaps => mi_Graphics_CopyTexture != null;

        internal MonoTextureHelper() : base()
        {
            RuntimeHelper.StartCoroutine(InitCoro());
        }

        static IEnumerator InitCoro()
        {
            while (ReflectionUtility.Initializing)
                yield return null;

            mi_Graphics_CopyTexture = AccessTools.Method(
                typeof(Graphics),
                "CopyTexture",
                new Type[]
                {
                    typeof(Texture), typeof(int), typeof(int), typeof(int), typeof(int), typeof(int), typeof(int),
                    typeof(Texture), typeof(int), typeof(int), typeof(int), typeof(int)
                });

            if (ReflectionUtility.GetTypeByName("UnityEngine.ImageConversion") is Type imageConversion)
                mi_EncodeToPNG = imageConversion.GetMethod("EncodeToPNG", ReflectionUtility.FLAGS);
            else
                mi_EncodeToPNG = typeof(Texture2D).GetMethod("EncodeToPNG", ReflectionUtility.FLAGS);
        }

        protected internal override void Internal_Blit(Texture tex, RenderTexture rt)
            => Graphics.Blit(tex, rt);

        protected internal override Sprite Internal_CreateSprite(Texture2D texture)
            => Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), Vector2.zero);

        protected internal override Sprite Internal_CreateSprite(Texture2D texture, Rect rect, Vector2 pivot, float pixelsPerUnit, uint extrude, Vector4 border)
             => Sprite.Create(texture, rect, pivot, pixelsPerUnit, extrude, SpriteMeshType.Tight, border);

        protected internal override Texture2D Internal_NewTexture2D(int width, int height)
            => new(width, height);

        protected internal override Texture2D Internal_NewTexture2D(int width, int height, TextureFormat textureFormat, bool mipChain)
            => new(width, height, textureFormat, mipChain);

        protected internal override byte[] Internal_EncodeToPNG(Texture2D tex)
        {
            if (mi_EncodeToPNG == null)
                throw new MissingMethodException("Could not find any Texture2D EncodeToPNG method!");

            return mi_EncodeToPNG.IsStatic
                ? (byte[])mi_EncodeToPNG.Invoke(null, new object[] { tex })
                : (byte[])mi_EncodeToPNG.Invoke(tex, ArgumentUtility.EmptyArgs);
        }

        internal override Texture Internal_CopyTexture(Texture src, int srcElement, int srcMip, int srcX, int srcY, 
            int srcWidth, int srcHeight, Texture dst, int dstElement, int dstMip, int dstX, int dstY)
        {
            if (mi_Graphics_CopyTexture == null)
                throw new MissingMethodException("This game does not ship with the required method 'Graphics.CopyTexture'.");

            mi_Graphics_CopyTexture.Invoke(null, new object[]
            {
                src, srcElement, srcMip, srcX, srcY, srcWidth, srcHeight, dst, dstElement, dstMip, dstX, dstY
            });

            return dst;
        }
    }
}
#endif