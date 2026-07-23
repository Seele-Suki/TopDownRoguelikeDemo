using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TopDownRoguelike.Gameplay.Upgrades
{
    [CreateAssetMenu(fileName = "UpgradeData", menuName = "TopDown Roguelike/Upgrade Data")]
    public class UpgradeData : ScriptableObject
    {
        [SerializeField] private string upgradeName;
        [TextArea]
        [SerializeField] private string description;
        [SerializeField] private UpgradeType upgradeType;
        [SerializeField] private float floatValue;
        [SerializeField] private int intValue;

        public string UpgradeName => upgradeName;
        public string Description => description;
        public UpgradeType UpgradeType => upgradeType;
        public float FloatValue => floatValue;
        public int IntValue => intValue;
    }
}