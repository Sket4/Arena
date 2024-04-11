using System.Collections.Generic;
using UnityEngine;

public class MeasureDistance : MonoBehaviour
{
    [SerializeField]
    Transform tr1 = default;

    [SerializeField]
    Transform tr2 = default;

    [SerializeField]
    float guiSpace = 100;

    List<float> distances = new List<float>();

    void OnGUI()
    {
        if(tr1 == null || tr2 == null)
        {
            return;
        }

        var dist = (tr1.position - tr2.position).magnitude;
        distances.Add(dist);
        if(distances.Count > 10)
        {
            distances.RemoveAt(0);
        }

        GUILayout.Space(guiSpace);

        GUILayout.Label("Last Distance: " + dist);

        float average = 0;
        foreach(var d in distances)
        {
            average += d;
        }

        GUILayout.Label("Avg distance: " + average / distances.Count);
    }
}
