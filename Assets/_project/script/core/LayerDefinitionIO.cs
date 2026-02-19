using System;
using System.IO;
using System.Text;
using UnityEngine;

public static class LayerDefinitionIO
{
    public static LayerDefinitionList Load(string path)
    {
        string json;
        if (TryLoadFromResources(path, out json))
        {
            return LoadLayerListFromJson(json);
        }

        string fullPath = ResolvePath(path);
        if (!File.Exists(fullPath))
        {
            Debug.LogWarning("LayerDefinitionIO: file not found " + fullPath);
            return null;
        }

        json = File.ReadAllText(fullPath, Encoding.UTF8);
        return LoadLayerListFromJson(json);
    }

    public static void Save(string path, LayerDefinitionList list)
    {
        if (Application.platform == RuntimePlatform.WebGLPlayer)
        {
            Debug.LogWarning("LayerDefinitionIO: Save is not supported on WebGL.");
            return;
        }

        if (list == null)
        {
            Debug.LogWarning("LayerDefinitionIO: list is null");
            return;
        }

        if (list.layers == null)
        {
            list.layers = new System.Collections.Generic.List<LayerDefinition>();
        }

        string json = JsonUtility.ToJson(list, true);
        string fullPath = ResolvePath(path);
        string dir = Path.GetDirectoryName(fullPath);
        if (!string.IsNullOrEmpty(dir))
        {
            Directory.CreateDirectory(dir);
        }

        File.WriteAllText(fullPath, json, Encoding.UTF8);
    }

    private static LayerDefinitionList LoadLayerListFromJson(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            Debug.LogWarning("LayerDefinitionIO: json is empty");
            return null;
        }

        LayerDefinitionList list = JsonUtility.FromJson<LayerDefinitionList>(json);
        if (list == null)
        {
            return null;
        }

        if (list.layers == null)
        {
            list.layers = new System.Collections.Generic.List<LayerDefinition>();
        }

        return list;
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
