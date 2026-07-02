using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TopDownRoguelike.Gameplay.UI
{
    public class PlayerWorldUIFollow : MonoBehaviour
    {
        [SerializeField] private Transform target;
        [SerializeField] private Vector3 offset = new Vector3(0f, 1f, 0f);

        private void LateUpdate()
        {
            if (target == null)
            {
                return;
            }

            transform.position = target.position + offset;
            transform.rotation = Quaternion.identity;
        }
    }
}