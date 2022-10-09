using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Grid : MonoBehaviour
{
    public Block OccupiedCube;
    public int Order;
    public Vector3 Pos => transform.position;
    public string Name => gameObject.name;

    public void SetOrder(int order) => Order = order;
}
