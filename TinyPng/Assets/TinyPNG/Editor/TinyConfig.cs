using System.Collections.Generic;
using Sirenix.OdinInspector;
using TinyPng;
using UnityEngine;

namespace ARK.EditorTools.TinyPNG
{
    public class TinyConfig : ScriptableObject
    {

        [ShowInInspector]
        public List<TinyAccount> TinyAccounts;

     
    }
}