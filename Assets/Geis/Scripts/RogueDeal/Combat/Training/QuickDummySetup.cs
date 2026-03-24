using UnityEngine;

namespace RogueDeal.Combat.Training
{
    public class QuickDummySetup : MonoBehaviour
    {
        [ContextMenu("Setup as Training Dummy")]
        public void SetupAsTrainingDummy()
        {
            CombatEntity entity = GetComponent<CombatEntity>();
            if (entity == null)
            {
                entity = gameObject.AddComponent<CombatEntity>();
                Debug.Log($"Added CombatEntity to {gameObject.name}");
            }
            
            TrainingDummy dummy = GetComponent<TrainingDummy>();
            if (dummy == null)
            {
                dummy = gameObject.AddComponent<TrainingDummy>();
                Debug.Log($"Added TrainingDummy to {gameObject.name}");
            }
            
            entity.InitializeStatsWithoutHeroData(1000f, 10f, 5f);
            
            Debug.Log($"✓ {gameObject.name} is now a Training Dummy!");
        }
    }
}
