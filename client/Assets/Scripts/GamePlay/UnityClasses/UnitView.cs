using UnityEngine;
using DG.Tweening; 

namespace GamePlay.UnityClasses
{
    public class UnitView : ViewBase
    {
        [Header("Unit Settings")]
        public int id;
        public float lastPosition;
        public float lastSpeed;
        public bool isAttacking;
        public bool wasMoving;
        public float? targetPos;

        [Header("Visuals")]
        public SkinnedMeshRenderer sr;
        public Animator characterAnimator;
        private static readonly int Attacking = Animator.StringToHash("attacking");
        private readonly Color attackingColor = new Color(1, 0.9f, 0.9f);

        [Header("Projectile Settings")]
        public GameObject projectilePrefab;
        public Transform projectileSpawnPoint;
        public float projectileSpeed = 10f; // units per second
        public Ease projectileEase = Ease.OutQuad;
        public float arcHeight = 1.5f; // height of the projectile curve

        private bool hasFiredThisAttack;

        public new void UpdateView()
        {
            base.UpdateView();
            characterAnimator.SetBool(Attacking, isAttacking);

            if (isAttacking)
            {
                sr.material.color = Color.Lerp(sr.material.color, attackingColor, Time.deltaTime * 5);

                if (targetPos.HasValue && !hasFiredThisAttack)
                {
                    ThrowProjectile(targetPos.Value);
                    hasFiredThisAttack = true;
                }
            }
            else
            {
                sr.material.color = Color.Lerp(sr.material.color, Color.white, Time.deltaTime * 5);
                hasFiredThisAttack = false;
            }

            var currentTransform = transform;
            var pos = currentTransform.position;
            pos.x = lastPosition;
            currentTransform.position = pos;
        }

        private void ThrowProjectile(float targetX)
        {
            if (!projectilePrefab || !projectileSpawnPoint)
            {
                Debug.LogWarning($"Projectile setup missing on {name}.");
                return;
            }

            Vector3 startPos = projectileSpawnPoint.position;
            Vector3 targetPos3D = new Vector3(targetX, startPos.y, startPos.z);
            float distance = Vector3.Distance(startPos, targetPos3D);
            float duration = distance / projectileSpeed;

            // Instantiate projectile
            GameObject projectile = Instantiate(projectilePrefab, startPos, Quaternion.identity);

            // Optional: scale pop-in
            projectile.transform.localScale = Vector3.zero;
            projectile.transform.DOScale(1f, 0.15f).SetEase(Ease.OutBack);

            // Create a smooth arc using a DOPath with mid-height control point
            Vector3 midPoint = (startPos + targetPos3D) / 2f + Vector3.up * arcHeight;
            Vector3[] path = { startPos, midPoint, targetPos3D };

            projectile.transform.DOPath(path, duration, PathType.CatmullRom)
                .SetEase(projectileEase)
                .OnComplete(() =>
                {
                    // Optional: impact effect
                    projectile.transform.DOScale(0f, 0.2f).SetEase(Ease.InBack);
                    hasFiredThisAttack = false;
                    Destroy(projectile, 0.25f);
                });
        }
    }
}
