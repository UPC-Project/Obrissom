using UnityEngine;
using UnityEngine.UI;

public class QuestsMenu : MenuPanel
{
    [SerializeField] private QuestEntryUI _questEntryPrefab;
    [SerializeField] private Transform _questListContainer;

    private PlayerQuestTracker _tracker;

    public void BindTracker(PlayerQuestTracker tracker)
    {
        if (_tracker != null)
            _tracker.OnQuestsChanged -= RefreshUI;

        _tracker = tracker;
        _tracker.OnQuestsChanged += RefreshUI;
        RefreshUI();
    }

    public override void SetMenuState(bool state)
    {
        base.SetMenuState(state);
        if (state) RefreshUI();
    }

    private void OnDestroy()
    {
        if (_tracker != null)
            _tracker.OnQuestsChanged -= RefreshUI;
    }

    private void RefreshUI()
    {
        if (_tracker == null) return;

        foreach (Transform child in _questListContainer)
            Destroy(child.gameObject);

        foreach (QuestInstance quest in _tracker.ActiveQuests)
        {
            QuestEntryUI entry = Instantiate(_questEntryPrefab, _questListContainer);
            entry.Setup(quest.template);
        }

        LayoutRebuilder.ForceRebuildLayoutImmediate(_questListContainer as RectTransform);
    }
}
