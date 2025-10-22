using UnityEngine;

public class DayNightCycle : MonoBehaviour
{
    public Light sun;

    public float DayDuration = 15f;

    private float time; //Checks how much time has passed

    // Update is called once per frame
    void Update()
    {
        time += Time.deltaTime;

        float angle = (time / DayDuration) * 360f;

        sun.transform.rotation = Quaternion.Euler(angle - 90, 170, 0f); //Rotates the sun and gives it a little tilt

        float t = Mathf.Sin(time / DayDuration * Mathf.PI * 2f) * 0.5f * 0.5f; //Goes from 0 to 1 smoothly in a sin wave

        sun.color = Color.Lerp(Color.black, Color.yellow, t);

        sun.intensity = Mathf.Lerp(0.1f, 1f, t);
    }
}
