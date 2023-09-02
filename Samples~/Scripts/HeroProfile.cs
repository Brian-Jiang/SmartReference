using System;
using System.Collections.Generic;
using SmartReference.Runtime;
using UnityEngine;

namespace SmartReference.Samples {
    [Serializable]
    public class AttackData {
        public float baseDamage;
        public float damageMultiplier;
        public SmartReference<GameObject> effect;
    }
    
    [CreateAssetMenu(fileName = "HeroProfile", menuName = "SmartReference/Samples/HeroProfile")]
    public class HeroProfile: ScriptableObject {
        public int id;
        public string heroName;
        public SmartReference<GameObject> prefab;
        public SmartReference<Texture2D> icon;
        public AttackData attackData;
        public List<SmartReference<GameObject>> skillEffects;
    }
}