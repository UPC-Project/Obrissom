using Obrissom.UI;
using TMPro;
using UnityEngine;

public class QuestEntryUI : MonoBehaviour
{
    [SerializeField] private TMP_Text _titleText;
    [SerializeField] private TMP_Text _descriptionText;
    [SerializeField] private TMP_Text _rewardText;

    public void Setup(QuestTemplate template)
    {
        _titleText.text = template.title;
        _descriptionText.text = template.offerDescription;
        _rewardText.text = QuestDisplayer.BuildRewardsString(template);
    }
}
