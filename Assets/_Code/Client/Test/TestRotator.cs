using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TestRotator : MonoBehaviour
{
    public float AutoRotationSpeed = 30;

    // Update is called once per frame
    void Update()
    {
        transform.RotateAround(Vector3.up, AutoRotationSpeed * Time.deltaTime);
    }
}
