extern alias textrender;

using Font = textrender::UnityEngine.Font;

using BepInEx;
using System;
using System.IO;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace TTFLoaderMono
{
    [BepInPlugin(PLUGIN_GUID, PLUGIN_NAME, PLUGIN_VERSION)]
    public class TTFLoaderPlugin : BaseUnityPlugin
    {
        public const string PLUGIN_GUID = "com.github.qwella.ttfloader";
        public const string PLUGIN_NAME = "TTF Thai Font Loader (Mono)";
        public const string PLUGIN_VERSION = "1.0.0";

        private static string fontsDirectory;
        private static Font dynamicFont;

        void Awake()
        {
            Logger.LogInfo($"Plugin {PLUGIN_NAME} is loaded!");
            fontsDirectory = BepInEx.Paths.GameRootPath;
            Logger.LogInfo($"TTF Loader initialized. Fonts directory: {fontsDirectory}");
        }

        void Start()
        {
            LoadDefaultFontFromDirectory();
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        void OnDestroy()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            StartCoroutine(ApplyFontAfterDelay());
        }

        private IEnumerator ApplyFontAfterDelay()
        {
            yield return null;
            yield return null;
            ApplyCustomFontToAllTexts();
        }

        private void ApplyCustomFontToAllTexts()
        {
            if (dynamicFont != null)
            {
                foreach (var text in FindAllTextComponents(includeInactive: true))
                {
                    text.font = dynamicFont;
                }
            }
        }

        private List<UnityEngine.UI.Text> FindAllTextComponents(bool includeInactive)
        {
            Type textType = typeof(UnityEngine.UI.Text);
            Type objectType = typeof(UnityEngine.Object);
            object found = null;

            MethodInfo includeInactiveMethod = objectType.GetMethod(
                "FindObjectsOfType",
                BindingFlags.Static | BindingFlags.Public,
                null,
                new[] { typeof(bool) },
                null);

            if (includeInactiveMethod != null)
            {
                found = includeInactiveMethod
                    .MakeGenericMethod(textType)
                    .Invoke(null, new object[] { includeInactive });
            }

            if (found == null)
            {
                MethodInfo basicMethod = objectType.GetMethod(
                    "FindObjectsOfType",
                    BindingFlags.Static | BindingFlags.Public,
                    null,
                    Type.EmptyTypes,
                    null);

                if (basicMethod != null)
                {
                    found = basicMethod
                        .MakeGenericMethod(textType)
                        .Invoke(null, null);
                }
            }

            var result = new List<UnityEngine.UI.Text>();
            if (found is object[] array)
            {
                foreach (var obj in array)
                {
                    if (obj is UnityEngine.UI.Text text)
                    {
                        result.Add(text);
                    }
                }
            }

            return result;
        }

        private void LoadDefaultFontFromDirectory()
        {
            try
            {
                string[] fontFiles = Directory.GetFiles(fontsDirectory, "*.ttf", SearchOption.TopDirectoryOnly);
                if (fontFiles.Length == 0)
                {
                    fontFiles = Directory.GetFiles(fontsDirectory, "*.TTF", SearchOption.TopDirectoryOnly);
                }

                if (fontFiles.Length == 0)
                {
                    Logger.LogWarning("No TTF font files found in the Fonts directory.");
                    return;
                }

                foreach (string ttfPath in fontFiles)
                {
                    string fontName = Path.GetFileNameWithoutExtension(ttfPath);
                    var customFont = LoadTMPTTF(fontName);

                    if (customFont != null)
                    {
                        // 1. กำหนดค่าเป็น defaultFontAsset ผ่าน Reflection สำหรับ Mono
                        try
                        {
                            var settingsType = typeof(TMP_Settings);
                            var prop = settingsType.GetProperty("defaultFontAsset", BindingFlags.Static | BindingFlags.Public);

                            if (prop != null && prop.CanWrite)
                            {
                                prop.SetValue(null, customFont, null);
                            }
                            else
                            {
                                var field = settingsType.GetField("k_DefaultFontAsset", BindingFlags.Static | BindingFlags.NonPublic);
                                field?.SetValue(null, customFont);
                            }
                            Logger.LogInfo($"Successfully set default TMP font via Reflection: {fontName}");
                        }
                        catch (Exception ex)
                        {
                            Logger.LogWarning($"Could not set defaultFontAsset: {ex.Message}");
                        }

                        // 2. ฉีดฟอนต์ไทยขยายขนาดนี้เข้า Fallback Table ของทุกฟอนต์ในเกม
                        var loadedFonts = Resources.FindObjectsOfTypeAll<TMP_FontAsset>();
                        foreach (var baseFontAsset in loadedFonts)
                        {
                            if (baseFontAsset != null && baseFontAsset != customFont && baseFontAsset.fallbackFontAssetTable != null)
                            {
                                if (!baseFontAsset.fallbackFontAssetTable.Contains(customFont))
                                {
                                    baseFontAsset.fallbackFontAssetTable.Add(customFont);
                                    Logger.LogInfo($"Added [{fontName}] as fallback to [{baseFontAsset.name}]");
                                }
                            }
                        }

                        return;
                    }
                    else
                    {
                        Logger.LogWarning($"Failed to load font: {fontName}, trying next...");
                    }
                }

                Logger.LogError("Failed to load any font from the Fonts directory.");
            }
            catch (Exception ex)
            {
                Logger.LogError($"Error loading default font from directory: {ex.Message}\n{ex.StackTrace}");
            }
        }

        public Font LoadTTF(string fontName, bool dynamic = false)
        {
            Font font = null;

            string ttfPath = Path.Combine(fontsDirectory, fontName + ".ttf");
            if (!File.Exists(ttfPath))
            {
                ttfPath = Path.Combine(fontsDirectory, fontName + ".TTF");
            }

            if (File.Exists(ttfPath))
            {
                Logger.LogInfo($"Found TTF file: {ttfPath}");
                font = dynamic ? Font.CreateDynamicFontFromOSFont(ttfPath, 16) : new Font(ttfPath);
            }

            if (font != null)
                return font;

            string[] variants = {
                fontName,
                $"{fontName}-Regular",
                $"{fontName} Regular",
                "Tahoma",
                "Cordia New",
                "Microsoft YaHei"
            };

            foreach (string variant in variants)
            {
                font = Font.CreateDynamicFontFromOSFont(variant, 16);
                if (font != null)
                {
                    font.name = variant;
                    Logger.LogInfo($"Loaded system font variant: {variant}");
                    return font;
                }
            }

            Logger.LogWarning($"Using fallback font 'Arial' for: {fontName}");
            Font fallbackFonts = Font.CreateDynamicFontFromOSFont("Arial", 16);
            if (fallbackFonts != null)
            {
                fallbackFonts.name = "Arial";
                return fallbackFonts;
            }

            return null;
        }

        public TMP_FontAsset LoadTMPTTF(string fontName)
        {
            try
            {
                Font baseFont = LoadTTF(fontName);
                if (baseFont == null)
                {
                    Logger.LogError($"Failed to load base font: {fontName}");
                    return null;
                }

                // สร้าง TMP_FontAsset พร้อมตั้งค่า pointSize = 120
                TMP_FontAsset tmpFont = TMP_FontAsset.CreateFontAsset(
                    baseFont,
                    120,
                    9,
                    UnityEngine.TextCore.LowLevel.GlyphRenderMode.SDF,
                    1024,
                    1024
                );

                if (tmpFont == null)
                {
                    Logger.LogError($"TMP_FontAsset.CreateFontAsset returned null for: {fontName}");
                    return null;
                }

                tmpFont.name = fontName;

                // === ขยายขนาดการแสดงผลของฟอนต์ ===
                var faceInfo = tmpFont.faceInfo;
                faceInfo.scale = 2.0f; // สามารถปรับเปลี่ยนตัวเลขตรงนี้ได้ (เช่น 1.2f ถึง 2.3f)
                tmpFont.faceInfo = faceInfo;

                // ค่าขอบหนา
                if (tmpFont.material != null)
                {
                    tmpFont.material.SetFloat(ShaderUtilities.ID_FaceDilate, 0f);
                    tmpFont.material.SetFloat(ShaderUtilities.ID_OutlineWidth, 0f);
                }

                Logger.LogInfo($"Successfully created scaled TMP font: {fontName}");
                return tmpFont;
            }
            catch (Exception ex)
            {
                Logger.LogError($"Failed to create TMP font {fontName}: {ex.Message}\n{ex.StackTrace}");

                Logger.LogInfo("Trying use UI.Text");
                Font baseFont = LoadTTF(fontName, true);
                if (baseFont == null)
                {
                    Logger.LogError($"Failed to load base font: {fontName}");
                    return null;
                }
                dynamicFont = baseFont;
                ApplyCustomFontToAllTexts();
                return null;
            }
        }
    }
}
