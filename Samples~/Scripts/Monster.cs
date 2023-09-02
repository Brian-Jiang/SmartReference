using System.Collections.Generic;
using SmartReference.Runtime;
using UnityEngine;

namespace SmartReference.Samples {
    public class Monster: MonoBehaviour {
        public int id;
        public string monsterName;
        public SmartReference<GameObject> prefab;
        public SmartReference<Sprite> icon;
        public SmartReference<GameObject>[] skillEffects;
    }
}