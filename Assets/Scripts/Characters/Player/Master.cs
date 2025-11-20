using Core.TurnManagement;
using UnityEngine;

namespace Characters.Player{

    [DefaultExecutionOrder(-100)]
    public class Master : MonoBehaviour{
        public static Controller       Controller => _instance.controller;
        public static Transform        Entity     => _instance.entity;
        public static Animator         Animator   => _instance.animator;
        public static Compass          Compass    => _instance.compass;
        public static TurnUser         TurnUser   => _instance.turnUser;
        public static ActionController Actions    => _instance.actionController;
        public static CharacterSheet   Sheet      => _instance.characterSheet;

        private static Master _instance;
        
        [SerializeField] private Animator         animator;
        [SerializeField] private Transform        entity;
        [SerializeField] private Controller       controller;
        [SerializeField] private Compass          compass;
        [SerializeField] private TurnUser         turnUser;
        [SerializeField] private ActionController actionController;
        [SerializeField] private CharacterSheet   characterSheet;

        public void Awake(){
            _instance  = this;

            foreach (Component component in GetComponents<Component>())
                component.Initialize(this);
            
            foreach (Slave behaviour in animator.GetBehaviours<Slave>())
                behaviour.Initialize(this);
        }
    }
}
