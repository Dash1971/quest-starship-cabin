using UnityEngine;

namespace StarshipCabin
{
    public class SessionButton : MonoBehaviour
    {
        [SerializeField] private CabinExperienceController controller;
        [SerializeField] private int minutes = 20;

        public void StartConfiguredSession()
        {
            if (controller != null)
            {
                controller.StartSession(minutes);
            }
        }
    }
}

