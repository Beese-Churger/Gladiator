using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ControllerHolder : MonoBehaviour
{
    public List<RuntimeAnimatorController> animators = new();
    public List<GameObject> helms = new();
    public List<GameObject> weapons = new();
}
