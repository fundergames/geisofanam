using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace FunderGames.RPG
{
    public class CombatSystem : MonoBehaviour
    {
        public ActionMenu actionMenu;
        public TargetSelectionMenu targetSelectionMenu;
        public GameObject combatantPrefab;

        [Header("Heros Information")]
        private List<Combatant> heroes = new();
        [SerializeField] private HeroData[] heroDataArray;  
        [SerializeField] private Transform[] heroSpawnPoints; 
        
        [Header("Enemies Information")]
        private List<Combatant> enemies = new();
        [SerializeField] private HeroData[] enemyDataArray;  
        [SerializeField] private Transform[] enemySpawnPoints;

        private int currentTurnIndex = 0;

        public void Start()
        {
            SetupCombatants();
            StartCoroutine(BattleLoop());
        }
        
        private void SetupCombatants()
        {
            for (var i = 0; i < heroDataArray.Length; i++)
            {
                // Instantiate a new Combatant at the given spawn point
                var newCombatantObject = Instantiate(combatantPrefab, heroSpawnPoints[i].transform);
                newCombatantObject.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);

                // Get the Combatant component and initialize it from HeroData
                var combatant = newCombatantObject.GetComponent<Combatant>();
                combatant.InitializeFromHeroData(heroDataArray[i], i);
                
                var stencil = newCombatantObject.GetComponent<StencilController>();
                stencil.SetStencilID(i+1);
                heroes.Add(combatant);
            }
            
            for (var i = 0; i < enemyDataArray.Length; i++)
            {
                // Instantiate a new Combatant at the given spawn point
                var newCombatantObject = Instantiate(combatantPrefab, enemySpawnPoints[i].transform);
                newCombatantObject.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);

                // Get the Combatant component and initialize it from HeroData
                var combatant = newCombatantObject.GetComponent<Combatant>();
                combatant.InitializeFromHeroData(enemyDataArray[i]);
                
                var stencil = newCombatantObject.GetComponent<StencilController>();
                stencil.SetStencilID(heroDataArray.Length + i + 1);
                enemies.Add(combatant);
            }
        }

        private IEnumerator BattleLoop()
        {
            while (true)
            {
                var currentCombatant = heroes[currentTurnIndex];
                Debug.Log($"{currentCombatant.Name}'s Turn");

                // Show the action menu for the current combatant
                actionMenu.DisplayActionsForCharacter(currentCombatant);

                CombatAction selectedAction = null;
                Combatant selectedTarget = null;

                // Handle action selection
                System.Action<CombatAction> actionSelectedHandler = (action) =>
                {
                    selectedAction = action;
                    actionMenu.HideMenu();
                };
                ActionMenu.OnActionSelected += actionSelectedHandler;

                // Wait for an action to be selected
                yield return new WaitUntil(() => selectedAction != null);
                ActionMenu.OnActionSelected -= actionSelectedHandler;

                // If the selected action requires a target
                if (selectedAction.RequiresTarget)
                {
                    // Show the target selection menu
                    targetSelectionMenu.DisplayTargets(GetValidTargets(selectedAction, currentCombatant));

                    // Handle target selection
                    System.Action<Combatant> targetSelectedHandler = (target) =>
                    {
                        selectedTarget = target;
                        targetSelectionMenu.HideMenu();
                    };
                    TargetSelectionMenu.OnTargetSelected += targetSelectedHandler;

                    yield return new WaitUntil(() => selectedTarget != null);
                    TargetSelectionMenu.OnTargetSelected -= targetSelectedHandler;

                    // Execute the selected action's sequence
                    yield return StartCoroutine(currentCombatant.PerformAction(selectedAction, selectedTarget));
                }
                else
                {
                    // Perform the action that doesn't require a target
                    yield return StartCoroutine(currentCombatant.PerformAction(selectedAction, null));
                }

                // Move to the next turn after action completion
                currentTurnIndex = (currentTurnIndex + 1) % heroes.Count;
            }
        }

        private List<Combatant> GetValidTargets(CombatAction action, Combatant performer)
        {
            return action.TargetType == TargetType.Enemy ? enemies : heroes;
        }
    }
}