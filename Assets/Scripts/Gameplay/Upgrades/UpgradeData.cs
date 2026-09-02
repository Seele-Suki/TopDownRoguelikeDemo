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
        [SerializeField, Min(1)] private ushort upgradeId;
        [SerializeField] private UpgradeType upgradeType;
        [SerializeField] private float floatValue;
        [SerializeField] private int intValue;

        public string UpgradeName => upgradeName;
        public string Description => description;
        public ushort UpgradeId => upgradeId;
        public UpgradeType UpgradeType => upgradeType;
        public float FloatValue => floatValue;
        public int IntValue => intValue;
    }
}
