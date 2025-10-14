using System;
using System.Collections;
using System.Threading.Tasks;
using DG.Tweening;
using GamePlay.Shared;
using GamePlay.UnityClasses;
using GamePlay.UnityClasses.Hub;
using MainMenu.Core;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MainMenu.LoginPage
{
    public class MainMenuController : MonoBehaviour
    {
        [Header("UI References")] [SerializeField]
        private GameObject loginCanvas;

        [SerializeField] private GameObject matchmakingCanvas;
        [SerializeField] private TextMeshProUGUI errorText;
        [SerializeField] private GameObject loadingScreen;
        [SerializeField] private TMP_InputField usernameInputField;
        [SerializeField] private TMP_InputField passwordInputField;
        [SerializeField] private TMP_InputField joinCodeInput;
        [SerializeField] private TextMeshProUGUI roomCodeText;
        [SerializeField] private TextMeshProUGUI userNameText;

        [Header("HTTP Requests")] [SerializeField]
        private HttpRequestDefinition loginHttpRequest;

        [SerializeField] private HttpRequestDefinition registrationHttpRequest;

        private const string JwtKey = "jwt_token";
        private const string UserNameKey = "user_name";
        private GameHubBase _gameHubBase;

        private void Start()
        {
            _gameHubBase = FindObjectOfType<GameHubBase>();
            string jwt = PlayerPrefs.GetString(JwtKey, string.Empty);
            ActivateProperCanvas(!string.IsNullOrEmpty(jwt));
            _gameHubBase.Error += HubError;
            _gameHubBase.InitState += SetInitState;
            _gameHubBase.RoomCreated += RoomCreated;
        }

        private void OnDisable()
        {
            _gameHubBase.Error -= HubError;
            _gameHubBase.RoomCreated -= RoomCreated;
            _gameHubBase.InitState -= SetInitState;
        }


        private void ActivateProperCanvas(bool hasJwt)
        {
            userNameText.text = PlayerPrefs.GetString(UserNameKey, string.Empty);
            if (hasJwt)
            {
                loginCanvas.gameObject.SetActive(false);
                matchmakingCanvas.gameObject.SetActive(true);
                loadingScreen.SetActive(true);
                if (!_gameHubBase.connected)
                    _gameHubBase.Connect();
                StartCoroutine(SendCreateRoom());
            }
            else
            {
                loginCanvas.gameObject.SetActive(true);
                matchmakingCanvas.gameObject.SetActive(false);
                loadingScreen.SetActive(false);
            }
        }

        private IEnumerator SendCreateRoom()
        {
            yield return new WaitUntil(() => _gameHubBase.connected);
            _gameHubBase.SendCreateRoom();
        }


        private void HubError(string obj)
        {
            ShowError(obj);
            loadingScreen.SetActive(false);
        }


        private void RoomCreated(string code)
        {
            roomCodeText.text = code;
            loadingScreen.SetActive(false);
        }

        private void SetInitState(GameState obj)
        {
            GamePlayController.LocalState = obj;
            SceneManager.LoadScene("GamePlay");
        }

        public void Login()
        {
            if (passwordInputField.text.Length < 3)
            {
                ShowError("Password must be at least 3 characters long.");
                return;
            }

            if (usernameInputField.text.Length < 3)
            {
                ShowError("Username must be at least 3 characters long.");
                return;
            }

            PlayerPrefs.SetString(UserNameKey, usernameInputField.text);

            loadingScreen.SetActive(true);

            var payload = new LoginRequest
            {
                Username = usernameInputField.text,
                Password = passwordInputField.text
            };

            HttpManager.Instance.SendJson<LoginRequest, AuthResponse>(
                loginHttpRequest,
                payload,
                onSuccess: OnSuccessLogin,
                onError: errorMsg =>
                {
                    loadingScreen.SetActive(false);
                    ShowError($"Error: {errorMsg}");
                }
            );
        }

        public void Register()
        {
            if (passwordInputField.text.Length < 3)
            {
                ShowError("Password must be at least 3 characters long.");
                return;
            }

            if (usernameInputField.text.Length < 3)
            {
                ShowError("Username must be at least 3 characters long.");
                return;
            }

            PlayerPrefs.SetString(UserNameKey, usernameInputField.text);


            loadingScreen.SetActive(true);

            var payload = new RegisterRequest
            {
                Username = usernameInputField.text,
                Password = passwordInputField.text
            };

            HttpManager.Instance.SendJson<RegisterRequest, AuthResponse>(
                registrationHttpRequest,
                payload,
                onSuccess: OnSuccessRegister,
                onError: errorMsg =>
                {
                    loadingScreen.SetActive(false);
                    ShowError($"Error: {errorMsg}");
                }
            );
        }

        public void Join()
        {
            if (joinCodeInput.text.Length < 3)
            {
                ShowError("code must be at least 3 characters long.");
                return;
            }

            loadingScreen.SetActive(false);

            _gameHubBase.SendJoinRoom(joinCodeInput.text);
        }

        private async void OnSuccessLogin(AuthResponse res)
        {
            await HandleAuthResponse(res, "Login");
        }

        private async void OnSuccessRegister(AuthResponse res)
        {
            await HandleAuthResponse(res, "Registration");
        }

        private async Task HandleAuthResponse(AuthResponse res, string action)
        {
            await Task.Delay(300);
            loadingScreen.SetActive(false);

            if (res == null)
            {
                ShowError($"{action} failed: Empty response");
                return;
            }

            if (res.success && !string.IsNullOrEmpty(res.token))
            {
                PlayerPrefs.SetString(JwtKey, res.token);
                PlayerPrefs.Save();

                ActivateProperCanvas(true);
            }
            else
            {
                ShowError($"{action} failed: {res.message}");
            }
        }

        public void Logout()
        {
            PlayerPrefs.DeleteKey(JwtKey);
            ActivateProperCanvas(false);
        }


        public void ShowError(string message)
        {
            errorText.text = message;
            var col = errorText.color;
            col.a = 0;
            errorText.color = col;
            errorText.gameObject.SetActive(true);

            var seq = DOTween.Sequence();

            seq.Append(DOTween.To(
                () => errorText.color.a,
                a =>
                {
                    var c = errorText.color;
                    c.a = a;
                    errorText.color = c;
                },
                1f,
                0.3f
            ).SetEase(Ease.OutQuad));

            seq.Append(errorText.transform
                .DOPunchScale(Vector3.one * 0.2f, 0.4f, 10, 0.8f)
                .SetEase(Ease.OutElastic)
            );

            seq.AppendInterval(2f);

            seq.Append(DOTween.To(
                () => errorText.color.a,
                a =>
                {
                    var c = errorText.color;
                    c.a = a;
                    errorText.color = c;
                },
                0f,
                0.5f
            ).SetEase(Ease.InQuad));

            seq.OnComplete(() => errorText.gameObject.SetActive(false));
        }
    }

    [Serializable]
    public class AuthResponse
    {
        public bool success;
        public string token;
        public string message;
    }


    [Serializable]
    public class LoginRequest
    {
        public string Username;
        public string Password;
    }

    [Serializable]
    public class RegisterRequest
    {
        public string Username;
        public string Password;
    }
}