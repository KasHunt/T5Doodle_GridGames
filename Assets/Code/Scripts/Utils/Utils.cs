using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Code.Scripts.Utils
{
    public static class LinqUtils
    {
        public static IEnumerable<T> TakeWhileInclusive<T>(
            this IEnumerable<T> source,
            Func<T, bool> predicate,
            Func<T, bool> includeLast = null)
        {
            foreach (var item in source)
            {
                var shouldInclude = predicate(item);
                if (shouldInclude || (includeLast != null && includeLast(item))) yield return item;
                if (!shouldInclude) break;
            }
        }
    }
    
    public static class ColorUtils
    {
        public static Color ColorWithAlpha(this Color color, float alpha)
        {
            color.a = alpha;
            return color;
        }
    }
    
    public static class VectorUtils
    {
        public static Vector3 RandomVector3(Vector3 minInclusive, Vector3 maxInclusive)
        {
            return new Vector3(
                Random.Range(minInclusive.x, maxInclusive.x),
                Random.Range(minInclusive.y, maxInclusive.y),
                Random.Range(minInclusive.z, maxInclusive.z)
            );
        }
        
        public static Vector3 RandomVector3(float minInclusive, float maxInclusive)
        {
            return new Vector3(
                Random.Range(minInclusive, maxInclusive),
                Random.Range(minInclusive, maxInclusive),
                Random.Range(minInclusive, maxInclusive)
            );
        }

        public static Vector3 LerpBezierArc(Vector3 start, Vector3 end, float apexHeight, float t)
        {
            var controlPoint = start;
            controlPoint.y += apexHeight;
            
            return new Vector3(
                Mathf.Lerp(start.x, end.x, t),
                Mathf.Pow(1 - t, 2) * start.y + 2 * (1 - t) * t * controlPoint.y + Mathf.Pow(t, 2) * end.y,
                Mathf.Lerp(start.z, end.z, t)
            );
        }
    }
    
    public static class Easing
    {
        public static float QuadInOut(float t)
        {
            return t switch
            {
                < 0.5f => 2 * t * t,
                _ => 1 - 2 * (1 - t) * (1 - t),
            };
        }
    }

    public static class CoroutineUtils
    {
        public static IEnumerator TimedCoroutine(float duration, float delay, 
            Action preAction, Action<float> loopAction, Action postAction)
        {
            // Wait before starting
            if (delay > 0) yield return new WaitForSeconds(delay);

            preAction?.Invoke();
            
            float elapsedTime = 0;
            while (elapsedTime < duration)
            {
                loopAction?.Invoke(elapsedTime / duration);

                elapsedTime += Time.deltaTime;
                yield return null;
            }

            postAction?.Invoke();
        }
    }
}
