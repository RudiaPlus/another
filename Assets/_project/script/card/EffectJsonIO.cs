using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;

public static class EffectJsonIO
{
    public static EffectDataList LoadEffectList(string path)
    {
        string json;
        if (TryLoadFromResources(path, out json))
        {
            return LoadEffectListFromJson(json);
        }

        string fullPath = ResolvePath(path);
        if (!File.Exists(fullPath))
        {
            Debug.LogWarning("EffectJsonIO: file not found " + fullPath);
            return null;
        }

        json = File.ReadAllText(fullPath, Encoding.UTF8);
        return LoadEffectListFromJson(json);
    }

    public static void SaveEffectList(string path, EffectDataList list)
    {
        if (Application.platform == RuntimePlatform.WebGLPlayer)
        {
            Debug.LogWarning("EffectJsonIO: SaveEffectList is not supported on WebGL.");
            return;
        }

        if (list == null)
        {
            Debug.LogWarning("EffectJsonIO: list is null");
            return;
        }

        NormalizeEffectList(list);
        string json = JsonUtility.ToJson(list, true);
        string fullPath = ResolvePath(path);
        string dir = Path.GetDirectoryName(fullPath);
        if (!string.IsNullOrEmpty(dir))
        {
            Directory.CreateDirectory(dir);
        }

        File.WriteAllText(fullPath, json, Encoding.UTF8);
    }

    private static EffectDataList LoadEffectListFromJson(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            Debug.LogWarning("EffectJsonIO: json is empty");
            return null;
        }

        EffectDataList list = JsonUtility.FromJson<EffectDataList>(json);
        NormalizeEffectList(list);
        return list;
    }

    private static void NormalizeEffectList(EffectDataList list)
    {
        if (list == null)
        {
            return;
        }

        if (list.effects == null)
        {
            list.effects = new List<EffectDefinition>();
        }

        foreach (EffectDefinition effect in list.effects)
        {
            if (effect == null)
            {
                continue;
            }

            if (effect.paramsInt == null)
            {
                effect.paramsInt = new List<LineParamInt>();
            }

            if (effect.paramsStr == null)
            {
                effect.paramsStr = new List<LineParamStr>();
            }
        }
    }

    private static string ResolvePath(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return path;
        }

        if (Path.IsPathRooted(path))
        {
            return path;
        }

        if (path.StartsWith("Assets/") || path.StartsWith("Assets\\"))
        {
            string projectRoot = Directory.GetParent(Application.dataPath).FullName;
            return Path.Combine(projectRoot, path);
        }

        return Path.Combine(Application.dataPath, path);
    }

    private static bool TryLoadFromResources(string path, out string json)
    {
        json = null;
        string resourcePath = GetResourcePath(path);
        if (string.IsNullOrEmpty(resourcePath))
        {
            return false;
        }

        TextAsset asset = Resources.Load<TextAsset>(resourcePath);
        if (asset == null)
        {
            return false;
        }

        json = asset.text;
        return true;
    }

    private static string GetResourcePath(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return null;
        }

        string normalized = path.Replace('\\', '/');
        if (normalized.StartsWith("Assets/", StringComparison.OrdinalIgnoreCase))
        {
            normalized = normalized.Substring("Assets/".Length);
        }

        int resourcesIndex = normalized.IndexOf("Resources/", StringComparison.OrdinalIgnoreCase);
        if (resourcesIndex >= 0)
        {
            normalized = normalized.Substring(resourcesIndex + "Resources/".Length);
        }

        if (normalized.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
        {
            normalized = normalized.Substring(0, normalized.Length - ".json".Length);
        }

        return normalized;
    }
}
