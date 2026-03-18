using System;
using System.Collections.Generic;
using System.Linq;
using Funder.Core.Services;
using Funder.Core.Events;
using RogueDeal.Events;
using UnityEngine;

namespace RogueDeal.Quests
{
    public class QuestService : IQuestService, IInitializable, IDisposable
    {
        private const string SAVE_KEY = "QuestProgress";
        private Dictionary<string, QuestProgress> _questProgress = new Dictionary<string, QuestProgress>();
        private Dictionary<string, QuestDefinition> _questDefinitions = new Dictionary<string, QuestDefinition>();
        private IEventBus _eventBus;
        private IDisposable _signalSubscription;

        public void Initialize()
        {
            _eventBus = GameBootstrap.ServiceLocator.Resolve<IEventBus>();
            _signalSubscription = _eventBus.Subscribe<QuestSignalEvent>(OnQuestSignal, 0);
            Load();
        }

        public void Dispose()
        {
            _signalSubscription?.Dispose();
            Save();
        }

        public bool TryStartQuest(string questId)
        {
            if (_questProgress.ContainsKey(questId))
            {
                var existing = _questProgress[questId];
                if (existing.status == QuestStatus.Active)
                {
                    Debug.LogWarning($"[QuestService] Quest {questId} is already active");
                    return false;
                }
                if (existing.IsTerminal)
                {
                    Debug.LogWarning($"[QuestService] Quest {questId} is already completed/failed/abandoned");
                    return false;
                }
            }

            var questDef = LoadQuestDefinition(questId);
            if (questDef == null)
            {
                Debug.LogError($"[QuestService] Quest definition not found: {questId}");
                return false;
            }

            var progress = new QuestProgress
            {
                questId = questId,
                status = QuestStatus.Active,
                startedAtUnix = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                objectives = questDef.objectives.Select(obj => new ObjectiveProgress
                {
                    objectiveId = obj.objectiveId,
                    currentAmount = 0,
                    completed = false
                }).ToList()
            };

            _questProgress[questId] = progress;
            
            // Save first to ensure quest is persisted
            Save();
            
            Debug.Log($"[QuestService] ✅ Started quest: {questId}");
            
            // Publish event AFTER saving to ensure quest is in GetAllProgress()
            if (_eventBus != null)
            {
                var evt = new QuestStateChangedEvent
                {
                    questId = questId,
                    status = QuestStatus.Active
                };
                Debug.Log($"[QuestService] Publishing QuestStateChangedEvent - QuestId: {evt.questId}, Status: {evt.status}");
                _eventBus.Publish(evt);
                Debug.Log($"[QuestService] ✅ QuestStateChangedEvent published");
            }
            else
            {
                Debug.LogError("[QuestService] ❌ Cannot publish QuestStateChangedEvent - _eventBus is null!");
            }

            return true;
        }

        public bool TryFailQuest(string questId)
        {
            if (!_questProgress.TryGetValue(questId, out var progress) || progress.status != QuestStatus.Active)
                return false;

            progress.status = QuestStatus.Failed;
            Save();

            _eventBus?.Publish(new QuestStateChangedEvent
            {
                questId = questId,
                status = QuestStatus.Failed
            });

            return true;
        }

        public bool TryAbandonQuest(string questId)
        {
            if (!_questProgress.TryGetValue(questId, out var progress) || progress.status != QuestStatus.Active)
                return false;

            progress.status = QuestStatus.Abandoned;
            Save();

            _eventBus?.Publish(new QuestStateChangedEvent
            {
                questId = questId,
                status = QuestStatus.Abandoned
            });

            return true;
        }

        public bool IsQuestCompleted(string questId)
        {
            return _questProgress.TryGetValue(questId, out var progress) && progress.status == QuestStatus.Completed;
        }

        public bool IsQuestActive(string questId)
        {
            return _questProgress.TryGetValue(questId, out var progress) && progress.status == QuestStatus.Active;
        }

        public IReadOnlyList<QuestProgress> GetAllProgress()
        {
            return _questProgress.Values.ToList();
        }

        public bool TryGetProgress(string questId, out QuestProgress progress)
        {
            return _questProgress.TryGetValue(questId, out progress);
        }

        public void Save()
        {
            var saveData = new QuestSaveData
            {
                quests = _questProgress.Values.ToList()
            };

            string json = JsonUtility.ToJson(saveData);
            PlayerPrefs.SetString(SAVE_KEY, json);
            PlayerPrefs.Save();
        }

        public void Load()
        {
            if (!PlayerPrefs.HasKey(SAVE_KEY))
                return;

            string json = PlayerPrefs.GetString(SAVE_KEY);
            var saveData = JsonUtility.FromJson<QuestSaveData>(json);

            _questProgress.Clear();
            foreach (var quest in saveData.quests)
            {
                _questProgress[quest.questId] = quest;
            }
        }

        public void ClearAllProgress()
        {
            _questProgress.Clear();
            PlayerPrefs.DeleteKey(SAVE_KEY);
            PlayerPrefs.Save();
        }

        private void OnQuestSignal(QuestSignalEvent signal)
        {
            foreach (var progress in _questProgress.Values.Where(p => p.status == QuestStatus.Active))
            {
                var questDef = LoadQuestDefinition(progress.questId);
                if (questDef == null)
                    continue;

                bool anyUpdated = false;
                bool allCompleted = true;

                foreach (var objProgress in progress.objectives)
                {
                    if (objProgress.completed)
                        continue;

                    var objDef = questDef.objectives.FirstOrDefault(o => o.objectiveId == objProgress.objectiveId);
                    if (objDef == null)
                        continue;

                    if (objDef.signalKey == signal.key && (string.IsNullOrEmpty(objDef.targetId) || objDef.targetId == signal.targetId))
                    {
                        objProgress.currentAmount = Mathf.Min(objProgress.currentAmount + signal.amount, objDef.targetAmount);
                        objProgress.completed = objProgress.currentAmount >= objDef.targetAmount;
                        anyUpdated = true;
                    }

                    if (!objProgress.completed)
                        allCompleted = false;
                }

                if (anyUpdated)
                {
                    if (allCompleted && progress.status == QuestStatus.Active)
                    {
                        progress.status = QuestStatus.Completed;
                        progress.completedAtUnix = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

                        _eventBus?.Publish(new QuestStateChangedEvent
                        {
                            questId = progress.questId,
                            status = QuestStatus.Completed
                        });

                        Debug.Log($"[QuestService] Quest completed: {progress.questId}");
                    }

                    Save();
                }
            }
        }

        private QuestDefinition LoadQuestDefinition(string questId)
        {
            if (_questDefinitions.TryGetValue(questId, out var cached))
                return cached;

            // Try to load from Resources - check both possible locations
            QuestDefinition quest = null;
            
            // First try Data/Quests (matches LevelManager pattern)
            var allQuests = Resources.LoadAll<QuestDefinition>("Data/Quests");
            quest = allQuests.FirstOrDefault(q => q.questId == questId);
            
            // If not found, try Quests (legacy location)
            if (quest == null)
            {
                allQuests = Resources.LoadAll<QuestDefinition>("Quests");
                quest = allQuests.FirstOrDefault(q => q.questId == questId);
            }
            
            if (quest != null)
            {
                _questDefinitions[questId] = quest;
                Debug.Log($"[QuestService] Loaded quest definition: {questId} from Resources");
            }
            else
            {
                Debug.LogError($"[QuestService] Quest definition not found: {questId}. Searched in Resources/Data/Quests and Resources/Quests");
            }

            return quest;
        }
    }
}