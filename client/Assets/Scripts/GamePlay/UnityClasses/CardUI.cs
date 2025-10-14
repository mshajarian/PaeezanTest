using System;
using System.Collections.Generic;
using GamePlay.UnityClasses.Hub;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GamePlay.UnityClasses
{
    public class CardUI : MonoBehaviour
    {
        [SerializeField] private GamePlayController gamePlayController;
        [SerializeField] private List<UnitUIButton> unitUis;

        private GameHubBase signalR;

        private void Start()
        {
            signalR = FindObjectOfType<GameHubBase>();

            foreach (var unit in unitUis)
            {
                unit.button.onClick.AddListener(() => Deploy(unit.unitType));
            }
        }


        private void Deploy(UnitType type)
        {
            // var canDeploy = gamePlayController.PredictDeploy(type);
            // if (canDeploy)
                signalR.DeployUnit(type);
        }

        public void UpdateCooldown(Dictionary<UnitType, float> cds, float currentMana)
        {
            foreach (var unit in unitUis)
            {
                var cd = cds[unit.unitType];
                unit.cdText.gameObject.SetActive(cd > 0);
                unit.cdText.text = cd.ToString("F1");
                unit.button.interactable = cd <= 0 && currentMana >= gamePlayController.GetUnitCost(unit.unitType);
            }
        }

        [Serializable]
        public class UnitUIButton
        {
            public UnitType unitType;
            public Button button;
            public TextMeshProUGUI cdText;
        }
    }
}