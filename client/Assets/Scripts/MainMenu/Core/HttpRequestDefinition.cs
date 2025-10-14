using System;
using System.Collections.Generic;
using Best.HTTP;
using UnityEngine;

namespace MainMenu.Core
{
    [CreateAssetMenu(fileName = "HttpRequestDefinition", menuName = "Networking/Http Request Definition")]
    public class HttpRequestDefinition : ScriptableObject
    {
        public string requestName;
        public string url;
        public HTTPMethods method = HTTPMethods.Get;
        public List<Header> headers = new List<Header>();
        public string queryParameters;
        public string bodyJson;

        [Serializable]
        public struct Header
        {
            public string key;
            public string value;
        }
    }
}