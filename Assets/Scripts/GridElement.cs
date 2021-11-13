using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GridElement : MonoBehaviour
{
    public int row;
    public int col;
    public Color color;
    public int colorCode;
    public SpriteRenderer spriteRenderer;

    public enum State
    {
        Placed = 0,
        Collected = 1,
        Moving = 2,
        Animating = 3
    }

    public int fromRow;
    public int fromCol;
    public Vector3 targetPos;


    public State state = State.Placed;
}
