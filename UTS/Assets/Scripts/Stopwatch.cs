using UnityEngine;
using UnityEngine.UI;

public class Stopwatch : MonoBehaviour
{
    public float elapsedTime = 0.0f;
    public Text stopwatchText;
    public bool TimerOn = true;


    void Update()
    {
        elapsedTime += Time.deltaTime;
        if (TimerOn)
        {
            stopwatchText.text = "Timer: " + FormatTime(elapsedTime);
        }

    }

    string FormatTime(float time)
    {
        int minutes = Mathf.FloorToInt(time / 60);
        int seconds = Mathf.FloorToInt(time % 60);
        int milliseconds = Mathf.FloorToInt((time * 1000) % 1000);
        return string.Format("{0:00}:{1:00}:{2:000}", minutes, seconds, milliseconds);
    }


    private void OnCollisionEnter(Collision collision)
    {

        if (collision.transform.CompareTag("Player"))
        {
            TimerOn = false;
        }
    }


}