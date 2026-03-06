using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace HypnicEmpire
{
    public class SmoothScrollRect : ScrollRect
    {
        // A value to control how fast the scrolling accelerates/decelerates
        [SerializeField] public float scrollSpeed = 3f; 
        
        // A variable to store the desired target scroll position
        private Vector2 m_TargetNormalizedPosition;
        private bool m_IsScrolling = false;

        protected override void Start()
        {
            base.Start();
            m_TargetNormalizedPosition = normalizedPosition;
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            // Set the scroll sensitivity of the base ScrollRect to 0 to disable its default instant scrolling
            // You can set this in the Inspector as well.
            scrollSensitivity = 0f; 
        }

        // Use Update or LateUpdate to apply the smoothing over frames
        void Update()
        {
            if (m_IsScrolling)
            {
                // Smoothly interpolate towards the target normalized position
                normalizedPosition = Vector2.Lerp(normalizedPosition, m_TargetNormalizedPosition, Time.deltaTime * scrollSpeed);

                // Stop smoothing if we are very close to the target
                if (Vector2.Distance(normalizedPosition, m_TargetNormalizedPosition) < 0.001f)
                {
                    normalizedPosition = m_TargetNormalizedPosition;
                    m_IsScrolling = false;
                }
            }
        }

        // Override the OnScroll method to set the target position
        public override void OnScroll(PointerEventData data)
        {
            // Don't call the base.OnScroll to prevent the instant jump
            // base.OnScroll(data); // Removed

            // Adjust the target normalized position based on the mouse wheel delta
            // data.scrollDelta.y is positive for scrolling up, negative for scrolling down
            Vector2 delta = data.scrollDelta;
            
            // You might need different sensitivity for different platforms (e.g., Windows vs Mac).
            float sensitivityMultiplier = 0.1f; // Adjust this multiplier as needed

            m_TargetNormalizedPosition += new Vector2(delta.x * sensitivityMultiplier, delta.y * sensitivityMultiplier);

            // Clamp the target position to ensure it stays within the valid scroll range (0 to 1)
            m_TargetNormalizedPosition = new Vector2(
                Mathf.Clamp01(m_TargetNormalizedPosition.x),
                Mathf.Clamp01(m_TargetNormalizedPosition.y)
            );

            m_IsScrolling = true;
        }
    }
}