using UnityEngine;

namespace com.zibraai.smoke_and_fire.Samples
{
    public class Rotator : MonoBehaviour
    {
        public float rotationSpeed = 20.0f;

        protected void Update()
        {
            transform.Rotate(Vector3.up, rotationSpeed * Time.deltaTime);
        }
    }
}
