using UnityEngine;

[System.Serializable]
public class ObjectGroup
{
    public string ID;
    public bool State;
    public GameObject[] Objects;

    public void Update()
    {
        foreach (var obj in Objects)
        {
            if (obj == null)
            {
                continue;
            }
            obj.SetActive(State);
        }
    }
}

public class Tester : MonoBehaviour
{
    [SerializeField]
    int rotationStep = 0;

    [SerializeField]
    ObjectGroup[] groups;

    int currentRotation = 0;

    void updateObjectGroups()
    {
        foreach(var group in groups)
        {
            group.Update();
        }
    }

    public void ToggleGroupActive(string groupId)
    {
        foreach(var group in groups)
        {
            if(group.ID != groupId)
            {
                continue;
            }
            group.State = !group.State;
            group.Update();
            break;
        }
    }

    private void Start()
    {
        Application.targetFrameRate = 60;

        currentRotation = Mathf.RoundToInt(transform.rotation.eulerAngles.y);
        updateObjectGroups();
    }

    public void RotateBack()
    {
        currentRotation -= rotationStep;
        transform.rotation = Quaternion.Euler(0, currentRotation, 0);
    }

    public void Rotate()
    {
        currentRotation += rotationStep;
        transform.rotation = Quaternion.Euler(0, currentRotation, 0);
    }
}
