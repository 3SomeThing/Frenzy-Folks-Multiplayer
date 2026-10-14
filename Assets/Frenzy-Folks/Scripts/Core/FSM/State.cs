namespace Frenzy_Folks.Scripts.Core.FSM
{
    public abstract class State {

        protected FSM Fsm;
        
        public virtual void Init(FSM fsm) { Fsm = fsm; }
        public virtual void Enter() {}
        public virtual void Update() {}
        public virtual void FixedUpdate() {}
        public virtual void Exit() {}

    }
}
