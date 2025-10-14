using System;
using System.Collections.Generic;
using System.Text;
using Best.HTTP;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.Serialization;

namespace MainMenu.Core
{
    public class HttpManager : MonoBehaviour
    {
        public static HttpManager Instance { get; private set; }

        public string baseURL = "http://localhost:5001/api/";

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        public void Send(HttpRequestDefinition def, Action<string> onSuccess, Action<string> onError)
        {
            StartCoroutine(SendCoroutine(def, onSuccess, onError));
        }

        private IEnumerator<UnityWebRequestAsyncOperation> SendCoroutine(HttpRequestDefinition def,
            Action<string> onSuccess, Action<string> onError)
        {
            var url = baseURL + def.url;
            if (!string.IsNullOrEmpty(def.queryParameters))
                url += "?" + def.queryParameters;

            UnityWebRequest request;

            switch (def.method)
            {
                case HTTPMethods.Post:
                case HTTPMethods.Put:
                    var bodyRaw = Encoding.UTF8.GetBytes(def.bodyJson ?? string.Empty);
                    request = new UnityWebRequest(url, def.method.ToString().ToUpper());
                    request.method = def.method.ToString().ToUpper();
                    request.uploadHandler = new UploadHandlerRaw(bodyRaw);
                    request.downloadHandler = new DownloadHandlerBuffer();
                    request.SetRequestHeader("Content-Type", "application/json");
                    break;
                case HTTPMethods.Delete:
                    request = UnityWebRequest.Delete(url);
                    break;
                case HTTPMethods.Get:
                case HTTPMethods.Head:
                case HTTPMethods.Patch:
                case HTTPMethods.Trace:
                case HTTPMethods.Merge:
                case HTTPMethods.Options:
                case HTTPMethods.Connect:
                case HTTPMethods.Query:
                default:
                    request = UnityWebRequest.Get(url);
                    break;
            }

            foreach (var h in def.headers)
                request.SetRequestHeader(h.key, h.value);

            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
                onSuccess?.Invoke(request.downloadHandler.text);
            else
                onError?.Invoke(request.error);
        }

        public void SendJson<TReq, TRes>(HttpRequestDefinition def, TReq payload, Action<TRes> onSuccess,
            Action<string> onError)
        {
            def.bodyJson = JsonConvert.SerializeObject(payload);
            Send(def,
                respText =>
                {
                    try
                    {
                        onSuccess(JsonUtility.FromJson<TRes>(respText));
                    }
                    catch (Exception e)
                    {
                        onError?.Invoke(e.Message);
                    }
                },
                err => onError?.Invoke(err)
            );
        }
    }

    [Serializable]
    public class ApiResponse<T>
    {
        public bool Success;

        public string Message;

        public T Data;

        public List<string> Errors;
    }


    [Serializable]
    public class EmptyData
    {
    }
}