using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// add delay feature

[DefaultExecutionOrder(-1000)]
public class BasicTween : MonoBehaviour
{
    public LinkedList<Tween> tweens;
    public static BasicTween Instance = null;

    public class Tween
    {
        public float time = 0f;
        public Vector3 endValue;
        public Vector3 startValue;
        public bool circularMotion;
        public float startTime;
        public Transform transform;
        public bool isDone;
        public System.Action<Tween> updateMethod;
        public System.Action onComplete;
        public Vector3 circularOffset;
    }

    protected void Awake()
    {
        Instance = this;
        tweens = new LinkedList<Tween>();
    }

    void Update()
    {
        foreach(var tween in tweens)
        {
            if(Time.time < tween.startTime)
            {
                continue;
            }

            float t = Time.time - tween.startTime;

            if(t > tween.time)
            {
                tween.isDone = true;
                continue;
            }

            if(tween.updateMethod != null)
            {
                tween.updateMethod.Invoke(tween);
            }
        }

        var node = tweens.Last;

        while(node != null)
        {
            var temp = node.Previous;

            if(node.Value.isDone)
            {
                if(node.Value.updateMethod != null)
                {
                    node.Value.startValue = node.Value.endValue;
                    node.Value.updateMethod.Invoke(node.Value);
                }


                if(node.Value.onComplete != null)
                {
                    node.Value.onComplete.Invoke();
                }
                tweens.Remove(node);
            }

            node = temp;
        }

    }

    public void DelayedCall(float time, System.Action onComplete, float delay = 0f)
    {
        Tween tween = new Tween();
        tween.time = time;
        tween.startTime = Time.time + delay;
        tween.onComplete = onComplete;
        tweens.AddLast(tween);
    }

    public void AppendPositionCircular(Transform target, Vector3 endValue, float time, float delay)
    {
        Tween tween = new Tween();
        tween.startValue = target.position;
        tween.endValue = endValue;
        tween.time = time;
        tween.transform = target;
        tween.startTime = Time.time + delay;
        tween.updateMethod = PositionCircular;
        tween.circularMotion = true;

        float scale = Vector3.Distance(tween.startValue, tween.endValue) * .5f;
        tween.circularOffset = Random.insideUnitCircle * scale;

        tweens.AddLast(tween);
    }

    public void AppendPosition(Transform target, Vector3 endValue, float time, float delay)
    {
        Tween tween = new Tween();
        tween.startValue = target.position;
        tween.endValue = endValue;
        tween.time = time;
        tween.transform = target;
        tween.startTime = Time.time + delay;
        tween.updateMethod = Position;

        tweens.AddLast(tween);
    }

    public void AppendScale(Transform target, Vector3 endValue, float time, float delay)
    {
        Tween tween = new Tween();
        tween.startValue = target.localScale;
        tween.endValue = endValue;
        tween.time = time;
        tween.transform = target;
        tween.startTime = Time.time + delay;
        tween.updateMethod = Scale;

        tweens.AddLast(tween);
    }

    public void PositionCircular(Tween tween)
    {
        float t = Time.time - tween.startTime;

        tween.transform.position = Vector3.Lerp(tween.startValue, tween.endValue, t / tween.time) + tween.circularOffset * Mathf.Sin((t / tween.time) * Mathf.PI);
    }

    public void Position(Tween tween)
    {
        float t = Time.time - tween.startTime;

        tween.transform.position = Vector3.Lerp(tween.startValue, tween.endValue, t / tween.time);
    }

    public void Scale(Tween tween)
    {
        float t = Time.time - tween.startTime;

        tween.transform.localScale = Vector3.Lerp(tween.startValue, tween.endValue, t / tween.time);
    }

    private void OnDestroy()
    {
        tweens.Clear();
    }
}
