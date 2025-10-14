using System;
using GamePlay.UnityClasses.Hub;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace GamePlay.UnityClasses
{
    public class GameUIManager : MonoBehaviour
    {
        public static GameUIManager Instance;
        public GameObject winPanel;
        public GameObject loosePanel;
        public ManaBar manaBar;
        public CardUI cardUI;
        public TextMeshProUGUI userNameText;
        private GameHubBase _gameHubBase;
        private string _userName;
        private const string UserNameKey = "user_name";

        private void Awake()
        {
            Instance = this;
            _gameHubBase = FindObjectOfType<GameHubBase>();
            _userName = PlayerPrefs.GetString(UserNameKey, string.Empty);
        }

        private void Update()
        {
            userNameText.text = "User " + (_gameHubBase.playerId + 1) + " : " + _userName;
        }

        public void BackToMainMenu()
        {
            SceneManager.LoadScene("LoginScene");
        }
    }
}