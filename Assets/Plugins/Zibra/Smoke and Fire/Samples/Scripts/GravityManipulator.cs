using UnityEngine;

namespace com.zibraai.smoke_and_fire.Samples
{
    public class GravityManipulator : MonoBehaviour
    {
        Solver.ZibraSmokeAndFire smokeAndFire;

        // Start is called before the first frame update
        void Start()
        {
            smokeAndFire = GetComponent<Solver.ZibraSmokeAndFire>();
        }

        // Update is called once per frame
        void Update()
        {
            //if (Input.GetKey(KeyCode.UpArrow))
            //{
            //    smokeAndFire.solverParameters.Gravity.y = 9.81f;
            //    smokeAndFire.solverParameters.Gravity.x = 0.0f;
            //}

            //if (Input.GetKey(KeyCode.DownArrow))
            //{
            //    smokeAndFire.solverParameters.Gravity.y = -9.81f;
            //    smokeAndFire.solverParameters.Gravity.x = 0.0f;
            //}

            //if (Input.GetKey(KeyCode.RightArrow))
            //{
            //    smokeAndFire.solverParameters.Gravity.y = 0.0f;
            //    smokeAndFire.solverParameters.Gravity.x = 9.81f;
            //}

            //if (Input.GetKey(KeyCode.LeftArrow))
            //{
            //    smokeAndFire.solverParameters.Gravity.x = -9.81f;
            //    smokeAndFire.solverParameters.Gravity.y = 0.0f;
            //}

            //if (Input.GetKey(KeyCode.O))
            //{
            //    smokeAndFire.solverParameters.Gravity.x = 0.0f;
            //    smokeAndFire.solverParameters.Gravity.y = 0.0f;
            //}

            //if (Input.GetKey(KeyCode.LeftShift))
            //{
            //    smokeAndFire.solverParameters.Gravity *= 1.02f;
            //}
            //if (Input.GetKey(KeyCode.LeftControl))
            //{
            //    smokeAndFire.solverParameters.Gravity *= 0.98f;
            //}
        }
    }
}
