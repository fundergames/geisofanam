using UnityEngine;
using Funder.Core.Services;
using Funder.GameFlow;
using System.Threading.Tasks;
using RogueDeal.Player;

namespace RogueDeal.CharacterSelect
{
    public class CharacterSelectBootstrap : MonoBehaviour
    {
        [Header("Hero Selection")]
        [SerializeField] private CharacterSelectionView selectionView;

        private async void Start()
        {
            await WaitForBootstrap();
            InitializeCharacterSelect();
        }

        private async Task WaitForBootstrap()
        {
            while (!GameBootstrap.IsInitialized)
            {
                await Task.Yield();
            }
        }

        private void InitializeCharacterSelect()
        {
            if (selectionView != null)
            {
                selectionView.Initialize();
            }
            else
            {
                Debug.LogError("[CharacterSelectBootstrap] CharacterSelectionView not assigned!");
            }
        }
    }
}
