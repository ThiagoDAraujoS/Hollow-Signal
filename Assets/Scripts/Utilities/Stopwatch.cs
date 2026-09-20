using System;
using UnityEngine;

namespace Utilities{
    public class Stopwatch{
        public Action<float> action = _ => { };

        public  float duration;
        private float _startTime = -1f;
        public  bool  IsRunning => _startTime >= 0f;

        public Stopwatch(float duration) => this.duration = duration;

        public Stopwatch(float duration, Action<float> action){
            this.duration = duration;
            this.action   = action;
        }

        /// Records the start timestamp to begin timing.
        public void Start() => _startTime = Time.time;

        /// Resets the stopwatch to an idle state.
        public void Stop() => _startTime = -1f;

        /// Executes the action with normalized progress (0 to 1) and returns true while actively running.
        public bool Run(){
            if (_startTime < 0f) return false;
            float progress = duration > 0f ? Mathf.Clamp01(Time.time - _startTime / duration) : 1f;
            action(progress);
            if (progress < 1f) return true;
            _startTime = -1f;
            return false;
        }
    }
}
