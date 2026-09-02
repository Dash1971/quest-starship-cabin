using System.Collections;
using UnityEngine;

namespace StarshipCabin
{
    /// <summary>
    /// Milestone 10 — the living sky. On a long random timer, one distant
    /// object drifts slowly across the view far beyond the glass, then hides
    /// until the next. Everything here is comfort-first:
    ///   - far away (tens of metres) and slow (well under ~1.5°/s angular),
    ///   - rare (a minute or more between events),
    ///   - never triggered by the viewer, never rushes, never flickers.
    ///
    /// The event objects are built by QuartersSceneSetup.BuildSkyEvents and
    /// handed to this controller as `events`, oriented for travel along +X
    /// (front features at +X, trailing features at -X). This controller only
    /// moves/orients/toggles them.
    /// </summary>
    public class SkyEventController : MonoBehaviour
    {
        [Tooltip("Distant objects to cycle through (built by the scene generator, oriented for +X travel).")]
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
                    if (skyEvent != null)
                    {
                        skyEvent.SetActive(false);
                    }
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

            // Geometry is authored for +X travel; a 180-degree yaw flips it for -X.
            skyEvent.transform.localPosition = start;
            skyEvent.transform.localRotation = goingRight
                ? Quaternion.identity
                : Quaternion.Euler(0f, 180f, 0f);
            skyEvent.SetActive(true);

            var isAsteroid = skyEvent.name.Contains("Asteroid");
            var cross = Random.Range(crossMin, crossMax);
            var elapsed = 0f;

            while (elapsed < cross)
            {
                elapsed += Time.deltaTime;
                var progress = Mathf.Clamp01(elapsed / cross);
                skyEvent.transform.localPosition = Vector3.Lerp(start, end, progress);

                if (isAsteroid)
                {
                    skyEvent.transform.Rotate(new Vector3(7f, 5f, 3f) * Time.deltaTime, Space.Self);
                }

                yield return null;
            }

            skyEvent.SetActive(false);
        }
    }
}
