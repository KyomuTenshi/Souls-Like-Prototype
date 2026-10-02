using UnityEditor;
using UnityEngine;

namespace SG {
    // Разовая настройка пака Dungeon_Environment под URP: меню Tools → Dungeon Environment.
    // Пак сконвертирован из Unreal: часть материалов ссылается на отсутствующие шейдеры,
    // часть сделана на built-in Standard, а текстуры импортируются в 8K.
    public static class DungeonEnvironmentSetup
    {
        const string FolderPath = "Assets/_ThirdParty/Dungeon_Environment";
        const int MaxTextureSize = 2048;
        const float DefaultSmoothness = 0.25f;

        static readonly string[] Platforms = { "Standalone", "Android", "iPhone", "WebGL" };

        #region Textures
        [MenuItem("Tools/Dungeon Environment/1. Optimize Textures")]
        static void OptimizeTextures()
        {
            string[] guids = AssetDatabase.FindAssets("t:Texture2D", new[] { FolderPath });
            int changed = 0;

            // Все реимпорты выполняются одним пакетом после StopAssetEditing.
            AssetDatabase.StartAssetEditing();
            try
            {
                for (int i = 0; i < guids.Length; i++)
                {
                    string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                    EditorUtility.DisplayProgressBar("Optimize Textures", path, (float)i / guids.Length);

                    TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;

                    if (importer == null)
                        continue;

                    if (LimitTextureSize(importer))
                    {
                        importer.SaveAndReimport();
                        changed++;
                    }
                }
            }
            finally
            {
                EditorUtility.ClearProgressBar();
                AssetDatabase.StopAssetEditing();
            }

            Debug.Log($"Dungeon Environment: размер ограничен до {MaxTextureSize} у {changed} из {guids.Length} текстур.");
        }

        private static bool LimitTextureSize(TextureImporter importer)
        {
            bool changed = false;

            if (importer.maxTextureSize > MaxTextureSize)
            {
                importer.maxTextureSize = MaxTextureSize;
                changed = true;
            }

            foreach (string platform in Platforms)
            {
                TextureImporterPlatformSettings settings = importer.GetPlatformTextureSettings(platform);

                if (settings.overridden && settings.maxTextureSize > MaxTextureSize)
                {
                    settings.maxTextureSize = MaxTextureSize;
                    importer.SetPlatformTextureSettings(settings);
                    changed = true;
                }
            }

            return changed;
        }
        #endregion

        #region Materials
        [MenuItem("Tools/Dungeon Environment/2. Convert Materials To URP Lit")]
        static void ConvertMaterials()
        {
            Shader litShader = Shader.Find("Universal Render Pipeline/Lit");

            if (litShader == null)
            {
                Debug.LogError("Dungeon Environment: шейдер Universal Render Pipeline/Lit не найден.");
                return;
            }

            string[] guids = AssetDatabase.FindAssets("t:Material", new[] { FolderPath });
            int converted = 0;

            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                Material material = AssetDatabase.LoadAssetAtPath<Material>(path);

                if (material == null || !NeedsConversion(material))
                    continue;

                ConvertMaterial(material, litShader);
                converted++;
            }

            AssetDatabase.SaveAssets();
            Debug.Log($"Dungeon Environment: переведено на URP/Lit {converted} материалов.");
        }

        // Hidden/InternalErrorShader — шейдер, сгенерированный конвертером Unreal, отсутствует в проекте.
        // Частицы (огонь, дым) не трогаются: их переводит Render Pipeline Converter.
        private static bool NeedsConversion(Material material)
        {
            string shaderName = material.shader != null ? material.shader.name : "";
            return shaderName == "Standard" || shaderName == "Hidden/InternalErrorShader";
        }

        private static void ConvertMaterial(Material material, Shader litShader)
        {
            // Текстуры читаются из сохранённых свойств до смены шейдера: у отсутствующего шейдера
            // слоты называются Material_Texture2D_0..2, и через material.GetTexture их не получить.
            SerializedObject serializedMaterial = new SerializedObject(material);
            SerializedProperty textures = serializedMaterial.FindProperty("m_SavedProperties.m_TexEnvs");
            SerializedProperty colors = serializedMaterial.FindProperty("m_SavedProperties.m_Colors");

            // Имя предмета без префикса: MI_Bottle_02 → Bottle_02. Нужно, если в материале несколько _BC.
            string itemName = material.name.StartsWith("MI_") ? material.name.Substring(3) : material.name;

            Texture baseMap = null;
            Texture normalMap = null;
            Vector2 baseScale = Vector2.one;
            Vector2 baseOffset = Vector2.zero;

            for (int i = 0; i < textures.arraySize; i++)
            {
                SerializedProperty entry = textures.GetArrayElementAtIndex(i);
                string slot = entry.FindPropertyRelative("first").stringValue;
                Texture texture = entry.FindPropertyRelative("second.m_Texture").objectReferenceValue as Texture;

                if (texture == null)
                    continue;

                // _AO_R_MT (AO, Roughness, Metallic в каналах R, G, B) — упаковка Unreal,
                // URP/Lit читает каналы иначе, поэтому карта пропускается.
                if (texture.name.EndsWith("_AO_R_MT"))
                    continue;

                if (texture.name.EndsWith("_N") || slot == "_BumpMap")
                {
                    if (normalMap == null)
                        normalMap = texture;
                }
                else if (texture.name.EndsWith("_BC") || slot == "_MainTex")
                {
                    if (baseMap == null || texture.name.Contains(itemName))
                    {
                        baseMap = texture;
                        baseScale = entry.FindPropertyRelative("second.m_Scale").vector2Value;
                        baseOffset = entry.FindPropertyRelative("second.m_Offset").vector2Value;
                    }
                }
            }

            Color baseColor = Color.white;

            for (int i = 0; i < colors.arraySize; i++)
            {
                SerializedProperty entry = colors.GetArrayElementAtIndex(i);

                if (entry.FindPropertyRelative("first").stringValue == "_Color")
                    baseColor = entry.FindPropertyRelative("second").colorValue;
            }

            material.shader = litShader;
            material.SetColor("_BaseColor", baseColor);
            material.SetFloat("_Metallic", 0f);
            material.SetFloat("_Smoothness", DefaultSmoothness);

            if (baseMap != null)
            {
                material.SetTexture("_BaseMap", baseMap);
                material.SetTextureScale("_BaseMap", baseScale);
                material.SetTextureOffset("_BaseMap", baseOffset);
            }

            if (normalMap != null)
            {
                material.SetTexture("_BumpMap", normalMap);
                material.EnableKeyword("_NORMALMAP");
            }

            EditorUtility.SetDirty(material);
        }
        #endregion
    }
}
