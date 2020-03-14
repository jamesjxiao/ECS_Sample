using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UtilClass : MonoBehaviour
{
    public static Vector3 GetMouseWorldPosition(Camera worldCamera)
    {
        var ray = worldCamera.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;
        if(Physics.Raycast(ray, out hit, 1000, 1 << LayerMask.NameToLayer("Ground")))
        {
            return hit.point;
        }
        return Vector3.zero;
    }
}
