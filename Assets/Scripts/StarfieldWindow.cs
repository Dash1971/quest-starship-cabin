using UnityEngine;

namespace StarshipCabin
{
    [RequireComponent(typeof(ParticleSystem))]
    public class StarfieldWindow : MonoBehaviour
    {
        [SerializeField] private float forwardSpeed = 0.025f;
        [SerializeField] private float lateralDrift = 0.0f;
        [SerializeField] private float maxOffset = 4f;

        private ParticleSystem stars;
        private Vector3 startPosition;

        private void Awake()
        {
            stars = GetComponent<ParticleSystem>();
            startPosition = transform.localPosition;
        }

        private void Start()
        {
            if (!stars.isPlaying)
            {
                stars.Play();
            }
        }

        private void Update()
        {
            var offset = Mathf.PingPong(Time.time * forwardSpeed, maxOffset);
            var drift = Mathf.Sin(Time.time * lateralDrift) * 0.4f;
            transform.localPosition = startPosition + new Vector3(drift, 0f, -offset);
        }

        public void SetMotion(float speed, float drift)
        {
            forwardSpeed = Mathf.Max(0f, speed);
            lateralDrift = Mathf.Max(0f, drift);
        }
    }
}

