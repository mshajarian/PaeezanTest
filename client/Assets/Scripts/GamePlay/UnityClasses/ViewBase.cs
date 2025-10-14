using System.Globalization;
using DG.Tweening;
using TMPro;
using UnityEngine;

namespace GamePlay.UnityClasses
{
    public abstract class ViewBase : MonoBehaviour
    {
        [SerializeField] private GameObject destroyParticle;
        [SerializeField] private TextMeshPro hpText;
        [SerializeField] private SpriteRenderer healthFill;

        public int owner;
        public float hp;
        public float maxHp;


        public void UpdateView()
        {
        
            if (hp <= 0)
            {
                Debug.Log($"Tower {owner} destroyed");
                if (destroyParticle != null)
                {
                    var particle = Instantiate(destroyParticle, transform);
                    particle.SetActive(true);
                    particle.transform.SetParent(null);
                }

                DOVirtual.DelayedCall(.3f, () =>
                {
                    if (hp > 0) return;
                    if (gameObject != null)
                        Destroy(gameObject);
                });
            }
            else
            {
                if(maxHp > 0.1f)
                 healthFill.transform.localScale = new Vector3(hp/maxHp,1,1);
                hpText.text = hp.ToString("F1");
            }
        }
    }
}