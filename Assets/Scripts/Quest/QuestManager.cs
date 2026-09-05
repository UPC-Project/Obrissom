using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

/// Server-authoritative, manages shared quest instances.
/// Tracks progress across all participating players and syncs.
public class QuestManager : NetworkBehaviour
{
    public static QuestManager Instance { get; private set; }

    // Server-only
    private Dictionary<string, QuestInstance> _sharedQuests = new Dictionary<string, QuestInstance>();
    //(questId, (itemTargetIndex, (clientId, amount of items)))
    private Dictionary<string, Dictionary<int, Dictionary<ulong, int>>> _sharedCollectProgress = new Dictionary<string, Dictionary<int, Dictionary<ulong, int>>>();
    private Dictionary<string, List<PlayerQuestTracker>> _participants = new Dictionary<string, List<PlayerQuestTracker>>();

    // Client-side
    private PlayerQuestTracker _localTracker = null;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public override void OnDestroy()
    {
        base.OnDestroy();
        if (Instance == this) Instance = null;
    }

    // CLIENT REGISTRATION

    /// Registers a local PlayerQuestTracker so it can receive shared quest sync updates.
    public void RegisterLocalTracker(PlayerQuestTracker tracker)
    {
        if (_localTracker == null) _localTracker = tracker;
    }

    // SHARED QUEST MANAGEMENT

