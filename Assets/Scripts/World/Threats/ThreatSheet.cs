using System;
using System.Collections.Generic;
using Core.Crisis;
using Core.State;
using UnityEngine;
using UnityEngine.AI;
using World.Actors;
using World.Actors.Player;
using World.Tactical;

namespace World.Threats{
    [Serializable]
    public struct ThreatAttributeEntry{
        public string key;
        public float value;

        public ThreatAttributeEntry(string key, float value){
            this.key = key;
            this.value = value;
        }
    }

    /// Master threat representation in the world for environments, monsters, and swarms.
    [DisallowMultipleComponent]
    public class ThreatSheet : Sheet{
        [Header("Universal Threat Attributes")]
        [SerializeField] private VitalityPool integrity = new("threat_integrity", 10);
        [SerializeField] private Tracked<int> threatLevel = new("threat_level", 1);
        [SerializeField] private Tracked<int> escalation = new("threat_escalation", 0);
        [SerializeField] private Tracked<int> unitCount = new("threat_unit_count", 1);
        [SerializeField] private Tracked<int> actionPoints = new("threat_ap", 1);

        [Header("Tactical Areas / Zones")]
        [SerializeField] private TacticalZone currentZone;

        [Header("Arbitrary Attribute Toolbox")]
        [SerializeField] private List<ThreatAttributeEntry> defaultAttributes = new();

        [Header("Deck & Brain")]
        [SerializeField] private ThreatDeck deck = new();
        [SerializeField] private ThreatBrain brain;

        private readonly Dictionary<string, float> _attributes = new();
        private readonly Dictionary<TacticalZone, int> _zoneControl = new();
        private readonly HashSet<string> _tags = new();

        public VitalityPool Integrity => integrity;
        public int ThreatLevel => threatLevel;
        public int Escalation => escalation;
        public int UnitCount => unitCount;
        public int ActionPoints => actionPoints;

        public TacticalZone CurrentZone => currentZone;
        public IEnumerable<TacticalZone> ControlledZones => _zoneControl.Keys;
        public IReadOnlyCollection<string> Tags => _tags;
        public ThreatDeck Deck => deck;
        public ThreatBrain Brain => brain;

        public bool IsEngaged { get; private set; }
        public bool IsNeutralized => integrity.IsDead;

        public event Action<ThreatSheet> OnThreatEngaged;
        public event Action<ThreatSheet> OnThreatNeutralized;
        public event Action<ThreatSheet> OnThreatDisengaged;
        public event Action<ThreatSheet, TacticalZone> OnZoneChanged;
        public event Action<TacticalZone, int> OnZoneControlChanged;
        public event Action<string, float> OnAttributeChanged;
        public event Action<string> OnTagAdded;
        public event Action<string> OnTagRemoved;

        protected override void OnAwake(){
            base.OnAwake();
            if (!brain)
                brain = GetComponent<ThreatBrain>();

            _attributes.Clear();
            foreach (ThreatAttributeEntry entry in defaultAttributes)
                _attributes[entry.key] = entry.value;

            deck.Initialize();
            integrity.OnDead += HandleIntegrityDepleted;

            if (!currentZone)
                currentZone = TacticalZone.GetZoneAt(transform.position);

            if (currentZone)
                SetZoneControl(currentZone, 1);
        }

        private void OnDestroy() => integrity.OnDead -= HandleIntegrityDepleted;

        /// Retrieves an arbitrary attribute value, or defaultValue if unassigned.
        public float GetAttribute(string key, float defaultValue = 0f) =>
            _attributes.TryGetValue(key, out float val) ? val : defaultValue;

        /// Sets an arbitrary attribute value and notifies listeners.
        public void SetAttribute(string key, float val){
            _attributes[key] = val;
            OnAttributeChanged?.Invoke(key, val);
        }

        /// Modifies an arbitrary attribute by delta.
        public void ModifyAttribute(string key, float delta) =>
            SetAttribute(key, GetAttribute(key) + delta);

        /// Checks if this threat has the specified attribute configured.
        public bool HasAttribute(string key) => _attributes.ContainsKey(key);

        /// Removes a custom attribute.
        public bool RemoveAttribute(string key) => _attributes.Remove(key);

        /// Adds a status tag keyword to this threat.
        public void AddTag(string tag){
            if (_tags.Add(tag))
                OnTagAdded?.Invoke(tag);
        }

        /// Removes a status tag keyword from this threat.
        public bool RemoveTag(string tag){
            bool removed = _tags.Remove(tag);
            if (removed)
                OnTagRemoved?.Invoke(tag);
            return removed;
        }

