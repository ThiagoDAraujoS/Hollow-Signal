using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Utils{
    public abstract class CoroutineComponent : MonoBehaviour{
        public abstract IEnumerator Routine();
    }

    public static class Coroutine{
        public static IEnumerator Multicast(this MonoBehaviour runner, ICollection<CoroutineComponent> coroutines){
            yield return Multicast(runner,routine => routine.Routine() ,coroutines);
        }
        public static IEnumerator Multicast(this MonoBehaviour runner, ICollection<IEnumerator> coroutines){
            yield return Multicast(runner,routine => routine ,coroutines);
        }
        public static IEnumerator Multicast(this MonoBehaviour runner, ICollection<Func<IEnumerator>> coroutines){
            yield return Multicast(runner,routine => routine() ,coroutines);
        }
        private static IEnumerator Multicast<T>(MonoBehaviour runner, Func<T, IEnumerator> unpackingMethod, ICollection<T> coroutines){
            if (coroutines == null)
                yield break;
            List<T> routines = coroutines.ToList();
            int active = routines.Count;
            if (active == 0)
                yield break;
            foreach (T routine in routines)
                runner.StartCoroutine(RunAndNotify(unpackingMethod(routine), () => active--));
            yield return new WaitWhile(() => active > 0);
        }
        private static IEnumerator RunAndNotify(IEnumerator routine, Action onComplete){
            yield return routine;
            onComplete.Invoke();
        }
    }
}
