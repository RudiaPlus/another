using UnityEngine;
using TMPro;
using System.Drawing;
using System.Collections.Generic;

public class CardView : MonoBehaviour
{
    [SerializeField] private Transform linesContainer; // Vertical Layout Group
    [SerializeField] private GameObject lineTextPrefab; // 行のテキストプレハブ

    public void RenderCard(CardData data)
    {
        // 既存の表示をクリア
        foreach (Transform child in linesContainer) Destroy(child.gameObject);

        // データの順番通りにUIを生成（これで見た目の順番は保証される）
        foreach (var line in data.lines)
        {
            var textObj = Instantiate(lineTextPrefab, linesContainer);
            var textComp = textObj.GetComponent<TextMeshProUGUI>();
            
            // 色分け（Curseは赤、Buffは青など）
            UnityEngine.Color color = GetColorByType(line.type);
            textComp.color = color;
            
            // テキスト設定 "【吐血】HPを5失う"
            textComp.text = $"【{line.displayName}】{line.description}";
        }
    }
}