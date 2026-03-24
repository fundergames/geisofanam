using RogueDeal.Combat;
using UnityEngine;

namespace RogueDeal.Player
{
    public class PlayerDataManager
    {
        private const string PLAYER_DATA_KEY = "PlayerData";
        private const string ENERGY_KEY = "PlayerEnergy";
        private const string LAST_ENERGY_UPDATE_KEY = "LastEnergyUpdate";
        
        private static PlayerDataManager instance;
        public static PlayerDataManager Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new PlayerDataManager();
                }
                return instance;
            }
        }

        private PlayerCharacter currentPlayer;
        private int currentEnergy = 10;
        private int maxEnergy = 10;
        private float energyRegenMinutes = 10f;

        public PlayerCharacter CurrentPlayer => currentPlayer;
        public int CurrentEnergy => currentEnergy;
        public int MaxEnergy => maxEnergy;

        public void InitializePlayer(ClassDefinition classDefinition, string playerName)
        {
            currentPlayer = new PlayerCharacter(classDefinition, playerName);
            currentEnergy = maxEnergy;
            SavePlayerData();
        }

        public void SetCurrentPlayer(PlayerCharacter player)
        {
            currentPlayer = player;
        }

        public bool SpendEnergy(int amount)
        {
            if (currentEnergy < amount)
                return false;

            currentEnergy -= amount;
            SaveEnergy();
            return true;
        }

        public void AddEnergy(int amount)
        {
            currentEnergy += amount;
            if (currentEnergy > maxEnergy)
                currentEnergy = maxEnergy;
            SaveEnergy();
        }

        public void UpdateEnergyRegen()
        {
            if (currentEnergy >= maxEnergy)
                return;

            string lastUpdateStr = PlayerPrefs.GetString(LAST_ENERGY_UPDATE_KEY, "");
            if (string.IsNullOrEmpty(lastUpdateStr))
            {
                SaveLastEnergyUpdate();
                return;
            }

            if (System.DateTime.TryParse(lastUpdateStr, out System.DateTime lastUpdate))
            {
                System.TimeSpan elapsed = System.DateTime.Now - lastUpdate;
                int energyToAdd = Mathf.FloorToInt((float)elapsed.TotalMinutes / energyRegenMinutes);

                if (energyToAdd > 0)
                {
                    AddEnergy(energyToAdd);
                    SaveLastEnergyUpdate();
                }
            }
        }

        private void SaveEnergy()
        {
            PlayerPrefs.SetInt(ENERGY_KEY, currentEnergy);
            PlayerPrefs.Save();
        }

        private void SaveLastEnergyUpdate()
        {
            PlayerPrefs.SetString(LAST_ENERGY_UPDATE_KEY, System.DateTime.Now.ToString());
            PlayerPrefs.Save();
        }

        public void SavePlayerData()
        {
            if (currentPlayer == null)
                return;

            var saveData = new PlayerSaveData
            {
                characterName = currentPlayer.characterName,
                classType = currentPlayer.classDefinition.classType,
                level = currentPlayer.level,
                currentXP = currentPlayer.currentXP,
                currentHealth = currentPlayer.effectiveStats.currentHealth,
                gold = currentPlayer.inventory.Gold
            };

            string json = JsonUtility.ToJson(saveData);
            PlayerPrefs.SetString(PLAYER_DATA_KEY, json);
            SaveEnergy();
            PlayerPrefs.Save();
        }

        public void LoadPlayerData(ClassDefinition[] availableClasses)
        {
            if (!PlayerPrefs.HasKey(PLAYER_DATA_KEY))
                return;

            string json = PlayerPrefs.GetString(PLAYER_DATA_KEY);
            var saveData = JsonUtility.FromJson<PlayerSaveData>(json);

            var classDef = System.Array.Find(availableClasses, c => c.classType == saveData.classType);
            if (classDef == null)
                return;

            currentPlayer = new PlayerCharacter(classDef, saveData.characterName)
            {
                level = saveData.level,
                currentXP = saveData.currentXP
            };

            currentPlayer.effectiveStats.currentHealth = saveData.currentHealth;
            currentPlayer.inventory.AddGold(saveData.gold);

            currentEnergy = PlayerPrefs.GetInt(ENERGY_KEY, maxEnergy);
            UpdateEnergyRegen();
        }
    }

    [System.Serializable]
    public class PlayerSaveData
    {
        public string characterName;
        public CharacterClass classType;
        public int level;
        public int currentXP;
        public int currentHealth;
        public int gold;
    }
}
