using System.Collections;
using UnityEngine;

namespace StarshipCabin
{
    /// <summary>
    /// One distant object occasionally drifts across the view. Asteroids and
    /// the Leviathan tumble; the Leviathan crosses much more slowly.
    /// </summary>
    public class SkyEventController : MonoBehaviour
    {
        public GameObject[] events;

        [Header("Timing (seconds)")]
        public float firstDelayMin = 20f;
        public float firstDelayMax = 40f;
        public float gapMin = 55f;
        public float gapMax = 150f;
        public float crossMin = 60f;
        public float crossMax = 100f;

        [Header("Path (world space, outboard is -Z)")]
        public float distance = 55f;
        public float spanHalfWidth = 42f;
        public float heightMin = 6f;
        public float heightMax = 20f;

        private void Start()
        {
            if (events != null)
            {
                foreach (var skyEvent in events)
                {
                    if (skyEvent != null) skyEvent.SetActive(false);
                }
            }

            StartCoroutine(Run());
        }

        private IEnumerator Run()
        {
            yield return new WaitForSeconds(Random.Range(firstDelayMin, firstDelayMax));

            while (true)
            {
                if (events == null || events.Length == 0)
                {
                    yield break;
                }

                var skyEvent = events[Random.Range(0, events.Length)];
                if (skyEvent != null)
                {
                    yield return CrossOnce(skyEvent);
                }

                yield return new WaitForSeconds(Random.Range(gapMin, gapMax));
            }
        }

        private IEnumerator CrossOnce(GameObject skyEvent)
        {
            var goingRight = Random.value < 0.5f;
            var direction = goingRight ? 1f : -1f;
            var height = Random.Range(heightMin, heightMax);
            var z = -Mathf.Abs(distance);

            var start = new Vector3(-direction * spanHalfWidth, height, z);
            var end = new Vector3(direction * spanHalfWidth, height, z);

            skyEvent.transform.localPosition = start;
            skyEvent.transform.localRotation = goingRight
                ? Quaternion.identity
                : Quaternion.Euler(0f, 180f, 0f);
            skyEvent.SetActive(true);

            var tumbles = skyEvent.name.Contains("Asteroid") || skyEvent.name.Contains("Leviathan");
            var isLeviathan = skyEvent.name.Contains("Leviathan");
            var cross = Random.Range(crossMin, crossMax) * (isLeviathan ? 2.4f : 1f);
            var elapsed = 0f;

            while (elapsed < cross)
            {
                elapsed += Time.deltaTime;
                var progress = Mathf.Clamp01(elapsed / cross);
                skyEvent.transform.localPosition = Vector3.Lerp(start, end, progress);

                if (tumbles)
                {
                    var rate = isLeviathan ? 1.5f : 7f;
                    skyEvent.transform.Rotate(
                        new Vector3(rate, rate * 0.7f, rate * 0.4f) * Time.deltaTime,
                        Space.Self);
                }

                yield return null;
            }

            skyEvent.SetActive(false);
        }
    }
}