        /// Checks if this threat possesses a specific status tag keyword.
        public bool HasTag(string tag) => _tags.Contains(tag);

        /// Forwards an incoming impact payload to the brain for resolution.
        public void ReceiveImpact(ImpactPayload payload) => brain.ProcessImpact(payload);

        /// Returns the integer control value this threat holds over a tactical zone.
        public int GetZoneControl(TacticalZone zone) =>
            zone && _zoneControl.TryGetValue(zone, out int control) ? control : 0;

        /// Sets the integer control value for a tactical zone.
        public void SetZoneControl(TacticalZone zone, int control){
            if (!zone)
                return;

            if (control <= 0)
                _zoneControl.Remove(zone);
            else
                _zoneControl[zone] = control;

            OnZoneControlChanged?.Invoke(zone, control);
        }

        /// Modifies the control value of a tactical zone by delta.
        public void ModifyZoneControl(TacticalZone zone, int delta) =>
            SetZoneControl(zone, GetZoneControl(zone) + delta);

        /// Checks whether the specified zone is occupied or controlled by this threat.
        public bool ControlsZone(TacticalZone zone) => GetZoneControl(zone) > 0;

        /// Sets and moves the threat to a new current zone.
        public void MoveToZone(TacticalZone newZone){
            if (currentZone)
                SetZoneControl(currentZone, 0);

            currentZone = newZone;
            if (currentZone)
                SetZoneControl(currentZone, 1);

            if (TryGetComponent(out NavMeshAgent agent) && agent.isActiveAndEnabled && agent.isOnNavMesh)
                agent.SetDestination(newZone.Center);

            OnZoneChanged?.Invoke(this, newZone);
        }

        /// Returns all player heroes currently docked in the threat's primary zone.
        public List<Character> GetHeroesInCurrentZone() =>
            GetHeroesInZone(currentZone);

        /// Returns all player heroes currently docked in a specific tactical zone.
        public List<Character> GetHeroesInZone(TacticalZone zone){
            List<Character> heroes = new();
            if (!zone)
                return heroes;

            foreach (TacticalSlot slot in zone.ChildSlots)
                if (slot.Occupant)
                    heroes.Add(slot.Occupant);
            return heroes;
        }

        /// Returns all player heroes docked in any zone controlled by this threat.
        public List<Character> GetHeroesInAllControlledZones(){
            HashSet<Character> heroes = new();
            foreach (TacticalZone zone in _zoneControl.Keys)
                foreach (Character h in GetHeroesInZone(zone))
                    heroes.Add(h);
            return new List<Character>(heroes);
        }

        /// Increments the escalation doom clock (clamped 0 to 100).
        public void AdvanceEscalation(int amount) =>
            escalation.Value = Mathf.Clamp(escalation.Value + amount, 0, 100);

        /// Reduces active unit count and damages integrity if depleted.
        public void ReduceUnits(int count){
            unitCount.Value = Mathf.Max(0, unitCount.Value - count);
            if (unitCount.Value <= 0)
                integrity.Damage(integrity.Current);
        }

        /// Engages this threat, entering combat and notifying CrisisManager.
        [ContextMenu("Engage Threat")]
        public void Engage(){
            if (IsEngaged)
                return;

            IsEngaged = true;
            OnThreatEngaged?.Invoke(this);
            CrisisManager.Instance.EngageThreat(this);
        }

        /// Disengages this threat from active combat.
        [ContextMenu("Disengage Threat")]
        public void Disengage(){
            if (!IsEngaged)
                return;

            IsEngaged = false;
            OnThreatDisengaged?.Invoke(this);
            CrisisManager.Instance.DisengageThreat(this);
        }

        /// Immediately neutralizes the threat, dropping integrity to 0.
        [ContextMenu("Neutralize Threat")]
        public void Neutralize() => integrity.Damage(integrity.Current);

        /// Handles integrity reaching 0 by notifying listeners and disengaging.
        private void HandleIntegrityDepleted(){
            OnThreatNeutralized?.Invoke(this);
            Disengage();
        }

        /// Serializes custom attributes to blackboard state dictionary.
        public override void OnSaveState(Dictionary<string, object> state){
            base.OnSaveState(state);
            foreach (KeyValuePair<string, float> kvp in _attributes)
                state[$"attr_{kvp.Key}"] = kvp.Value;
        }

        /// Restores custom attributes from blackboard state dictionary.
        public override void OnLoadState(Dictionary<string, object> state){
            base.OnLoadState(state);
            foreach (string key in new List<string>(_attributes.Keys))
                if (state.TryGetValue($"attr_{key}", out object rawVal))
                    _attributes[key] = Convert.ToSingle(rawVal);
        }
    }
}
