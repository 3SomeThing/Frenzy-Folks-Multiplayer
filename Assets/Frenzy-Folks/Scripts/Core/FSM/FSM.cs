using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

namespace Frenzy_Folks.Scripts.Core.FSM
{
    public abstract class FSM : NetworkBehaviour {
        protected string CurrentState = "";
        protected readonly Dictionary<string, State> States = new();
        [NonSerialized]
        public Dictionary<string, object> Data = new();

        protected virtual void Start() {
            foreach ((string k, State s) in States) {
                s.Init(this);
                if (k == CurrentState)
                    s.Enter();
            }
        }

        void Update() {
            if (IsSpawned && !IsServer)
                return;

            if (CurrentState != string.Empty)
                States[CurrentState].Update();
        }

        void FixedUpdate() {
            if (IsSpawned && !IsServer)
                return;

            if (CurrentState != string.Empty)
                States[CurrentState].FixedUpdate();
        }

        public void TransitionTo(string state) {
            if (IsSpawned && !IsServer)
                return;

            if (CurrentState != string.Empty)
                States[CurrentState].Exit();
            CurrentState = state;
            if (CurrentState != string.Empty)
                States[CurrentState].Enter();
        }

    }
}