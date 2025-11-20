using Core.TurnManagement;
using UnityEngine;

namespace Characters.Player{
    public interface ISlave{
        public Master           Master    { get; set; }
        public Transform        Entity    { get; set; }
        public Controller       Controller{ get; set; }
        public Animator         Animator  { get; set; }
        public Compass          Compass   { get; set; }
        public TurnUser         TurnUser  { get; set; }
        public ActionController Actions   { get; set; }
        public CharacterSheet   Sheet     { get; set; }
    }

    public static class SlaveExtensions{
        public static void Initialize(this ISlave slave, Master master){
            slave.Master     = master;
            slave.Entity     = Master.Entity;
            slave.Controller = Master.Controller;
            slave.Animator   = Master.Animator;
            slave.Compass    = Master.Compass;
            slave.TurnUser   = Master.TurnUser;
            slave.Actions    = Master.Actions;
            slave.Sheet      = Master.Sheet;
        }
    }

    public abstract class Slave : StateMachineBehaviour, ISlave{
        public Master           Master    { get; set; }
        public Transform        Entity    { get; set; }
        public Controller       Controller{ get; set; }
        public Animator         Animator  { get; set; }
        public Compass          Compass   { get; set; }
        public TurnUser         TurnUser  { get; set; }
        public ActionController Actions   { get; set; }
        public CharacterSheet   Sheet     { get; set; }
    }

    public abstract class Component : MonoBehaviour, ISlave{
        public Master           Master    { get; set; }
        public Transform        Entity    { get; set; }
        public Controller       Controller{ get; set; }
        public Animator         Animator  { get; set; }
        public Compass          Compass   { get; set; }
        public TurnUser         TurnUser  { get; set; }
        public ActionController Actions   { get; set; }
        public CharacterSheet   Sheet     { get; set; }
    }
}
