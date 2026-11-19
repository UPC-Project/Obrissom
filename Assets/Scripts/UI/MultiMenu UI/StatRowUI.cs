using TMPro;
using UnityEngine;

public class StatRowUI : MonoBehaviour
{
    [SerializeField] private TMP_Text _nameText;
    [SerializeField] private TMP_Text _valueText;

    public void Setup(string statName, string statValue)
    {
        _nameText.text = statName;
        _valueText.text = statValue;
    }
}
