using UnityEngine;
using Unity.Profiling;
using System.Text;
using TMPro;

/***
 * 
 * PerformanceCounters can be used to display the current "Draw Calls" & Vertices count on a TextMeshProUGUI component.
 * 
 ***/
public class PerformanceCounters: MonoBehaviour
{

    ProfilerRecorder drawCallsCounter;
    ProfilerRecorder verticesCounter;
    public TextMeshProUGUI performanceTMP;

    // Start is called  before the first frame update
    void Start()
    {
        if (performanceTMP == null)
            performanceTMP = GetComponent<TMPro.TextMeshProUGUI>();
    }
    void OnEnable()
    {
        drawCallsCounter = ProfilerRecorder.StartNew(ProfilerCategory.Render, "Draw Calls Count");
        verticesCounter = ProfilerRecorder.StartNew(ProfilerCategory.Render, "Vertices Count");
    }

    void Update()
    {
        StringBuilder stringBuilder = new StringBuilder(500);
        stringBuilder.AppendLine($"Draw Calls: {drawCallsCounter.LastValue}");
        stringBuilder.AppendLine($"Vertices: {verticesCounter.LastValue / 1000} k");
        performanceTMP.text = stringBuilder.ToString();
    }

    void OnDisable()
    {
        drawCallsCounter.Dispose();
        verticesCounter.Dispose();
    }


}
