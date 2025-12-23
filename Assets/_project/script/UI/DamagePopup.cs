using System.Drawing;
using TMPro;
using UnityEngine.UI;
using UnityEngine;

public class DamagePopup: MonoBehaviour
{
    [SerializeField] private TextMeshPro textMesh;

    public void Setup(int damageAmount, UnityEngine.Color color)
    {
        textMesh.text = damageAmount.ToString();
        textMesh.color = color;
        //アニメーション

    }
}