using System;
using UnityEngine;

namespace com.zibraai.smoke_and_fire.Samples
{
    public class Mover : MonoBehaviour
    {
        public Vector3 direction;
        public float amplitude;
        public float speed;

        private int frame = 0;

        protected void FixedUpdate()
        {

            transform.Translate(Time.deltaTime * amplitude * direction * speed * (float)Math.Sin(speed * Time.time) /
                                (2.0f * (float)Math.PI));

            frame++;
        }
    }
}
