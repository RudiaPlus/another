using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;

public static class CardJsonIO
{
    public static CardData LoadCard(string path)
    {
        string json;
        if (TryLoadFromResources(path, out json))
        {
            return LoadCardFromJson(json);
        }

        string fullPath = ResolvePath(path);
        if (!File.Exists(fullPath))
        {
            Debug.LogWarning("CardJsonIO: file not found " + fullPath);
            return null;
        }

        json = File.ReadAllText(fullPath, Encoding.UTF8);
        return LoadCardFromJson(json);
    }

    public static CardData LoadCardFromJson(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            Debug.LogWarning("CardJsonIO: json is empty");
            return null;
        }

        CardData card = JsonUtility.FromJson<CardData>(json);
        NormalizeCard(card);
        return card;
    }

    public static void SaveCard(string path, CardData card)
    {
        if (Application.platform == RuntimePlatform.WebGLPlayer)
        {
            Debug.LogWarning("CardJsonIO: SaveCard is not supported on WebGL.");
            return;
        }

        if (card == null)
        {
            Debug.LogWarning("CardJsonIO: card is null");
            return;
        }

        NormalizeCard(card);
        string json = JsonUtility.ToJson(card, true);
        string fullPath = ResolvePath(path);
        string dir = Path.GetDirectoryName(fullPath);
        if (!string.IsNullOrEmpty(dir))
        {
            Directory.CreateDirectory(dir);
        }

        File.WriteAllText(fullPath, json, Encoding.UTF8);
    }

    public static CardDataList LoadCardList(string path)
    {
        string json;
        if (TryLoadFromResources(path, out json))
        {
            return LoadCardListFromJson(json);
        }

        string fullPath = ResolvePath(path);
        if (!File.Exists(fullPath))
        {
            Debug.LogWarning("CardJsonIO: file not found " + fullPath);
            return null;
        }

        json = File.ReadAllText(fullPath, Encoding.UTF8);
        return LoadCardListFromJson(json);
    }

    public static void SaveCardList(string path, CardDataList list)
    {
        if (Application.platform == RuntimePlatform.WebGLPlayer)
        {
            Debug.LogWarning("CardJsonIO: SaveCardList is not supported on WebGL.");
            return;
        }

        if (list == null)
        {
            Debug.LogWarning("CardJsonIO: list is null");
            return;
        }

        NormalizeCardList(list);
        string json = JsonUtility.ToJson(list, true);
        string fullPath = ResolvePath(path);
        string dir = Path.GetDirectoryName(fullPath);
        if (!string.IsNullOrEmpty(dir))
        {
            Directory.CreateDirectory(dir);
        }

        File.WriteAllText(fullPath, json, Encoding.UTF8);
    }

    private static CardDataList LoadCardListFromJson(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            Debug.LogWarning("CardJsonIO: json is empty");
            return null;
        }

        CardDataList list = JsonUtility.FromJson<CardDataList>(json);
        NormalizeCardList(list);
        return list;
    }

    private static void NormalizeCard(CardData card)
    {
        if (card == null)
        {
            return;
        }

        if (card.lines == null)
        {
            card.lines = new List<LineData>();
        }

        foreach (var line in card.lines)
        {
            if (line.paramsInt == null)
            {
                line.paramsInt = new List<LineParamInt>();
            }

            if (line.paramsStr == null)
            {
                line.paramsStr = new List<LineParamStr>();
            }

            EffectLibrary.ApplyDefinition(line);
        }
    }

    private static void NormalizeCardList(CardDataList list)
    {
        if (list == null)
        {
            return;
        }

        if (list.cards == null)
        {
            list.cards = new List<CardData>();
        }

        foreach (var card in list.cards)
        {
            NormalizeCard(card);
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

[System.Serializable]
public class CardDataList
{
    public List<CardData> cards;
}
