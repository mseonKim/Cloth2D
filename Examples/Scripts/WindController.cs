using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Cloth2D;

public class WindController : MonoBehaviour
{
    public List<Text> windTexts = new List<Text>();
    public List<RectTransform> windImageTranforms = new List<RectTransform>();

    // Start is called before the first frame update
    private void Start()
    {
        InvokeRepeating("UpdateWinds", 10f, 10f);
    }

    private void UpdateWinds()
    {
        int i = 0;
        foreach(var wind2d in Wind2DReceiver.Instance.Winds.Values)
        {
            wind2d.SetWindStrength(Random.Range(0f, 1f));
            wind2d.transform.rotation = Random.rotation;
            windTexts[i].text = "Wind" + (i + 1) + ": " + (Mathf.Round(wind2d.windStrength * 100f) / 100f);
            windImageTranforms[i].rotation = Quaternion.Euler(0f, 0f, wind2d.transform.rotation.eulerAngles.z);
            i++;
        }
    }
}