    public void ShareAcceptQuest(string questId)
    {
        ShareAcceptQuestServerRpc(questId);
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    private void ShareAcceptQuestServerRpc(string questId)
    {
        RegisterSharedQuestServerRpc(questId);
        ShareAcceptQuestClientRpc(questId);
    }

    [Rpc(SendTo.Everyone)]
    private void ShareAcceptQuestClientRpc(string questId)
    {
        if (_localTracker != null)
        {
            QuestTemplate template = FindQuestTemplateById(questId);
            if (template != null)
            {
                _localTracker.AcceptQuestLocally(template);
            }
            else
            {
                Debug.LogError($"[QuestManager] ShareAcceptQuestClientRpc failed: Could not find QuestTemplate with questId '{questId}'. Make sure the ScriptableObject has a unique questId assigned");
            }
        }
    }

    public void ShareCompleteQuest(string questId)
    {
        ShareCompleteQuestServerRpc(questId);
    }

    // COLLECT PROGRESS
    // questId -> targetIndex -> clientId -> item count
    public void ReportCollectProgress(string questId, int itemTargetIndex, int localCount)
    {
        ReportCollectProgressServerRpc(questId, itemTargetIndex, localCount);
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    private void ReportCollectProgressServerRpc(string questId, int itemTargetIndex, int localCount, RpcParams rpcParams = default)
    {
        ulong clientId = rpcParams.Receive.SenderClientId;
        if (!_sharedQuests.TryGetValue(questId, out QuestInstance quest)) return;

        if (!_sharedCollectProgress.ContainsKey(questId))
            _sharedCollectProgress[questId] = new Dictionary<int, Dictionary<ulong, int>>();

        if (!_sharedCollectProgress[questId].ContainsKey(itemTargetIndex))
            _sharedCollectProgress[questId][itemTargetIndex] = new Dictionary<ulong, int>();

        _sharedCollectProgress[questId][itemTargetIndex][clientId] = localCount;

        // Calculate total
        int totalCount = 0;
        foreach (int count in _sharedCollectProgress[questId][itemTargetIndex].Values)
        {
            totalCount += count;
        }

        SyncCollectProgressClientRpc(questId, itemTargetIndex, totalCount);
    }

    [Rpc(SendTo.Everyone)]
    private void SyncCollectProgressClientRpc(string questId, int itemTargetIndex, int totalProgress)
    {
        if (_localTracker != null)
            _localTracker.SyncCollectProgress(questId, itemTargetIndex, totalProgress);
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    private void ShareCompleteQuestServerRpc(string questId, RpcParams rpcParams = default)
    {
        ulong completingClientId = rpcParams.Receive.SenderClientId;
        QuestTemplate template = FindQuestTemplateById(questId);

        if (template != null && template.objective != null && template.objective.type == QuestObjectiveType.Collect)
        {
            for (int i = 0; i < template.objective.itemTargets.Length; i++)
            {
                int needed = template.objective.itemTargets[i].amount;

                if (_sharedCollectProgress.TryGetValue(questId, out var questCounts) && 
                    questCounts.TryGetValue(i, out var targetCounts))
                {
                    // Priority 1: completing client
                    if (targetCounts.TryGetValue(completingClientId, out int completerHas))
                    {
                        int fromCompleter = Mathf.Min(needed, completerHas);
                        if (fromCompleter > 0)
                        {
                            needed -= fromCompleter;
                            DeductCollectItemClientRpc(questId, i, fromCompleter, new ClientRpcParams { Send = new ClientRpcSendParams { TargetClientIds = new ulong[] { completingClientId } } });
                        }
                    }

                    // Priority 2: other clients
                    if (needed > 0)
                    {
                        foreach (var kvp in targetCounts)
                        {
                            if (kvp.Key == completingClientId) continue;

                            int fromOther = Mathf.Min(needed, kvp.Value);
                            if (fromOther > 0)
                            {
                                needed -= fromOther;
                                DeductCollectItemClientRpc(questId, i, fromOther, new ClientRpcParams { Send = new ClientRpcSendParams { TargetClientIds = new ulong[] { kvp.Key } } });
                            }

                            if (needed <= 0) break;
                        }
                    }
                }
            }
        }

        if (_sharedQuests.ContainsKey(questId))
        {
            _sharedQuests.Remove(questId);
        }
        ShareCompleteQuestClientRpc(questId);
    }

    [Rpc(SendTo.Everyone)]
    private void ShareCompleteQuestClientRpc(string questId)
    {
        if (_localTracker != null)
        {
            QuestTemplate template = FindQuestTemplateById(questId);
            if (template != null)
            {
                _localTracker.CompleteQuestLocally(template);
            }
        }
    }

    /// Registers a player as a participant of a shared quest.
    /// Creates the shared QuestInstance on the server if it doesn't exist.
    public void RegisterSharedQuest(string questId, PlayerQuestTracker tracker)
    {
        RegisterSharedQuestServerRpc(questId);

        if (IsServer)
        {
            if (!_participants.ContainsKey(questId))
            {
                _participants[questId] = new List<PlayerQuestTracker>();
            }
            if (!_participants[questId].Contains(tracker))
            {
                _participants[questId].Add(tracker);
            }
        }
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    private void RegisterSharedQuestServerRpc(string questId)
    {
        if (_sharedQuests.ContainsKey(questId)) return;

        QuestTemplate template = FindQuestTemplateById(questId);
        if (template == null)
        {
            Debug.LogWarning($"[QuestManager] Cannot find QuestTemplate with ID: {questId}");
            return;
        }

        QuestInstance sharedInstance = new(template);
        _sharedQuests[questId] = sharedInstance;

        if (!_participants.ContainsKey(questId))
        {
            _participants[questId] = new List<PlayerQuestTracker>();
        }
    }

    // PROGRESS REPORTING

    public void ReportKillProgress(string questId, int enemyTargetIndex, int amount)
    {
        ReportKillProgressServerRpc(questId, enemyTargetIndex, amount);
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    private void ReportKillProgressServerRpc(string questId, int enemyTargetIndex, int amount)
    {
        if (!_sharedQuests.TryGetValue(questId, out QuestInstance quest)) return;

        quest.AddKillProgress(enemyTargetIndex, amount);
        int currentProgress = quest.GetKillProgress(enemyTargetIndex);

        SyncKillProgressClientRpc(questId, enemyTargetIndex, currentProgress);
    }

    [Rpc(SendTo.Everyone)]
    private void SyncKillProgressClientRpc(string questId, int enemyTargetIndex, int currentProgress)
    {
        _localTracker.SyncKillProgress(questId, enemyTargetIndex, currentProgress);
    }

    public void ReportTalkProgress(string questId)
    {
        ReportTalkProgressServerRpc(questId);
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    private void ReportTalkProgressServerRpc(string questId)
    {
        if (!_sharedQuests.TryGetValue(questId, out QuestInstance quest)) return;

        quest.SetTalkCompleted();

        SyncTalkProgressClientRpc(questId);
    }

    [Rpc(SendTo.Everyone)]
    private void SyncTalkProgressClientRpc(string questId)
    {
        _localTracker.SyncTalkProgress(questId);
    }

    [ClientRpc]
    private void DeductCollectItemClientRpc(string questId, int targetIndex, int amount, ClientRpcParams rpcParams = default)
    {
        if (_localTracker != null)
        {
            _localTracker.DeductSpecificCollectTarget(questId, targetIndex, amount);
        }
    }

    // HELPERS

    private QuestTemplate FindQuestTemplateById(string questId)
    {
        QuestTemplate[] allTemplates = Resources.FindObjectsOfTypeAll<QuestTemplate>();
        foreach (QuestTemplate template in allTemplates)
        {
            if (template.questId == questId)
            {
                return template;
            }
        }
        return null;
    }
}
