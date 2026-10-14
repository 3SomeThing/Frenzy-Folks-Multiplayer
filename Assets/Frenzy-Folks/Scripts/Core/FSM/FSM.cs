using System.Collections.Generic;
using UnityEngine;

namespace Frenzy_Folks.Scripts.Core.FSM
{
    public abstract class FSM : MonoBehaviour {
        protected string CurrentState = "";
        protected readonly Dictionary<string, State> States = new();
        public Dictionary<string, object> Data = new();

        protected virtual void Start() {
            foreach ((string k, State s) in States) {
                s.Init(this);
                if (k == CurrentState)
                    s.Enter();
            }
        }

        void Update() {
            if (CurrentState != string.Empty)
                States[CurrentState].Update();
        }

        void FixedUpdate() {
            if (CurrentState != string.Empty)
                States[CurrentState].FixedUpdate();
        }

        public void TransitionTo(string state) {
            if (CurrentState != string.Empty)
                States[CurrentState].Exit();
            CurrentState = state;
            if (CurrentState != string.Empty)
                States[CurrentState].Enter();
        }

    }
}