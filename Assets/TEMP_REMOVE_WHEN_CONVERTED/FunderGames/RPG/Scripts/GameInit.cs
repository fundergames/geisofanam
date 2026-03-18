using System.Threading.Tasks;
using FunderGames.UI;
using UnityEngine;

public class GameInit : MonoBehaviour
{
    private async void Start()
    {
        // Wait for GameManager initialization
        while (!UIWindowManager.Instance.IsInitialized)
        {
            await Task.Yield();
        }
        InitializeLogic();
    }

    private void InitializeLogic()
    {
        UIWindowManager.Instance.ShowWindow("Login");
    }
}
