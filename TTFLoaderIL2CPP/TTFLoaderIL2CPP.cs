using BepInEx;
using BepInEx.Unity.IL2CPP;
using System;
using System.IO;
using UnityEngine;
using TMPro;

namespace TTFLoaderIL2CPP_Thaifont
{
    [BepInPlugin(PLUGIN_GUID, PLUGIN_NAME, PLUGIN_VERSION)]
    public class TTFLoaderPlugin : BasePlugin
    {
        public const string PLUGIN_GUID = "com.github.qwella.ttfloader";
        public const string PLUGIN_NAME = "TTF Thai Font Loader (IL2CPP)";
        public const string PLUGIN_VERSION = "1.0.0";

        private static string fontsDirectory;

        public override void Load()
        {
            Log.LogInfo($"Plugin {PLUGIN_NAME} is loaded!");

            fontsDirectory = Paths.GameRootPath;
            LoadDefaultFontFromDirectory();

            Log.LogInfo($"TTF Loader initialized. Fonts directory: {fontsDirectory}");
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
                    Log.LogWarning("No TTF font files found in the Fonts directory.");
                    return;
                }

                foreach (string ttfPath in fontFiles)
                {
                    string fontName = Path.GetFileNameWithoutExtension(ttfPath);
                    TMP_FontAsset customFont = LoadTMPTTF(fontName);

                    if (customFont != null)
                    {
                        try
                        {
                            TMP_Settings.defaultFontAsset = customFont;
                            Log.LogInfo($"Successfully set default TMP font to: {fontName}");
                        }
                        catch (Exception)
                        {
                            var settingsType = typeof(TMPro.TMP_Settings);
                            var prop = settingsType.GetProperty("defaultFontAsset", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public);

                            if (prop != null && prop.CanWrite)
                            {
                                prop.SetValue(null, customFont);
                            }
                            else
                            {
                                var field = settingsType.GetField("k_DefaultFontAsset", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);
                                field?.SetValue(null, customFont);
                            }

                            Log.LogInfo($"Successfully set default TMP font via Reflection: {fontName}");
                        }

                        var loadedFonts = Resources.FindObjectsOfTypeAll<TMP_FontAsset>();
                        foreach (var baseFontAsset in loadedFonts)
                        {
                            if (baseFontAsset != null && baseFontAsset != customFont && baseFontAsset.fallbackFontAssetTable != null)
                            {
                                if (!baseFontAsset.fallbackFontAssetTable.Contains(customFont))
                                {
                                    baseFontAsset.fallbackFontAssetTable.Add(customFont);
                                    Log.LogInfo($"Added [{fontName}] as fallback to [{baseFontAsset.name}]");
                                }
                            }
                        }

                        return;
                    }
                    else
                    {
                        Log.LogWarning($"Failed to load font: {fontName}, trying next...");
                    }
                }

                Log.LogError("Failed to load any font from the Fonts directory.");
            }
            catch (Exception ex)
            {
                Log.LogError($"Error loading default font from directory: {ex.Message}\n{ex.StackTrace}");
            }
        }

        public Font LoadTTF(string fontName)
        {
            Font font = null;

            string ttfPath = Path.Combine(fontsDirectory, fontName + ".ttf");
            if (!File.Exists(ttfPath))
            {
                ttfPath = Path.Combine(fontsDirectory, fontName + ".TTF");
            }

            if (File.Exists(ttfPath))
            {
                Log.LogInfo($"Found TTF file: {ttfPath}");
                font = new Font(ttfPath);
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
                font = Font.CreateDynamicFontFromOSFont(variant, 12);
                if (font != null)
                {
                    Log.LogInfo($"Loaded system font variant: {variant}");
                    return font;
                }
            }

            Log.LogWarning($"Using fallback font 'Arial' for: {fontName}");
            return Font.CreateDynamicFontFromOSFont("Arial", 12);
        }

        public TMP_FontAsset LoadTMPTTF(string fontName)
        {
            try
            {
                Font baseFont = LoadTTF(fontName);
                if (baseFont == null)
                {
                    Log.LogError($"Failed to load base font: {fontName}");
                    return null;
                }

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
                    Log.LogError($"TMP_FontAsset.CreateFontAsset returned null for: {fontName}");
                    return null;
                }

                tmpFont.name = fontName;
				
				//ปรับไซส์ฟอนท์ในเกม
				var faceInfo = tmpFont.faceInfo;
				faceInfo.scale = 1.4f; 
				tmpFont.faceInfo = faceInfo;

                if (tmpFont.material != null)
                {
                    tmpFont.material.SetFloat(ShaderUtilities.ID_FaceDilate, 0f);
                    tmpFont.material.SetFloat(ShaderUtilities.ID_OutlineWidth, 0f);
                }

                Log.LogInfo($"Successfully created TMP font with fixed material: {fontName}");
                return tmpFont;
            }
            catch (Exception ex)
            {
                Log.LogError($"Failed to create TMP font {fontName}: {ex.Message}\n{ex.StackTrace}");
                return null;
            }
        }

        // === ฟังก์ชันจัดตำแหน่งสระไทย ===
        public static string FixThaiGlyphs(string text)
        {
            if (string.IsNullOrEmpty(text)) return text;

            char[] chars = text.ToCharArray();
            for (int i = 0; i < chars.Length; i++)
            {
                if (i > 0 && IsUpperThaiMark(chars[i]))
                {
                    char prev = chars[i - 1];
                    if (IsTallConsonant(prev))
                    {
                        chars[i] = ShiftToShiftedUnicode(chars[i]);
                    }
                }
            }
            return new string(chars);
        }

        private static bool IsTallConsonant(char c) => c == 'ป' || c == 'ฝ' || c == 'ฟ' || c == 'ฬ';
        private static bool IsUpperThaiMark(char c) => (c >= '\u0E31' && c <= '\u0E37') || (c >= '\u0E47' && c <= '\u0E4E');

        private static char ShiftToShiftedUnicode(char c)
        {
            return c switch
            {
                '\u0E48' => '\uF70A',
                '\u0E49' => '\uF70B',
                '\u0E4A' => '\uF70C',
                '\u0E4B' => '\uF70D',
                '\u0E31' => '\uF710',
                _ => c
            };
        }
    }
}
