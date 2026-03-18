using System.Collections;
using UnityEngine;

namespace FunderGames.RPG
{
    public abstract class ActionStep : ScriptableObject
    {
        public abstract IEnumerator Execute(Combatant performer, Combatant target);
    }
}