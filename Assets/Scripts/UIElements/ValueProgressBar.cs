using UnityEngine;
using UnityEngine.UI;

namespace HypnicEmpire
{
    public class ValueProgressBar : MonoBehaviour
    {
        [SerializeField]
        public Slider slider;
        public ResourceType resourceType;

        // Start is called once before the first execution of Update after the MonoBehaviour is created
        void Start()
        {
            GameController.CurrentGameState.SubscribeToResourceAmount(resourceType, ResourceChangeCallback);
        }

        void OnDestroy()
        {
            GameController.CurrentGameState.UnsubscribeToResourceAmount(resourceType, ResourceChangeCallback);
        }

        // Update is called once per frame
        void Update()
        {

        }

        public void ResourceChangeCallback(int delta, int value)
        {
            slider.value = value;
        }
    }
}
