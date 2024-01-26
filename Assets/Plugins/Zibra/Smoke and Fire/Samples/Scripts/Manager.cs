using UnityEngine;

namespace com.zibraai.smoke_and_fire.Samples
{
    public class Manager : MonoBehaviour
    {
        public bool pause = true;
        public GameObject simulation;
        public GameObject ui;

        protected void Start()
        {
            simulation.SetActive(false);
            ui.SetActive(true);
        }

        protected void Update()
        {
            if (Input.GetKeyDown(KeyCode.Escape) && pause == false)
            {
                Switch();
            }
        }

        public void Switch()
        {
            pause = !pause;
            if (pause)
            {
                simulation.SetActive(false);
                ui.SetActive(true);
            }
            else
            {
                simulation.SetActive(true);
                ui.SetActive(false);
            }
        }
    }
}
